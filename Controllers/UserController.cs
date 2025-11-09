using MediaBridge.Models.Admin;
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
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest user)
        {
            var response = await _userService.AddUser(user.UserName, user.Email);
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