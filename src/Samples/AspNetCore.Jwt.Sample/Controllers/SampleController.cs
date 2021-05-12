using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetDevPack.Identity.Authorization;
using NetDevPack.Identity.User;

namespace AspNetCore.Jwt.Sample.Controllers
{
    [Authorize]
    [Route("api/sample")]
    public class SampleController : MainController
    {
        private readonly IAspNetUser _user;

        public SampleController(IAspNetUser user)
        {
            _user = user;
        }

        [HttpGet("read")]
        [CustomAuthorize("Sample","Read")]
        public IActionResult SampleActionRead()
        {
            return CustomResponse($"The user {_user.GetUserEmail()} have permission to get this!");
        }

        [HttpGet("list")]
        [CustomAuthorize("Sample", "List")]
        public IActionResult SampleActionList()
        {
            return CustomResponse($"The user {_user.GetUserEmail()} have permission to get this!");
        }
    }
}