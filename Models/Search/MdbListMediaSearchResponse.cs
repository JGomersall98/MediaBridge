using MediaBridge.Models.Dashboard;

namespace MediaBridge.Models.Search
{
    public class MdbListMediaSearchResponse : StandardResponse
    {
        public List<MediaItem> Media { get; set; } = new List<MediaItem>();

    }
    public class MdbListShowResponse
    {
        public List<MdbListShowSeason> Seasons { get; set; }
    }
    public class MdbListShowSeason
    {

    }
}
