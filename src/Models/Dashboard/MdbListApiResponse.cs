namespace MediaBridge.Models.Dashboard
{
    public class MdbListApiResponse
    {
        public List<MediaItem> Movies { get; set; } = new List<MediaItem>();
        public List<MediaItem> Shows { get; set; } = new List<MediaItem>();
    }
}
