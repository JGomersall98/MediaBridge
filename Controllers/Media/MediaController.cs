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
        public async Task<IActionResult> Post([FromQuery] int tmbId, string mediaType)
        {
            StandardResponse response = new StandardResponse();

            var userId = User.GetUserId();
            var username = User.GetUsername();

            if (mediaType.ToLower() == "movie")
            {
                response = await _mediaService.DownloadMovie(tmbId, userId, username);
            }
            else
            {
                response = new StandardResponse { Reason = "Invalid media type." };
            }

            return Ok(response);
        }
    }
}
