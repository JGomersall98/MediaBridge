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

            string fullUrl = "";
            string httpResponse = "";
            fullUrl = await BuildSearchRequest(media, query, false);
            httpResponse = await _httpClientService.GetStringAsync(fullUrl);
            MbListSearchResult searchResult = null;
            searchResult = JsonSerializer.Deserialize<MbListSearchResult>(httpResponse, _jsonOptions);

            if (searchResult!.SearchResult!.Count == 0)
            {
                fullUrl = await BuildSearchRequest(media, query, true);
                httpResponse = await _httpClientService.GetStringAsync(fullUrl);
                searchResult = JsonSerializer.Deserialize<MbListSearchResult>(httpResponse, _jsonOptions);
            }

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
                await AddMovieAvailability("movie", mediaItems);
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
        private async Task AddMovieAvailability(string mediaType, List<MediaItem> mediaItems)
        {
            string url = await _config.GetConfigValueAsync("tmdb_release_date_endpoint");
            string apiKey = await _config.GetConfigValueAsync("tmdb_api_key");
            
            foreach (var mediaItem in mediaItems)
            {
                string releaseDateUrl = BuildTmdbReleaseDateUrl(mediaType, mediaItem.TmdbId, apiKey!);

                var releaseDates = await _httpClientService.GetStringAsync(releaseDateUrl);
                TmdbIdResponse tmdbIdResponse = JsonSerializer.Deserialize<TmdbIdResponse>(releaseDates, _jsonOptions);

                if (tmdbIdResponse?.ResultsPerRegion == null)
                {
                    mediaItem.MediaAvailability = new MediaAvailability
                    {
                        IsAvailable = false,
                        NotReleased = true
                    };
                    continue;
                }

                List<ResultsPerRegion> releaseData = tmdbIdResponse.ResultsPerRegion;

                if (releaseData.Count == 0)
                {
                    mediaItem.MediaAvailability = new MediaAvailability
                    {
                        IsAvailable = false,
                        NotReleased = true
                    };
                    continue;
                }

               if(releaseData.Any(rpr => rpr.ReleaseData != null && rpr.ReleaseData!
                    .Any(rd => rd.Type == TmdbReleaseType.Digital || rd.Type == TmdbReleaseType.Physical || rd.Type == TmdbReleaseType.TV)))
                {
                    mediaItem.MediaAvailability = new MediaAvailability
                    {
                        IsAvailable = true,
                        NotReleased = false,
                        English = IsEnglish(releaseData),
                    };
                }
                else
                {
                    // If the media was released more than a year ago, it has probably been released
                    bool notReleased = true;
                    if (mediaItem.ReleaseYear < DateTime.UtcNow.Year - 1)
                    {
                        notReleased = false;
                    }

                    mediaItem.MediaAvailability = new MediaAvailability
                    {
                        IsAvailable = false,
                        NotReleased = notReleased,
                        English = IsEnglish(releaseData),
                    };
                    
                }
            }

            // Sort list by English first, then availability
            mediaItems.Sort((x, y) =>
            {
                // First sort by English (English content comes first)
                int englishComparison = (y.MediaAvailability?.English ?? false).CompareTo(x.MediaAvailability?.English ?? false);
                if (englishComparison != 0)
                {
                    return englishComparison;
                }
                // Then sort by availability (available content comes first)
                return (y.MediaAvailability?.IsAvailable ?? false).CompareTo(x.MediaAvailability?.IsAvailable ?? false);
            });
        }
        private bool IsEnglish(List<ResultsPerRegion> releaseData)
        {
            List<string>? countryCodes = releaseData
                .Where(rpr => rpr.CountryCode != null)
                .Select(rpr => rpr.CountryCode!)
                .ToList();

            if (countryCodes.Contains("US") || countryCodes.Contains("GB") || countryCodes.Contains("CA") || countryCodes.Contains("AU"))
            {
                return true;
            }
            return false;
        }
        private string BuildTmdbReleaseDateUrl(string mediaType, int tmdbId, string apiKey)
        {
            return $"https://api.themoviedb.org/3/{mediaType}/{tmdbId}/release_dates?api_key={apiKey}";
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
                if (existingShows.Any(es => es.ImdbId == show.ImdbId))
                {
                    List<DownloadedShows> existingSeasons = existingShows
                        .Where(es => es.ImdbId == show.ImdbId && es.Type == "season")
                        .ToList();

                    foreach (var season in show.Seasons!)
                    {
                        if (existingSeasons.Any(es => es.SeasonNumber == season.SeasonNumber))
                        {
                            DownloadedShows existingSeason = existingSeasons
                                .Where(es => es.ImdbId == show.ImdbId && es.SeasonNumber == season.SeasonNumber)
                                .FirstOrDefault();

                            if (existingSeason!.EpisodesDownloaded == existingSeason.EpisodesInSeason)
                            {
                                // Season is downloaded in full
                                season.HasFile = true;
                            }
                            else if(existingSeason.EpisodesDownloaded! <= 0)
                            {
                                // Season exists but no episodes are downloaded
                                season.HasFile = false;
                                season.HasPartFile = false;
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

                // Part media check
                if (show.Seasons != null)
                {
                    show.HasPartMedia = show.Seasons.Any(s => s.HasFile == true || s.HasPartFile == true);
                }
                else
                {
                    show.HasPartMedia = false;
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
                        TvdbId = search.Ids.TvdbId,
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
            HttpResponseString httpResponseString = await _httpClientService.PostAsync(fullUrl, payload);
            return httpResponseString.Response;
        }
        private async Task<string> BuildSearchRequest(string media, string query, bool fuzzySearch)
        {
            // Get Endpoint
            string configUrl = await _config.GetConfigValueAsync(SEARCH_ENDPOINT_KEY);

            if (string.IsNullOrEmpty(configUrl))
            {
                return string.Empty;
            }

            string apiUrl = "";
            if (!fuzzySearch)
            {
                // Build apiUrl
                apiUrl = configUrl
                    .Replace("{mediaType}", media)
                    .Replace("{searchQuery}", query);
            }
            else
            {
                apiUrl = configUrl
                    .Replace("\"{searchQuery}\"", "{searchQuery}")
                    .Replace("{mediaType}", media)
                    .Replace("{searchQuery}", query);
            }


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
        [JsonPropertyName("tvdbId")]
        public int? TvdbId { get; set; }
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
    public class TmdbIdResponse
    {
        [JsonPropertyName("results")]
        public List<ResultsPerRegion>? ResultsPerRegion { get; set; }
    }
    public class ResultsPerRegion
    {
        [JsonPropertyName("iso_3166_1")]
        public string? CountryCode  { get; set; }
        [JsonPropertyName("release_dates")]
        public List<TmdbReleaseDates>? ReleaseData { get; set; }
    }
    public class TmdbReleaseDates
    {
        [JsonPropertyName("type")]
        public TmdbReleaseType Type { get; set; }
    }
    public enum TmdbReleaseType
    {
        // Premiere - Initial showing/premiere event
        Premiere = 1,
        // Theatrical (limited) - Limited theatrical release
        TheatricalLimited = 2,
        // Theatrical - Wide theatrical release
        Theatrical = 3,
        // Digital (VOD / streaming) - Digital/streaming release
        Digital = 4,
        // Physical (Blu-ray/DVD) - Physical media release
        Physical = 5,
        // TV
        TV	= 6
    }
}

