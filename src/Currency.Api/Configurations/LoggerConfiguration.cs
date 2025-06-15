using Currency.Api.Settings;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace Currency.Api.Configurations;

public static class CurrencyLoggerConfiguration
{
    public static void AddLogger(this IHostBuilder host, IServiceCollection services, StartupSettings settings)
    {
        var loggerSettings = settings.LoggerSettings;
        
        if (loggerSettings.DisableLogger) return;
        
        if (loggerSettings.EnableDebugOptions)
        {
            Serilog.Debugging.SelfLog.Enable(msg =>
            {
                File.AppendAllText("serilog-selflog.txt", msg + Environment.NewLine);
            });
        }

        var config = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", loggerSettings.Application)
            .WriteTo.Console(
                outputTemplate: loggerSettings.ConsoleTemplate, 
                restrictedToMinimumLevel: loggerSettings.ConsoleLogLevel)
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(loggerSettings.ElasticEndpoint))
            {
                AutoRegisterTemplate = true,
                IndexFormat = loggerSettings.ElasticIndexFormat,
                MinimumLogEventLevel = loggerSettings.ElasticLogLevel,
                FailureCallback = (l, _) => Console.WriteLine("Unable to submit event " + l.MessageTemplate),
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                   EmitEventFailureHandling.RaiseCallback,
            });
        
        Log.Logger = config.CreateLogger();
        
        host.UseSerilog();
        services.AddLogging();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: loggerSettings.Application,
                    serviceVersion: loggerSettings.AppVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = loggerSettings.Environment,
                }))
            .WithTracing(builder =>
            {
                builder.AddSource(loggerSettings.Application)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(loggerSettings.JaegerEndpoint);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });

                if (loggerSettings.EnableDebugOptions)
                {
                    builder.AddConsoleExporter();
                }
            });
    }
}