using Infrastructure.Persistence.Repositories;
using Infrastructure.Services.Authorization;
using Moq;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Infrastructure.Tests.Repositories;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.Self)]
public sealed class RedisAccessTokenBlacklistTests
{
    private static RedisContainer _container = null!;
    private static ConnectionMultiplexer _cache = null!;

    private RedisAccessTokenBlacklist _repository = null!;

    private Mock<IJwtConfig> _configMock = null!;

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        _container = new RedisBuilder("redis:8.10").Build();
        await _container.StartAsync();

        _cache = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDown()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    [SetUp]
    public void SetUp()
    {
        _configMock = new();
        _repository = new(_cache, _configMock.Object);
    }

    [Test]
    public async Task Block_CommonUse_SetsAccessToRedis()
    {
        // Arrange
        const string accessToken = "=== Some Access Token ===" + nameof(Block_CommonUse_SetsAccessToRedis);

        _configMock.SetupGet(x => x.Expiration).Returns(TimeSpan.FromMinutes(10));

        // Act
        await _repository.BlockAsync(accessToken);
        var foundAccessToken = await _cache.GetDatabase().KeyExistsAsync(accessToken);

        // Assert
        Assert.That(foundAccessToken, Is.True);
    }

    [Test]
    public async Task Exists_NewKey_ReturnsPresence()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        const string existingAccessToken = "=== Some Access Token ===" + nameof(Exists_NewKey_ReturnsPresence);

        await _cache.GetDatabase().StringSetAsync(existingAccessToken, string.Empty);

        // Act
        var result = await _repository.ExistsAsync(existingAccessToken, cancellationToken);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task Exists_ExpiredKey_ReturnsAbsence()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        const string existingAccessToken = "=== Some Access Token ===" + nameof(Exists_ExpiredKey_ReturnsAbsence);

        await _cache.GetDatabase().StringSetAsync(existingAccessToken, string.Empty, TimeSpan.FromMilliseconds(1));

        // Act
        await Task.Delay(1);
        var result = await _repository.ExistsAsync(existingAccessToken, cancellationToken);

        // Assert
        Assert.That(result, Is.False);
    }
}