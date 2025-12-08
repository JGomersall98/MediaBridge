namespace MediaBridge.Models.Admin
{
    public class ConfigResponse : StandardResponse
    {
        public List<SingleConfig>? ConfigList { get; set; }
    }
    public class SingleConfig
    {
        public int Id { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
        public string? Description { get; set; }
        public string? DateUpdated { get; set; }
        public string? DateCreated { get; set; }

    }
}
