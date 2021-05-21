using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;

namespace AspNetCore.Jwt.Sample.Config
{
    public static class CustomIdentityConfig
    {
        public static void AddCustomIdentityConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // Your own EF Identity configuration - Use when you have another database like postgres
            services.AddDbContext<MyIdentityContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("CustomConnection")));

            // Your own Identity configuration
            services.AddCustomIdentity<MyIdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedEmail = false;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                })
                .AddCustomRoles<MyIdentityRoles>()
                .AddCustomEntityFrameworkStores<MyIdentityContext>()
                .AddDefaultTokenProviders();

            // Ours JWT configuration
            services.AddJwtConfiguration(configuration, "AppSettings");
        }
    }

    public class MyIdentityUser : IdentityUser
    {

    }

    public class MyIdentityRoles : IdentityRole
    {

    }

    public class MyIdentityContext : IdentityDbContext<MyIdentityUser, MyIdentityRoles, string>
    {
        public MyIdentityContext(DbContextOptions<MyIdentityContext> options) : base(options) { }
    }
}