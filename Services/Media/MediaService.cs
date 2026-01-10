using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models;
using MediaBridge.Models.Media.ExternalServices.Radarr;
using MediaBridge.Models.Media.ExternalServices.Sonarr;
using MediaBridge.Services.Helpers;
using MediaBridge.Services.Media.Downloads;
using MediaBridge.Services.Media.ExternalServices.Radarr;
using MediaBridge.Services.Media.ExternalServices.Sonarr;

namespace MediaBridge.Services.Media
{
    public class MediaService : IMediaService
    {
        private readonly IGetConfig _config;
        private readonly IHttpClientService _httpClientService;
        private string? _sonarrApiKey;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly MediaBridgeDbContext _db;
        private readonly IRadarrService _radarrService;
        private readonly ISonarrService _sonarrService;
        public MediaService(IGetConfig config, IHttpClientService httpClientService, MediaBridgeDbContext db, IRadarrService radarrService, ISonarrService sonarrService)
        {
            _config = config;
            _httpClientService = httpClientService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _db = db;
            _radarrService = radarrService;
            _sonarrService = sonarrService;
        }
        public async Task<StandardResponse> DownloadMedia(int mediaId, int userId, string username, int[]? seasonsRequested, string mediaType)
        {
            StandardResponse response = new StandardResponse();

            try
            {
                if (mediaType == "movie")
                {
                    // Get Movie Details from Radarr
                    MovieDetailsResponse movieDetails = await _radarrService.GetMovieDetails(mediaId);

                    // Send movie request to Radarr
                    response.IsSuccess = await _radarrService.SendMovieRequest(movieDetails.Title, movieDetails.TmdbId);

                    await ProcessMediaRequestResult(userId, username, movieDetails, null, response.IsSuccess);
                }
                else if (mediaType == "show")
                {
                    if (seasonsRequested == null)
                    {
                        response.IsSuccess = false;
                        response.Reason = "seasonsRequested must be provided when mediaType is 'show'.";
                        return response;
                    }

                    // Get Show Details from Sonarr
                    ShowDetailsResponse showDetails = await _sonarrService.GetShowDetails(mediaId);

                    // Send show request to Sonarr
                    response.IsSuccess = await _sonarrService.SendShowRequest(showDetails!.TvdbId, showDetails.Title!, seasonsRequested);

                    await ProcessMediaRequestResult(userId, username, null, showDetails, response.IsSuccess);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions during request sending
                response.IsSuccess = false;
                response.Reason = $"Exception occurred while sending {mediaType} request: {ex.Message}";

                if(mediaType == "movie")
                    await LogMediaRequest(userId, username, mediaId, null, mediaType, "Unknown Title", false, ex.Message);
                else if(mediaType == "show")
                    await LogMediaRequest(userId, username, null, mediaId, mediaType, "Unknown Title", false, ex.Message);

                return response;
            }

            // Handle unsuccessful response
            if (!response.IsSuccess)
            {
                response.IsSuccess = false;
                response.Reason = $"Failed sending {mediaType} request: Unknown error.";
            }

            return response;
        }    
        private async Task ProcessMediaRequestResult(int userId, string username, MovieDetailsResponse? movieDetails, ShowDetailsResponse? showDetails, bool success)
        {           
            if(movieDetails != null)
            {
                // Process Movie Request
                await LogMediaRequest(userId, username, movieDetails.TmdbId, null, "movie", movieDetails.Title!, true, null);
                if (success)
                {
                    await AddToDownloadRequests(movieDetails, null, userId);
                }
            }
            else if (showDetails != null)
            {
                // Process Show Request
                await LogMediaRequest(userId, username, null, showDetails.TvdbId, "show", showDetails.Title!, true, null);
                if (success)
                {
                    await AddToDownloadRequests(null, showDetails, userId);
                }
            }
        }
        public async Task<StandardResponse> PartialSeriesDownload(int tvdbId, int userId, string username, int[] seasonsRequested)
        {

            string configUrl = await _config.GetConfigValueAsync("sonarr_get_show_endpoint");

            await SetSonarrApiKeyAsync();

            if (!string.IsNullOrEmpty(configUrl))
            {
                configUrl = configUrl.Replace("{id}", tvdbId.ToString());
                configUrl = configUrl.Replace("{apiKey}", _sonarrApiKey);
            }

            string response = await _httpClientService.GetStringAsync(configUrl);

            List<SonarrShowDetails>? showDetails = JsonSerializer.Deserialize<List<SonarrShowDetails>>(response, _jsonOptions);

            if (showDetails == null || !showDetails.Any())
            {
                StandardResponse errorResponse = new StandardResponse
                {
                    IsSuccess = false,
                    Reason = "Show not found in Sonarr."
                };
                return errorResponse;
            }

            SonarrShowDetails show = showDetails.First();

            List<SonarrEpisode> episodeDetails = await GetEpisodeInfoFromShow(show.SonarrId);
            if(!episodeDetails.Any())
            {
                StandardResponse errorResponse = new StandardResponse
                {
                    IsSuccess = false,
                    Reason = "No episodes found for the show in Sonarr."
                };
                return errorResponse;
            }

            foreach (var season in show.Seasons!)
            {
                if (seasonsRequested.Contains(season.SeasonNumber!.Value))
                {
                    season.Monitored = true;

                    // Trigger Episode Search
                    var episodesInSeason = episodeDetails
                        .Where(e => e.SeasonNumber == season.SeasonNumber)
                        .Select(e => e.Id)
                        .ToList();

                    if (episodesInSeason.Any())
                    {
                        string episodeSearchUrl = await _config.GetConfigValueAsync("sonarr_command_endpoint");
                        string url = episodeSearchUrl + _sonarrApiKey;

                        var payload = new
                        {
                            name = "EpisodeSearch",
                            episodeIds = episodesInSeason
                        };

                        string payloadJson = JsonSerializer.Serialize(payload);
                        HttpResponseString episodeSearchResponse = await _httpClientService.PostAsync(url, payloadJson);

                        if (!episodeSearchResponse.IsSuccess)
                        {
                            StandardResponse errorResponse = new StandardResponse
                            {
                                IsSuccess = false,
                                Reason = "Failed to trigger episode search."
                            };
                            return errorResponse;
                        }

                        await LogMediaRequest(userId, username, null, show.TvdbId, "show", show.Title!, true, null);

                        List<DownloadRequests> existingDownload = _db.DownloadRequests
                            .Where(dr => dr.MediaType == "show" && dr.TvdbId == show.TvdbId)
                            .ToList();

                        // if no existing download request, add one
                        if (!existingDownload.Any())
                        {
                            await AddToDownloadRequests(null, new ShowDetailsResponse
                            {
                                TvdbId = show.TvdbId,
                                Title = show.Title,
                                Description = show.Description,
                                PosterUrl = show.PosterUrl,
                                ReleaseYear = show.ReleaseYear
                            }, userId);
                            //await AddToDownloadRequests(new RSGetMediaResponse
                            //{
                            //    MediaId = show.SonarrId!.Value,
                            //    TvdbId = show.TvdbId,
                            //    Title = show.Title,
                            //    Description = show.Description,
                            //    PosterUrl = show.PosterUrl,
                            //    ReleaseYear = show.ReleaseYear
                            //}, "show", userId);
                        }
                    }
                }
            }
            return new StandardResponse
            {
                IsSuccess = true
            };
        }

        private async Task<List<SonarrEpisode>> GetEpisodeInfoFromShow(int? sonarrId)
        {
            if (sonarrId == null)
            {
                return new List<SonarrEpisode>();
            }
            string apiUrl = await BuildSonarrEpisodeDataUrl(sonarrId.ToString()!);

            string response = await _httpClientService.GetStringAsync(apiUrl);

            List<SonarrEpisode>? sonarrEpisodes = JsonSerializer.Deserialize<List<SonarrEpisode>>(response, _jsonOptions);

            return sonarrEpisodes!;
        }
        private async Task<string> BuildSonarrEpisodeDataUrl(string seriesId)
        {
            string baseUrl = await _config.GetConfigValueAsync("sonarr_episode_data_endpoint");
            await SetSonarrApiKeyAsync();
            return baseUrl!.Replace("{ApiKey}", _sonarrApiKey!).Replace("{seriesId}", seriesId);
        }
        private async Task AddToDownloadRequests(MovieDetailsResponse? movieDetails, ShowDetailsResponse? showDetails, int userId)
        {

            DownloadRequests downloadRequest = new DownloadRequests
            {
                UserId = userId,
                Status = "queued",
                DownloadPercentage = 0,
                RequestedAt = DateTime.UtcNow
            };

            if (movieDetails != null)
            {
                downloadRequest.TmdbId = movieDetails.TmdbId;
                downloadRequest.MediaType = "movie";
                downloadRequest.Title = movieDetails.Title;
                downloadRequest.Description = movieDetails.Description;
                downloadRequest.PosterUrl = movieDetails.PosterUrl;
                downloadRequest.ReleaseYear = movieDetails.ReleaseYear;
            }
            else if (showDetails != null)
            {
                downloadRequest.TvdbId = showDetails.TvdbId;
                downloadRequest.MediaType = "show";
                downloadRequest.Title = showDetails.Title;
                downloadRequest.Description = showDetails.Description;
                downloadRequest.PosterUrl = showDetails.PosterUrl;
                downloadRequest.ReleaseYear = showDetails.ReleaseYear;
            }

            _db.DownloadRequests.Add(downloadRequest);
            await _db.SaveChangesAsync();
        }
        private async Task LogMediaRequest(int userId, string username, int? tmdbId, int? tvdbId, string mediaType, string mediaTitle, bool isSuccessful, string? errorMessage)
        {
            try
            {
                var logEntry = new MediaRequestLog
                {
                    UserId = userId,
                    Username = username,
                    MediaType = mediaType,
                    MediaTitle = mediaTitle,
                    IsSuccessful = isSuccessful,
                    ErrorMessage = errorMessage,
                    RequestedAt = DateTime.UtcNow
                };
                if (mediaType == "movie")
                {
                    logEntry.TmdbId = tmdbId;
                }
                else if (mediaType == "show")
                {
                    logEntry.TvdbId = tvdbId;
                }

                _db.MediaRequestLogs.Add(logEntry);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log media request: {ex.Message}");
            }
        }      
        private async Task SetSonarrApiKeyAsync()
        {
            if (string.IsNullOrEmpty(_sonarrApiKey))
            {
                var apiKey = await _config.GetConfigValueAsync("sonarr_api_key");
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("sonarr_api_key not found in configuration.");
                }
                _sonarrApiKey = apiKey;
            }
        }
    }
    public class Season
    {
        public int? SeasonNumber { get; set; }
        public bool? Monitored { get; set; }
    }
    public class SonarrShowDetails
    {
        [JsonPropertyName("id")]
        public int? SonarrId { get; set; }
        [JsonPropertyName("tvdbId")]
        public int TvdbId { get; set; }
        public string? Title { get; set; }
        public List<Season>? Seasons { get; set; }
        [JsonPropertyName("overview")]
        public string? Description { get; set; }
        [JsonPropertyName("remotePoster")]
        public string? PosterUrl { get; set; }
        [JsonPropertyName("year")]
        public int? ReleaseYear { get; set; }
    }
}