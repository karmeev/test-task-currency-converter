using System.Text.Json;
using Currency.Infrastructure.Contracts.Databases.Redis.Entries;
using Currency.Infrastructure.Redis;
using Currency.Infrastructure.Settings;
using Moq;
using StackExchange.Redis;

namespace Currency.Infrastructure.Tests.Redis;

[TestFixture]
public class RedisContextTests
{
    private Mock<IConnectionMultiplexer> _mockConnection = null!;
    private Mock<IDatabase> _mockDb = null!;
    private Mock<IDatabase> _mockIndexesDb = null!;
    private Mock<IDatabase> _mockLocksDb = null!;
    private InfrastructureSettings _settings = null!;
    private RedisContext _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mockConnection = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
        _mockDb = new Mock<IDatabase>(MockBehavior.Strict);
        _mockIndexesDb = new Mock<IDatabase>(MockBehavior.Strict);
        _mockLocksDb = new Mock<IDatabase>(MockBehavior.Strict);

        var redisSettings = new RedisSettings
        {
            RefreshTokensDatabaseNumber = 1,
            ExchangeRatesHistoryDatabaseNumber = 2,
            ExchangeRatesDatabaseNumber = 3,
            DataLockMilliseconds = 500,
            DataLockRetryCount = 3,
            DataLockRetryDelayMilliseconds = 100
        };

        var mockRedisOptions = new Mock<Microsoft.Extensions.Options.IOptionsMonitor<RedisSettings>>();
        mockRedisOptions.Setup(x => x.CurrentValue).Returns(redisSettings);

        var mockJwtOptions = new Mock<Microsoft.Extensions.Options.IOptionsMonitor<JwtSettings>>();
        mockJwtOptions.Setup(x => x.CurrentValue).Returns(new JwtSettings());

        var mockFrankfurterOptions = new Mock<Microsoft.Extensions.Options.IOptionsMonitor<FrankfurterSettings>>();
        mockFrankfurterOptions.Setup(x => x.CurrentValue).Returns(new FrankfurterSettings());

        _settings = new InfrastructureSettings(
            mockJwtOptions.Object,
            mockRedisOptions.Object,
            mockFrankfurterOptions.Object);

        // Setup connection multiplexer to return different DB mocks based on db number
        _mockConnection.Setup(c => c.GetDatabase(0, null)).Returns(_mockIndexesDb.Object);
        _mockConnection.Setup(c => c.GetDatabase(1, null)).Returns(_mockLocksDb.Object);
        _mockConnection.Setup(c => c.GetDatabase(redisSettings.RefreshTokensDatabaseNumber, null)).Returns(_mockDb.Object);
        _mockConnection.Setup(c => c.GetDatabase(redisSettings.ExchangeRatesHistoryDatabaseNumber, null)).Returns(_mockDb.Object);
        _mockConnection.Setup(c => c.GetDatabase(redisSettings.ExchangeRatesDatabaseNumber, null)).Returns(_mockDb.Object);
        _mockConnection.Setup(c => c.GetDatabase(15, null)).Returns(_mockDb.Object);
        _mockConnection.Setup(c => c.GetDatabase(14, null)).Returns(_mockDb.Object);

