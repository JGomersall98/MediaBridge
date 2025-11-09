using System.Threading.Tasks;
using MediaBridge.Models;
using MediaBridge.Models.Admin.AddUser;
using MediaBridge.Models.Admin.EditUser;
using MediaBridge.Models.Admin.GetUser;
using MediaBridge.Models.Admin.ResetPassword;
using MediaBridge.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // POST: api/User
        [HttpPost]
        [Route("/api/AddUser")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest newUser)
        {
            var response = await _userService.AddUser(newUser);
            return Ok(response);
        }

        // GET: api/User
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUsers()
        {
            var users = _userService.GetUserList();
            return Ok(users);
        }

        // GET: api/User/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUser(int id)
        {
            GetUserResponse response = _userService.GetUser(id);
            return Ok(response);
        }

        // POST: api/ResetPassword
        [HttpPost]
        [Route("/api/ResetPassword")]
        [Authorize(Roles = "Admin,Maintainer,User")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
        {
            StandardResponse response = _userService.ResetPassword(resetPasswordRequest);
            return Ok(response);
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult EditUser(int id, [FromBody] EditUserRequest editUserRequest)
        {
            StandardResponse standardResponse = _userService.EditUser(id, editUserRequest);
            return Ok(standardResponse);
        }

        // DELETE: api/user
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteUser(int id)
        {
            StandardResponse response = _userService.DeleteUser(id);
            return Ok(response);
        }


        //// GET: api/User/{id}
        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetUserById(int id)
        //{
        //    var user = await _userService.GetUserById(id);
        //    if (user == null)
        //        return NotFound();

        //    return Ok(user);
        //}

    }
}