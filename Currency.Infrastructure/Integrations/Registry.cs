using Autofac;
using Currency.Common.Providers;
using Currency.Infrastructure.Contracts.Integrations;
using Currency.Infrastructure.Integrations.Providers;
using Currency.Infrastructure.Integrations.Providers.Frankfurter;
using Currency.Infrastructure.Settings;
using Microsoft.Extensions.Logging;

namespace Currency.Infrastructure.Integrations;

internal static class Registry
{
    public static void RegisterDependencies(ContainerBuilder container)
    {
        CurrencyProvidersFactory.RegisterProviders(container);

        container.Register<IFrankfurterClient>(ctx =>
            {
                var httpClientFactory = ctx.Resolve<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient(ProvidersConst.Frankfurter);
                var settings = ctx.Resolve<InfrastructureSettings>().Integrations.Frankfurter;

                client.BaseAddress = settings.BaseAddressUri;
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
                
                var logger = ctx.Resolve<ILogger<FrankfurterClient>>();

                return new FrankfurterClient(client, logger);
            })
            .As<IFrankfurterClient>()
            .InstancePerLifetimeScope();
    }
}