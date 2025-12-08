using MediaBridge.Models.DownloadRequests;

namespace MediaBridge.Services.Media.Downloads
{
    public interface IRequestDownloadStatusService
    {
        Task<DownloadRequestsResponse> GetDownloadRequestsStatus(int userId);
        Task<DownloadRequestsResponse> GetAllDownloadRequests();
    }
}
