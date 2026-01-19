using System.ComponentModel.DataAnnotations;

namespace MediaBridge.Configuration
{
    public class RadarrOptions
    {
        [Required]
        public required string BaseUrl { get; init; }

        [Required]
        public required string ApiKey { get; init; }

        [Required]
        public required RadarrEndpointsOptions Endpoints { get; init; }
    }
    public class RadarrEndpointsOptions
    {
        [Required]
        public required string GetMovie { get; init; }

        [Required]
        public required string AddMovie { get; init; }
    }
}