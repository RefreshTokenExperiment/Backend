using System.Security.Cryptography;
using Domain.Users;
using Infrastructure.Services.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Tests.Services;

[TestFixture]
public sealed class JwtGeneratorTests
{
    private JwtGenerator _jwtGenerator = null!;
    private JwtConfig _config = null!;

    [SetUp]
    public void SetUp()
    {
        _config = new JwtConfig
        {
            Audience = "audience",
            Issuer = "issuer",
            Expiration = TimeSpan.Parse("00:10:00"),
            Secret = RandomNumberGenerator.GetHexString(384 / 8), // HMAC384 Min Length
            SecurityAlgorithm = SecurityAlgorithms.HmacSha384
        };

        _jwtGenerator = new(_config, new RefreshTokenHasher());
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

            ValidAudience = _config.Audience,
            ValidIssuer = _config.Issuer,
            ClockSkew = TimeSpan.Zero,

            IssuerSigningKey = _config.SymmetricSecurityKey
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
        Assert.That(expiresAt.UtcDateTime, Is.EqualTo(currentTime.Add(_config.Expiration)).Within(TimeSpan.FromSeconds(2)));
    }
}