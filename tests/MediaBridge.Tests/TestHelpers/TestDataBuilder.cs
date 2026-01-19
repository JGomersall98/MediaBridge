using MediaBridge.Models.Media.ExternalServices.Radarr;
using MediaBridge.Models.Media.ExternalServices.Sonarr;
using MediaBridge.Services.Helpers;

namespace MediaBridge.Tests.TestHelpers
{
    public static class TestDataBuilder
    {
        public static class MovieResponses
        {
            public static string GetMovieDetailsSuccess => @"[
            {
                ""title"": ""Test Movie"",
                ""tmdbId"": 12345,
                ""overview"": ""A test movie for integration testing"",
                ""remotePoster"": ""https://example.com/poster.jpg"",
                ""year"": 2023
            }]";

            public static string AddMovieSuccess => @"{
                ""id"": 1,
                ""title"": ""Test Movie"",
                ""tmdbId"": 12345,
                ""monitored"": true,
                ""status"": ""announced""
            }";

            public static MovieDetailsResponse GetMovieDetailsObject => new()
            {
                Title = "Test Movie",
                TmdbId = 12345,
                Description = "A test movie for integration testing",
                PosterUrl = "https://example.com/poster.jpg",
                ReleaseYear = 2023
            };
        }

        public static class ShowResponses
        {
            public static string GetShowDetailsSuccess => @"[
            {
                ""title"": ""Test Show"",
                ""tvdbId"": 67890,
                ""overview"": ""A test TV show for integration testing"",
                ""remotePoster"": ""https://example.com/show-poster.jpg"",
                ""year"": 2022
            }]";

            public static string AddShowSuccess => @"{
                ""id"": 1,
                ""title"": ""Test Show"",
                ""tvdbId"": 67890,
                ""monitored"": true,
                ""status"": ""continuing""
            }";

            public static ShowDetailsResponse GetShowDetailsObject => new()
            {
                Title = "Test Show",
                TvdbId = 67890,
                Description = "A test TV show for integration testing",
                PosterUrl = "https://example.com/show-poster.jpg",
                ReleaseYear = 2022
            };
        }

        public static class HttpResponses
        {
            public static HttpResponseString SuccessResponse(string content) => new()
            {
                IsSuccess = true,
                Response = content
            };

            public static HttpResponseString FailureResponse(string error = "Request failed") => new()
            {
                IsSuccess = false,
                Response = error
            };
        }

        public static class TestConstants
        {
            public const int TestUserId = 1;
            public const string TestUsername = "testuser";
            public const int TestMovieTmdbId = 12345;
            public const int TestShowTvdbId = 67890;
            public const string TestMovieTitle = "Test Movie";
            public const string TestShowTitle = "Test Show";
            public static readonly int[] TestSeasons = [1, 2];

            public const string RadarrBaseUrl = "https://radarr.local";
            public const string SonarrBaseUrl = "https://sonarr.local";
            public const string TestRadarrApiKey = "test-radarr-key";
            public const string TestSonarrApiKey = "test-sonarr-key";

            public const string RadarrGetMovieEndpoint = "/api/v3/movie?tmdbId={id}&apikey={ApiKey}";
            public const string RadarrAddMovieEndpoint = "/api/v3/movie?apikey={ApiKey}";
            public const string SonarrGetSeriesEndpoint = "/api/v3/series?tvdbId={id}&apikey={ApiKey}";
            public const string SonarrAddSeriesEndpoint = "/api/v3/series?apikey={ApiKey}";
        }
    }
}