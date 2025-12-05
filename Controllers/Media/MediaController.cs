using System.Security.Claims;
using MediaBridge.Database.DB_Models;
using MediaBridge.Extensions;
using MediaBridge.Models;
using MediaBridge.Services.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaBridge.Controllers.Media
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        // POST : /api/download
        [HttpPost]
        [Authorize(Roles = "Admin,Maintainer,User")]
        public async Task<IActionResult> Post([FromQuery] int tmbId, string mediaType, [FromBody] int[]? seasonsRequested)
        {
            StandardResponse response = new StandardResponse();

            int userId = User.GetUserId();
            string username = User.GetUsername();

            response = await _mediaService.DownloadMedia(tmbId, userId, username, seasonsRequested, mediaType);

            return Ok(response);
        }
    }
}