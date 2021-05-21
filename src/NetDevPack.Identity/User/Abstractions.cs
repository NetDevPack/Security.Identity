using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace NetDevPack.Identity.User
{
    public static class Abstractions
    {
        public static IServiceCollection AddAspNetUserConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAspNetUser, AspNetUser>();

            return services;
        }
    }
}