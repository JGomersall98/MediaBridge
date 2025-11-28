using MediaBridge.Models;

namespace MediaBridge.Services.Media
{
    public interface IMediaService
    {
        //Task<StandardResponse> DownloadMovie(int tmbId, int userId, string username, string mediaType);
        //Task<StandardResponse> DownloadTVShow(int tmbId, int userId, string username, int[]? seasonsRequested, string mediaType);
        Task<StandardResponse> DownloadMedia(int tmbId, int userId, string username, int[]? seasonsRequested, string mediaType);
    }
}
