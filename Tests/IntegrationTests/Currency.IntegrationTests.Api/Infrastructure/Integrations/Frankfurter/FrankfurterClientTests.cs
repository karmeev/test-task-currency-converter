using Currency.Api.Configurations;
using Currency.Api.Settings;
using Currency.Common.Providers;
using Currency.Infrastructure.Contracts.Integrations;
using Currency.Infrastructure.Integrations.Providers.Frankfurter;
using Currency.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;

namespace Currency.IntegrationTests.Api.Infrastructure.Integrations.Frankfurter;

[TestFixture]
[Category("Integration tests")]
public class FrankfurterClientTests
{
    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        var settings = new StartupSettings
        {
            Integrations = new IntegrationsSettings
            {
                Frankfurter = new FrankfurterSettings
                {
                    BaseAddress = "https://api.frankfurter.dev",
                    TimeoutSeconds = 1,
                    RetryCount = 1,
                    RetryExponentialIntervalSeconds = 5,
                    CircuitBreakerDurationBreakSeconds = 30,
                    CircuitBreakerMaxExceptions = 6
                }
            }
        };
        
        services.AddThirdPartyApis(settings);
        services.AddOptions();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(ProvidersConst.Frankfurter);

        _client = client;
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    private HttpClient _client;
    private const string WireMockAddress = "http://localhost:8080";

    [Test]
    public async Task GetLatestExchangeRateAsync_HappyPath_ReturnsLatestUsdRates()
    {
        //Arrange
        var sut = new FrankfurterClient(_client, null);

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
        var sut = new FrankfurterClient(_client, null);

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
        var sut = new FrankfurterClient(_client, null);

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
        var sut = new FrankfurterClient(_client, null);
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
        var sut = new FrankfurterClient(_client, null);

        //Act
        var result = await sut.GetLatestExchangeRatesAsync("EUR", ["USD"], CancellationToken.None);

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