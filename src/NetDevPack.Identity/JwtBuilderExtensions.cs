using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetDevPack.Identity.Data;
using NetDevPack.Identity.Interfaces;
using NetDevPack.Identity.Jwt;
using NetDevPack.Security.Jwt.Core;
using NetDevPack.Security.Jwt.Core.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class JwtBuilderExtensions
{
    public static IJwksBuilder UseNetDevPackIdentity<TIdentityUser, TKey>(this IServiceCollection services) where TIdentityUser : IdentityUser<TKey> where TKey : IEquatable<TKey>
    {
        services.TryAddScoped<IJwtBuilder, JwtBuilderInject<TIdentityUser, TKey>>();
        return services.AddJwksManager();
    }
    public static IJwksBuilder UseNetDevPackIdentity<TIdentityUser>(this IServiceCollection services) where TIdentityUser : IdentityUser
    {
        services.TryAddScoped<IJwtBuilder, JwtBuilderInject<TIdentityUser, string>>();
        return services.AddJwksManager();
    }
    public static IJwksBuilder UseNetDevPackIdentity(this IServiceCollection services)
    {
        services.TryAddScoped<IJwtBuilder, JwtBuilderInject<IdentityUser, string>>();
        return services.AddJwksManager();
    }

    public static IdentityBuilder AddIdentityConfiguration(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentException(nameof(services));
        services.UseNetDevPackIdentity();

        return services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<NetDevPackAppDbContext>()
            .AddDefaultTokenProviders();
    }

    public static IdentityBuilder AddDefaultIdentity(this IServiceCollection services, Action<IdentityOptions> options = null)
    {
        if (services == null) throw new ArgumentException(nameof(services));
        services.UseNetDevPackIdentity<IdentityUser>();
        return services.AddIdentityCore<IdentityUser>(options)
            .AddDefaultTokenProviders();
    }

    public static IdentityBuilder AddCustomIdentity<TIdentityUser>(this IServiceCollection services, Action<IdentityOptions> options = null)
        where TIdentityUser : IdentityUser
    {
        if (services == null) throw new ArgumentException(nameof(services));

        return services.AddIdentityCore<TIdentityUser>(options)
            .AddDefaultTokenProviders();
    }

    public static IdentityBuilder AddCustomIdentity<TIdentityUser, TKey>(this IServiceCollection services, Action<IdentityOptions> options = null)
        where TIdentityUser : IdentityUser<TKey>
        where TKey : IEquatable<TKey>
    {
        if (services == null) throw new ArgumentException(nameof(services));
        services.UseNetDevPackIdentity<TIdentityUser, TKey>();
        return services.AddIdentityCore<TIdentityUser>(options)
            .AddDefaultTokenProviders();
    }

    public static IdentityBuilder AddDefaultRoles(this IdentityBuilder builder)
    {
        return builder.AddRoles<IdentityRole>();
    }

    public static IdentityBuilder AddCustomRoles<TRole>(this IdentityBuilder builder)
        where TRole : IdentityRole
    {
        return builder.AddRoles<TRole>();
    }

    public static IdentityBuilder AddCustomRoles<TRole, TKey>(this IdentityBuilder builder)
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
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
