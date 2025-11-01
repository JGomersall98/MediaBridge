using MediaBridge.Models.Admin;
using MediaBridge.Services.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddUserController : ControllerBase
    {
        private readonly IAddUserService _addUserService;
        public AddUserController(IAddUserService addUserService)
        {
            _addUserService = addUserService;
        }
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest user)
        {
            AddUserResponse response = await _addUserService.AddUser(user.UserName, user.Email);
            return Ok();
        }
    }
}
