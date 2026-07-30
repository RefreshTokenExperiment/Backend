using Application.Abstractions;
using Application.Auth;
using Application.Auth.Commands.Logout;
using Domain.RefreshTokens;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace Application.Tests.Auth;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public sealed class LogoutTests
{
    private AppDbContext _db = null!;
    private LogoutCommandHandler _handler = null!;
    private Mock<IRefreshTokenHasher> _hasherMock = null!;
    private Mock<IAccessTokenBlacklist> _tokenBlacklistMock = null!;

    private IDbContextTransaction _transaction = null!;

    [SetUp]
    public async Task SetUp()
    {
        _hasherMock = new();
        _tokenBlacklistMock = new();
        _db = new(TestContainerFixture.Options);
        _handler = new(_hasherMock.Object, _db, _tokenBlacklistMock.Object);

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
    public async Task Logout_InvalidUserId_ReturnsFailure()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        const string accessToken = "=== AccessToken ===";
        const string refreshToken = "=== RefreshToken ===";
        const string userId = "=== Some Invalid User Id ===";

        var command = new LogoutCommand(accessToken, refreshToken, userId);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Never);
        _tokenBlacklistMock.Verify(x => x.Block(accessToken), Times.Never);
    }

    [Test]
    public async Task Logout_UnknownRefreshToken_ReturnsFailure()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        const string accessToken = "=== AccessToken ===";
        const string refreshToken = "=== RefreshToken ===";
        const string userId = "6ab717db-31f9-415c-b6c5-73e3908c2e2d";

        var command = new LogoutCommand(accessToken, refreshToken, userId);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.That(result.IsFailure, Is.False);

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Once);
        _tokenBlacklistMock.Verify(x => x.Block(accessToken), Times.Once);
    }

    [Test]
    public async Task Logout_UnknownUserId_ReturnsFailure()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        const string accessToken = "=== AccessToken ===";
        const string refreshToken = "=== RefreshToken ===";
        const string userId = "6ab717db-31f9-415c-b6c5-73e3908c2e2d";

        const string existingUserId = "e95b50ca-f191-4bad-8cc4-e4bf3abaebaa";

        var command = new LogoutCommand(accessToken, refreshToken, userId);

        var existingUser = new User(
            Email.Create("email@mail.com"),
            Username.Create("Username$"),
            Password.FromHash("S0m3-P@55w03d-H@5H")
        )
        {
            Id = new UserId(Guid.Parse(existingUserId))
        };
        await _db.Set<User>().AddAsync(existingUser, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.That(result.IsFailure, Is.False);

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Once);
        _tokenBlacklistMock.Verify(x => x.Block(accessToken), Times.Once);
    }

    [Test]
    public async Task Logout_WithRevokingRefresh_ReturnsSuccess()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        const string accessToken = "=== AccessToken ===";
        const string refreshToken = "=== RefreshToken ===";
        const string refreshTokenHash = "$S0@m3?R3!fr/35.HT=0k+3n?H@%5H$";
        const string existingUserId = "e95b50ca-f191-4bad-8cc4-e4bf3abaebaa";

        var command = new LogoutCommand(accessToken, refreshToken, existingUserId);

        var existingUser = new User(
            Email.Create("email@mail.com"),
            Username.Create("Username$"),
            Password.FromHash("S0m3-P@55w03d-H@5H")
        )
        {
            Id = new UserId(Guid.Parse(existingUserId))
        };

        var existingRefreshToken = new RefreshToken(refreshTokenHash, existingUser, "1234");
        await _db.Set<User>().AddAsync(existingUser, cancellationToken);
        await _db.Set<RefreshToken>().AddAsync(existingRefreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        _hasherMock.Setup(x => x.Hash(refreshToken)).Returns(refreshTokenHash);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);
        var foundToken = await _db.Set<RefreshToken>().SingleOrDefaultAsync(x => x.Hash == refreshTokenHash);

        // Assert
        Assert.That(result.IsFailure, Is.False);
        Assert.That(foundToken, Is.Not.Null);
        Assert.That(foundToken.IsRevoked, Is.True);

        _hasherMock.Verify(x => x.Hash(refreshToken), Times.Once);
        _tokenBlacklistMock.Verify(x => x.Block(accessToken), Times.Once);
    }
}