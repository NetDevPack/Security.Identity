<img src="https://repository-images.githubusercontent.com/268701472/8bf84980-a6ce-11ea-83da-e2133c5a3a7a" alt=".NET DevPack" width="300px" />

What is the .NET DevPack.Identity?
=====================
.NET DevPack Identity is a set of common implementations to help you implementing ASP.NET Identity, JWT, claims validation and another facilities

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/f1bd42eda59844ea95852606741147fa)](https://www.codacy.com/gh/NetDevPack/NetDevPack.Identity?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=NetDevPack/NetDevPack.Identity&amp;utm_campaign=Badge_Grade)
[![Build status](https://ci.appveyor.com/api/projects/status/e283g9ik4rk3ymsp?svg=true)](https://ci.appveyor.com/project/EduardoPires/netdevpack-identity)
![.NET Core](https://github.com/NetDevPack/NetDevPack.Identity/workflows/.NET%20Core/badge.svg)
[![License](http://img.shields.io/github/license/NetDevPack/NetDevPack.Identity.svg)](LICENSE)

## Give a Star! :star:
If you liked the project or if NetDevPack helped you, please give a star ;)

## Get Started

| Package |  Version | Popularity |
| ------- | ----- | ----- |
| `NetDevPack.Identity` | [![NuGet](https://img.shields.io/nuget/v/NetDevPack.Identity.svg)](https://nuget.org/packages/NetDevPack.Identity) | [![Nuget](https://img.shields.io/nuget/dt/NetDevPack.Identity.svg)](https://nuget.org/packages/NetDevPack.Identity) |


.NET DevPack.Identity can be installed in your ASP.NET Core application using the Nuget package manager or the `dotnet` CLI.

```
dotnet add package NetDevPack.Identity
```

If you want to use our IdentityDbContext (ASP.NET Identity standard) you will need to create the Identity tables. Set your connection string in the `appsettings.json` and follow the next steps:

Add the IdentityDbContext configuration in your `startup.cs`:

```csharp
services.AddIdentityEntityFrameworkContextConfiguration(options => 
	options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), 
	b=>b.MigrationsAssembly("AspNetCore.Jwt.Sample")));
```

>**Note:** You must inform the namespace to avoid migration errors
>
>**Note:** You must install the `Microsoft.EntityFrameworkCore.SqlServer` or another provider like `Npgsql.EntityFrameworkCore.PostgreSQL` package to have support from your database. Find the package for your database [here](https://docs.microsoft.com/en-us/ef/core/providers/?tabs=dotnet-core-cli)

Add the Identity configuration in `ConfigureServices` method of your `startup.cs`:

```csharp
services.AddIdentityConfiguration();
```

>**Note:** This extension returns an IdentityBuilder to allow you extending the configuration

Add the Identity configuration in `Configure` method of your `startup.cs`:

```csharp
app.UseAuthConfiguration();
```

>**Note:** This method need to be set between `app.UseRouting()` and `app.UseEndpoints()`

Run the command to generate the migration files:

```
dotnet ef migrations add Initial --context NetDevPackAppDbContext --project <Your patch>/<Your Project>.csproj
```

Run the command to generate the database:

```
dotnet ef database update --context NetDevPackAppDbContext --project <Your patch>/<Your Project>.csproj
```
>**Note:** If are you using your own `IdentityDbContext` you must change the `NetDevPackAppDbContext` value to your context class name in the commands above.

After execute this steps you will be all set to use the Identity in your Application.

### Configuring JWT
If you want to generate JSON Web Tokens in your application you need to add the JWT configuration in `ConfigureServices` method of your `startup.cs`
```csharp
services.AddJwtConfiguration(Configuration)
        .AddNetDevPackIdentity<IdentityUser>();
```

Set your `appsettings.json` file with this values:

```json
"AppJwtSettings": {
    "Audience": "MyApplication.Name"
}
``` 
It's possible to configure some aspects of token

|Key|Meaning|Default
|--|--|---|
|Expiration| Expiration time (in hours)  | 1 |
|Issuer| The party that "created" the token and signed it with its private key. Usually the application Url  | Get current root Url from `HttpContext` |
|Audience| API's that should accept the token. E.g your application Main name. | NetDevPack |
|RefreshTokenExpiration  | Refresh token expiration (In Days) | 30 |
|RefreshTokenType  | `OneTime` or `ReUse` | 30 |
|SecretKey `Deprecated`  | Is your key to build JWT. **Read notes**| Do not use it |

>**Note:** Now we are using [NetDevPack.Security.Jwt](https://github.com/NetDevPack/Security.Jwt) to generate and Store your keys. It generate a RSA 2048 by default. You can check the project for more info.

### Generating JWT
You will need to set a single dependency in your Authentication Controller:

```csharp

public AuthController(IJwtBuilder jwtBuilder)
{
    _jwtBuilder = jwtBuilder;
}
```

After user register or login process you can generate a JWT to respond the request. Use our implementation, you just need inform the user email and the dependencies injected in your controller:

```csharp
return _jwtBuilder
	.WithEmail(email)
    .WithRefreshToken()
	.BuildToken();
```

>**Note:** This builder can return a single string with JWT or a complex object `UserResponse` if you want return more data than a single JWT string.

#### Adding Claims to your JWT
You can call more methods in `JwtBuilder` to provide more information about the user:

```csharp
return _jwtBuilder
    .WithEmail(email)
    .WithJwtClaims()
    .WithUserClaims()
    .WithUserRoles()
    .WithRefreshToken()
    .BuildToken();
```

|Method|Meaning|
|--|--|
|WithJwtClaims()| Claims of JWT like `sub`, `jti`, `nbf` and others |
|WithUserClaims()| The user claims registered in `AspNetUserClaims` table|
|WithUserRoles()| The user roles (as claims) registered in `AspNetUserRoles` table  |
|BuildToken()| Build and return the JWT as single string  |

If you want return your complex object `UserResponse` you need to change the last method to:

```csharp
return _jwtBuilder
    .WithEmail(email)
    .WithJwtClaims()
    .WithUserClaims()
    .WithUserRoles()
    .WithRefreshToken()
    .BuildUserResponse();
```

## Examples
Use the [sample application](https://github.com/NetDevPack/NetDevPack.Identity/tree/master/src/Samples/AspNetCore.Jwt.Sample) to understand how NetDevPack.Identity can be implemented and help you to decrease the complexity of your application and development time.

## Compatibility
The **NetDevPack.Identity** was developed to be implemented in **ASP.NET Core**. It support all .NET versions since 3.1.

## About
.NET DevPack.Identity was developed by [Eduardo Pires](http://eduardopires.net.br) under the [MIT license](LICENSE).

