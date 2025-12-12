using MediaBridge.Models.Dashboard;

namespace MediaBridge.Services.Dashboard
{
    public interface IDashboardService
    {
        Task<DashboardMoviesResponse> GetTopMoviesAsync();
        Task<DashboardTvShowsResponse> GetTopTvShowsAsync();
        Task RefreshCaches();
    }
}