using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetDevPack.Identity.Data;
using NetDevPack.Identity.Data.Enums;
using System;

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

        [Obsolete("use AddNetDevPackContextIdentity")]
        public static IServiceCollection AddIdentityEntityFrameworkContextConfiguration(
            this IServiceCollection services, IConfiguration configuration, string migrationAssembly, string identityConnectionName = null)
        {
            if (services == null) throw new ArgumentException(nameof(services));
            if (configuration == null) throw new ArgumentException(nameof(configuration));
            if (string.IsNullOrEmpty(migrationAssembly)) throw new ArgumentException(nameof(migrationAssembly));

            return services.AddDbContext<NetDevPackAppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString(identityConnectionName ?? "IdentityConnection"),
                    b => b.MigrationsAssembly(migrationAssembly)));
        }

        /// <summary>
        /// Extension method that defines the database that will be used for the context
        /// </summary>
        /// <param name="services"></param>
        /// <param name="providerType">Defines the database connection provider</param>
        /// <param name="configuration">Package configuration</param>
        /// <param name="migrationAssembly">name of the assembly to which it contains information for creating the migration</param>
        /// <param name="identityConnectionName">connection string name in appsettings</param>
        /// <remarks>By Eduprog</remarks>
        /// <returns>Service Collections</returns>

        public static IServiceCollection AddNetDevPackContextIdentity(this IServiceCollection services, 
            DatabaseProviderType providerType, IConfiguration configuration, string migrationAssembly, string identityConnectionName = null)
        {
            switch (providerType)
            {
                case DatabaseProviderType.SqlServer:
                {
                        if (services == null) throw new ArgumentException(nameof(services));
                        if (configuration == null) throw new ArgumentException(nameof(configuration));
                        if (string.IsNullOrEmpty(migrationAssembly)) throw new ArgumentException(nameof(migrationAssembly));
                        return services.AddDbContext<NetDevPackAppDbContext>(options =>
                            options.UseSqlServer(configuration.GetConnectionString(identityConnectionName ?? "IdentityConnection"),
                                b => b.MigrationsAssembly(migrationAssembly)));
                    }
                case DatabaseProviderType.PostgreSql:
                    {
                        if (services == null) throw new ArgumentException(nameof(services));
                        if (configuration == null) throw new ArgumentException(nameof(configuration));
                        if (string.IsNullOrEmpty(migrationAssembly)) throw new ArgumentException(nameof(migrationAssembly));

                        return services.AddDbContext<NetDevPackAppDbContext>(options =>
                            options.UseNpgsql(configuration.GetConnectionString(identityConnectionName ?? "IdentityConnection"),
                                b => b.MigrationsAssembly(migrationAssembly)));
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(providerType), $@"The value needs to be one of {string.Join(", ", Enum.GetNames(typeof(DatabaseProviderType)))}.");
            }
        }

        public static IApplicationBuilder UseAuthConfiguration(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentException(nameof(app));

            return app.UseAuthentication()
                      .UseAuthorization();
        }
    }
}