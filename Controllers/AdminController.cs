using MediaBridge.Models;
using MediaBridge.Models.Admin;
using MediaBridge.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }
        // GET : /api/admin/config
        [HttpGet]
        [Route("config")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetConfig()
        {
            ConfigResponse configResponse = new ConfigResponse();
            configResponse = await _adminService.GetConfigAsync();
            return Ok(configResponse);
        }
        // PUT : /api/admin/config
        [HttpPut]
        [Route("config")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateConfig([FromQuery] int id, [FromBody] string value)
        {
            StandardResponse response = new StandardResponse();
            response = await _adminService.UpdateConfigAsync(id, value);
            return Ok(response);
        }
        // POST : /api/admin/config
        [HttpPost]
        [Route("config")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddConfig([FromBody] NewConfig newConfig)
        {
            StandardResponse response = new StandardResponse();
            if (newConfig.Key == null || newConfig.Value == null || newConfig.Description == null)
            {
                response.IsSuccess = false;
                response.Reason = "Key, Value, and Description are required.";
                return BadRequest(response);
            }
            response = await _adminService.AddConfigAsync(newConfig.Key, newConfig.Value, newConfig.Description);
            return Ok(response);
        }
    }
    public class NewConfig
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public string? Description { get; set; }
    }
}