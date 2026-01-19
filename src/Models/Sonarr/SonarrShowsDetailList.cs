using System.Text.Json.Serialization;

namespace MediaBridge.Models.Sonarr
{
    public class SonarrShowsDetailList
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("overview")]
        public string? Description { get; set; }


        [JsonPropertyName("images")]
        public List<SonarrImage>? Images { get; set; }


        [JsonPropertyName("seasons")]
        public List<SonarrSeason>? Seasons { get; set; }

        // --- Fields that map nicely to your DB model ---

        [JsonPropertyName("year")]
        public int ReleaseYear { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("monitored")]
        public bool Monitored { get; set; }

        [JsonPropertyName("runtime")]
        public int Runtime { get; set; }

        [JsonPropertyName("imdbId")]
        public string? ImdbId { get; set; }

        [JsonPropertyName("tvdbId")]
        public int TvdbId { get; set; }

        [JsonPropertyName("rootFolderPath")]
        public string? RootFolderPath { get; set; }

        [JsonPropertyName("firstAired")]
        public DateTime? FirstAired { get; set; }

        [JsonPropertyName("lastAired")]
        public DateTime? LastAired { get; set; }

        [JsonPropertyName("seriesType")]
        public string? SeriesType { get; set; }

        [JsonPropertyName("cleanTitle")]
        public string? CleanTitle { get; set; }

        [JsonPropertyName("titleSlug")]
        public string? TitleSlug { get; set; }

        [JsonPropertyName("certification")]
        public string? Certification { get; set; }

        [JsonPropertyName("genres")]
        public List<string>? Genres { get; set; }

        [JsonPropertyName("tags")]
        public List<int>? Tags { get; set; }

        [JsonPropertyName("added")]
        public DateTime Added { get; set; }


        [JsonPropertyName("statistics")]
        public SonarrSeriesStatistics? Statistics { get; set; }

        [JsonPropertyName("languageProfileId")]
        public int LanguageProfileId { get; set; }

        [JsonPropertyName("qualityProfileId")]
        public int QualityProfileId { get; set; }

        [JsonPropertyName("seasonFolder")]
        public bool SeasonFolder { get; set; }

        [JsonPropertyName("monitorNewItems")]
        public string? MonitorNewItems { get; set; }

        [JsonPropertyName("useSceneNumbering")]
        public bool UseSceneNumbering { get; set; }

        [JsonPropertyName("tvRageId")]
        public int TvRageId { get; set; }

        [JsonPropertyName("tvMazeId")]
        public int TvMazeId { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class SonarrAlternateTitle
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("seasonNumber")]
        public int SeasonNumber { get; set; }
    }

    public class SonarrImage
    {
        [JsonPropertyName("coverType")]
        public string? CoverType { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("remoteUrl")]
        public string? RemoteUrl { get; set; }
    }

    public class SonarrSeason
    {
        [JsonPropertyName("seasonNumber")]
        public int SeasonNumber { get; set; }

        [JsonPropertyName("monitored")]
        public bool Monitored { get; set; }

        [JsonPropertyName("statistics")]
        public SonarrSeasonStatistics? Statistics { get; set; }
    }

    public class SonarrSeasonStatistics
    {
        [JsonPropertyName("previousAiring")]
        public DateTime? PreviousAiring { get; set; }

        [JsonPropertyName("episodeFileCount")]
        public int EpisodeFileCount { get; set; }

        [JsonPropertyName("episodeCount")]
        public int EpisodeCount { get; set; }

        [JsonPropertyName("totalEpisodeCount")]
        public int TotalEpisodeCount { get; set; }

        [JsonPropertyName("sizeOnDisk")]
        public long SizeOnDisk { get; set; }

        [JsonPropertyName("releaseGroups")]
        public List<string>? ReleaseGroups { get; set; }

        [JsonPropertyName("percentOfEpisodes")]
        public double PercentOfEpisodes { get; set; }
    }

    public class SonarrSeriesStatistics
    {
        [JsonPropertyName("seasonCount")]
        public int SeasonCount { get; set; }

        [JsonPropertyName("episodeFileCount")]
        public int EpisodeFileCount { get; set; }

        [JsonPropertyName("episodeCount")]
        public int EpisodeCount { get; set; }

        [JsonPropertyName("totalEpisodeCount")]
        public int TotalEpisodeCount { get; set; }

        [JsonPropertyName("sizeOnDisk")]
        public long SizeOnDisk { get; set; }

        [JsonPropertyName("releaseGroups")]
        public List<string>? ReleaseGroups { get; set; }

        [JsonPropertyName("percentOfEpisodes")]
        public double PercentOfEpisodes { get; set; }
    }
}
