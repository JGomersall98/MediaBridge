using MediaBridge.Models;
using MediaBridge.Models.Admin.AddUser;
using MediaBridge.Models.Admin.ResetPassword;
using MediaBridge.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MediaBridge.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
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
            var users = _userService.GetUsers();
            return Ok(users);
        }

        // POST: api/ResetPassword
        [HttpPost]
        [Route("/api/ResetPassword")]
        [Authorize(Roles = "Admin, Maintainer, User")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
        {
            StandardResponse standardResponse = _userService.ResetPassword(resetPasswordRequest);
            return Ok(standardResponse);
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