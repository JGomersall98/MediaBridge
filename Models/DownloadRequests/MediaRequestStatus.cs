namespace MediaBridge.Models.DownloadRequests
{
    public class MediaRequestStatus
    {
        public int Id   { get; set; }
        public string? MediaType { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ReleaseYear { get; set; }
        public string? PosterUrl { get; set; } 
        public string? Status { get; set; }
        public int DownloadPercentage { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? MinutesLeft { get; set; }
        public List<ShowEpisodesStatus>? EpisodesStatus { get; set; }

    }
    public class ShowEpisodesStatus
    {
        public int Id { get; set; }
        public int? SeasonNumber { get; set; }
        public int? EpisodeNumber { get; set; }
        public string? Title { get; set; }
        public DateTime? EpisodeDate { get; set; }
        public string? Status { get; set; }
        public int? DownloadPercentage { get; set; }
        public int? MinutesLeft { get; set; }
    }

}
