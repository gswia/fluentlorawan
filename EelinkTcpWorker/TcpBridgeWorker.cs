using System.Net;
using System.Net.Sockets;
using System.Text;
using Azure.Messaging.ServiceBus;

namespace EelinkTcpWorker;

public class TcpBridgeWorker : BackgroundService
{
    private readonly ILogger<TcpBridgeWorker> _logger;
    private readonly IConfiguration _config;

    public TcpBridgeWorker(ILogger<TcpBridgeWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sbConnectionString = _config["ServiceBus:ConnectionString"]!;
        var sbQueueName = _config["ServiceBus:QueueName"]!;
        var port = int.Parse(_config["Tcp:Port"] ?? "10000");

        await using var sbClient = new ServiceBusClient(sbConnectionString);
        var sender = sbClient.CreateSender(sbQueueName);

        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        _logger.LogInformation("TCP listener started on port {Port}", port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleConnectionAsync(client, sender, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting connection");
            }
        }

        listener.Stop();
    }

    private async Task HandleConnectionAsync(TcpClient client, ServiceBusSender sender, CancellationToken ct)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogInformation("Connection from {Remote}", remote);
        string? imei = null;

        try
        {
            using var _ = client;
            var stream = client.GetStream();
            var frameHeader = new byte[5]; // 0x67 0x67 | type | length(2 BE)

            while (!ct.IsCancellationRequested)
            {
                if (!await ReadExactAsync(stream, frameHeader, 0, 5, ct))
                    break;

                if (frameHeader[0] != 0x67 || frameHeader[1] != 0x67)
                {
                    _logger.LogWarning("Bad header from {Remote}, dropping connection", remote);
                    break;
                }

                byte packetType = frameHeader[2];
                int length = (frameHeader[3] << 8) | frameHeader[4];

                var body = new byte[length];
                if (!await ReadExactAsync(stream, body, 0, length, ct))
                    break;

                _logger.LogTrace("Packet type=0x{Type:X2} length={Length} from {Remote}", packetType, length, remote);

                // Login packet — extract IMEI and ACK
                if (packetType == 0x01 && length >= 10)
                {
                    imei = DecodeBcdImei(body, offset: 2, count: 8); // body[0..1] = sequence
                    _logger.LogInformation("Login from IMEI={IMEI}", imei);
                    await SendAckAsync(stream, packetType, body[0], body[1], ct);
                }

                // Warning packet — ACK with empty content
                if (packetType == 0x14 && length >= 2)
                {
                    _logger.LogInformation("Warning packet 0x14 received, sequence={Seq:X2}{Seq2:X2}, sending ACK", body[0], body[1]);
                    await SendWarningAckAsync(stream, body[0], body[1], ct);
                }

                // Location packet — ACK
                if (packetType == 0x12 && length >= 2)
                {
                    _logger.LogInformation("Location packet 0x12 received, sequence={Seq:X2}{Seq2:X2}, sending ACK", body[0], body[1]);
                    await SendLocationAckAsync(stream, body[0], body[1], ct);
                }

                // Pedometer packet — ACK
                if (packetType == 0x1A && length >= 2)
                {
                    _logger.LogInformation("Pedometer packet 0x1A received, sequence={Seq:X2}{Seq2:X2}, sending ACK", body[0], body[1]);
                    await SendPedometerAckAsync(stream, body[0], body[1], ct);
                }

                // Forward raw packet as-is to Service Bus
                var rawPacket = new byte[5 + length];
                Buffer.BlockCopy(frameHeader, 0, rawPacket, 0, 5);
                Buffer.BlockCopy(body, 0, rawPacket, 5, length);

                var message = new ServiceBusMessage(rawPacket)
                {
                    ApplicationProperties =
                    {
                        ["packet_type"] = packetType.ToString("X2"),
                        ["imei"] = imei ?? "unknown"
                    }
                };

                await sender.SendMessageAsync(message, ct);
                _logger.LogInformation("Forwarded type=0x{Type:X2} imei={IMEI} to Service Bus", packetType, imei ?? "unknown");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling connection from {Remote}", remote);
        }

        _logger.LogInformation("Connection closed from {Remote}", remote);
    }

    // Eelink ACK: 0x67 0x67 <type> 0x00 0x04 <seq_high> <seq_low> 0x00 0x00
    private static async Task SendAckAsync(NetworkStream stream, byte packetType, byte seqHigh, byte seqLow, CancellationToken ct)
    {
        var ack = new byte[] { 0x67, 0x67, packetType, 0x00, 0x04, seqHigh, seqLow, 0x00, 0x00 };
        await stream.WriteAsync(ack, ct);
    }

    // Warning ACK: 0x67 0x67 0x14 <size> <seq_high> <seq_low> [content]
    private static async Task SendWarningAckAsync(NetworkStream stream, byte seqHigh, byte seqLow, CancellationToken ct)
    {
        // Send empty content (just sequence) - size=0x0002
        var ack = new byte[] { 0x67, 0x67, 0x14, 0x00, 0x02, seqHigh, seqLow };
        await stream.WriteAsync(ack, ct);
    }

    // Location ACK: 0x67 0x67 0x12 <size> <seq_high> <seq_low>
    private static async Task SendLocationAckAsync(NetworkStream stream, byte seqHigh, byte seqLow, CancellationToken ct)
    {
        // Minimal ACK (just sequence) - size=0x0002
        var ack = new byte[] { 0x67, 0x67, 0x12, 0x00, 0x02, seqHigh, seqLow };
        await stream.WriteAsync(ack, ct);
    }

    // Pedometer ACK: 0x67 0x67 0x1A <size> <seq_high> <seq_low>
    private static async Task SendPedometerAckAsync(NetworkStream stream, byte seqHigh, byte seqLow, CancellationToken ct)
    {
        // Minimal ACK (just sequence) - size=0x0002
        var ack = new byte[] { 0x67, 0x67, 0x1A, 0x00, 0x02, seqHigh, seqLow };
        await stream.WriteAsync(ack, ct);
    }

    // Eelink encodes IMEI as 8 bytes BCD, 15 digits + 1 padding nibble (0xF)
    private static string DecodeBcdImei(byte[] data, int offset, int count)
    {
        var sb = new StringBuilder(count * 2);
        for (int i = 0; i < count; i++)
        {
            sb.Append((data[offset + i] >> 4) & 0x0F);
            sb.Append(data[offset + i] & 0x0F);
        }
        return sb.ToString().TrimEnd('F', 'f');
    }

    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int read = 0;
        while (read < count)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(offset + read, count - read), ct);
            if (n == 0) return false; // connection closed cleanly
            read += n;
        }
        return true;
    }
}
