using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UNO.Application.DTOs.Common.identity;
using UNO.Application.Interfaces.Common;
using UNO.Application.Services.Common;

namespace UNO.Presentation.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService accountService;
        public AccountController(IAccountService accountService)
        {
            this.accountService = accountService;
        }
        [HttpPost]
        public async Task<IActionResult> HandleRefreshToken(GenrateRefreshToken refreshToken)
        {
            var Result = await accountService.HandleRefreshTokenAsync(refreshToken);
            return Ok(Result);
        }
    }
}
