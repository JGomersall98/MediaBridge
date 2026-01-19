using System.Security.Claims;
using MediaBridge.Models.Dashboard;
using MediaBridge.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaBridge.Controllers
{
    //[Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        [Route("api/dashboard/movies")]
        [Authorize(Roles = "Admin,Maintainer,User")]
        public async Task<IActionResult> GetDashboardMovies()
        {
            DashboardMoviesResponse movies = new DashboardMoviesResponse();
            movies = await _dashboardService.GetTopMoviesAsync();
            return Ok(movies);
        }
        [HttpGet]
        [Route("api/dashboard/tvshows")]
        [Authorize(Roles = "Admin,Maintainer,User")]
        public async Task<IActionResult> GetDashboardTvShows()
        {
            DashboardTvShowsResponse tvShows = new DashboardTvShowsResponse();
            tvShows = await _dashboardService.GetTopTvShowsAsync();
            return Ok(tvShows);
        }
    }
}
