using MediaBridge.Models.Dashboard;
using MediaBridge.Models.Search;

namespace MediaBridge.Services.Media
{
    public interface ISearchService
    {
        Task<MdbListMediaSearchResponse> MdbListSearch(string media, string query);
    }
}
