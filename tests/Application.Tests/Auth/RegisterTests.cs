using Domain.Users;
using Domain.RefreshTokens;
using Application.Auth;
using Application.Auth.Commands.Register;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Application.Abstractions;
using Application.Common;

namespace Application.Tests.Auth;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public sealed class RegisterTests
{
    private AppDbContext _db = null!;
    private RegisterCommandHandler _handler = null!;
    private Mock<IPasswordHasher> _hasherMock = null!;
    private Mock<IAuthorizationTokenGenerator> _tokenGeneratorMock = null!;

    private IDbContextTransaction _transaction = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = new(TestContainerFixture.Options);

        _transaction = await _db.Database.BeginTransactionAsync();

        _hasherMock = new();
        _tokenGeneratorMock = new();
        _handler = new(_db, new PasswordFactory(_hasherMock.Object), _tokenGeneratorMock.Object);
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
    public async Task Register_WithInvalidCredentials_ReturnsFailure(
        [Values("some-invalid@", "invalid@mail", "some@email@mail.com")] string email,
        [Values("_invalid", "invalid@", "$toolongusername$", "1USERNAME$")] string username,
        [Values("Som", "lowercase", "UPPERCASE", "1234")] string password)
    {
        // Arrange
        var command = new RegisterCommand(email, username, password, RandomNumberGenerator.GetHexString(32));
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        
        _hasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        _tokenGeneratorMock.Verify(x => x.GenerateAccess(It.IsAny<User>()), Times.Never);
        _tokenGeneratorMock.Verify(x => x.GenerateRefresh(), Times.Never);
    }

    [Test]
    public async Task Register_WithEmptyDeviceId_ReturnsFailure()
    {
        // Arrange
        var commandNull = new RegisterCommand("some-valid@mail.com", "SomeUser", "P@ssw0rD", null!);
        var commandEmpty = new RegisterCommand("some-valid@mail.com", "SomeUser", "P@ssw0rD", string.Empty);
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        // Act
        var resultNull = await _handler.HandleAsync(commandNull, cancellationToken);
        var resultEmpty = await _handler.HandleAsync(commandEmpty, cancellationToken);

        // Assert
        Assert.That(resultNull.IsFailure, Is.True);
        Assert.That(resultNull.Error, Is.TypeOf<AuthErrors.DeviceIdIsNull>());
        Assert.That(resultEmpty.IsFailure, Is.True);
        Assert.That(resultEmpty.Error, Is.TypeOf<AuthErrors.DeviceIdIsNull>());
        
        _hasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        _tokenGeneratorMock.Verify(x => x.GenerateAccess(It.IsAny<User>()), Times.Never);
        _tokenGeneratorMock.Verify(x => x.GenerateRefresh(), Times.Never);
    }

