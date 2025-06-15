using Currency.Infrastructure.Integrations.Providers.Frankfurter.Converters;
using Newtonsoft.Json;

namespace Currency.Infrastructure.Tests.Providers.Frankfurter.Converters;

[TestFixture]
[Category("Unit")]
public class DateOnlyConverterTests
{
    private JsonSerializerSettings _settings;

    [SetUp]
    public void Setup()
    {
        _settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new DateOnlyConverter() }
        };
    }

    [Test]
    public void Serialize_DateOnly_ShouldBeFormattedCorrectly()
    {
        var date = new DateOnly(2023, 4, 15);
        var json = JsonConvert.SerializeObject(date, _settings);

        Assert.That(json, Is.EqualTo("\"2023-04-15\""));
    }

    [Test]
    public void Deserialize_ValidDateString_ShouldReturnDateOnly()
    {
        var json = "\"2023-04-15\"";

        var date = JsonConvert.DeserializeObject<DateOnly>(json, _settings);

        Assert.That(date, Is.EqualTo(new DateOnly(2023, 4, 15)));
    }

    [Test]
    public void Deserialize_InvalidDateString_ShouldThrow()
    {
        var json = "\"15-04-2023\""; // wrong format

        var ex = Assert.Throws<JsonSerializationException>(() =>
            JsonConvert.DeserializeObject<DateOnly>(json, _settings));

        Assert.That(ex.Message, Does.Contain("Invalid date format"));
    }
}
