using MediaBridge.Models;

namespace MediaBridge.Services.Media
{
    public interface IMediaService
    {
        Task<StandardResponse> DownloadMedia(int mediaId, int userId, string username, int[]? seasonsRequested, string mediaType);
        Task<StandardResponse> PartialSeriesDownload(int tvdbId, int userId, string username, int[] seasonsRequested);
    }
}
