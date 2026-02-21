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

// Load account configuration
var accountJsonPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "account-sample.json"));
if (!File.Exists(accountJsonPath))
{
	Console.Error.WriteLine($"Account config not found: {accountJsonPath}");
	return;
}

var accountJson = await File.ReadAllTextAsync(accountJsonPath);
var account = JsonSerializer.Deserialize<Account>(accountJson);

if (account is null)
{
	Console.Error.WriteLine("Failed to deserialize account-sample.json");
	return;
}

// Deserialize ChirpStack message
var json = await File.ReadAllTextAsync(inputPath);
var chirpStackMessage = JsonSerializer.Deserialize<ChirpStackMessage>(json);

if (chirpStackMessage is null)
{
	Console.Error.WriteLine("Failed to deserialize message into ChirpStackMessage.");
	return;
}

if (string.IsNullOrWhiteSpace(chirpStackMessage.DeviceInfo?.DevEui))
{
	Console.Error.WriteLine("deviceInfo.devEui is missing in message payload.");
	return;
}

// Find the device in the account hierarchy by DevEui
Device? device = null;
string? applicationId = null;
string? siteId = null;

foreach (var application in account.Applications)
{
	foreach (var site in application.Sites)
	{
		device = site.Devices.FirstOrDefault(d => d.DeviceId == chirpStackMessage.DeviceInfo.DevEui);
		if (device != null)
		{
			applicationId = application.ApplicationId;
			siteId = site.SiteId;
			break;
		}
	}
	if (device != null) break;
}

if (device is null || applicationId is null || siteId is null)
{
	Console.Error.WriteLine($"Device {chirpStackMessage.DeviceInfo.DevEui} not found in account configuration");
	return;
}

// Removed try-catch so debugger breaks at the actual exception point
var readings = device.CreateReadings(chirpStackMessage, account.AccountId, applicationId, siteId);

Console.WriteLine($"Account ID     : {account.AccountId}");
Console.WriteLine($"Device Profile : {chirpStackMessage.DeviceInfo.DeviceProfileName}");
Console.WriteLine($"DevEUI         : {chirpStackMessage.DeviceInfo.DevEui}");
Console.WriteLine($"Sensor Count   : {device.Sensors.Count}");
Console.WriteLine($"Readings Count : {readings.Count}");
Console.WriteLine();

foreach (var reading in readings)
{
	Console.WriteLine($"Type           : {reading.Type}");
	Console.WriteLine($"TimestampUTC   : {reading.TimestampUTC:O}");
	Console.WriteLine($"AccountId      : {reading.AccountId}");
	Console.WriteLine($"ApplicationId  : {reading.ApplicationId}");
	Console.WriteLine($"SiteId         : {reading.SiteId}");
	Console.WriteLine($"DeviceId       : {reading.DeviceId}");
	Console.WriteLine($"SensorId       : {reading.SensorId}");
	Console.WriteLine($"MessageId      : {reading.MessageId}");
	Console.WriteLine($"Payload        : {JsonSerializer.Serialize(reading.GetPayload())}");
	Console.WriteLine();
}
