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
            var response = await _searchFunction.MdbListMovieSearch(mediaType, query);
            return Ok(response);
        }
    }
}
