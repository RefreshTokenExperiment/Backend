using System.Security.Cryptography;
using Domain.Users;
using Infrastructure.Services.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Infrastructure.Tests.Services;

[TestFixture]
public sealed class JwtGeneratorTests
{
    private JwtGenerator _jwtGenerator = null!;
    private Mock<IJwtConfig> _configMock = null!;

    [SetUp]
    public void SetUp()
    {
        _configMock = new();
        _configMock.SetupGet(x => x.Audience).Returns("===Audience===");
        _configMock.SetupGet(x => x.Issuer).Returns("===Issuer===");
        _configMock.SetupGet(x => x.Expiration).Returns(TimeSpan.FromMinutes(10));
        _configMock.SetupGet(x => x.Secret).Returns("===*** Some Really Interesting and Long Secret Code ***===");
        _configMock.SetupGet(x => x.SecurityAlgorithm).Returns(SecurityAlgorithms.HmacSha384);
        _configMock.SetupGet(x => x.SymmetricSecurityKey).CallBase();

        _jwtGenerator = new(_configMock.Object, new RefreshTokenHasher());
    }

    [Test]
    public void GenerateAccess_ValidateClaims_ReturnsSuccess()
    {
        // Arrange
        var email = Email.Create("some-valid.email@mail.com");
        var username = Username.Create("SomeUsername$");
        var password = Password.FromHash(RandomNumberGenerator.GetHexString(32));
        var user = new User(email, username, password);

        var validationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,

            ValidAudience = _configMock.Object.Audience,
            ValidIssuer = _configMock.Object.Issuer,
            ClockSkew = TimeSpan.Zero,

            IssuerSigningKey = _configMock.Object.SymmetricSecurityKey
        };



        // Act
        var token = _jwtGenerator.GenerateAccess(user);
        var currentTime = DateTime.UtcNow;
        var parsedToken = new JsonWebTokenHandler().ValidateTokenAsync(token, validationParameters).GetAwaiter().GetResult();

        var sub = parsedToken.Claims.FirstOrDefault(x => x.Key == JwtRegisteredClaimNames.Sub).Value;
        var nickname = parsedToken.Claims.FirstOrDefault(x => x.Key == JwtRegisteredClaimNames.Nickname).Value;
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(parsedToken.Claims.FirstOrDefault(x => x.Key == JwtRegisteredClaimNames.Exp).Value.ToString()!));

        // Assert
        Assert.That(parsedToken.IsValid, Is.True);
        Assert.That(sub, Is.EqualTo(user.Id.Value.ToString()));
        Assert.That(nickname, Is.EqualTo(user.Username.Value));
        Assert.That(expiresAt.UtcDateTime, Is.EqualTo(currentTime.Add(_configMock.Object.Expiration)).Within(TimeSpan.FromSeconds(2)));
    }
}