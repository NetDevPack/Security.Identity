using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;

namespace AspNetCore.Jwt.Sample.Config
{
    public static class CustomIdentityAndKeyConfig
    {
        public static void AddCustomIdentityAndKeyConfiguration(this IServiceCollection services, IConfiguration configuration) 
        {
            // Your own EF Identity configuration - Use when you have another database like postgres
            services.AddDbContext<MyIntIdentityContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("CustomKeyConnection")));

            // Your own Identity configuration
            services.AddCustomIdentity<MyIntIdentityUser, int>(options =>
                {
                    options.SignIn.RequireConfirmedEmail = false;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                })
                .AddCustomRoles<MyIntIdentityRoles, int>()
                .AddCustomEntityFrameworkStores<MyIntIdentityContext>()
                .AddDefaultTokenProviders();

            // Ours JWT configuration
            services.AddJwtConfiguration(configuration, "AppSettings");
        }
    }

    public class MyIntIdentityUser : IdentityUser<int>
    {

    }

    public class MyIntIdentityRoles : IdentityRole<int>
    {

    }

    public class MyIntIdentityContext : IdentityDbContext<MyIntIdentityUser, MyIntIdentityRoles, int>
    {
        public MyIntIdentityContext(DbContextOptions<MyIntIdentityContext> options) : base(options) { }
    }
}