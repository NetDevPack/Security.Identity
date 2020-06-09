using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NetDevPack.Identity.User;

namespace AspNetCore.Jwt.Sample.Config
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection AddDependencyInjectionConfiguration(this IServiceCollection services)
        {
            // Resolve another dependencies here!

            return services;
        }
    }
}