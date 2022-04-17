using System.Threading.Tasks;
using AspNetCore.Jwt.Sample.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NetDevPack.Identity.Interfaces;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Jwt.Model;
using NetDevPack.Identity.Model;
using NetDevPack.Identity.User;

namespace AspNetCore.Jwt.Sample.Controllers
{
    [Route("api/custom-account-key")]
    public class AuthCustomConfigAndKeyController : MainController
    {
        private readonly SignInManager<MyIntIdentityUser> _signInManager;
        private readonly UserManager<MyIntIdentityUser> _userManager;
        private readonly IJwtBuilder _jwtBuilder;

        public AuthCustomConfigAndKeyController(SignInManager<MyIntIdentityUser> signInManager,
            UserManager<MyIntIdentityUser> userManager,
            IJwtBuilder jwtBuilder)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtBuilder = jwtBuilder;
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
                return CustomResponse(await GetUserResponse(user.Email));
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

        private Task<UserResponse> GetUserResponse(string email)
        {
            return _jwtBuilder
                .WithEmail(email)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .WithRefreshToken()
                .BuildUserResponse();
        }

        private Task<string> GetFullJwt(string email)
        {
            return _jwtBuilder
                .WithEmail(email)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .WithRefreshToken()
                .BuildToken();
        }

        private Task<string> GetJwtWithoutClaims(string email)
        {
            return _jwtBuilder
                .WithEmail(email)
                .WithRefreshToken()
                .BuildToken();
        }

        private Task<string> GetJwtWithUserClaims(string email)
        {
            return _jwtBuilder
                .WithEmail(email)
                .WithUserClaims()
                .WithRefreshToken()
                .BuildToken();
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken([FromForm] string refreshToken)
        {
            var tokenValidation = await _jwtBuilder.ValidateRefreshToken(refreshToken);

            if (!tokenValidation)
            {
                ModelState.AddModelError("RefreshToken", "Expired token");
                return BadRequest(ModelState);
            }

            return CustomResponse(await _jwtBuilder
                .WithUserId(tokenValidation.UserId)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .WithRefreshToken()
                .BuildUserResponse());

        }
    }
}