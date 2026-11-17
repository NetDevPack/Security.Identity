using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Identity.Interfaces;
using NetDevPack.Identity.Jwt;
using NetDevPack.Security.Jwt.Core.Interfaces;
using Xunit;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace NetDevPack.Identity.Tests;

public class JwtBuilderTests : IAsyncLifetime
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
            o.RefreshTokenType = RefreshTokenType.OneTime;
        });
        services.AddNetDevPackIdentity().PersistKeysInMemory();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        _user = new IdentityUser { UserName = "netdevpack", Email = Email };
        (await users.CreateAsync(_user, "NetDevPack@2026!")).Succeeded.Should().BeTrue();
        (await roles.CreateAsync(new IdentityRole("Admin"))).Succeeded.Should().BeTrue();
        (await users.AddToRoleAsync(_user, "Admin")).Succeeded.Should().BeTrue();
        (await users.AddClaimAsync(_user, new Claim("permission", "orders:read"))).Succeeded.Should().BeTrue();
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    private async Task<TokenValidationResult> ValidateAccessToken(string token)
    {
        using var scope = _provider.CreateScope();
        var key = await scope.ServiceProvider.GetRequiredService<IJwtService>().GetCurrentSecurityKey();
        return await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = key,
        });
    }

    [Fact]
    public async Task BuildUserResponse_ShouldIssueAccessTokenSignedWithCurrentJwksKey()
    {
        using var scope = _provider.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IJwtBuilder>();

        var response = await builder.WithEmail(Email).WithJwtClaims().WithUserClaims().WithUserRoles().BuildUserResponse();

        var result = await ValidateAccessToken(response.AccessToken);
        result.IsValid.Should().BeTrue(result.Exception?.Message);
        ((JsonWebToken)result.SecurityToken).Typ.Should().Be("at+jwt");
        result.ClaimsIdentity.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == _user.Id);
        result.ClaimsIdentity.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == Email);
        result.ClaimsIdentity.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
        result.ClaimsIdentity.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "orders:read");

        ((string)response.UserToken.Id).Should().Be(_user.Id);
        response.UserToken.Email.Should().Be(Email);
        response.UserToken.Claims.Should().ContainSingle(c => c.Type == "permission" && c.Value == "orders:read");
        response.ExpiresIn.Should().Be(TimeSpan.FromHours(1).TotalSeconds);
    }

    [Fact]
    public async Task BuildToken_ShouldFindUserByIdUsernameOrEmail()
    {
        // IJwtBuilder is scoped and stateful: use one scope per token
        async Task<string> Build(Func<IJwtBuilder, IJwtBuilder> select)
        {
            using var scope = _provider.CreateScope();
            return await select(scope.ServiceProvider.GetRequiredService<IJwtBuilder>()).WithJwtClaims().BuildToken();
        }

        var byId = await Build(b => b.WithUserId(_user.Id));
        var byName = await Build(b => b.WithUsername("netdevpack"));
        var byEmail = await Build(b => b.WithEmail(Email));

        foreach (var token in new[] { byId, byName, byEmail })
        {
            var result = await ValidateAccessToken(token);
            result.IsValid.Should().BeTrue(result.Exception?.Message);
            result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)!.Value.Should().Be(_user.Id);
        }
    }

    [Fact]
    public async Task BuildToken_WithoutUserIdentifier_ShouldThrow()
    {
        using var scope = _provider.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IJwtBuilder>();

        await builder.Invoking(b => b.WithJwtClaims().BuildToken()).Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RefreshToken_OneTime_ShouldOnlyAcceptTheLatestToken()
    {
        string first, second;
        using (var scope = _provider.CreateScope())
            first = (await scope.ServiceProvider.GetRequiredService<IJwtBuilder>().WithEmail(Email).WithRefreshToken().BuildUserResponse()).RefreshToken;

        using (var scope = _provider.CreateScope())
        {
            var validation = await scope.ServiceProvider.GetRequiredService<IJwtBuilder>().ValidateRefreshToken(first);
            validation.IsValid.Should().BeTrue();
            validation.UserId.Should().Be(_user.Id);
        }

        using (var scope = _provider.CreateScope())
            second = (await scope.ServiceProvider.GetRequiredService<IJwtBuilder>().WithEmail(Email).WithRefreshToken().BuildUserResponse()).RefreshToken;

        using (var scope = _provider.CreateScope())
        {
            var builder = scope.ServiceProvider.GetRequiredService<IJwtBuilder>();
            var old = await builder.ValidateRefreshToken(first);
            old.IsValid.Should().BeFalse();
            old.Reason.Should().Be("Refresh Token already used");
            (await builder.ValidateRefreshToken(second)).IsValid.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ValidateRefreshToken_ShouldRejectGarbage()
    {
        using var scope = _provider.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IJwtBuilder>();

        (await builder.ValidateRefreshToken("not-a-jwt")).IsValid.Should().BeFalse();
    }
}
