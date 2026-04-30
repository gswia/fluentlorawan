using Azure.Messaging.ServiceBus;
using MQTTnet;
using MQTTnet.Client;

namespace TrackerWorker;

public class MqttBridgeWorker : BackgroundService
{
    private readonly ILogger<MqttBridgeWorker> _logger;
    private readonly IConfiguration _config;

    public MqttBridgeWorker(ILogger<MqttBridgeWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttHost = _config["Mqtt:Host"]!;
        var mqttPort = int.Parse(_config["Mqtt:Port"] ?? "1883");
        var mqttUser = _config["Mqtt:Username"]!;
        var mqttPass = _config["Mqtt:Password"]!;
        var sbConnectionString = _config["ServiceBus:ConnectionString"]!;
        var sbQueueName = _config["ServiceBus:QueueName"]!;

        await using var sbClient = new ServiceBusClient(sbConnectionString);
        var sender = sbClient.CreateSender(sbQueueName);

        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.PayloadSegment.ToArray();

            _logger.LogTrace("Received {Bytes} bytes on {Topic}", payload.Length, topic);

            var message = new ServiceBusMessage(payload)
            {
                ApplicationProperties = { ["topic"] = topic }
            };

            await sender.SendMessageAsync(message, stoppingToken);
            _logger.LogInformation("Forwarded {Bytes} bytes from {Topic} to Service Bus", payload.Length, topic);
        };

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttHost, mqttPort)
            .WithCredentials(mqttUser, mqttPass)
            .WithClientId("tracker-worker")
            .WithCleanSession(false)
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!mqttClient.IsConnected)
                {
                    await mqttClient.ConnectAsync(options, stoppingToken);
                    await mqttClient.SubscribeAsync("Lansitec/pub/+", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
                    _logger.LogInformation("Connected to MQTT broker at {Host}:{Port}", mqttHost, mqttPort);
                }

                await Task.Delay(5000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT error, reconnecting in 10s");
                await Task.Delay(10000, stoppingToken);
            }
        }

        if (mqttClient.IsConnected)
            await mqttClient.DisconnectAsync();
    }
}
