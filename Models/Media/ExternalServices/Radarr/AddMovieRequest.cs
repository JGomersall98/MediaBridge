using MediaBridge.Services.Media;

namespace MediaBridge.Models.Media.ExternalServices.Radarr
{
    public class AddMovieRequest
    {
        public int? TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int QualityProfileId { get; set; } = 1;
        public string RootFolderPath { get; set; } = "/mnt/server/Movies";
        public bool Monitored { get; set; } = true;
        public AddOptions AddOptions { get; set; } = new AddOptions { SearchForMovie = true };
    }
    public class AddOptions
    {
        public bool SearchForMovie { get; set; } = true;
    }
}
