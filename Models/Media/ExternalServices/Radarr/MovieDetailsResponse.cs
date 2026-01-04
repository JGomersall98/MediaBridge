using System.Text.Json.Serialization;

namespace MediaBridge.Models.Media.ExternalServices.Radarr
{
    public class MovieDetailsResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("tmdbId")]
        public int TmdbId { get; set; }
        [JsonPropertyName("overview")]
        public string? Description { get; set; }
        [JsonPropertyName("remotePoster")]
        public string? PosterUrl { get; set; }
        [JsonPropertyName("year")]
        public int? ReleaseYear { get; set; }
    }
}


//public class RSGetMediaResponse
//{
//    [JsonPropertyName("id")]
//    public int MediaId { get; set; }
//    [JsonPropertyName("title")]
//    public string? Title { get; set; }
//    [JsonPropertyName("overview")]
//    public string? Description { get; set; }
//    [JsonPropertyName("tmdbId")]
//    public int? TmdbId { get; set; } // Movie
//    [JsonPropertyName("tvdbId")]
//    public int? TvdbId { get; set; } // Tv Show
//    [JsonPropertyName("imdbId")]
//    public string? ImdbId { get; set; }
//    [JsonPropertyName("remotePoster")]
//    public string? PosterUrl { get; set; }
//    [JsonPropertyName("year")]
//    public int? ReleaseYear { get; set; }
//}