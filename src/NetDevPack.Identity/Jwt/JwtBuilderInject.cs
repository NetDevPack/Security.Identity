using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Identity.Interfaces;
using NetDevPack.Identity.Jwt.Model;
using NetDevPack.Identity.User;
using NetDevPack.Security.Jwt.Core.Interfaces;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;


namespace NetDevPack.Identity.Jwt;

internal class JwtBuilderInject<TIdentityUser, TKey> : IJwtBuilder
    where TIdentityUser : IdentityUser<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly UserManager<TIdentityUser> _userManager;
    private readonly IOptions<AppJwtSettings> _settings;
    private readonly IJwtService _jwtService;

    private ICollection<Claim> _userClaims;
    private ICollection<Claim> _jwtClaims;
    private ClaimsIdentity _identityClaims;
    private bool _useEmail = false;
    private bool _useId = false;
    private bool _useUsername = false;

    private string _key;
    private string _email;
    private string _username;

    private bool _useClaims;
    private bool _useRoles;
    private bool _useRt;
    private bool _useDefaultJwtClaims;
    private TIdentityUser _user;

    public JwtBuilderInject(UserManager<TIdentityUser> userManager, IOptions<AppJwtSettings> settings, IJwtService jwtService)
    {
        _userManager = userManager;
        _settings = settings;
        _jwtService = jwtService;
        _userClaims = new List<Claim>();
        _jwtClaims = new List<Claim>();
        _identityClaims = new ClaimsIdentity();
    }

    public IJwtBuilder WithEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) throw new ArgumentException(nameof(email));

        _useUsername = false;
        _useId = false;

        _useEmail = true;
        _email = email;

        return this;
    }

    public IJwtBuilder WithUserId(string id)
    {
        _useEmail = false;
        _useUsername = false;
        _useId = true;
        _key = id ?? throw new ArgumentException(nameof(id));

        return this;
    }
    public IJwtBuilder WithUsername(string username)
    {
        if (string.IsNullOrEmpty(username)) throw new ArgumentException(nameof(username));

        _useId = false;
        _useEmail = false;

        _useUsername = true;
        _username = username;

        return this;
    }
    public IJwtBuilder WithJwtClaims()
    {
        _useDefaultJwtClaims = true;
        return this;
    }

    public IJwtBuilder WithUserClaims()
    {
        _useClaims = true;

        return this;
    }

    public IJwtBuilder WithUserRoles()
    {
        _useRoles = true;
        return this;
    }

    public IJwtBuilder WithRefreshToken()
    {
        _useRt = true;
        return this;
    }

    public async Task<string> BuildRefreshToken()
    {
        var jti = Guid.NewGuid().ToString();
        var key = await GetCurrentKey();
        var user = await GetUser();
        var claims = new List<Claim>(2)
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti)
        };

        var identityClaims = new ClaimsIdentity();
        identityClaims.AddClaims(claims);

        var handler = new JwtSecurityTokenHandler();

        var securityToken = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _settings.Value.Issuer,
            Audience = _settings.Value.Audience,
            SigningCredentials = key,
            Subject = identityClaims,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(_settings.Value.RefreshTokenExpiration),
            TokenType = "rt+jwt"
        });
        await UpdateLastGeneratedClaim(jti, user);
        var encodedJwt = handler.WriteToken(securityToken);
        return encodedJwt;
    }

    private async Task UpdateLastGeneratedClaim(string jti, TIdentityUser user)
    {

        var claims = await _userManager.GetClaimsAsync(user);
        var newLastRtClaim = new Claim("LastRefreshToken", jti);

        var claimLastRt = claims.FirstOrDefault(f => f.Type == "LastRefreshToken");
        if (claimLastRt != null)
            await _userManager.ReplaceClaimAsync(user, claimLastRt, newLastRtClaim);
        else
            await _userManager.AddClaimAsync(user, newLastRtClaim);
    }

    public async Task<string> BuildToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = await GetCurrentKey();
        var user = await GetUser();

        if (_useDefaultJwtClaims)
        {
            _jwtClaims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()));
            _jwtClaims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            _jwtClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            _jwtClaims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));

            _identityClaims.AddClaims(_jwtClaims);
        }

        if (_useRoles)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            userRoles.ToList().ForEach(r => _identityClaims.AddClaim(new Claim("role", r)));
        }

        if (_useClaims)
        {
            _userClaims = await _userManager.GetClaimsAsync(user);
            _userClaims.RemoveRefreshToken();
            _identityClaims.AddClaims(_userClaims);
        }


        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _settings.Value.Issuer,
            Audience = _settings.Value.Audience,
            Subject = _identityClaims,
            Expires = DateTime.UtcNow.AddHours(_settings.Value.Expiration),
            NotBefore = DateTime.UtcNow,
            SigningCredentials = key,
            TokenType = "at+jwt"
        });

        return tokenHandler.WriteToken(token);
    }


    private async Task<TIdentityUser> GetUser()
    {
        if (!_useEmail && !_useId && !_useUsername)
            throw new ArgumentNullException("User id or email should be provided. You should call WithEmail(email), WithUsername(username) or WithUserId(id).");

        if (_user is not null)
            return _user;

        if (_useId)
            _user = await _userManager.FindByIdAsync(_key);
        else if (_useEmail)
            _user = await _userManager.FindByEmailAsync(_email);
        else
            _user = await _userManager.FindByNameAsync(_username);

        return _user;
    }

    private Task<SigningCredentials> GetCurrentKey()
    {
        if (string.IsNullOrEmpty(_settings.Value.SecretKey))
            return _jwtService.GetCurrentSigningCredentials();

        return Task.FromResult(new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_settings.Value.SecretKey)), SecurityAlgorithms.HmacSha256Signature));
    }

    public async Task<UserResponse> BuildUserResponse()
    {
        var user = new UserResponse
        {
            AccessToken = await BuildToken(),
            ExpiresIn = TimeSpan.FromHours(_settings.Value.Expiration).TotalSeconds,
            UserToken = new UserToken
            {
                Id = _user.Id,
                Email = _user.Email,
                Claims = _userClaims.Any() ? _userClaims.Select(c => new UserClaim { Type = c.Type, Value = c.Value }) : null
            }
        };
        if (_useRt)
            user.RefreshToken = await BuildRefreshToken();

        return user;
    }

    public async Task<RefreshTokenValidation> ValidateRefreshToken(string refreshToken)
    {
        var handler = new JsonWebTokenHandler();

        var result = handler.ValidateToken(refreshToken, new TokenValidationParameters()
        {
            RequireSignedTokens = false,
            ValidIssuer = _settings.Value.Issuer,
            ValidAudience = _settings.Value.Audience,
            IssuerSigningKey = await _jwtService.GetCurrentSecurityKey(),
        });

        if (!result.IsValid)
            return new RefreshTokenValidation(false);


        var user = await _userManager.FindByIdAsync(result.ClaimsIdentity.GetUserId());
        var claims = await _userManager.GetClaimsAsync(user);

        var jti = result.ClaimsIdentity.GetJwtId();

        if (_settings.Value.RefreshTokenType == RefreshTokenType.OneTime)
            if (!claims.Any(c => c.Type == "LastRefreshToken" && c.Value == jti))
                return new RefreshTokenValidation(false, reason: "Refresh Token already used");

        if (user.LockoutEnabled)
            if (user.LockoutEnd < DateTime.Now)
                return new RefreshTokenValidation(false, reason: "User blocked");

        return new RefreshTokenValidation(true, result.ClaimsIdentity.GetUserId());
    }

    private static long ToUnixEpochDate(DateTime date)
        => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
            .TotalSeconds);
}