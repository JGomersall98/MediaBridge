namespace MediaBridge.Models.Dashboard
{
    public class DashboardTvShowsResponse : StandardResponse
    {
        public List<MediaItem> Shows { get; set; } = new List<MediaItem>();
        public DateTime LastUpdated { get; set; }
        public bool FromCache { get; set; }
    }
}
