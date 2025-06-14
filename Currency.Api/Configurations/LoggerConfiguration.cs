using System.Security.Claims;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace Currency.Api.Configurations;

public static class CurrencyLoggerConfiguration
{
    public static void AddLogger(this IHostBuilder host, IServiceCollection services, string env)
    {
        //TODO: pass from config
        #if DEBUG
        Serilog.Debugging.SelfLog.Enable(msg =>
        {
            File.AppendAllText("serilog-selflog.txt", msg + Environment.NewLine);
        });
        #endif

        var config = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", "CurrencyConverter")
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}")
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
            {
                AutoRegisterTemplate = true,
                IndexFormat = "currency-converter-logs-{0:yyyy.MM.dd}",
                MinimumLogEventLevel = LogEventLevel.Information,
                FailureCallback = (l, _) => Console.WriteLine("Unable to submit event " + l.MessageTemplate),
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                   EmitEventFailureHandling.RaiseCallback,
            });
        
        #if DEBUG
        config.MinimumLevel.Debug();
        #else 
        config.MinimumLevel.Information();
        #endif
        
        Log.Logger = config.CreateLogger();
        
        host.UseSerilog();
        services.AddLogging();
        
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: "CurrencyConverter",
                    serviceVersion: "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = env
                }))
            .WithTracing(tracing => tracing
                .AddSource("CurrencyConverter")
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options => 
                {
                    options.Endpoint = new Uri("http://jaeger:4317"); // Default Jaeger OTLP endpoint
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })
                .AddConsoleExporter());  // Optional for local debugging
    }

    public static void UseRequestLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
            {
                diagCtx.Set("ClientIP", httpCtx.Connection.RemoteIpAddress?.ToString() ?? "undefined");
                diagCtx.Set("RequestHost", httpCtx.Request.Host.ToString());
                diagCtx.Set("RequestScheme", httpCtx.Request.Scheme);
                diagCtx.Set("RequestPath", httpCtx.Request.Path);
                diagCtx.Set("RequestId", httpCtx.TraceIdentifier);

                var clientId = httpCtx.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (clientId is not null)
                    diagCtx.Set("ClientId", clientId);
            };
        });
    }
}