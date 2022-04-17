using System;
using System.Collections.Generic;

namespace NetDevPack.Identity.Jwt
{
    public class AppJwtSettings
    {
        [Obsolete("For better security use IJwtBuilder and set null for this field")]
        public string SecretKey { get; set; }

        /// <summary>
        /// Hours
        /// </summary>
        public int Expiration { get; set; } = 1;
        /// <summary>
        /// Days
        /// </summary>
        public int RefreshTokenExpiration { get; set; } = 30;
        public string Issuer { get; set; } = "NetDevPack.Identity";
        public string Audience { get; set; } = "Api";

        /// <summary>
        /// One Time => Only the lastest refresh token is valid. Ignore olders refresh token.
        ///    Better security and best suit for application with only one Frontend
        /// 
        /// ReUse => Accept olders refresh tokens
        ///    Decrease security but better for scenarios where there are more than one frontend. Like a browser + mobile app
        /// </summary>
        public RefreshTokenType RefreshTokenType { get; set; } = RefreshTokenType.OneTime;


    }
}