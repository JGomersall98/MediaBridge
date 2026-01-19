using System.Text.Json.Serialization;

namespace MediaBridge.Models.Radarr
{
    public class RadarrMovieDetailList
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("overview")]
        public string? Description { get; set; }

        [JsonPropertyName("sizeOnDisk")]
        public long SizeOnDisk { get; set; }

        [JsonPropertyName("images")]
        public List<RadarrImage>? Images { get; set; }

        [JsonPropertyName("year")]
        public int ReleaseYear { get; set; }

        [JsonPropertyName("hasFile")]
        public bool HasFile { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("monitored")]
        public bool Monitored { get; set; }

        [JsonPropertyName("runtime")]
        public int Runtime { get; set; }

        [JsonPropertyName("imdbId")]
        public string? ImdbId { get; set; }

        [JsonPropertyName("tmdbId")]
        public int TmdbId { get; set; }

        [JsonPropertyName("rootFolderPath")]
        public string? RootFolderPath { get; set; }

        [JsonPropertyName("added")]
        public DateTime Added { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class RadarrImage
    {
        [JsonPropertyName("coverType")]
        public string? CoverType { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("remoteUrl")]
        public string? RemoteUrl { get; set; }
    }
}
