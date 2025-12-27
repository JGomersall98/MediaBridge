using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Dashboard;
using MediaBridge.Models.Search;
using MediaBridge.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Services;

namespace MediaBridge.Services.Media
{
    public class SearchService : ISearchService
    {
        private readonly IGetConfig _config;
        private readonly IHttpClientService _httpClientService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IUtilService _utilService;
        private readonly MediaBridgeDbContext _db;
        private string? _apiKey;
        private const string SEARCH_ENDPOINT_KEY = "mdblist_search_endpont";
        private const string INFO_ENDPOINT_KEY = "mdblist_info_endpoint";

        public SearchService(IGetConfig config, IHttpClientService httpClientService, 
            IUtilService utilService, MediaBridgeDbContext db)
        {
            _config = config;
            _httpClientService = httpClientService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _utilService = utilService;
            _db = db;
        }

        public async Task<MdbListMediaSearchResponse> MdbListMediaSearch(string media, string query)
        {
            MdbListMediaSearchResponse response = new MdbListMediaSearchResponse();

            string fullUrl = await BuildSearchRequest(media, query);
            string httpResponse = await _httpClientService.GetStringAsync(fullUrl);
            MbListSearchResult searchResult = JsonSerializer.Deserialize<MbListSearchResult>(httpResponse, _jsonOptions);

            List<int> traktIds = searchResult?.SearchResult?
                    .Where(s => s.Ids != null)
                    .Select(s => s.Ids!.Traktid)
                    .ToList();

            if (traktIds?.Count < 1 || traktIds == null)
            {
                response.IsSuccess = false;
                response.Reason = "Cannot get media info, failed to extract traktIds";
                return response;
            }

            string mediaInfoResponse = await GetMediaInfo(traktIds, media, "trakt");

            List<MediaItem> mediaItems = new List<MediaItem>();

            if (searchResult == null)
            {
                response.IsSuccess = false;
                response.Reason = "No media found";
                return response;
            }

            if (media == "movie")
            {
                var movieInfoList = JsonSerializer.Deserialize<List<MediaMovieInfo>>(mediaInfoResponse, _jsonOptions);
                mediaItems = BuildMediaItemList(searchResult, movieInfoList);
                await AlreadyExistingMovies(mediaItems);
            }
            else if (media == "show")
            {
                var showInfoList = JsonSerializer.Deserialize<List<MediaShowInfo>>(mediaInfoResponse, _jsonOptions);
                mediaItems = BuildMediaItemList(searchResult, showInfoList);
                await AlreadyExistingShows(mediaItems);
            }

            response.Media = mediaItems;
            response.IsSuccess = true;
            return response;
        }
        private async Task<List<MediaItem>> AlreadyExistingMovies(List<MediaItem> mediaItems)
        {
            List<DownloadedMovies> existingMovies = await _db.DownloadedMovies.AsNoTracking().ToListAsync();          

            foreach (var movie in mediaItems)
            {
                if (existingMovies.Any(em => em.TmdbId == movie.TmdbId))
                {
                    movie.HasMedia = true;
                }
                else
                {
                    movie.HasMedia = false;
                }
            }
            return mediaItems;
        }
        private async Task<List<MediaItem>> AlreadyExistingShows(List<MediaItem> mediaItems)
        {
            List<DownloadedShows> existingShows = await _db.DownloadedShows.AsNoTracking().ToListAsync();

            foreach (var show in mediaItems)
            {
                if(existingShows.Any(es => es.ImdbId == show.ImdbId))
                {
                    List<DownloadedShows> existingSeasons = existingShows
                        .Where(es => es.ImdbId == show.ImdbId && es.Type == "season")
                        .ToList();

                    foreach (var season in show.Seasons!)
                    {
                        if(existingSeasons.Any(es => es.SeasonNumber == season.SeasonNumber))
                        {
                            DownloadedShows existingSeason = existingSeasons
                                .Where(es => es.ImdbId == show.ImdbId && es.SeasonNumber == season.SeasonNumber)
                                .FirstOrDefault();

                            if(existingSeason!.EpisodesDownloaded == existingSeason.EpisodesInSeason)
                            {
                                // Season is downloaded in full
                                season.HasFile = true;
                            }
                            else
                            {
                                // Season exists but is not downloaded in full
                                season.HasPartFile = true;
                            }
                        }
                        else
                        {
                            // Season does not exist in the db
                            season.HasFile = false;
                        }
                    }
                }
                // Mark HasMedia as true if all episodes of all non-special seasons are downloaded
                if (show.Seasons != null)
                {
                    // Exclude season number 0 (Specials)
                    var seasonsToCheck = show.Seasons.Where(s => s.SeasonNumber != 0).ToList();

                    // If there are no non-special seasons, treat as not having full media
                    show.HasMedia = seasonsToCheck.Any() && seasonsToCheck.All(s => s.HasFile == true);
                }
                else
                {
                    show.HasMedia = false;
                }
            }
            return mediaItems;
        }
        public List<MediaItem> BuildMediaItemList<T>(MbListSearchResult searchResult, List<T>? infoList)
        {
            var mediaItems = new List<MediaItem>();

            if (infoList == null || searchResult?.SearchResult == null) return mediaItems;

            foreach (var search in searchResult.SearchResult)
            {
                if (search?.Ids == null) continue;

                var searchTraktId = search.Ids.Traktid;

                foreach (var info in infoList)
                {
                    // Use dynamic to access common properties regardless of type
                    dynamic dynamicInfo = info;

                    if (dynamicInfo == null)
                        continue;

                    var infoIds = dynamicInfo.InfoIds;
                    if (infoIds == null)
                        continue;

                    if (searchTraktId != (int)infoIds.TraktId)
                        continue;

                    // Handle seasons (only exists on shows)
                    List<MediaSeasonItem>? mediaSeasonItems = null;
                    var seasons = GetPropertyValue(dynamicInfo, "Seasons") as List<MediaShowInfoSeasons>;
                    if (seasons != null)
                    {
                        mediaSeasonItems = new List<MediaSeasonItem>();
                        foreach (var season in seasons)
                        {
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
                    }

                    // Handle runtime (only exists on movies)
                    string? runtime = null;
                    var runTimeValue = GetPropertyValue(dynamicInfo, "RunTime") as int?;
                    if (runTimeValue.HasValue)
                    {
                        runtime = _utilService.CalculateRunTimeHours(runTimeValue);
                    }

                    // Get rating value from MediaMovieInfo where source is "imdb"
                    double imbdRating = 0;
                    foreach (var rating in dynamicInfo.Ratings)
                    {
                        if (rating.Source == "imdb")
                        {
                            imbdRating = rating.Value;
                            break; // Exit loop once we find the IMDB rating
                        }
                    }

                    var mediaItem = new MediaItem
                    {
                        Id = (int)dynamicInfo.Id,
                        Genre = ((List<Genre>?)dynamicInfo.Genres)?.Select(g => g.Title).Where(t => t != null).Cast<string>().ToList(),
                        Title = search.Title ?? string.Empty,
                        Poster = (string?)dynamicInfo.Poster,
                        ImdbId = search.Ids.Imdbid,
                        TmdbId = search.Ids.TmdbId,
                        ReleaseYear = (int?)dynamicInfo.Year,
                        Description = (string?)dynamicInfo.Description,
                        Runtime = runtime,
                        Seasons = mediaSeasonItems,
                        ImbdRating = imbdRating
                    };
                    mediaItems.Add(mediaItem);
                }
            }
            return mediaItems;
        }

        private static object? GetPropertyValue(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
        }
        public async Task<string> GetMediaInfo<T>(List<T> ids, string mediaType, string idkey)
        {
            MbdListMovieInfoResponse mbdListInfoResponse = new MbdListMovieInfoResponse();

            // Get Endpoint
            string configUrl = await _config.GetConfigValueAsync(INFO_ENDPOINT_KEY);

            if (string.IsNullOrEmpty(configUrl))
            {
                return "";
            }

            string apiUrl = configUrl
                .Replace("{mediaType}", mediaType)
                .Replace("{idKey}", idkey);

            if (string.IsNullOrEmpty(_apiKey))
            {
                await SetMdbListApiKeyAsync();
            }

            string fullUrl = apiUrl + _apiKey;

            string payload = JsonSerializer.Serialize(new
            {
                ids = ids,
            });
            HttpResponseString httpResponseString = await _httpClientService.PostStringAsync(fullUrl, payload);
            return httpResponseString.Response;
        }
        private async Task<string> BuildSearchRequest(string media, string query)
        {
            // Get Endpoint
            string configUrl = await _config.GetConfigValueAsync(SEARCH_ENDPOINT_KEY);

            if (string.IsNullOrEmpty(configUrl))
            {
                return string.Empty;
            }

            // Build apiUrl
            string apiUrl = configUrl
                .Replace("{mediaType}", media)
                .Replace("{searchQuery}", query);

            if (string.IsNullOrEmpty(_apiKey))
            {
                await SetMdbListApiKeyAsync();
            }

            string fullUrl = apiUrl + _apiKey;

            return fullUrl;
        }
        private async Task SetMdbListApiKeyAsync()
        {
            var apiKey = await _config.GetConfigValueAsync("mdblist_api_key");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("mdblist_api_key not found in configuration.");
            }
            _apiKey = apiKey;
        }

    }
    public class MbListSearchResult
    {
        [JsonPropertyName("search")]
        public List<SearchResult>? SearchResult { get; set; }
    }
    public class SearchResult
    {
        public string? Title { get; set; }
        public int Year { get; set; }
        public int Score_Average { get; set; }
        public Ids? Ids { get; set; }
    }

