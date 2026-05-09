using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoReturn.LoRaWAN.Postgres;
using Npgsql;
using Azure.AI.OpenAI;
using Azure.Identity;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString") 
            ?? throw new InvalidOperationException("PostgresConnectionString not configured");
        
        // Register NpgsqlDataSource for direct database access
        services.AddSingleton(sp =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            return dataSourceBuilder.Build();
        });
        
        // Register PostgreSQL data provider
        services.AddSingleton<SqlLoRaWANDataProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SqlLoRaWANDataProvider>>();
            return new SqlLoRaWANDataProvider(connectionString, logger);
        });
        
        // Register Azure OpenAI client with managed identity
        services.AddSingleton<AzureOpenAIClient>(sp =>
        {
            var endpoint = Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint")
                ?? "https://fluentcor-aoai.openai.azure.com/";
            var clientId = Environment.GetEnvironmentVariable("AzureOpenAI__ManagedIdentityClientId")
                ?? "d9896a04-2c7b-492a-b11c-fcf51e551b47";
            
            var credential = new ManagedIdentityCredential(clientId);
            return new AzureOpenAIClient(new Uri(endpoint), credential);
        });
        
        // Register HttpClient for internal API calls
        services.AddHttpClient();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddApplicationInsights();
        logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("", LogLevel.Information);
    })
    .Build();

host.Run();
