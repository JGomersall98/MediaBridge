namespace MediaBridge.Models.Dashboard
{
    public class MediaItem
    {
        public int Id { get; set; }
        public List<string>? Genre { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Poster { get; set; }
        public string? ImdbId { get; set; }
        public int? ReleaseYear { get; set; }
        public string? Description { get; set; } = "";
        public string? Runtime { get; set; }
        public List<MediaSeasonItem>? Seasons { get; set; }
        public int TmdbId { get; set; }

    }
    public class MediaSeasonItem
    {
        public int TmdbId { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeCount { get; set; }
        public string? Title { get; set; }
        public string? AirDate { get; set; }
        
    }
}