    [Test]
    public async Task Register_WithExistingUser_ReturnsAlreadyExistsError()
    {
        // Arrange
        const string emailOfExists = "i-am-already-registered@mail.com";
        const string newEmail = "i-am-new-email@mail.com";
        const string usernameOfExists = "IAmAlreadyIn";
        const string newUsername = "IAmNewbie";

        const string password = "ValidPassword1234";
        const string deviceId = "1234-5678-9abc-def0";

        var existingUser = new User(
            Email.Create(emailOfExists).Value!,
            Username.Create(usernameOfExists).Value!,
            Password.FromHash(password));
        await _db.Set<User>().AddAsync(existingUser);
        await _db.SaveChangesAsync();

        var commandWithEmail = new RegisterCommand(emailOfExists, newUsername, password, deviceId);
        var commandWithUsername = new RegisterCommand(newEmail, usernameOfExists, password, deviceId);

        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        // Act
        var resultWithEmail = await _handler.HandleAsync(commandWithEmail, cancellationToken);
        var resultWithUsername = await _handler.HandleAsync(commandWithUsername, cancellationToken);

        // Assert
        Assert.That(resultWithEmail.IsFailure, Is.True);
        Assert.That(resultWithEmail.Error, Is.TypeOf<AuthErrors.UserAlreadyExists>());
        Assert.That(resultWithUsername.IsFailure, Is.True);
        Assert.That(resultWithUsername.Error, Is.TypeOf<AuthErrors.UserAlreadyExists>());

        Assert.That(resultWithEmail.Error, Is.TypeOf<AuthErrors.UserAlreadyExists>());
        Assert.That(resultWithUsername.Error, Is.TypeOf<AuthErrors.UserAlreadyExists>());
        
        _hasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Exactly(2));
        _tokenGeneratorMock.Verify(x => x.GenerateAccess(It.IsAny<User>()), Times.Never);
        _tokenGeneratorMock.Verify(x => x.GenerateRefresh(), Times.Never);
    }

    [Test]
    public async Task Register_ValidUser_CreatesUserAndTokens()
    {
        // Arrange
        const string email = "some-valid@mail.com";
        const string username = "hikashi$";
        const string password = "S0m3-S3cuR3-p@55w0Rd";
        const string passwordHash = "JNGFIOJIOIhnguhdfuogh";
        const string deviceId = "1234-5678-9abc-def0";

        const string accessToken = "SomeAccessToken";
        const string refreshToken = "SomeRefreshToken";
        const string refreshTokenHash = "S0m3R3fr3sHT0k3enH@5H";

        _hasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns(passwordHash);
        _tokenGeneratorMock.Setup(x => x.GenerateAccess(It.IsAny<User>())).Returns(accessToken);
        _tokenGeneratorMock.Setup(x => x.GenerateRefresh()).Returns((refreshToken, refreshTokenHash));

        var command = new RegisterCommand(email, username, password, deviceId);
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        var foundUser = await _db.Set<User>().SingleOrDefaultAsync(x => x.Email == email);
        var foundToken = await _db.Set<RefreshToken>().SingleOrDefaultAsync(x => x.User.Email == email);

        // Assert
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.Value!.AccessToken, Is.Not.Empty);
        Assert.That(result.Value!.AccessToken, Is.EqualTo(accessToken));
        Assert.That(result.Value!.RefreshToken, Is.Not.Empty);
        Assert.That(result.Value!.RefreshToken, Is.EqualTo(refreshToken));

        Assert.That(foundUser, Is.Not.Null);
        Assert.That(foundUser.Email.Value, Is.EqualTo(email));
        Assert.That(foundUser.Username.Value, Is.EqualTo(username));
        Assert.That(foundUser.Password.Value, Is.EqualTo(passwordHash));

        Assert.That(foundToken, Is.Not.Null);
        Assert.That(foundToken.IsRevoked, Is.False);
        Assert.That(foundToken.DeviceId, Is.EqualTo(deviceId));
        Assert.That(foundToken.Hash, Is.EqualTo(refreshTokenHash));

        _hasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Once);
        _tokenGeneratorMock.Verify(x => x.GenerateAccess(It.IsAny<User>()), Times.Once);
        _tokenGeneratorMock.Verify(x => x.GenerateRefresh(), Times.Once);
    }

    [Test]
    public async Task Register_WithSameDeviceId_RevokesOldRefreshToken()
    {
        // Arrange
        const string existingEmail = "existing-email@mail.com";
        const string existingUsername = "ExistingUser";
        const string existingPasswordHash = "S0me-V@l1d-P@55w0rD-H@5H";
        const string existingTokenHash = "S0me-R3fr35H-T0k3n-H@5H";

        const string email = "some-valid@mail.com";
        const string username = "hikashi$";
        const string password = "S0m3-S3cuR3-p@55w0Rd";
        const string passwordHash = "JNGFIOJIOIhnguhdfuogh";
        const string deviceId = "1234-5678-9abc-def0";

        const string accessToken = "SomeAccessToken";
        const string refreshToken = "SomeRefreshToken";
        const string refreshTokenHash = "S0m3R3fr3sHT0k3enH@5H";

        _hasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns(passwordHash);
        _tokenGeneratorMock.Setup(x => x.GenerateAccess(It.IsAny<User>())).Returns(accessToken);
        _tokenGeneratorMock.Setup(x => x.GenerateRefresh()).Returns((refreshToken, refreshTokenHash));

        var command = new RegisterCommand(email, username, password, deviceId);
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        var existingUser = new User(
            Email.Create(existingEmail).Value!,
            Username.Create(existingUsername).Value!,
            Password.FromHash(existingPasswordHash)
        );

        var existingRefreshToken = new RefreshToken(
            hash: existingTokenHash,
            user: existingUser,
            deviceId: deviceId
        );
        await _db.Set<User>().AddAsync(existingUser, cancellationToken);
        await _db.Set<RefreshToken>().AddAsync(existingRefreshToken, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        var foundUser = await _db.Set<User>().SingleOrDefaultAsync(x => x.Email == email);
        var foundToken = await _db.Set<RefreshToken>().SingleOrDefaultAsync(x => x.Hash == refreshTokenHash);
        var existingToken = await _db.Set<RefreshToken>().SingleOrDefaultAsync(x => x.Hash == existingTokenHash);

        // Assert
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.Value!.AccessToken, Is.EqualTo(accessToken));
        Assert.That(result.Value!.RefreshToken, Is.EqualTo(refreshToken));

        Assert.That(foundUser, Is.Not.Null);
        Assert.That(foundUser.Email.Value, Is.EqualTo(email));
        Assert.That(foundUser.Username.Value, Is.EqualTo(username));
        Assert.That(foundUser.Password.Value, Is.EqualTo(passwordHash));

        Assert.That(foundToken, Is.Not.Null);
        Assert.That(foundToken.IsRevoked, Is.False);
        Assert.That(foundToken.DeviceId, Is.EqualTo(deviceId));
        Assert.That(foundToken.Hash, Is.EqualTo(refreshTokenHash));

        Assert.That(existingToken, Is.Not.Null);
        Assert.That(existingToken.DeviceId, Is.EqualTo(deviceId));
        Assert.That(existingToken.IsRevoked, Is.True);

        _hasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Once);
        _tokenGeneratorMock.Verify(x => x.GenerateAccess(It.IsAny<User>()), Times.Once);
        _tokenGeneratorMock.Verify(x => x.GenerateRefresh(), Times.Once);
    }
}