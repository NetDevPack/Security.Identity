using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NetDevPack.Identity.User
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }

            var claim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            if (claim is null)
                claim = principal.FindFirst(ClaimTypes.NameIdentifier);

            return claim?.Value;
        }

        public static string GetUserEmail(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }
            var claim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            if (claim is null)
                claim = principal.FindFirst(ClaimTypes.Email);

            return claim?.Value;
        }
        public static string GetUserId(this ClaimsIdentity principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }

            var claim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            if (claim is null)
                claim = principal.FindFirst(ClaimTypes.NameIdentifier);

            return claim?.Value;
        }

        public static string GetUserEmail(this ClaimsIdentity principal)
        {
            if (principal == null)
            {
                throw new ArgumentException(nameof(principal));
            }
            var claim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            if (claim is null)
                claim = principal.FindFirst(ClaimTypes.Email);

            return claim?.Value;
        }
    }
}