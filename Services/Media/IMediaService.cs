using MediaBridge.Models;

namespace MediaBridge.Services.Media
{
    public interface IMediaService
    {
        Task<StandardResponse> DownloadMedia(int tmbId, int userId, string username, int[]? seasonsRequested, string mediaType);
        Task<StandardResponse> PartialSeriesDownload(int tvdbid, int userId, string username, int[] seasonsRequested);
    }
}
