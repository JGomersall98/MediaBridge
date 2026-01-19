using System.Text.Json;
using MediaBridge.Configuration;
using MediaBridge.Models.Media.ExternalServices.Radarr;
using MediaBridge.Services.Helpers;
using Microsoft.Extensions.Options;

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
        private readonly RadarrOptions _radarrOptions;

        public RadarrService(IRadarrHttp radarrHttp, IOptions<RadarrOptions> radarrOptions)
        {
            _radarrHttp = radarrHttp;
            _radarrOptions = radarrOptions.Value;
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

            // Get endpoint and replace any placeholders if needed
            var endpoint = _radarrOptions.Endpoints.AddMovie;

            // Send POST request to Radarr
            HttpResponseString response = await _radarrHttp.RadarrHttpPost(endpoint, payload);

            return response.IsSuccess;
        }

        public async Task<MovieDetailsResponse> GetMovieDetails(int? tmdbId)
        {
            // Validate input
            if (tmdbId == null)
                throw new ArgumentNullException(nameof(tmdbId), "TMDB ID is required to retrieve movie details");

            // Get endpoint and replace ID placeholder
            var endpoint = _radarrOptions.Endpoints.GetMovie.Replace("{id}", tmdbId.Value.ToString());

            // Send GET request to Radarr with the prepared endpoint
            HttpResponseString response = await _radarrHttp.RadarrHttpGet(endpoint, tmdbId.Value);

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
