using System.Text.Json.Serialization;

namespace MediaBridge.Models.Media.ExternalServices.Sonarr
{
    public class ShowDetailsResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("tvdbId")]
        public int? TvdbId { get; set; }
        [JsonPropertyName("overview")]
        public string? Description { get; set; }
        [JsonPropertyName("remotePoster")]
        public string? PosterUrl { get; set; }
        [JsonPropertyName("year")]
        public int? ReleaseYear { get; set; }
    }
}