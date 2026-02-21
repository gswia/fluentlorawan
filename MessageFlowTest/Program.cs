using System.Text.Json;
using IotHubFunction;
using IotHubFunction.Configuration;
using IotHubFunction.Readings;

var inputPath = args.Length > 0
	? args[0]
	: Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LHT52message.json"));

if (!File.Exists(inputPath))
{
	Console.Error.WriteLine($"Input file not found: {inputPath}");
	Console.Error.WriteLine("Usage: dotnet run --project .\\MessageFlowTest <path-to-message-json>");
	return;
}

var json = await File.ReadAllTextAsync(inputPath);
ChirpStackMessage? chirpStackMessage;

try
{
	chirpStackMessage = JsonSerializer.Deserialize<ChirpStackMessage>(json);
}
catch (Exception ex)
{
	Console.Error.WriteLine($"JSON deserialize failed: {ex.Message}");
	Environment.ExitCode = 1;
	return;
}

if (chirpStackMessage is null)
{
	Console.Error.WriteLine("Failed to deserialize message into ChirpStackMessage.");
	return;
}

if (string.IsNullOrWhiteSpace(chirpStackMessage.DeviceInfo?.DeviceProfileName))
{
	Console.Error.WriteLine("deviceInfo.deviceProfileName is missing in message payload.");
	return;
}

var device = DeviceFactory.Create(chirpStackMessage.DeviceInfo.DeviceProfileName);

// Removed try-catch so debugger breaks at the actual exception point
var readings = device.CreateReadings(chirpStackMessage, "temp-account-id");

Console.WriteLine($"Device Profile : {chirpStackMessage.DeviceInfo.DeviceProfileName}");
Console.WriteLine($"DevEUI         : {chirpStackMessage.DeviceInfo.DevEui}");
Console.WriteLine($"Readings Count : {readings.Count}");
Console.WriteLine();

foreach (var reading in readings)
{
	Console.WriteLine($"[{reading.Type}] sensorId={reading.SensorId} messageId={reading.MessageId}");
	Console.WriteLine(JsonSerializer.Serialize(reading));
}
