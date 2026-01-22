using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Dashboard;
using MediaBridge.Services.Helpers;
using MediaBridge.Services.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Services;

namespace MediaBridge.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly MediaBridgeDbContext _db;
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<DashboardService> _logger;
        private readonly ICaching _caching;
        private readonly IGetConfig _config;
        private readonly IUtilService _utilService;
        private readonly ISearchService _searchService;
        private readonly JsonSerializerOptions _jsonOptions;
        private const int CACHE_DURATION_HOURS = 12;
        private const string MOVIES_CACHE_KEY = "top_movies";
        private const string TVSHOWS_CACHE_KEY = "top_tvshows";
        private const string MOVIES_ENDPOINT_KEY = "mdblist_movies_endpoint";
        private const string TVSHOWS_ENDPOINT_KEY = "mdblist_tvshows_endpoint";
        private const string MEDIA_TMBDID_INFO = "media_tmbd_id_endpoint";


        public DashboardService(MediaBridgeDbContext db, ILogger<DashboardService> logger,
            ICaching caching, IHttpClientService httpClientService, IGetConfig config,
            IUtilService utilService, ISearchService searchService)
        {
            _db = db;
            _logger = logger;
            _caching = caching;
            _httpClientService = httpClientService;
            _config = config;
            _utilService = utilService;
            _searchService = searchService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<DashboardMoviesResponse> GetTopMoviesAsync()
        {
            var response = new DashboardMoviesResponse();

            try
            {
                var cachedData = await _caching.GetCachedDataAsync(MOVIES_CACHE_KEY);

                if (cachedData != null)
                {
                    return BuildDashboardMoviesResponse(cachedData, true);
                }

                response.IsSuccess = false;
                response.Reason = "Failed to fetch movie data";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top movies");
                response.IsSuccess = false;
                response.Reason = "Movie cache failed";
                return response;
            }
        }
        public async Task<DashboardTvShowsResponse> GetTopTvShowsAsync()
        {
            try
            {
                var cachedData = await _caching.GetCachedDataAsync(TVSHOWS_CACHE_KEY);

                if (cachedData != null)
                {
                    return BuildDashboardTvShowsResponse(cachedData, true);
                }

                var response = new DashboardTvShowsResponse();
                response.IsSuccess = false;
                response.Reason = "Failed to fetch TV show data";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top TV shows");
                var response = new DashboardTvShowsResponse();
                response.IsSuccess = false;
                response.Reason = "Tv show cache failed";
                return response;
            }
        }
        public async Task<MdbListApiResponse?> GetMoreTmdIdForMediaAsync(MdbListApiResponse mdbListApiResponse, string mediaType)
        {
            if (mediaType == "movie")
            {
                List<MediaMovieInfo> mediaMovieInfos = await GetTmdIdForMovieAsync(mdbListApiResponse, mediaType);
            }
            else if (mediaType == "show")
            {
                await GetTmdIdForTvShowAsync(mdbListApiResponse, mediaType); // Remove the assignment       
            }
            else
            {
                _logger.LogError("Invalid media type specified for Tmdb ID retrieval.");
                return mdbListApiResponse;
            }

            return mdbListApiResponse;
        }
        private async Task<List<MediaMovieInfo>> GetTmdIdForMovieAsync(MdbListApiResponse mdbListApiResponse, string mediaType)
        {
            // Get all the imdb from the movie items
            List<string> imdbIds = mdbListApiResponse.Movies.Select(m => m.ImdbId!).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            // Prepare the request payload
            var requestData = new { ids = imdbIds };

            // Serialize the request payload to JSON
            string jsonBody = JsonSerializer.Serialize(requestData, _jsonOptions);

            string movieUrl = await BuildTmbdIdInfoEndpoint(mediaType);

            // Make the POST request using the correct IHttpClientService method
            var httpResponse = await _httpClientService.PostStringAsync(movieUrl, jsonBody);

            // deserialize the response
            List<MediaMovieInfo> mediaMovieInfos = JsonSerializer.Deserialize<List<MediaMovieInfo>>(httpResponse.Response, _jsonOptions);

            if (mediaMovieInfos == null)
            {
                mediaMovieInfos = new List<MediaMovieInfo>();
                return mediaMovieInfos;
            }

            // add the tmbid into the original movie items
            foreach (var movie in mdbListApiResponse.Movies)
            {
                var info = mediaMovieInfos.FirstOrDefault(m => m.InfoIds!.ImdbId == movie.ImdbId);

                bool isReleased = await CheckReleased(info!.InfoIds!.TmdbId, movie.Title);
                if (!isReleased)
                {
                    continue;
                }

                if (info != null)
                {
                    movie.TmdbId = info.InfoIds!.TmdbId;
                    movie.ImbdRating = GetImdbRatingFromRatingsList(mediaMovieInfos, info);
                    movie.Description = info.Description;
                    movie.ReleaseYear = info.Year;
                    movie.Runtime = _utilService.CalculateRunTimeHours(info.RunTime);
                    movie.ReleaseDateString = info.ReleaseDateString;
                }
            }
            Console.WriteLine("Completed fetching Tmdb IDs for movies.");
            return mediaMovieInfos;
        }
        private async Task<bool> CheckReleased(int tmdbId, string title)
        {
            string url = await _config.GetConfigValueAsync("radarr_get_movie_endpoint");
            string apiKey = await _config.GetConfigValueAsync("radarr_api_key");

            string fullUrl = url.Replace("{id}", tmdbId.ToString()) + apiKey;

            var httpResponse = await _httpClientService.GetStringAsync(fullUrl);
            List<CheckReleasedResponse>? response = null;
            try
            {
                response = JsonSerializer.Deserialize<List<CheckReleasedResponse>>(httpResponse, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking release date for movie: {title} (TMDB ID: {tmdbId})");
                return false;
            }
            
            if (response != null && response.Count > 0)
            {
                var releaseInfo = response[0];
                if (releaseInfo.DigitalRelease.HasValue)
                {
                    return releaseInfo.DigitalRelease.Value <= DateTime.UtcNow;
                }
            }
            return false;
        }

        private async Task GetTmdIdForTvShowAsync(MdbListApiResponse mdbListApiResponse, string mediaType)
        {
            // Get all the imdb from the tv show items
            List<string> imdbIds = mdbListApiResponse.Shows.Select(m => m.ImdbId!).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            string response = await _searchService.GetMediaInfo(imdbIds, mediaType, "imdb");

            var showInfoList = JsonSerializer.Deserialize<List<MediaShowInfo>>(response, _jsonOptions);

            if (showInfoList != null)
            {
                // Use our new private method to build MediaItems
                var mediaItems = BuildTvShowMediaItems(mdbListApiResponse, showInfoList);

                // Update the original shows with the enriched data
                foreach (var show in mdbListApiResponse.Shows)
                {
                    var enrichedItem = mediaItems.FirstOrDefault(m => m.ImdbId == show.ImdbId);
                    if (enrichedItem != null)
                    {
                        show.TmdbId = enrichedItem.TmdbId;
                        show.ImbdRating = enrichedItem.ImbdRating;
                        show.Description = enrichedItem.Description;
                        show.ReleaseYear = enrichedItem.ReleaseYear;
                        show.Seasons = enrichedItem.Seasons;
                    }
                }

                Console.WriteLine("Completed fetching Tmdb IDs for TV shows.");
            }
        }
        private List<MediaItem> BuildTvShowMediaItems(MdbListApiResponse mdbListApiResponse, List<MediaShowInfo> showInfoList)
        {
            var mediaItems = new List<MediaItem>();

            if (showInfoList == null || mdbListApiResponse?.Shows == null)
                return mediaItems;

            foreach (var show in mdbListApiResponse.Shows)
            {
                if (string.IsNullOrEmpty(show.ImdbId))
                    continue;

                // Find the matching show info by IMDB ID
                var matchingShowInfo = showInfoList.FirstOrDefault(info =>
                    info.InfoIds?.ImdbId == show.ImdbId);

                if (matchingShowInfo == null)
                    continue;

                // Handle seasons - filter out seasons with 0 episodes
                List<MediaSeasonItem>? mediaSeasonItems = null;
                if (matchingShowInfo.Seasons != null && matchingShowInfo.Seasons.Any())
                {
                    mediaSeasonItems = new List<MediaSeasonItem>();
                    foreach (var season in matchingShowInfo.Seasons)
                    {
                        // Skip seasons with 0 episodes
                        if (season.EpisodeCount <= 0)
                            continue;

                        var mediaSeasonItem = new MediaSeasonItem
                        {
                            TmbdId = season.TmdbId,
                            SeasonNumber = season.SeasonNumber,
                            EpisodeCount = season.EpisodeCount,
                            Title = season.Name,
                            AirDate = season.AirDate?.Split('T')[0]
                        };
                        mediaSeasonItems.Add(mediaSeasonItem);
                    }

                    // If no seasons have episodes, set to null
                    if (!mediaSeasonItems.Any())
                    {
                        mediaSeasonItems = null;
                    }
                }

                // Get IMDB rating from ratings list
                double? imdbRating = null;
                if (matchingShowInfo.Ratings != null)
                {
                    var imdbRatingInfo = matchingShowInfo.Ratings.FirstOrDefault(r => r.Source == "imdb");
                    imdbRating = imdbRatingInfo?.Score;
                }

                // Build the MediaItem
                var mediaItem = new MediaItem
                {
                    Id = matchingShowInfo.Id,
                    Genre = matchingShowInfo.Genres?.Select(g => g.Title).Where(t => !string.IsNullOrEmpty(t)).ToList(),
                    Title = show.Title ?? string.Empty,
                    Poster = matchingShowInfo.Poster,
                    ImdbId = show.ImdbId,
                    TmdbId = matchingShowInfo.InfoIds?.TmdbId ?? 0,
                    ReleaseYear = matchingShowInfo.Year,
                    Description = matchingShowInfo.Description,
                    Runtime = null,
                    Seasons = mediaSeasonItems,
                    ImbdRating = imdbRating,
                    ReleaseDateString = null
                };

                mediaItems.Add(mediaItem);
            }

            return mediaItems;
        }
        private double? GetImdbRatingFromRatingsList(List<MediaMovieInfo> ratings, MediaMovieInfo info)
        {
            foreach (var rating in info.Ratings)
            {
                if (rating.Source == "imdb")
                {
                    return rating.Score;
                }
            }
            return null;
        }
        private async Task<string> BuildTmbdIdInfoEndpoint(string mediaType)
        {
            string apiUrl = await _config.GetConfigValueAsync(MEDIA_TMBDID_INFO);

            if (string.IsNullOrEmpty(apiUrl))
            {
                _logger.LogError("MDBList media TMBD ID endpoint is not configured.");
                return string.Empty;
            }

            var apiKey = await _config.GetConfigValueAsync("mdblist_api_key");

            if (mediaType == "movie")
            {
                apiUrl = apiUrl.Replace("{mediaType}", mediaType) + apiKey;
            }
            else if (mediaType == "show")
            {
                apiUrl = apiUrl.Replace("{mediaType}", mediaType) + apiKey;
            }
            return apiUrl;
        }
        private DashboardTvShowsResponse BuildDashboardTvShowsResponse(object data, bool isCachedResponse)
        {
            DashboardTvShowsResponse response = new DashboardTvShowsResponse();

            if (isCachedResponse && data is CachedData cachedData)
            {
                var tvData = JsonSerializer.Deserialize<MdbListApiResponse>(cachedData.JsonData, _jsonOptions);
                if (tvData != null)
                {
                    // Filter out shows with null seasons and sort by season count (highest first)
                    response.Shows = tvData.Shows
                        .Where(show => show.Seasons != null && show.Seasons.Any())
                        .ToList();
                    response.LastUpdated = cachedData.CachedAt;
                }
            }
            else if (!isCachedResponse && data is MdbListApiResponse freshData)
            {
                // Filter out shows with null seasons and sort by season count (highest first)
                response.Shows = freshData.Shows
                    .Where(show => show.Seasons != null && show.Seasons.Any())
                    .ToList();
                response.LastUpdated = DateTime.UtcNow;
            }

            response.FromCache = isCachedResponse;
            response.IsSuccess = true;
            return response;
        }
        private DashboardMoviesResponse BuildDashboardMoviesResponse(object data, bool isCachedResponse)
        {
            DashboardMoviesResponse response = new DashboardMoviesResponse();

            if (isCachedResponse && data is CachedData cachedData)
            {
                var movieData = JsonSerializer.Deserialize<MdbListApiResponse>(cachedData.JsonData, _jsonOptions);
                if (movieData != null)
                {
                    response.Movies = movieData.Movies;
                    response.LastUpdated = cachedData.CachedAt;
                }
            }
            else if (!isCachedResponse && data is MdbListApiResponse freshData)
            {
                response.Movies = freshData.Movies;
                response.LastUpdated = DateTime.UtcNow;
            }

            response.FromCache = isCachedResponse;
            response.IsSuccess = true;
            return response;
        }
        private async Task<MdbListApiResponse?> FetchMediaFromApiAsync(string? apiUrl, string mediaType)
        {
            var apiKey = await _config.GetConfigValueAsync("mdblist_api_key");

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Missing MDBList configuration");
                return null;
            }

            var fullUrl = $"{apiUrl}{apiKey}";

            var httpResponse = await _httpClientService.GetStringAsync(fullUrl);
            var deserializedResponse = JsonSerializer.Deserialize<MdbListApiResponse>(httpResponse, _jsonOptions);
            if (deserializedResponse == null)
            {
                _logger.LogError("Failed to deserialize MdbListApiResponse from API response.");
                return null;
            }
            MdbListApiResponse response = await GetMoreTmdIdForMediaAsync(deserializedResponse, mediaType);
            return response;
        }
        public List<MediaItem> FilterExistingMoviesOut(List<MediaItem> movieList)
        {
            List<DownloadedMovies> existingMovies = _db.DownloadedMovies.AsNoTracking().ToList();

            List<MediaItem> filteredMovies = new List<MediaItem>();

            foreach (var movie in movieList)
            {
                if (!existingMovies.Any(em => em.TmdbId == movie.TmdbId))
                {
                    filteredMovies.Add(movie);
                }
            }
            filteredMovies = RemoveUnreleased(filteredMovies);
            return filteredMovies;
        }
        private List<MediaItem> FilterMoviesMostRecent(List<MediaItem> movieList)
        {
            return movieList.OrderByDescending(m => m.ReleaseYear).Take(30).ToList();
        }
        private List<MediaItem> RemoveUnreleased(List<MediaItem> movieList)
        {
            List<MediaItem> moviesToRemove = new List<MediaItem>();
            foreach (var movie in movieList)
            {
                // Convert ReleaseDateString to DateTime for comparison
                if (DateTime.TryParse(movie.ReleaseDateString, out DateTime releaseDate))
                {
                    if (releaseDate > DateTime.UtcNow)
                    {
                        moviesToRemove.Add(movie);
                    }
                }
            }
            movieList.RemoveAll(m => moviesToRemove.Contains(m));
            return FilterMoviesMostRecent(movieList);
        }


        // Refresh cache methods called by a scheduled job
        public async Task RefreshCaches()
        {
            await RefreshTopMoviesCacheAsync();

            await RefreshTopShowsCacheAsync();

        }
        private async Task RefreshTopMoviesCacheAsync()
        {
            try
            {
                string apiUrl = await _config.GetConfigValueAsync(MOVIES_ENDPOINT_KEY);
                MdbListApiResponse freshData = await FetchMediaFromApiAsync(apiUrl, "movie");
                freshData!.Movies = FilterExistingMoviesOut(freshData.Movies);

                if (freshData != null)
                {
                    await _caching.CacheDataAsync(MOVIES_CACHE_KEY, JsonSerializer.Serialize(freshData, _jsonOptions), CACHE_DURATION_HOURS);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top movies");
            }
        }
        private async Task RefreshTopShowsCacheAsync()
        {
            try
            {
                string apiUrl = await _config.GetConfigValueAsync(TVSHOWS_ENDPOINT_KEY);

                var freshData = await FetchMediaFromApiAsync(apiUrl, "show");
                if (freshData != null)
                {
                    string jsonData = JsonSerializer.Serialize(freshData, _jsonOptions);
                    await _caching.CacheDataAsync(TVSHOWS_CACHE_KEY, jsonData, CACHE_DURATION_HOURS);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top TV shows");
            }
        }
    }
    public class CheckReleasedResponse
    {
        [JsonPropertyName("digitalRelease")]
        public DateTime? DigitalRelease { get; set; }
    }
}