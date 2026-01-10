using System.Text.Json;
using MediaBridge.Models.Media.ExternalServices.Radarr;
using MediaBridge.Services.Helpers;

namespace MediaBridge.Services.Media.ExternalServices.Radarr
{
    public interface IRadarrService
    {
        Task<bool> SendMovieRequest(string? title, int? tmdbId);
        Task<MovieDetailsResponse> GetMovieDetails(int? tmdbId);
    }
    public class RadarrService : IRadarrService
    {
        private readonly IRadarrHttp _radarrHttp;
        private readonly JsonSerializerOptions _jsonOptions;
        private const string RadarrAddMovieKey = "radarr_post_movie_endpoint";
        private const string RadarrGetMovieKey = "radarr_get_movie_endpoint";

        public RadarrService(IRadarrHttp radarrHttp)
        {
            _radarrHttp = radarrHttp;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }
        public async Task<bool> SendMovieRequest(string? title, int? tmdbId)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentNullException(nameof(title));

            if (tmdbId == null)
                throw new ArgumentNullException(nameof(tmdbId), "TMDB ID is required to download movie");

            // Create payload
            string payload = JsonSerializer.Serialize(new AddMovieRequest
            {
                Title = title,
                TmdbId = tmdbId
            });

            // Send POST request to Radarr
            HttpResponseString response = await _radarrHttp.RadarrHttpPost(RadarrAddMovieKey, payload);

            return response.IsSuccess;
        }
        public async Task<MovieDetailsResponse> GetMovieDetails(int? tmdbId)
        {
            // Validate input
            if (tmdbId == null)
                throw new ArgumentNullException(nameof(tmdbId), "TMDB ID is required to retrieve movie details");

            // Make GET request to Radarr
            HttpResponseString response = await _radarrHttp.RadarrHttpGet(RadarrGetMovieKey, tmdbId.Value);

            // Deserialize response
            List<MovieDetailsResponse>? movieDetailList = JsonSerializer.Deserialize<List<MovieDetailsResponse>>(response.Response, _jsonOptions);

            // Validate response
            if (movieDetailList == null || movieDetailList.Count == 0)
                throw new InvalidOperationException("No movie details found for the provided TMDB ID.");

            // Get first movie detail
            MovieDetailsResponse movieDetail = movieDetailList[0];

            return movieDetail;
        }
    }
}
