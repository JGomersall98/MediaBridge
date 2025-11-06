using MediaBridge.Models.Authentication;
using MediaBridge.Services.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }
        
        [HttpPost]
        public IActionResult Authenticate([FromBody] LoginRequest request)
        {
            LoginResponse lr =  _authenticationService.LoginAsync(request.Username, request.Password);

            return Ok(lr);
        }
    }
}
