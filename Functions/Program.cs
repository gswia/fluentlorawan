using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoReturn.LoRaWAN.Postgres;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Register PostgreSQL data provider
        services.AddSingleton<SqlLoRaWANDataProvider>(sp =>
        {
            var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString") 
                ?? throw new InvalidOperationException("PostgresConnectionString not configured");
            var logger = sp.GetRequiredService<ILogger<SqlLoRaWANDataProvider>>();
            return new SqlLoRaWANDataProvider(connectionString, logger);
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.AddApplicationInsights();
        logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("", LogLevel.Information);
    })
    .Build();

host.Run();
