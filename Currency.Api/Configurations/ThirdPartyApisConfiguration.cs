using Currency.Api.Settings;
using Currency.Common.Providers;
using Currency.Infrastructure.Contracts.Integrations.Providers.Frankfurter.Base;
using Currency.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly.Timeout;

namespace Currency.Api.Configurations;

public static class ThirdPartyApisConfiguration
{
    public static void AddThirdPartyApis(this IServiceCollection services, StartupSettings startupSettings)
    {
        services.AddHttpClient();

        var frankfurterSettings = startupSettings.Integrations.Frankfurter;
        services.AddHttpClient(ProvidersConst.Frankfurter,
                client => { client.BaseAddress = new Uri(frankfurterSettings.BaseAddress); })
            .AddPolicyHandler((sp, _) => GetFrankfurterTimeoutPolicy(sp))
            .AddPolicyHandler((sp, _) => GetFrankfurterRetryPolicy(sp))
            .AddPolicyHandler((sp, _) => GetFrankfurterCircuitBreakerPolicy(sp));
    }

    private static AsyncTimeoutPolicy<HttpResponseMessage> GetFrankfurterTimeoutPolicy(IServiceProvider serviceProvider)
    {
        var settings = ResolveSettings(serviceProvider);
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(settings.TimeoutSeconds));
    }


    private static AsyncRetryPolicy<HttpResponseMessage> GetFrankfurterRetryPolicy(IServiceProvider serviceProvider)
    {
        var settings = ResolveSettings(serviceProvider);
        var logger = ResolveLogger(serviceProvider);
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: settings.RetryCount,
                sleepDurationProvider: _ => TimeSpan.FromSeconds(Math.Pow(2, settings.RetryExponentialIntervalSeconds)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    if (outcome.Exception is not null)
                    {
                        logger.LogError("Request ended with error: {exception}, Time: {timespan}, Retry: {retryAttempt}, " +
                                        "PolicyKey: {context}", outcome.Exception, timespan, retryAttempt, context.PolicyKey);
                    }
                    else
                    {
                        logger.LogInformation("DelegateResult: {outcome}, Time: {timespan}, Retry: {retryAttempt}, " +
                                              "PolicyKey: {context}", outcome, timespan, retryAttempt, context.PolicyKey);
                    }
                });
    }

    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetFrankfurterCircuitBreakerPolicy(
        IServiceProvider serviceProvider)
    {
        const string onBreakState = "onBreak";
        const string onResetState = "onReset";
        const string onHalfOpenState = "onHalfOpen";
        
        var settings = ResolveSettings(serviceProvider);
        var logger = ResolveLogger(serviceProvider);
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: settings.CircuitBreakerMaxExceptions,
                durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreakerDurationBreakSeconds),
                onBreak: (outcome, timespan) =>
                {
                    if (outcome.Exception is not null)
                    {
                        logger.LogError("Circuit Breaker: {state}, Request ended with error: {exception}, " +
                                        "StatusCode: {status}, Time: {time}", 
                            onBreakState, outcome.Exception, outcome.Result.StatusCode, timespan);
                    }
                    else
                    {
                        logger.LogInformation("Circuit Breaker: {state}, StatusCode: {status}, Time: {time}", 
                            onBreakState, outcome.Result.StatusCode, timespan);
                    }
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit Breaker: {state}", onResetState);
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit Breaker: {state}", onHalfOpenState);
                });
    }

    private static ILogger ResolveLogger(IServiceProvider sp)
    {
        return sp.GetRequiredService<ILogger<IFrankfurterProvider>>();
    }

    private static FrankfurterSettings ResolveSettings(IServiceProvider sp)
    {
        return sp.GetRequiredService<IOptionsMonitor<FrankfurterSettings>>().CurrentValue;
    }
}