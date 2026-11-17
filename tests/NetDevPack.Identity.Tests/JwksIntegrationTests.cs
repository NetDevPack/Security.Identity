using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Identity.Interfaces;
using NetDevPack.Identity.Jwt;
using Xunit;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace NetDevPack.Identity.Tests;

/// <summary>
/// The point of NetDevPack.Identity is to issue tokens that other services validate through JWKS:
/// they only need the public keys published at /jwks, never a shared secret.
/// </summary>
public class JwksIntegrationTests : IAsyncLifetime
{
    private const string Issuer = "https://identity.netdevpack.test";
    private const string Audience = "netdevpack-api";
    private const string Email = "user@netdevpack.test";

    private WebApplication _app = null!;
    private HttpClient _http = null!;
    private IdentityUser _user = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var databaseName = Guid.NewGuid().ToString();
        builder.Services.AddIdentityEntityFrameworkContextConfiguration(o => o.UseInMemoryDatabase(databaseName));
        builder.Services.AddIdentityConfiguration();
        builder.Services.Configure<AppJwtSettings>(o =>
        {
            o.Issuer = Issuer;
            o.Audience = Audience;
        });
        builder.Services.AddNetDevPackIdentity().PersistKeysInMemory();

        _app = builder.Build();
        _app.UseJwksDiscovery();
        await _app.StartAsync();
        _http = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };

        using var scope = _app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        _user = new IdentityUser { UserName = "netdevpack", Email = Email };
        (await users.CreateAsync(_user, "NetDevPack@2026!")).Succeeded.Should().BeTrue();
    }

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _app.DisposeAsync();
    }

    private async Task<JsonWebKeySet> GetPublishedJwks() =>
        new(await _http.GetStringAsync("/jwks", TestContext.Current.CancellationToken));

    [Fact]
    public async Task JwksEndpoint_ShouldPublishOnlyPublicKeys()
    {
        // Issue a token first so the store has a key
        using (var scope = _app.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IJwtBuilder>().WithEmail(Email).WithJwtClaims().BuildToken();

        var jwks = await GetPublishedJwks();

        jwks.Keys.Should().NotBeEmpty();
        jwks.Keys.Should().OnlyContain(k => k.D == null && k.P == null && k.Q == null && k.DP == null && k.DQ == null && k.QI == null && k.K == null,
            "the JWKS document must never expose private or symmetric key material");
    }

    [Fact]
    public async Task AccessToken_ShouldBeValidatedByThirdPartyUsingOnlyThePublishedJwks()
    {
        string accessToken;
        using (var scope = _app.Services.CreateScope())
        {
            accessToken = (await scope.ServiceProvider.GetRequiredService<IJwtBuilder>()
                .WithEmail(Email).WithJwtClaims().WithRefreshToken().BuildUserResponse()).AccessToken;
        }

        // A resource server only knows the issuer, the audience and the JWKS URL
        var jwks = await GetPublishedJwks();
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKeys = jwks.GetSigningKeys(),
        });

        result.IsValid.Should().BeTrue(result.Exception?.Message);
        result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)!.Value.Should().Be(_user.Id);
        ((JsonWebToken)result.SecurityToken).Alg.Should().NotStartWith("HS", "tokens must be signed with the asymmetric JWKS key, not a shared secret");
    }

    [Fact]
    public async Task RefreshToken_ShouldBeSignedWithTheJwksKeyAndStillWork()
    {
        string refreshToken;
        using (var scope = _app.Services.CreateScope())
        {
            refreshToken = (await scope.ServiceProvider.GetRequiredService<IJwtBuilder>()
                .WithEmail(Email).WithRefreshToken().BuildUserResponse()).RefreshToken;
        }

        var jwks = await GetPublishedJwks();
        var signature = await new JsonWebTokenHandler().ValidateTokenAsync(refreshToken, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKeys = jwks.GetSigningKeys(),
        });
        signature.IsValid.Should().BeTrue(signature.Exception?.Message);

        using (var scope = _app.Services.CreateScope())
        {
            var validation = await scope.ServiceProvider.GetRequiredService<IJwtBuilder>().ValidateRefreshToken(refreshToken);
            validation.IsValid.Should().BeTrue(validation.Reason);
        }
    }
}
