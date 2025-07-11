using System.Threading.Channels;
using Autofac;
using Currency.Domain.Operations;
using Currency.Domain.Rates;
using Currency.Services.Application;
using Currency.Services.Application.Consumers;
using Currency.Services.Application.Consumers.Base;
using Currency.Services.Application.Settings;
using Currency.Services.Contracts.Application;
using Currency.Services.Contracts.Domain;
using Currency.Services.Domain;

namespace Currency.Services;

public static class Registry
{
    public static void RegisterDependencies(ContainerBuilder container, ServicesSettings settings)
    {
        RegisterConsumers(container, settings.Worker);
        container.RegisterType<UserService>().As<IUserService>();
        container.RegisterType<TokenService>().As<ITokenService>();
        container.RegisterType<ConverterService>().As<IConverterService>();
        container.RegisterType<ExchangeRatesService>().As<IExchangeRatesService>();
    }

    private static void RegisterConsumers(ContainerBuilder container, WorkerSettings settings)
    {
        container.RegisterType<PublisherService>().As<IPublisherService>().SingleInstance();
        
        //Consumers
        container.RegisterType<ConsumerService>().As<IConsumerService>().SingleInstance();
        container.RegisterType<ExchangeRatesConsumer>()
            .As<IConsumer<ExchangeRates>>()
            .As<IConsumer<ExchangeRatesHistory>>()
            .InstancePerLifetimeScope();
        container.RegisterType<CurrencyConversionConsumer>()
            .As<IConsumer<CurrencyConversion>>()
            .InstancePerLifetimeScope();

        //Channels
        if (settings.ExchangeRatesHistoryWorkers > 0)
        {
            var historyCapacity = new BoundedChannelOptions(100);
            container.Register(_ => Channel.CreateBounded<ExchangeRatesHistory>(historyCapacity))
                .As<Channel<ExchangeRatesHistory>>()
                .SingleInstance();
            container.Register(c => c.Resolve<Channel<ExchangeRatesHistory>>().Writer)
                .As<ChannelWriter<ExchangeRatesHistory>>()
                .SingleInstance();
        }
        
        if (settings.CurrencyConversionWorkers > 0)
        {
            var currencyConversionCapacity = new BoundedChannelOptions(100);
            container.Register(_ => Channel.CreateBounded<CurrencyConversion>(currencyConversionCapacity))
                .As<Channel<CurrencyConversion>>()
                .SingleInstance();

            container.Register(c => c.Resolve<Channel<CurrencyConversion>>().Writer)
                .As<ChannelWriter<CurrencyConversion>>()
                .SingleInstance();
        }
        
        if (settings.ExchangeRatesWorkers > 0)
        {
            var exchangeRatesCapacity = new BoundedChannelOptions(100);
            container.Register(_ => Channel.CreateBounded<ExchangeRates>(exchangeRatesCapacity))
                .As<Channel<ExchangeRates>>()
                .SingleInstance();

            container.Register(c => c.Resolve<Channel<ExchangeRates>>().Writer)
                .As<ChannelWriter<ExchangeRates>>()
                .SingleInstance();
        }
    }
}