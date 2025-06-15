using Currency.Infrastructure.Integrations.Providers.Frankfurter.Converters;
using Newtonsoft.Json;

namespace Currency.Infrastructure.Tests.Providers.Frankfurter.Converters;

[TestFixture]
[Category("Unit")]
public class RatesDateOnlyConverterTests
{
    private JsonSerializerSettings _settings;

    [SetUp]
    public void Setup()
    {
        _settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new RatesDateOnlyConverter() }
        };
    }

    [Test]
    public void Serialize_Dictionary_ShouldProduceCorrectJson()
    {
        var dict = new Dictionary<DateOnly, Dictionary<string, decimal>>
        {
            [new DateOnly(2023, 4, 15)] = new Dictionary<string, decimal>
            {
                ["USD"] = 1.1m,
                ["EUR"] = 0.9m
            }
        };

        var json = JsonConvert.SerializeObject(dict, _settings);

        var expectedJson = "{\"2023-04-15\":{\"USD\":1.1,\"EUR\":0.9}}";
        Assert.That(json, Is.EqualTo(expectedJson));
    }

    [Test]
    public void Deserialize_ValidJson_ShouldReturnDictionary()
    {
        var json = "{\"2023-04-15\":{\"USD\":1.1,\"EUR\":0.9}}";

        var dict = JsonConvert.DeserializeObject<Dictionary<DateOnly, Dictionary<string, decimal>>>(json, _settings);

        Assert.That(dict, Contains.Key(new DateOnly(2023, 4, 15)));
        Assert.That(dict[new DateOnly(2023, 4, 15)], Contains.Key("USD"));
        Assert.That(dict[new DateOnly(2023, 4, 15)]["USD"], Is.EqualTo(1.1m));
        Assert.That(dict[new DateOnly(2023, 4, 15)]["EUR"], Is.EqualTo(0.9m));
    }

    [Test]
    public void Deserialize_InvalidDateKey_ShouldSkipEntry()
    {
        var json = "{\"invalid-date\":{\"USD\":1.1}}";

        var dict = JsonConvert.DeserializeObject<Dictionary<DateOnly, Dictionary<string, decimal>>>(json, _settings);

        Assert.That(dict, Is.Empty);
    }
}
