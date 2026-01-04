using System.Text.Json;
using MediaBridge.Models.Media.ExternalServices.Radarr;
using MediaBridge.Services.Helpers;

namespace MediaBridge.Services.Media.ExternalServices.Radarr
{
    public interface IRadarrService
    {
        Task<bool> SendMovieRequest(string? title, int? tmbId);
        Task<MovieDetailsResponse> GetMovieDetails(int? tmbId);
    }
    public class RadarrService : IRadarrService
    {
        private readonly IRadarrHttp _radarrHttp;
        private const string RadarrAddMovieKey = "radarr_post_movie_endpoint";
        private const string RadarrGetMovieKey = "radarr_get_movie_endpoint";

        public RadarrService(IGetConfig config, IHttpClientService httpClientService, IRadarrHttp radarrHttp)
        {
            _radarrHttp = radarrHttp;
        }
        public async Task<bool> SendMovieRequest(string? title, int? tmbId)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentNullException(nameof(title));

            if (tmbId == null)
                throw new ArgumentNullException(nameof(tmbId), "TMDB ID is required to download movie");

            // Create payload
            string payload = JsonSerializer.Serialize(new AddMovieRequest
            {
                Title = title,
                TmdbId = tmbId
            });

            // Send POST request to Radarr
            HttpResponseString response = await _radarrHttp.RadarrHttpPost(RadarrAddMovieKey, payload);

            // Return status
            return response.IsSuccess;
        }
        public async Task<MovieDetailsResponse> GetMovieDetails(int? tmbId)
        {
            // Validate input
            if (tmbId == null)
                throw new ArgumentNullException(nameof(tmbId), "TMDB ID is required to retrieve movie details");

            // Make GET request to Radarr
            HttpResponseString response = await _radarrHttp.RadarrHttpGet(RadarrGetMovieKey, tmbId.Value);

            // Deserialize response
            List<MovieDetailsResponse>? movieDetailList = JsonSerializer.Deserialize<List<MovieDetailsResponse>>(response.Response);

            // Get first movie detail
            MovieDetailsResponse movieDetail = movieDetailList!.FirstOrDefault()!;

            return movieDetail;
        }
    }
}
