using Serilog.Events;

namespace Currency.Api.Settings;

public class LoggerSettings
{
    public bool DisableLogger { get; set; }
    public string AppVersion { get; set; }
    public string Application { get; set; }
    public bool EnableDebugOptions { get; set; }
    public string ConsoleTemplate { get; set; }
    public LogEventLevel ConsoleLogLevel { get; set; }
    public string ElasticIndexFormat { get; set; }
    public LogEventLevel ElasticLogLevel { get; set; }
    public string ElasticEndpoint { get; set; }
    public string JaegerEndpoint { get; set; }
    public string Environment { get; set; }
}