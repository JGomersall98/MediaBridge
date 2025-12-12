namespace MediaBridge.Database.DB_Models
{
    public class DownloadedMovies
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public bool HasFile { get; set; }
        public DateTime DownloadedAt { get; set; }
        public int ReleaseYear { get; set; }
        public string? ImdbId { get; set; }
        public int TmdbId { get; set; }
        public string? PosterPath { get; set; }
        public double SizeOnDiskGB { get; set; } = 0.0;
        public string PhysicalPath { get; set; }
        public bool Monitored { get; set; }
        public int Runtime { get; set; }
        public DateTime Added { get; set; }
    }
}




