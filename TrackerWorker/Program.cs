using TrackerWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<MqttBridgeWorker>();

var host = builder.Build();
host.Run();
