using System.Security.Claims;
using MediaBridge.Extensions;
using MediaBridge.Models.DownloadRequests;
using MediaBridge.Services.Media.Downloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaBridge.Controllers.Media
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadRequestsController : ControllerBase
    {
        private readonly IRequestDownloadStatusService _requestDownloadStatusService;
        public DownloadRequestsController(IRequestDownloadStatusService requestDownloadStatusService)
        {
            _requestDownloadStatusService = requestDownloadStatusService;
        }

        // GET : /api/downloadrequests
        [HttpGet]
        [Authorize(Roles = "Admin,Maintainer,User")]
        public async Task<ActionResult<DownloadRequestsResponse>> GetRequestsAsync()
        {
            int userId = User.GetUserId();
            var response = await _requestDownloadStatusService.GetDownloadRequestsStatus(userId);
            return Ok(response);
        }
    }
}
