using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;

namespace NetDevPack.Identity.Jwt
{
    internal static class Extensions
    {
        public static void RemoveRefreshToken(this ICollection<Claim> claims)
        {
            var refreshToken = claims.FirstOrDefault(f => f.Type == "LastRefreshToken");
            if (refreshToken is not null)
                claims.Remove(refreshToken);
        }

        public static string GetJwtId(this ClaimsIdentity principal)
        {
            return principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        }
    }
}
