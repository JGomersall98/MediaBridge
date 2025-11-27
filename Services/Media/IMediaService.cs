using MediaBridge.Models;

namespace MediaBridge.Services.Media
{
    public interface IMediaService
    {
        Task<StandardResponse> DownloadMovie(int tmbId, int userId, string username);
    }
}
