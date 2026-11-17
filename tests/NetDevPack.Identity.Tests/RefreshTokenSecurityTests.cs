using System.Security.Claims;
using System.Security.Cryptography;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Identity.Interfaces;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Jwt.Model;
using Xunit;

namespace NetDevPack.Identity.Tests;

/// <summary>
/// Regression tests for refresh token validation: only signed rt+jwt tokens issued by this
/// application, for an existing and not locked out user, may be accepted.
/// </summary>
public class RefreshTokenSecurityTests : IAsyncLifetime
{
    private const string Issuer = "https://identity.netdevpack.test";
    private const string Audience = "netdevpack-api";
    private const string Email = "user@netdevpack.test";

    private ServiceProvider _provider = null!;
    private IdentityUser _user = null!;

    public async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var databaseName = Guid.NewGuid().ToString();
        services.AddIdentityEntityFrameworkContextConfiguration(o => o.UseInMemoryDatabase(databaseName));
        services.AddIdentityConfiguration();
        services.Configure<AppJwtSettings>(o =>
        {
            o.Issuer = Issuer;
            o.Audience = Audience;
            // ReUse skips the "last refresh token" check, so the token itself is the only protection
            o.RefreshTokenType = RefreshTokenType.ReUse;
        });
        services.AddNetDevPackIdentity().PersistKeysInMemory();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        _user = new IdentityUser { UserName = "netdevpack", Email = Email };
        (await users.CreateAsync(_user, "NetDevPack@2026!")).Succeeded.Should().BeTrue();
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    private async Task<RefreshTokenValidation> Validate(string token)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IJwtBuilder>().ValidateRefreshToken(token);
    }

    private async Task<string> IssueRefreshToken()
    {
        using var scope = _provider.CreateScope();
        var response = await scope.ServiceProvider.GetRequiredService<IJwtBuilder>()
            .WithEmail(Email).WithRefreshToken().BuildUserResponse();
        return response.RefreshToken;
    }

    private string ForgeToken(SigningCredentials? credentials, string type = "rt+jwt") =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, _user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            }),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = credentials,
            TokenType = type,
        });

    [Fact]
    public async Task ShouldAcceptRefreshTokenIssuedByTheApplication()
    {
        var result = await Validate(await IssueRefreshToken());

        result.IsValid.Should().BeTrue(result.Reason);
        result.UserId.Should().Be(_user.Id);
    }

    [Fact]
    public async Task ShouldRejectUnsignedRefreshToken()
    {
        var forged = ForgeToken(credentials: null);
        forged.Split('.')[2].Should().BeEmpty("the token must be unsigned (alg: none) for this test");

        (await Validate(forged)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldRejectRefreshTokenSignedWithUnknownKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var forged = ForgeToken(new SigningCredentials(new ECDsaSecurityKey(ecdsa) { KeyId = "attacker" }, SecurityAlgorithms.EcdsaSha256));

        (await Validate(forged)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldRejectAccessTokenUsedAsRefreshToken()
    {
        using var scope = _provider.CreateScope();
        var accessToken = await scope.ServiceProvider.GetRequiredService<IJwtBuilder>()
            .WithEmail(Email).WithJwtClaims().BuildToken();

        (await Validate(accessToken)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    public async Task ShouldRejectMalformedRefreshToken(string token)
    {
        (await Validate(token)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldRejectRefreshTokenOfLockedOutUser()
    {
        var token = await IssueRefreshToken();
        using (var scope = _provider.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await users.FindByIdAsync(_user.Id);
            (await users.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddHours(1))).Succeeded.Should().BeTrue();
        }

        var result = await Validate(token);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Be("User blocked");
    }

    [Fact]
    public async Task ShouldAcceptRefreshTokenWhenLockoutHasExpired()
    {
        var token = await IssueRefreshToken();
        using (var scope = _provider.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await users.FindByIdAsync(_user.Id);
            (await users.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddHours(-1))).Succeeded.Should().BeTrue();
        }

        (await Validate(token)).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldRejectRefreshTokenOfDeletedUser()
    {
        var token = await IssueRefreshToken();
        using (var scope = _provider.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            (await users.DeleteAsync((await users.FindByIdAsync(_user.Id))!)).Succeeded.Should().BeTrue();
        }

        var result = await Validate(token);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Be("User not found");
    }
}
