namespace MediaBridge.Database.DB_Models
{
    public class Config
    {
        public int Id { get; set; }
        public required string Key { get; set; }
        public required string Value { get; set; }
        public string? Description { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Updated { get; set; }
    }
}
