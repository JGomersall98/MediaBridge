using System.Security.Claims;
using MediaBridge.Services.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaBridge.Controllers.Media
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchFunction;
        public SearchController(ISearchService searchFunction)
        {
            _searchFunction = searchFunction;
        }

        // GET : /api/search
        [HttpGet]
        [Authorize(Roles = "Admin,Maintainer,User")]
        public async Task<IActionResult> SearchMedia([FromQuery] string mediaType, string query)
        {
            // Extract user information from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var usernameClaim = User.FindFirst(ClaimTypes.Name);

            var response = await _searchFunction.MdbListMovieSearch(mediaType, query);
            return Ok(response);
        }
    }
}
