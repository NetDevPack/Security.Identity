﻿using System.Threading.Tasks;
using AspNetCore.Jwt.Sample.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Jwt.Model;
using NetDevPack.Identity.Model;

namespace AspNetCore.Jwt.Sample.Controllers
{
    [Route("api/custom-account-key")]
    public class AuthCustomConfigAndKeyController : MainController
    {
        private readonly SignInManager<MyIntIdentityUser> _signInManager;
        private readonly UserManager<MyIntIdentityUser> _userManager;
        private readonly AppJwtSettings _appJwtSettings;

        public AuthCustomConfigAndKeyController(SignInManager<MyIntIdentityUser> signInManager,
            UserManager<MyIntIdentityUser> userManager,
            IOptions<AppJwtSettings> appJwtSettings)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _appJwtSettings = appJwtSettings.Value;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterUser registerUser)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var user = new MyIntIdentityUser
            {
                UserName = registerUser.Email,
                Email = registerUser.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerUser.Password);

            if (result.Succeeded)
            {
                return CustomResponse(GetUserResponse(user.Email));
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            return CustomResponse();
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginUser loginUser)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var result = await _signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

            if (result.Succeeded)
            {
                /* ANOTHER OPTIONS */
                var userResponse = GetUserResponse(loginUser.Email);
                var jwtUserClaims = GetJwtWithUserClaims(loginUser.Email);
                var jwtNoClaims = GetJwtWithoutClaims(loginUser.Email);

                var fullJwt = GetFullJwt(loginUser.Email);
                return CustomResponse(fullJwt);
            }

            if (result.IsLockedOut)
            {
                AddError("This user is blocked");
                return CustomResponse();
            }

            AddError("Incorrect user or password");
            return CustomResponse();
        }

        private UserResponse<int> GetUserResponse(string email)
        {
            return new JwtBuilder<MyIntIdentityUser, int>()
                .WithUserManager(_userManager)
                .WithJwtSettings(_appJwtSettings)
                .WithEmail(email)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .BuildUserResponse();
        }

        private string GetFullJwt(string email)
        {
            return new JwtBuilder<MyIntIdentityUser, int>()
                .WithUserManager(_userManager)
                .WithJwtSettings(_appJwtSettings)
                .WithEmail(email)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .BuildToken();
        }

        private string GetJwtWithoutClaims(string email)
        {
            return new JwtBuilder<MyIntIdentityUser, int>()
                .WithUserManager(_userManager)
                .WithJwtSettings(_appJwtSettings)
                .WithEmail(email)
                .BuildToken();
        }

        private string GetJwtWithUserClaims(string email)
        {
            return new JwtBuilder<MyIntIdentityUser, int>()
                .WithUserManager(_userManager)
                .WithJwtSettings(_appJwtSettings)
                .WithEmail(email)
                .WithUserClaims()
                .BuildToken();
        }
    }
}