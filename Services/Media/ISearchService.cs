using MediaBridge.Models.Dashboard;
using MediaBridge.Models.Search;

namespace MediaBridge.Services.Media
{
    public interface ISearchService
    {
        Task<MdbListMediaSearchResponse> MdbListMediaSearch(string media, string query);
        Task<string> GetMediaInfo<T>(List<T> ids, string mediaType, string idkey);
        List<MediaItem> BuildMediaItemList<T>(MbListSearchResult searchResult, List<T>? infoList);
    }
}
