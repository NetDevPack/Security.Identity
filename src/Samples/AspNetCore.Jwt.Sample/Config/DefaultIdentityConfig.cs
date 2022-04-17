using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;

namespace AspNetCore.Jwt.Sample.Config
{
    public static class DefaultIdentityConfig
    {
        public static void AddDefaultIdentityConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // Default EF Context for Identity (inside of the NetDevPack.Identity)
            services.AddIdentityEntityFrameworkContextConfiguration(options => options.UseInMemoryDatabase("NetDevPack.Identity"));
            
            // Default Identity configuration
            services.AddIdentityConfiguration();  // <== This extension returns IdentityBuilder to extends configuration

            // Default JWT configuration
            services.AddJwtConfiguration(configuration);
        }
    }
}