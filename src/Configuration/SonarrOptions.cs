using System.ComponentModel.DataAnnotations;

namespace MediaBridge.Configuration
{
    public class SonarrOptions
    {
        [Required]
        public required string BaseUrl { get; init; }

        [Required]
        public required string ApiKey { get; init; }

        [Required]
        public required SonarrEndpointsOptions Endpoints { get; init; }
    }
    public class SonarrEndpointsOptions
    {
        [Required]
        public required string GetSeries { get; init; }

        [Required]
        public required string AddSeries { get; init; }
    }
}