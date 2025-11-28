namespace MediaBridge.Database.DB_Models
{
    public class MediaRequestLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string Username { get; set; }
        public int TmdbId { get; set; }
        public required string MediaType { get; set; }
        public required string MediaTitle { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = default!;
    }
}