    public class Ids
    {
        public string? Imdbid { get; set; }
        public int Traktid { get; set; }
        [JsonPropertyName("tmdbid")]
        public int TmdbId { get; set; }
    }
    public class MbdListInfoRequest
    {
        public required List<int> ids { get; set; }
    }
    public class MbdListMovieInfoResponse
    {
        public List<MediaMovieInfo>? MediaInfo { get; set; }
    }
    public class MediaMovieInfo
    {
        public int Id { get; set; }
        public List<Genre>? Genres { get; set; }
        public string? Poster { get; set; }
        public int? RunTime { get; set; }
        public int Year { get; set; }
        [JsonPropertyName("released")]
        public string? ReleaseDateString { get; set; }
        [JsonPropertyName("ids")]
        public InfoId? InfoIds { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("ratings")]
        public List<Rating> Ratings { get; set; }
    }
    public class InfoId
    {
        [JsonPropertyName("trakt")]
        public int TraktId { get; set; }
        [JsonPropertyName("imdb")]
        public string? ImdbId { get; set; }
        [JsonPropertyName("tmdb")]
        public int TmdbId { get; set; }
    }
    public class Rating
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }
        [JsonPropertyName("value")]
        public double? Value { get; set; }
    }
    public class Genre
    {
        public string? Title { get; set; }
    }
    public class MbdListShowInfoResponse
    {
        public List<MediaShowInfo>? ShowInfo { get; set; }
    }
    public class MediaShowInfo
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Year { get; set; }
        public string? Poster { get; set; }
        public List<Genre>? Genres { get; set; }
        [JsonPropertyName("ids")]
        public InfoId? InfoIds { get; set; }
        public List<MediaShowInfoSeasons>? Seasons { get; set; }
        [JsonPropertyName("ratings")]
        public List<Rating> Ratings { get; set; } = new List<Rating>();
    }
    public class MediaShowInfoSeasons
    {
        [JsonPropertyName("tmdbid")]
        public int TmdbId { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("air_date")]
        public string? AirDate { get; set; }
        [JsonPropertyName("season_number")]
        public int SeasonNumber { get; set; }
        [JsonPropertyName("episode_count")]
        public int EpisodeCount { get; set; }
    }
}

