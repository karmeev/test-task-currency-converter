using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Currency.IntegrationTests.Infrastructure.Utility;

public static class Test
{
    public static void StartTest([CallerFilePath] string file = "Caller", 
        [CallerMemberName] string name = "Unverified test")
    {
        TestContext.Progress.WriteLine($"Started test ({GetClass(file)}): {name}");
    }
    
    public static void CompleteTest([CallerFilePath] string file = "Caller",
        [CallerMemberName] string name = "Unverified test")
    {
        TestContext.Progress.WriteLine($"Completed test ({GetClass(file)}): {name}");
        TestContext.Progress.WriteLine("\nOutput\n-------------");
    }

    public static ILogger<T> GetLogger<T>()
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TextWriter(TestContext.Out)
            .CreateLogger();
        
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(serilogLogger, dispose: true);
        });
        
        return loggerFactory.CreateLogger<T>();
    }

    private static string GetClass(string name)
    {
        return Path.GetFileNameWithoutExtension(name);
    }
}