        _sut = new RedisContext(_settings, _mockConnection.Object);
    }

    [Test]
    public async Task SetAsync_ShouldSerializeAndSetValue()
    {
        var key = "auth:token1";
        var obj = new { Name = "Kirill", Age = 35 };

        _mockDb
            .Setup(db => db.StringSetAsync(
                key,
                It.Is<RedisValue>(v => v.ToString().Contains("Kirill")),
                null,
                When.Always,
                CommandFlags.None))
            .ReturnsAsync(true)
            .Verifiable();

        await _sut.SetAsync(key, obj);

        _mockDb.Verify();
    }

    [Test]
    public async Task TryGetAsync_ShouldDeserializeValue_WhenValueExists()
    {
        var key = "user:1";
        var obj = new { Name = "My Lord", Age = 40 };
        var serialized = JsonSerializer.Serialize(obj);

        _mockDb
            .Setup(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(serialized)
            .Verifiable();

        var result = await _sut.TryGetAsync<dynamic>(key);

        Assert.That(result.Name.ToString(), Is.EqualTo("My Lord"));
        Assert.That(result.Age, Is.EqualTo(40));
        _mockDb.Verify();
    }

    [Test]
    public async Task TryGetAsync_ShouldReturnDefault_WhenValueDoesNotExist()
    {
        var key = "user:nonexistent";

        _mockDb
            .Setup(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(RedisValue.Null)
            .Verifiable();

        var result = await _sut.TryGetAsync<object>(key);

        Assert.That(result, Is.Null);
        _mockDb.Verify();
    }

    [Test]
    public async Task TryGetByIndexAsync_ShouldGetKeyFromIndexAndReturnEntity()
    {
        var indexKey = "index:key1";
        var storedKey = "user:123";
        var obj = new { Name = "Indexed User" };
        var serialized = JsonSerializer.Serialize(obj);

        // Index DB returns storedKey
        _mockIndexesDb.Setup(db => db.StringGetAsync(indexKey, CommandFlags.None))
                      .ReturnsAsync(storedKey)
                      .Verifiable();

        // Data DB returns serialized object
        _mockDb.Setup(db => db.StringGetAsync(storedKey, CommandFlags.None))
               .ReturnsAsync(serialized)
               .Verifiable();

        var result = await _sut.TryGetByIndexAsync<dynamic>(indexKey);

        Assert.That(result.Name.ToString(), Is.EqualTo("Indexed User"));
        _mockIndexesDb.Verify();
        _mockDb.Verify();
    }

    [Test]
    public async Task TryGetByIndexAsync_ShouldReturnDefault_WhenEntityDoesNotExist()
    {
        var indexKey = "index:key2";
        var storedKey = "user:456";

        _mockIndexesDb.Setup(db => db.StringGetAsync(indexKey, CommandFlags.None))
                      .ReturnsAsync(storedKey)
                      .Verifiable();

        _mockDb.Setup(db => db.StringGetAsync(storedKey, CommandFlags.None))
               .ReturnsAsync(RedisValue.Null)
               .Verifiable();

        var result = await _sut.TryGetByIndexAsync<dynamic>(indexKey);

        Assert.That(result, Is.Null);
        _mockIndexesDb.Verify();
        _mockDb.Verify();
    }

    [Test]
    public async Task KeyExistsAsync_ShouldReturnCorrectValue()
    {
        var key = "auth:somekey";

        _mockDb.Setup(db => db.KeyExistsAsync(key, CommandFlags.None))
               .ReturnsAsync(true)
               .Verifiable();

        var exists = await _sut.KeyExistsAsync(key);

        Assert.That(exists, Is.True);
        _mockDb.Verify();
    }

    [Test]
    public async Task SortedSetAddAsync_ShouldAddEntriesAndSetExpiry_WhenTtlProvided()
    {
        var key = "sortedset:key";
        var entries = new[]
        {
            new RedisSortedSetEntry("val1", 1),
            new RedisSortedSetEntry("val2", 2)
        };
        var ttl = TimeSpan.FromMinutes(5);

        _mockDb.Setup(db => db.SortedSetAddAsync(
            key,
            It.Is<SortedSetEntry[]>(arr => arr.Length == 2
                                          && arr.Any(e => e.Element == "val1" && e.Score == 1)
                                          && arr.Any(e => e.Element == "val2" && e.Score == 2)),
            CommandFlags.None))
            .ReturnsAsync(2)
            .Verifiable();

        _mockDb.Setup(db => db.KeyExpireAsync(key, ttl, CommandFlags.None))
               .ReturnsAsync(true)
               .Verifiable();

        await _sut.SortedSetAddAsync(key, entries, ttl);

        _mockDb.Verify();
    }

    [Test]
    public async Task SortedSetRangeByRankAsync_ShouldReturnSortedValues()
    {
        var key = "sortedset:key";

        var redisValues = new RedisValue[] { "val1", "val2", "val3" };
        _mockDb.Setup(db => db.SortedSetRangeByRankAsync(key, 0, 2, Order.Ascending, CommandFlags.None))
               .ReturnsAsync(redisValues)
               .Verifiable();

        var result = await _sut.SortedSetRangeByRankAsync(key, 0, 2);

        Assert.That(result, Is.EqualTo(redisValues.Select(rv => rv.ToString()).ToArray()));
        _mockDb.Verify();
    }

    [Test]
    public async Task AcquireLockAsync_ShouldSetLockKeyWithTtl()
    {
        var key = "lockkey";
        var lockId = "lock123";
        var ttl = TimeSpan.FromMilliseconds(_settings.RedisSettings.DataLockMilliseconds);

        _mockLocksDb.Setup(db => db.StringSetAsync(
            $"lock:{key}",
            lockId,
            ttl,
            When.NotExists,
            CommandFlags.None))
            .ReturnsAsync(true)
            .Verifiable();

        var result = await _sut.AcquireLockAsync(key, lockId);

        Assert.That(result, Is.True);
        _mockLocksDb.Verify();
    }

    [Test]
    public async Task ReleaseLockAsync_ShouldRunScriptAndReturnTrue_WhenDeleted()
    {
        var key = "lockkey";
        var lockId = "lock123";
        var script = """
                             if redis.call('get', KEYS[1]) == ARGV[1] then
                                 return redis.call('del', KEYS[1])
                             else
                                 return 0
                             end
                     """;

        _mockLocksDb.Setup(db => db.ScriptEvaluateAsync(
            script,
            It.Is<RedisKey[]>(keys => keys.Length == 1 && keys[0] == $"lock:{key}"),
            It.Is<RedisValue[]>(vals => vals.Length == 1 && vals[0] == lockId),
            CommandFlags.None))
            .ReturnsAsync(RedisResult.Create(1))
            .Verifiable();

        var result = await _sut.ReleaseLockAsync(key, lockId);

        Assert.That(result, Is.True);
        _mockLocksDb.Verify();
    }

    [Test]
    public async Task ReleaseLockAsync_ShouldReturnFalse_WhenNotDeleted()
    {
        var key = "lockkey";
        var lockId = "lock123";
        var script = """
                             if redis.call('get', KEYS[1]) == ARGV[1] then
                                 return redis.call('del', KEYS[1])
                             else
                                 return 0
                             end
                     """;

        _mockLocksDb.Setup(db => db.ScriptEvaluateAsync(
            script,
            It.Is<RedisKey[]>(keys => keys.Length == 1 && keys[0] == $"lock:{key}"),
            It.Is<RedisValue[]>(vals => vals.Length == 1 && vals[0] == lockId),
            CommandFlags.None))
            .ReturnsAsync(RedisResult.Create(0))
            .Verifiable();

        var result = await _sut.ReleaseLockAsync(key, lockId);

        Assert.That(result, Is.False);
        _mockLocksDb.Verify();
    }

    [TestCase("auth:token1", 1)]
    [TestCase("rateshistory:entry", 2)]
    [TestCase("exchangerates:data", 3)]
    [TestCase("user:42", 15)]
    [TestCase("somekey", 14)]
    public void GetDatabase_ShouldReturnCorrectDatabaseBasedOnKey(string key, int expectedDb)
    {
        var db = _sut.GetType()
            .GetMethod("GetDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_sut, new object[] { key }) as IDatabase;

        Assert.That(db, Is.Not.Null);

        _mockConnection.Verify(c => c.GetDatabase(expectedDb, null), Times.AtLeastOnce);
    }
}
