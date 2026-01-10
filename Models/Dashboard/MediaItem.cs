using System.Text.Json.Serialization;

namespace MediaBridge.Models.Dashboard
{
    public class MediaItem
    {
        public int Id { get; set; }
        public List<string>? Genre { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Poster { get; set; }
        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }
        [JsonPropertyName("released")]
        public string? ReleaseDateString { get; set; }
        public int? ReleaseYear { get; set; }
        public string? Description { get; set; } = "";
        public string? Runtime { get; set; }
        public List<MediaSeasonItem>? Seasons { get; set; }
        public int TmdbId { get; set; }
        public int? TvdbId { get; set; }
        public double? ImbdRating { get; set; }
        public bool? HasMedia { get; set; }
        public bool? HasPartMedia { get; set; }
        public MediaAvailability? MediaAvailability { get; set; }
    }
    public class MediaSeasonItem
    {
        public int TmbdId { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeCount { get; set; }
        public string? Title { get; set; }
        public string? AirDate { get; set; }
        public bool? HasFile { get; set; }
        public bool? HasPartFile { get; set; }
    }
    public class MediaAvailability
    {
        public bool? IsAvailable { get; set; }
        public bool? NotReleased { get; set; }
        public bool? English { get; set; }
    }
}