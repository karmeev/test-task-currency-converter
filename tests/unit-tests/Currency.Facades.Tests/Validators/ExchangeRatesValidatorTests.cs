using Currency.Facades.Contracts.Requests;
using Currency.Facades.Validators;

namespace Currency.Facades.Tests.Validators;

[Category("Unit")]
public class ExchangeRatesValidatorTests
{
    [SetUp]
    public void Setup()
    {
        CurrencyCodesResolver.GetCurrenciesByCodeOverride = code =>
            new[] { "USD", "EUR", "GBP" }.Contains(code.ToUpper()) ? new[] { code } : Array.Empty<string>();
    }

    [Test]
    public void ValidateRequest_GetHistoryRequest_InvalidRequest_ShouldReturnErrors()
    {
        var request = new GetHistoryRequest
        {
            Page = 0,
            PageSize = 0,
            Currency = "XYZ5",
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var result = ExchangeRatesValidator.ValidateRequest(request, out var errors);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(errors, Has.Exactly(4).Items);
            Assert.That(errors, Does.Contain("Page size must be greater than or equal to 1."));
            Assert.That(errors, Does.Contain("Currency not valid."));
            Assert.That(errors, Does.Contain("The end date must be after or equal the start date."));
            Assert.That(errors, Does.Contain("The end date can not be in future.").IgnoreCase);
        });
    }

    [Test]
    public void ValidateRequest_GetHistoryRequest_ValidRequest_ShouldSucceed()
    {
        var request = new GetHistoryRequest
        {
            Page = 1,
            PageSize = 25,
            Currency = "USD",
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow
        };

        var result = ExchangeRatesValidator.ValidateRequest(request, out var errors);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(errors, Is.Empty);
        });
    }

    [Test]
    public void ValidateRequest_ConvertToCurrencyRequest_InvalidAmount_ShouldReturnError()
    {
        var request = new ConvertToCurrencyRequest
        {
            Amount = -5,
            FromCurrency = "USD",
            ToCurrency = "EUR"
        };

        var result = ExchangeRatesValidator.ValidateRequest(request, out var errors);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(errors, Does.Contain("Amount must be greater than or equal to 0."));
        });
    }

    [Test]
    public void ValidateRequest_ConvertToCurrencyRequest_BannedCurrencies_ShouldReturnError()
    {
        var request = new ConvertToCurrencyRequest
        {
            Amount = 100,
            FromCurrency = "TRY",
            ToCurrency = "PLN"
        };

        var result = ExchangeRatesValidator.ValidateRequest(request, out var errors);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(errors, Has.Some.Contains("TRY"));
            Assert.That(errors, Has.Some.Contains("PLN"));
        });
    }

    [Test]
    public void ValidateRequest_ConvertToCurrencyRequest_InvalidCurrencies_ShouldReturnError()
    {
        var request = new ConvertToCurrencyRequest
        {
            Amount = 100,
            FromCurrency = "XXX4",
            ToCurrency = "YY1Y"
        };

        var result = ExchangeRatesValidator.ValidateRequest(request, out var errors);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(errors, Does.Contain("Invalid currency: XXX4"));
            Assert.That(errors, Does.Contain("Invalid currency: YY1Y."));
        });
    }

    [Test]
    public void ValidateRequest_ConvertToCurrencyRequest_ValidRequest_ShouldSucceed()
    {
        var request = new ConvertToCurrencyRequest
        {
            Amount = 500,
            FromCurrency = "USD",
            ToCurrency = "EUR"
        };

        var result = ExchangeRatesValidator.ValidateRequest(request, out var errors);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(errors, Is.Empty);
        });
    }

    [Test]
    public void ValidateRequest_StringCurrency_Invalid_ShouldFail()
    {
        var result = ExchangeRatesValidator.ValidateRequest("ABC", out var _);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ValidateRequest_StringCurrency_Valid_ShouldSucceed()
    {
        var result = ExchangeRatesValidator.ValidateRequest("EUR", out var _);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateRequest_UnknownRequestType_ShouldFail()
    {
        var dummy = new object();
        var result = ExchangeRatesValidator.ValidateRequest(dummy, out var errors);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(errors, Is.Empty);
        });
    }

    private static class CurrencyCodesResolver
    {
        public static Func<string, IEnumerable<string>> GetCurrenciesByCodeOverride;

        public static IEnumerable<string> GetCurrenciesByCode(string code)
        {
            return GetCurrenciesByCodeOverride?.Invoke(code) ?? Array.Empty<string>();
        }
    }
}