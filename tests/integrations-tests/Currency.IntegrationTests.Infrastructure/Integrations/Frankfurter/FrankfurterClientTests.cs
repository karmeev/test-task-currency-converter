using Currency.Api.Configurations;
using Currency.Api.Settings;
using Currency.Common.Providers;
using Currency.Infrastructure.Integrations.Providers.Frankfurter;
using Currency.Infrastructure.Settings;
using Currency.IntegrationTests.Infrastructure.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace Currency.IntegrationTests.Infrastructure.Integrations.Frankfurter;

[TestFixture]
[Category("Integration")]
public class FrankfurterClientTests
{
    private ILogger<FrankfurterClient> _logger;
    
    private HttpClient _client;
    private string WireMockAddress { get; set; }
    
    [SetUp]
    public void Setup()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        var services = new ServiceCollection();

        var settings = new StartupSettings
        {
            Integrations = new IntegrationsSettings
            {
                Frankfurter = new FrankfurterSettings
                {
                    BaseAddress = config.GetSection("FrankfurterUrl").Value,
                    TimeoutSeconds = 1,
                    RetryCount = 1,
                    RetryExponentialIntervalSeconds = 5,
                    CircuitBreakerDurationBreakSeconds = 30,
                    CircuitBreakerMaxExceptions = 6
                }
            }
        };
        
        _logger = Test.GetLogger<FrankfurterClient>();
        
        services.AddThirdPartyApis(settings);
        services.AddOptions();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(ProvidersConst.Frankfurter);

        _client = client;
        
        WireMockAddress = config.GetSection("WireMockUrl").Value;
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    [Test]
    public async Task GetLatestExchangeRateAsync_HappyPath_ReturnsLatestUsdRates()
    {
        //Arrange
        var sut = new FrankfurterClient(_client, _logger);

        //Act
        var result = await sut.GetLatestExchangeRateAsync("USD", CancellationToken.None);

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Base, Is.EqualTo("USD"));
            Assert.That(result.Date, Is.LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date)));
            Assert.That(result.Rates, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task GetLatestExchangeRateAsync_ReturnsInternalServerErrorOneTime_ShouldRetry()
    {
        //Arrange
        _client.BaseAddress = new Uri(WireMockAddress);
        var sut = new FrankfurterClient(_client, _logger);

        //Act
        var result = await sut.GetLatestExchangeRateAsync("USD", CancellationToken.None);

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Base, Is.EqualTo("USD"));
            Assert.That(result.Date, Is.LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date)));
            Assert.That(result.Rates, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task GetLatestExchangeRateAsync_ReturnsInternalServerErrorManyTime_ShouldThrowBrokenCircuitException()
    {
        //Arrange
        _client.BaseAddress = new Uri(WireMockAddress);
        var sut = new FrankfurterClient(_client, _logger);

        // Act
        for (var i = 0; i < 10; i++)
            try
            {
                await sut.GetLatestExchangeRateAsync("EUR");
            }
            catch
            {
            }

        // Assert
        Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
        {
            await sut.GetLatestExchangeRateAsync("EUR");
        });
    }

    [Test]
    public async Task GetExchangeRatesHistoryAsync_HappyPath_ReturnsHistory()
    {
        //Arrange
        var sut = new FrankfurterClient(_client, _logger);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        //Act
        var result = await sut.GetExchangeRatesHistoryAsync("USD", startDate,
            endDate, CancellationToken.None);

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Amount, Is.EqualTo(1));
            Assert.That(result.Base, Is.EqualTo("USD"));
            Assert.That(result.StartDate, Is.EqualTo(startDate));
            Assert.That(result.EndDate, Is.LessThanOrEqualTo(endDate));
            Assert.That(result.Rates, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Rates, Is.Not.Empty, "Expected at least one entry with USD rate.");
        });
    }

    [Test]
    public async Task GetLatestExchangeRatesAsync_HappyPath_ReturnsRates()
    {
        //Arrange
        var sut = new FrankfurterClient(_client, _logger);

        //Act
        var result = await sut.GetLatestExchangeRatesAsync("EUR", ["USD"], 
            CancellationToken.None);

        //Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Amount, Is.EqualTo(1));
            Assert.That(result.Base, Is.EqualTo("EUR"));
            Assert.That(result.Rates, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Rates.Keys.Any(r => r == "USD"), Is.True,
                "Expected at least one entry with USD rate.");
        });
    }
}