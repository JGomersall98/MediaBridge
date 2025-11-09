//using MediaBridge.Models.Admin;
//using MediaBridge.Services.Admin;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace MediaBridge.Controllers
//{
//    [Route("api/[controller]")]
//    [Authorize(Roles = "Admin")]
//    [ApiController]
//    public class AddUserController : ControllerBase
//    {
//        private readonly IUserService _addUserService;
//        public AddUserController(IUserService addUserService)
//        {
//            _addUserService = addUserService;
//        }
//        [HttpPost]
//        public async Task<IActionResult> AddUser([FromBody] AddUserRequest user)
//        {
//            AddUserResponse response = await _addUserService.AddUser(user.UserName, user.Email);
//            return Ok(response);
//        }
//    }
//}
