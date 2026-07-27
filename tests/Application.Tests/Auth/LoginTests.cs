using System.Security.Cryptography;
using Application.Abstractions;
using Application.Auth;
using Application.Auth.Commands.Login;
using Domain.RefreshTokens;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace Application.Tests.Auth;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public sealed class LoginTests
{
    private AppDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;
    private Mock<IPasswordHasher> _hasher = null!;
    private Mock<IAuthorizationTokenGenerator> _tokenGenerator = null!;
    private LoginCommandHandler _handler = null!;


    [SetUp]
    public async Task SetUp()
    {
        _db = new AppDbContext(TestContainerFixture.Options);
        _hasher = new();
        _tokenGenerator = new();
        _handler = new LoginCommandHandler(_db, _hasher.Object, _tokenGenerator.Object);

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
    public async Task Login_WithInvalidCredentials_ReturnsFailure(
        [Values("s0m email@mail.com", "_InvalidUsername", "invalid@mail.", "usr")] string emailOrUsername
    )
    {
        // Arrange
        var loginCommand = new LoginCommand(emailOrUsername, "S0meP@5w0rD", RandomNumberGenerator.GetHexString(32));
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        // Act
        var result = await _handler.HandleAsync(loginCommand, cancellationToken);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());

        _hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Login_WithEmptyDeviceId_ReturnsFailure()
    {
        // Arrange
        var loginCommandNull = new LoginCommand("ValidUsername", "S0meP@5w0rD", null!);
        var loginCommandEmpty = new LoginCommand("ValidUsername", "S0meP@5w0rD", string.Empty);
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        // Act
        var resultNull = await _handler.HandleAsync(loginCommandNull, cancellationToken);
        var resultEmpty = await _handler.HandleAsync(loginCommandEmpty, cancellationToken);

        // Assert
        Assert.That(resultNull.IsFailure, Is.True);
        Assert.That(resultEmpty.IsFailure, Is.True);
        Assert.That(resultNull.Error, Is.TypeOf<AuthErrors.DeviceIdIsNull>());
        Assert.That(resultEmpty.Error, Is.TypeOf<AuthErrors.DeviceIdIsNull>());

        _hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Login_WithUnknownCredentials_ReturnsFailure()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        const string existingEmail = "some-email@mail.com";
        const string existingUsername = "SomeUsername$";
        var existingUser = new User(
            Email.Create(existingEmail).Value!,
            Username.Create(existingUsername).Value!,
            Password.FromHash("S0me-P@55w0rD-H@5H")
        );

        await _db.Set<User>().AddAsync(existingUser);
        await _db.SaveChangesAsync(cancellationToken);

        var loginCommand1 = new LoginCommand("another-email@mail.com", "S0meP@5w0rD", RandomNumberGenerator.GetHexString(32));
        var loginCommand2 = new LoginCommand("AnotherUsername$", "S0meP@5w0rD", RandomNumberGenerator.GetHexString(32));

        // Act
        var result1 = await _handler.HandleAsync(loginCommand1, cancellationToken);
        var result2 = await _handler.HandleAsync(loginCommand2, cancellationToken);

        // Assert
        Assert.That(result1.IsFailure, Is.True);
        Assert.That(result1.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());
        Assert.That(result2.IsFailure, Is.True);
        Assert.That(result2.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());

        _hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Login_WithIncorrectPassword_ReturnsFailure()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        const string existingEmail = "some-email@mail.com";
        const string existingUsername = "SomeUsername$";
        var existingUser = new User(
            Email.Create(existingEmail).Value!,
            Username.Create(existingUsername).Value!,
            Password.FromHash("S0me-P@55w0rD-H@5H")
        );

        await _db.Set<User>().AddAsync(existingUser);
        await _db.SaveChangesAsync(cancellationToken);

        var loginCommand1 = new LoginCommand("some-email@mail.com", "S0me1nv@l1DP@5w0rD", RandomNumberGenerator.GetHexString(32));
        var loginCommand2 = new LoginCommand("SomeUsername$", "S0me1nv@l1DP@5w0rD", RandomNumberGenerator.GetHexString(32));

        _hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<Password>())).Returns(false);

        // Act
        var result1 = await _handler.HandleAsync(loginCommand1, cancellationToken);
        var result2 = await _handler.HandleAsync(loginCommand2, cancellationToken);

        // Assert
        Assert.That(result1.IsFailure, Is.True);
        Assert.That(result1.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());
        Assert.That(result2.IsFailure, Is.True);
        Assert.That(result2.Error, Is.TypeOf<AuthErrors.InvalidCredentials>());

        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<Password>()), Times.Exactly(2));
    }

    [Test]
    public async Task Login_SuccessfullyWithEmail_ReturnsTokens()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        const string existingEmail = "some-email@mail.com";
        const string existingUsername = "SomeUsername$";
        const string existingPasswordHash = "S0me-P@55w0rD-H@5H";
        const string accessToken = "=== SomeAccessToken ===";
        const string refreshToken = "=== SomeRefreshToken ===";
        const string refreshTokenHash = "=== S0m3R3fR35HT0k3nH@5h ===";

        var existingUser = new User(
            Email.Create(existingEmail).Value!,
            Username.Create(existingUsername).Value!,
            Password.FromHash(existingPasswordHash)
        );

        await _db.Set<User>().AddAsync(existingUser);
        await _db.SaveChangesAsync(cancellationToken);

        var loginCommand = new LoginCommand(existingEmail, existingPasswordHash, RandomNumberGenerator.GetHexString(32));

        _hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<Password>())).Returns(true);
        _tokenGenerator.Setup(x => x.GenerateAccess(It.IsAny<User>())).Returns(accessToken);
        _tokenGenerator.Setup(x => x.GenerateRefresh()).Returns((refreshToken, refreshTokenHash));

        // Act
        var result = await _handler.HandleAsync(loginCommand, cancellationToken);
        var foundToken = await _db.Set<RefreshToken>().SingleOrDefaultAsync(x => x.Hash == refreshTokenHash);

        // Assert
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.Value!.AccessToken, Is.EqualTo(accessToken));
        Assert.That(result.Value!.RefreshToken, Is.EqualTo(refreshToken));

        Assert.That(foundToken, Is.Not.Null);
        Assert.That(foundToken.IsRevoked, Is.False);
        
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<Password>()), Times.Exactly(1));
    }

    [Test]
    public async Task Login_SuccessfullyWithUsername_ReturnsTokens()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        const string existingEmail = "some-email@mail.com";
        const string existingUsername = "SomeUsername$";
        const string existingPasswordHash = "S0me-P@55w0rD-H@5H";
        const string accessToken = "=== SomeAccessToken ===";
        const string refreshToken = "=== SomeRefreshToken ===";
        const string refreshTokenHash = "=== S0m3R3fR35HT0k3nH@5h ===";

        var existingUser = new User(
            Email.Create(existingEmail).Value!,
            Username.Create(existingUsername).Value!,
            Password.FromHash(existingPasswordHash)
        );

        await _db.Set<User>().AddAsync(existingUser);
        await _db.SaveChangesAsync(cancellationToken);

        var loginCommand = new LoginCommand(existingUsername, existingPasswordHash, RandomNumberGenerator.GetHexString(32));

        _hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<Password>())).Returns(true);
        _tokenGenerator.Setup(x => x.GenerateAccess(It.IsAny<User>())).Returns(accessToken);
        _tokenGenerator.Setup(x => x.GenerateRefresh()).Returns((refreshToken, refreshTokenHash));

        // Act
        var result = await _handler.HandleAsync(loginCommand, cancellationToken);
        var foundToken = await _db.Set<RefreshToken>().SingleOrDefaultAsync(x => x.Hash == refreshTokenHash);

        // Assert
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.Value!.AccessToken, Is.EqualTo(accessToken));
        Assert.That(result.Value!.RefreshToken, Is.EqualTo(refreshToken));

        Assert.That(foundToken, Is.Not.Null);
        Assert.That(foundToken.IsRevoked, Is.False);
        
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<Password>()), Times.Exactly(1));
        _tokenGenerator.Verify(x => x.GenerateAccess(It.IsAny<User>()), Times.Once);
        _tokenGenerator.Verify(x => x.GenerateRefresh(), Times.Once);
    }

    [Test]
    public async Task Login_OnSameDevice_RevokePreviousToken()
    {
        // Arrange
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        const string existingEmail = "some-email@mail.com";
        const string existingUsername = "SomeUsername$";
        const string existingPasswordHash = "S0me-P@55w0rD-H@5H";
        const string deviceId = "1234-5678-9abc-def0";

        const string existingRefreshTokenHash = "*** === S0m3Pr3v10uSR3fR35HT0k3nH@5h === ***";

        const string accessToken = "=== SomeAccessToken ===";
        const string refreshToken = "=== SomeRefreshToken ===";
        const string refreshTokenHash = "=== S0m3R3fR35HT0k3nH@5h ===";

        var existingUser = new User(
            Email.Create(existingEmail).Value!,
            Username.Create(existingUsername).Value!,
            Password.FromHash(existingPasswordHash)
        );

        var existingRefreshToken = new RefreshToken(existingRefreshTokenHash, existingUser, deviceId);

        await _db.Set<User>().AddAsync(existingUser);
        await _db.Set<RefreshToken>().AddAsync(existingRefreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        var loginCommand = new LoginCommand(existingUsername, existingPasswordHash, deviceId);

        _hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<Password>())).Returns(true);
        _tokenGenerator.Setup(x => x.GenerateAccess(It.IsAny<User>())).Returns(accessToken);
        _tokenGenerator.Setup(x => x.GenerateRefresh()).Returns((refreshToken, refreshTokenHash));

        // Act
        var result = await _handler.HandleAsync(loginCommand, cancellationToken);
        var foundToken = await _db.Set<RefreshToken>().SingleOrDefaultAsync(x => x.Hash == refreshTokenHash);
        var previousToken = await _db.Set<RefreshToken>().SingleOrDefaultAsync(x => x.Hash == existingRefreshTokenHash);

        // Assert
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.Value!.AccessToken, Is.EqualTo(accessToken));
        Assert.That(result.Value!.RefreshToken, Is.EqualTo(refreshToken));

        Assert.That(foundToken, Is.Not.Null);

        Assert.That(previousToken, Is.Not.Null);
        Assert.That(previousToken.IsRevoked, Is.True);
        
        _hasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<Password>()), Times.Exactly(1));
        _tokenGenerator.Verify(x => x.GenerateAccess(It.IsAny<User>()), Times.Once);
        _tokenGenerator.Verify(x => x.GenerateRefresh(), Times.Once);
    }
}