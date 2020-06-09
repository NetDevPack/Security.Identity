using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetDevPack.Identity.Data;

namespace NetDevPack.Identity
{
    public static class Abstractions
    {
        public static IdentityBuilder AddIdentityConfiguration(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentException(nameof(services));

            return services.AddDefaultIdentity<IdentityUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<NetDevPackAppDbContext>()
                .AddDefaultTokenProviders();
        }

        public static IdentityBuilder AddDefaultIdentity(this IServiceCollection services, Action<IdentityOptions> options = null)
        {
            if (services == null) throw new ArgumentException(nameof(services));

            return services.AddDefaultIdentity<IdentityUser>(options);
        }

        public static IdentityBuilder AddCustomIdentity<TIdentityUser>(this IServiceCollection services, Action<IdentityOptions> options = null) where TIdentityUser : IdentityUser
        {
            if (services == null) throw new ArgumentException(nameof(services));

            return services.AddDefaultIdentity<TIdentityUser>(options);
        }

        public static IdentityBuilder AddDefaultRoles(this IdentityBuilder builder)
        {
            return builder.AddRoles<IdentityRole>();
        }

        public static IdentityBuilder AddCustomRoles<TRole>(this IdentityBuilder builder) where TRole : IdentityRole
        {
            return builder.AddRoles<TRole>();
        }

        public static IdentityBuilder AddDefaultEntityFrameworkStores(this IdentityBuilder builder)
        {
            return builder.AddEntityFrameworkStores<NetDevPackAppDbContext>();
        }

        public static IdentityBuilder AddCustomEntityFrameworkStores<TContext>(this IdentityBuilder builder) where TContext : DbContext
        {
            return builder.AddEntityFrameworkStores<TContext>();
        }

        public static IServiceCollection AddIdentityEntityFrameworkContextConfiguration(
            this IServiceCollection services, Action<DbContextOptionsBuilder> options)
        {
            if (services == null) throw new ArgumentException(nameof(services));
            if (options == null) throw new ArgumentException(nameof(options));
            return services.AddDbContext<NetDevPackAppDbContext>(options);
        }

        public static IApplicationBuilder UseAuthConfiguration(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentException(nameof(app));

            return app.UseAuthentication()
                      .UseAuthorization();
        }
    }
}