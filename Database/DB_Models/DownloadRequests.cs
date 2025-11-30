using Microsoft.EntityFrameworkCore.Query;

namespace MediaBridge.Database.DB_Models
{
    public class DownloadRequests
    {
        public int Id { get; set; }
        public int TmdbId { get; set; }
        public int UserId { get; set; }
        public string? MediaType { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? PosterUrl { get; set; }
        public int ReleaseYear { get; set; }
        public string? Status { get; set; }
        public int DownloadPercentage { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime CompletedAt { get; set; }

        public User? User { get; set; }
    }
}
