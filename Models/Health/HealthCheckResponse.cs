namespace MediaBridge.Models.Health
{
    public class HealthCheckResponse
    {
        public string Status { get; set; } = default!;
        public double TotalDurationMs { get; set; }
    }
}