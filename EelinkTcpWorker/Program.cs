using EelinkTcpWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TcpBridgeWorker>();

var host = builder.Build();
host.Run();
