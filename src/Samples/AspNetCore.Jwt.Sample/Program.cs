using System.Text.Json.Serialization;
using AspNetCore.Jwt.Sample.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetDevPack.Identity.User;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault);

// When you just want the default configuration
//builder.Services.AddDefaultIdentityConfiguration(builder.Configuration);    

// When you have specifics configurations (see inside this method)
//builder.Services.AddCustomIdentityConfiguration(builder.Configuration);

// When you have specifics configurations (with Key type [see inside this method])
builder.Services.AddCustomIdentityAndKeyConfiguration(builder.Configuration);

// Setting the interactive AspNetUser (logged in)
builder.Services.AddAspNetUserConfiguration();

builder.Services.AddSwaggerConfiguration();

builder.Services.AddDependencyInjectionConfiguration();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwaggerConfiguration();

app.UseHttpsRedirection();

app.UseRouting();

// Custom NetDevPack abstraction here!
app.UseAuthConfiguration();

app.MapControllers();

app.Run();