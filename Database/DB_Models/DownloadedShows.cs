namespace MediaBridge.Database.DB_Models
{
    public class DownloadedShows
    {
        public int Id { get; set; }
        public string Type { get; set; } // Show or Season
        public string Title { get; set; }
        public string? Description { get; set; }
        public bool HasFile { get; set; }
        public int? AmountOfSeasons { get; set; }
        public int? SeasonNumber { get; set; }
        public int? EpisodesInSeason { get; set; }
        public int? EpisodesDownloaded { get; set; }
        public DateTime DownloadedAt { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string? ImdbId { get; set; }
        public int TvdbId { get; set; }
        public string? PosterPath { get; set; }
        public double? SizeOnDiskGB { get; set; } = 0.0;
        public string? PhysicalPath { get; set; }
        public bool Monitored { get; set; }
        public DateTime Added { get; set; }
    }
}