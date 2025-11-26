using MediaBridge.Models.Dashboard;
using MediaBridge.Models.Search;

namespace MediaBridge.Services.Media
{
    public interface ISearchService
    {
        Task<MdbListMediaSearchResponse> MdbListMovieSearch(string media, string query);
    }
}
