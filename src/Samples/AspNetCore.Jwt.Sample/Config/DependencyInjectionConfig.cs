using Microsoft.Extensions.DependencyInjection;

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