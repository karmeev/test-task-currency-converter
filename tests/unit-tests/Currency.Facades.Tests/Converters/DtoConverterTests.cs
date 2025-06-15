using Currency.Data.Contracts.Entries;
using Currency.Domain.Rates;
using Currency.Facades.Contracts.Dtos;
using Currency.Facades.Converters;

namespace Currency.Facades.Tests.Converters;

[Category("Unit")]
public class DtoConverterTests
{
    [Test]
    public void ConvertToRatesHistoryPartDto_FromExchangeRateEntries_ShouldReturnMappedDtos()
    {
        // Arrange
        var entries = new List<ExchangeRateEntry>
        {
            new() { Currency = "USD", Date = new DateTime(2024, 1, 1), Value = 1.1m },
            new() { Currency = "EUR", Date = new DateTime(2024, 1, 2), Value = 0.9m }
        };

        // Act
        var result = DtoConverter.ConvertToRatesHistoryPartDto(entries).ToArray();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Length, Is.EqualTo(2));
            Assert.That(result[0].Currency, Is.EqualTo("USD"));
            Assert.That(result[0].Date, Is.EqualTo(new DateTime(2024, 1, 1)));
            Assert.That(result[0].Value, Is.EqualTo(1.1m));

            Assert.That(result[1].Currency, Is.EqualTo("EUR"));
            Assert.That(result[1].Date, Is.EqualTo(new DateTime(2024, 1, 2)));
            Assert.That(result[1].Value, Is.EqualTo(0.9m));
        });
    }

    [Test]
    public void ConvertToRatesHistoryPartDto_FromExchangeRatesHistory_ShouldFlattenAndMap()
    {
        // Arrange
        var history = new ExchangeRatesHistory
        {
            Rates = new Dictionary<DateTime, Dictionary<string, decimal>>
            {
                [new DateTime(2024, 1, 1)] = new Dictionary<string, decimal>
                {
                    ["USD"] = 1.1m,
                    ["EUR"] = 0.9m
                },
                [new DateTime(2024, 1, 2)] = new Dictionary<string, decimal>
                {
                    ["GBP"] = 0.8m
                }
            }
        };

        // Act
        var result = DtoConverter.ConvertToRatesHistoryPartDto(history).ToArray();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Length, Is.EqualTo(3));

            Assert.That(result, Has.Some.Matches<RatesHistoryPartDto>(dto =>
                dto.Currency == "USD" && dto.Date == new DateTime(2024, 1, 1) && dto.Value == 1.1m));

            Assert.That(result, Has.Some.Matches<RatesHistoryPartDto>(dto =>
                dto.Currency == "EUR" && dto.Date == new DateTime(2024, 1, 1) && dto.Value == 0.9m));

            Assert.That(result, Has.Some.Matches<RatesHistoryPartDto>(dto =>
                dto.Currency == "GBP" && dto.Date == new DateTime(2024, 1, 2) && dto.Value == 0.8m));
        });
    }

    [Test]
    public void ConvertToRatesHistoryPartDto_FromEmptyExchangeRateEntries_ShouldReturnEmpty()
    {
        var result = DtoConverter.ConvertToRatesHistoryPartDto(Enumerable.Empty<ExchangeRateEntry>());
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ConvertToRatesHistoryPartDto_FromEmptyExchangeRatesHistory_ShouldReturnEmpty()
    {
        var history = new ExchangeRatesHistory
        {
            Rates = new Dictionary<DateTime, Dictionary<string, decimal>>()
        };

        var result = DtoConverter.ConvertToRatesHistoryPartDto(history);
        Assert.That(result, Is.Empty);
    }
}

