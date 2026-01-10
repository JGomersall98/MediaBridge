using MediaBridge.Services.Media;

namespace MediaBridge.Models.Media.ExternalServices.Sonarr
{
    public class AddShowRequest
    {
        public int? TvdbId { get; set; }
        public string? Title { get; set; }
        public int QualityProfileId { get; set; } = 1;
        public string? RootFolderPath { get; set; } = "/mnt/server/TV Shows";
        public bool Monitored { get; set; } = true;
        public List<Season>? Seasons { get; set; }
        public AddOptionsSonarr? AddOptions { get; set; } = new AddOptionsSonarr
        {
            SearchForMissingEpisodes = true,
            SearchForCutoffUnmetEpisodes = true
        };
    }
    public class AddOptionsSonarr
    {
        public bool? SearchForMissingEpisodes { get; set; } = true;
        public bool? SearchForCutoffUnmetEpisodes { get; set; } = true;
    }
}