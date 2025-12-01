using Microsoft.EntityFrameworkCore.Query;

namespace MediaBridge.Database.DB_Models
{
    public class DownloadRequests
    {
        public int Id { get; set; }
        public int? MediaId { get; set; }
        public int? EpisodeId { get; set; }
        public int? TmdbId { get; set; }
        public int? TvdbId { get; set; }
        public int UserId { get; set; }
        public string? MediaType { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? PosterUrl { get; set; }
        public int? ReleaseYear { get; set; }
        public string? Status { get; set; }
        public int DownloadPercentage { get; set; }
        public int? MinutesLeft { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? EpisodeDate { get; set; }
        public int? SeasonNumber { get; set; }
        public int? EpisodeNumber { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public User? User { get; set; }
    }
}
