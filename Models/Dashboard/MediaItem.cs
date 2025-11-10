namespace MediaBridge.Models.Dashboard
{
    public class MediaItem
    {
        public int Id { get; set; }
        public List<string> Genre { get; set; } = new List<string>();
        public string Title { get; set; } = string.Empty;
        public string? Poster { get; set; }
        public string? Imdb_id { get; set; }
        public int? Release_year { get; set; }
    }
}
