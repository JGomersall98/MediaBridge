using MediaBridge.Models.Dashboard;

namespace MediaBridge.Models.Search
{
    public class MdbListMediaSearchResponse : StandardResponse
    {
        public List<MediaItem> Media { get; set; } = new List<MediaItem>();
    }
}
