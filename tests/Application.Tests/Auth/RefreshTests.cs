using Application.Abstractions;
using Application.Auth;
using Application.Auth.Commands.Refresh;
using Domain.RefreshTokens;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace Application.Tests.Auth;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public sealed class RefreshTests
{
    private RefreshCommandHandler _handler = null!;
    private AppDbContext _db = null!;
    private Mock<IRefreshTokenHasher> _hasherMock = null!;
    private Mock<IRefreshTokenConfig> _configMock = null!;
    private Mock<IAuthorizationTokenGenerator> _generatorMock = null!;

    private IDbContextTransaction _transaction = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = new(TestContainerFixture.Options);
        _hasherMock = new();
        _configMock = new();
        _generatorMock = new();
        _handler = new(_hasherMock.Object, _db, _configMock.Object, _generatorMock.Object);

        _transaction = await _db.Database.BeginTransactionAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }
        await _db.DisposeAsync();
    }

    [Test]
    public async Task Refresh_WithUnknownHash_ReturnsFailure()
    {
        // Arrange
        const string refreshToken = "1234-5678-9abc-def0";
        const string refreshTokenHash = "=== S0m3R3fr35HT0k3nH@5H ===";
        const string deviceId = "0123456789abcdef";
        var command = new RefreshCommand(refreshToken, deviceId);

        _hasherMock.Setup(x => x.Hash(refreshToken)).Returns(refreshTokenHash);

        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Once);
    }

    [Test]
    public async Task Refresh_WithUnknownDeviceId_ReturnsFailure()
    {
        // Arrange
        const string refreshToken = "1234-5678-9abc-def0";
        const string refreshTokenHash = "=== S0m3R3fr35HT0k3nH@5H ===";
        const string deviceId = "0123456789abcdef";
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        var command = new RefreshCommand(refreshToken, "Some Invalid Device Id");


        var newUser = new User(
            Email.Create("somevalid@mail.com").Value!,
            Username.Create("SomeValid").Value!,
            Password.FromHash("S0m3H@5H3DP@55w0rD"));
        var newRefresh = new RefreshToken(refreshTokenHash, newUser, deviceId);
        await _db.Set<RefreshToken>().AddAsync(newRefresh, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _hasherMock.Setup(x => x.Hash(refreshToken)).Returns(refreshTokenHash);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Once);
    }

    [Test]
    public async Task Refresh_TokenIsRevoked_ReturnsFailure()
    {
        // Arrange
        const string refreshToken = "1234-5678-9abc-def0";
        const string refreshTokenHash = "=== S0m3R3fr35HT0k3nH@5H ===";
        const string deviceId = "0123456789abcdef";
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        var command = new RefreshCommand(refreshToken, deviceId);


        var newUser = new User(
            Email.Create("somevalid@mail.com").Value!,
            Username.Create("SomeValid").Value!,
            Password.FromHash("S0m3H@5H3DP@55w0rD"));
        var newRefresh = new RefreshToken(refreshTokenHash, newUser, deviceId);
        newRefresh.Revoke();
        await _db.Set<RefreshToken>().AddAsync(newRefresh, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        
        _hasherMock.Setup(x => x.Hash(refreshToken)).Returns(refreshTokenHash);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Once);
    }

    [Test]
    public async Task Refresh_LifecycleIsEnded_ReturnsFailure()
    {
        // Arrange
        const string refreshToken = "1234-5678-9abc-def0";
        const string refreshTokenHash = "=== S0m3R3fr35HT0k3nH@5H ===";
        const string deviceId = "0123456789abcdef";
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        var command = new RefreshCommand(refreshToken, deviceId);

        var maxLifetime = TimeSpan.Zero;
        var maxInactivity = TimeSpan.MaxValue;

        var newUser = new User(
            Email.Create("somevalid@mail.com").Value!,
            Username.Create("SomeValid").Value!,
            Password.FromHash("S0m3H@5H3DP@55w0rD"));
        var newRefresh = new RefreshToken(refreshTokenHash, newUser, deviceId);

        await _db.Set<RefreshToken>().AddAsync(newRefresh, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        
        _hasherMock.Setup(x => x.Hash(refreshToken)).Returns(refreshTokenHash);
        _configMock.SetupGet(x => x.MaxLifetime).Returns(maxLifetime);
        _configMock.SetupGet(x => x.MaxInactivity).Returns(maxInactivity);

        // Act
        await Task.Delay(1000);
        var result = await _handler.HandleAsync(command, cancellationToken);
        var foundToken = await _db.Set<RefreshToken>().SingleAsync(x => x.Hash == refreshTokenHash);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());
        Assert.That(foundToken.IsRevoked, Is.True);

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Once);
        _configMock.Verify(x => x.MaxLifetime, Times.Once);
        _configMock.Verify(x => x.MaxInactivity, Times.Once);
    }

    [Test]
    public async Task Refresh_LastUsedTooLongAgo_ReturnsFailure()
    {
        // Arrange
        const string refreshToken = "1234-5678-9abc-def0";
        const string refreshTokenHash = "=== S0m3R3fr35HT0k3nH@5H ===";
        const string deviceId = "0123456789abcdef";
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        var command = new RefreshCommand(refreshToken, deviceId);

        var maxLifetime = TimeSpan.MaxValue;
        var maxInactivity = TimeSpan.Zero;


        var newUser = new User(
            Email.Create("somevalid@mail.com").Value!,
            Username.Create("SomeValid").Value!,
            Password.FromHash("S0m3H@5H3DP@55w0rD"));
        var newRefresh = new RefreshToken(refreshTokenHash, newUser, deviceId);

        await _db.Set<RefreshToken>().AddAsync(newRefresh, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        
        _hasherMock.Setup(x => x.Hash(refreshToken)).Returns(refreshTokenHash);
        _configMock.SetupGet(x => x.MaxLifetime).Returns(maxLifetime);
        _configMock.SetupGet(x => x.MaxInactivity).Returns(maxInactivity);

        // Act
        await Task.Delay(1000);
        var result = await _handler.HandleAsync(command, cancellationToken);
        var foundToken = await _db.Set<RefreshToken>().SingleAsync(x => x.Hash == refreshTokenHash);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());
        Assert.That(foundToken.IsRevoked, Is.True);

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Once);
        _configMock.Verify(x => x.MaxLifetime, Times.Once);
        _configMock.Verify(x => x.MaxInactivity, Times.Once);
    }

    [Test]
    public async Task Refresh_TokenIsValid_ReturnsAccessToken()
    {
        // Arrange
        const string refreshToken = "1234-5678-9abc-def0";
        const string refreshTokenHash = "=== S0m3R3fr35HT0k3nH@5H ===";
        const string deviceId = "0123456789abcdef";
        const string accessToken = "=== Valid Access Token ===";
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        var command = new RefreshCommand(refreshToken, deviceId);

        var maxLifetime = TimeSpan.MaxValue;
        var maxInactivity = TimeSpan.MaxValue;

        var newUser = new User(
            Email.Create("somevalid@mail.com").Value!,
            Username.Create("SomeValid").Value!,
            Password.FromHash("S0m3H@5H3DP@55w0rD"));
        var newRefresh = new RefreshToken(refreshTokenHash, newUser, deviceId);

        await _db.Set<RefreshToken>().AddAsync(newRefresh, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        
        _hasherMock.Setup(x => x.Hash(refreshToken)).Returns(refreshTokenHash);
        _generatorMock.Setup(x => x.GenerateAccess(It.IsAny<User>())).Returns(accessToken);
        _configMock.SetupGet(x => x.MaxLifetime).Returns(maxLifetime);
        _configMock.SetupGet(x => x.MaxInactivity).Returns(maxInactivity);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);
        var foundToken = await _db.Set<RefreshToken>().SingleAsync(x => x.Hash == refreshTokenHash);

        // Assert
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.AccessToken, Is.EqualTo(accessToken));
        Assert.That(foundToken.IsRevoked, Is.False);
        Assert.That(foundToken.LastTimeUsed, Is.EqualTo(DateTimeOffset.Now).Within(TimeSpan.FromSeconds(5)));

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Once);
        _generatorMock.Verify(x => x.GenerateAccess(It.IsAny<User>()), Times.Once);
        _configMock.Verify(x => x.MaxLifetime, Times.Once);
        _configMock.Verify(x => x.MaxInactivity, Times.Once);
    }
}