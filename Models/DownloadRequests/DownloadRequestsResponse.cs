namespace MediaBridge.Models.DownloadRequests
{
    public class DownloadRequestsResponse : StandardResponse
    {
        public List<MediaRequestStatus>? Requests { get; set; }
    }
}
