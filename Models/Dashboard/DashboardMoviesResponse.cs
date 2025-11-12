namespace MediaBridge.Models.Dashboard
{
    public class DashboardMoviesResponse : StandardResponse
    {
        public List<MediaItem> Movies { get; set; } = new List<MediaItem>();
        public DateTime LastUpdated { get; set; }
        public bool FromCache { get; set; }
    }
}