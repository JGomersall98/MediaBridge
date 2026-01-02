using System;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Radarr;
using MediaBridge.Models.Sonarr;
using MediaBridge.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Services.Media.Downloads
{
    public class DownloadProcessorService : IDownloadProcessorService
    {
        private readonly MediaBridgeDbContext _db;
        private readonly IGetConfig _config;
        private readonly IHttpClientService _httpClientService;
        private string? _sonarrApiKey;
        private string? _radarrApiKey;

        public DownloadProcessorService(MediaBridgeDbContext db, IGetConfig config, IHttpClientService httpClientService)
        {
            _db = db;
            _config = config;
            _httpClientService = httpClientService;
        }
        public async Task ProcessRadarrQueue()
        {
            await PokeMediaDownloads(true);
            try
            {
                bool hasActiveDownloads = await _db.DownloadRequests
                    .AnyAsync(dr => (dr.MediaType == "movie") &&
                    (dr.Status == "queued" || dr.Status == "downloading" || dr.Status == "warning"));

                if (!hasActiveDownloads)
                {
                    Console.WriteLine("No active movie downloads found in database. Skipping Radarr queue check.");
                    return;
                }

                string queueApiUrl = await BuildRadarrQueueUrl();
                string queueResponse = await _httpClientService.GetStringAsync(queueApiUrl);
                RadarrQueueResponse queueData = System.Text.Json.JsonSerializer.Deserialize<RadarrQueueResponse>(queueResponse)!;

                if (queueData?.Records == null || !queueData.Records.Any())
                {
                    Console.WriteLine("No items found in Radarr queue.");
                    return;
                }


                // Check Torrent Health in QBittorent
                List<string> torrentsToDelete = await CheckTorrentHealth(queueData.Records
                    .Where(r => !string.IsNullOrEmpty(r.TorrentId))
                    .Select(r => r.TorrentId!)
                    .ToList());

                if (torrentsToDelete.Any())
                {
                    foreach (var torrentId in torrentsToDelete)
                    {
                        // get Id of Radarr queue item from torrentId
                        var queueItem = queueData.Records.FirstOrDefault(r =>
                            string.Equals(r.TorrentId, torrentId, StringComparison.OrdinalIgnoreCase));

                        if (queueItem != null)
                        {
                            await RemoveMediaItem(queueItem!.Id, true);

                            // Call post to search for movie again
                            string movieSearchUrl = await _config.GetConfigValueAsync("radarr_command_endpoint");
                            string url = movieSearchUrl + _radarrApiKey;

                            int movieId = queueItem.MovieId!.Value;

                            var payload = new
                            {
                                name = "MoviesSearch",
                                movieIds = new List<int> { movieId }
                            };

                            string payloadJson = JsonSerializer.Serialize(payload);
                            HttpResponseString movieSearchResponse = await _httpClientService.PostStringAsync(url, payloadJson);
                        }
                    }
                }

                Console.WriteLine($"Found {queueData.Records.Count} items in Radarr queue.");

                await ProcessRadarrQueueItems(queueData.Records);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Radarr queue: {ex.Message}");
            }
        }
        public async Task ProcessRadarrQueueItems(List<RadarrQueueRecord> queueRecords)
        {
            // Implementation for processing Radarr queue items would go here
            foreach (var record in queueRecords)
            {
                string movieDataUrl = await BuildRadarrMovieDataEndpoint(record.MovieId!.Value);
                string movieResponse = await _httpClientService.GetStringAsync(movieDataUrl);
                RadarrMovieId radarrMovieId = System.Text.Json.JsonSerializer.Deserialize<RadarrMovieId>(movieResponse)!;

                var downloadRequest = await _db.DownloadRequests
                    .Where(dr => dr.MediaType == "movie" && dr.TmdbId == radarrMovieId.TmbdId)
                    .FirstOrDefaultAsync();
                
                if (downloadRequest == null)
                {
                    Console.WriteLine("No download request found for movie.");
                    continue;
                }

                // Calculate download percentage
                int downloadPercentage = GetDownloadPercentage(record.Size, record.SizeLeft);      
                Console.WriteLine($"Download percentage for movie {record.MovieId}: {downloadPercentage}%");

                // Update the download request
                downloadRequest.Status = GetDownloadStatus(record.Status);
                downloadRequest.DownloadPercentage = downloadPercentage;
                downloadRequest.MinutesLeft = ParseTimeLeftToMinutes(record.TimeLeft!);
                downloadRequest.UpdatedAt = DateTime.UtcNow;
                downloadRequest.MediaId = record.MovieId;


                if (downloadRequest.Status == "completed" && !downloadRequest.CompletedAt.HasValue)
                {
                    downloadRequest.CompletedAt = DateTime.UtcNow;
                    await ScrapeRadarrMovies();
                }

                Console.WriteLine($"Updated movie {downloadRequest.Title}: {downloadPercentage}% - {downloadRequest.Status}");
            }
            await _db.SaveChangesAsync();
        }
        private async Task PokeMediaDownloads(bool isMovie)
        {
            string url = "";
            if(isMovie)
            {
                url = await _config.GetConfigValueAsync("radarr_command_endpoint");
                await SetRadarrApiKeyAsync();
                url = url + _radarrApiKey;
            }
            else
            {
                url = await _config.GetConfigValueAsync("sonarr_command_endpoint");
                await SetSonarrApiKeyAsync();
                url = url + _sonarrApiKey;
            }

            var payload = new
            {
                name = "RefreshMonitoredDownloads",
            };

            string payloadJson = JsonSerializer.Serialize(payload);
            await _httpClientService.PostStringAsync(url, payloadJson);         
        }
        public async Task ProcessStuckMedia()
        {
            List<DownloadRequests> stuckMovies = await _db.DownloadRequests
                .Where(dr => dr.MediaType == "movie" && dr.Status == "downloading" &&
                dr.UpdatedAt <= DateTime.UtcNow.AddMinutes(-2))
                .ToListAsync();

            if (stuckMovies.Any())
            {
                foreach (var movie in stuckMovies)
                {
                    movie.Status = "completed";
                    movie.DownloadPercentage = 100;
                    movie.MinutesLeft = 0;
                    movie.CompletedAt = DateTime.UtcNow;
                    movie.UpdatedAt = DateTime.UtcNow;
                    Console.WriteLine($"Marked movie '{movie.Title}' as completed due to inactivity.");
                }
                await ScrapeRadarrMovies();
            }

            // Get stuck episodes 
            List<DownloadRequests> stuckEpisodes = await _db.DownloadRequests
                .Where(dr => dr.MediaType == "Episode" && dr.Status == "downloading" &&
                dr.UpdatedAt <= DateTime.UtcNow.AddMinutes(-2))
                .ToListAsync();
            
            if (stuckEpisodes.Any())
            {
                foreach (var episode in stuckEpisodes)
                {
                    episode.Status = "completed";
                    episode.DownloadPercentage = 100;
                    episode.MinutesLeft = 0;
                    episode.CompletedAt = DateTime.UtcNow;
                    episode.UpdatedAt = DateTime.UtcNow;
                    Console.WriteLine($"Marked episode '{episode.Title}' as completed due to inactivity.");
                }
                await ScrapeSonarrShows();
            }

            List<DownloadRequests> downloadingShows = await _db.DownloadRequests
                .Where(dr => (dr.MediaType == "show") && dr.Status == "downloading")
                .ToListAsync();

            foreach (var show in downloadingShows)
            {
                List<DownloadRequests> showEpisodes = await _db.DownloadRequests
                    .Where(dr => dr.MediaType == "Episode" && dr.TvdbId == show.TvdbId)
                    .ToListAsync();

                show.MediaId = showEpisodes.FirstOrDefault()?.MediaId;

                bool allEpisodesCompleted = showEpisodes.All(e => e.Status == "completed");

                if (allEpisodesCompleted)
                {
                    show.Status = "completed";
                    show.DownloadPercentage = 100;
                    show.MinutesLeft = 0;
                    show.CompletedAt = DateTime.UtcNow;
                    show.UpdatedAt = DateTime.UtcNow;                
                    Console.WriteLine($"Marked show '{show.Title}' as completed since all episodes are completed.");
                }
            }

            await _db.SaveChangesAsync();
        }
        private async Task<string> BuildRadarrMovieDataEndpoint(int movieId)
        {
            string baseUrl = await _config.GetConfigValueAsync("radarr_movie_data_endpoint");
            await SetRadarrApiKeyAsync();
            string apiUrl = baseUrl!.Replace("{movieId}", movieId.ToString());
            return apiUrl + _radarrApiKey;
        }
        public async Task ProcessSonarrQueue()
        {
            await PokeMediaDownloads(false);
            try
            {
                bool hasActiveDownloads = await _db.DownloadRequests
                     .AnyAsync(dr => (dr.MediaType == "show" || dr.MediaType == "Episode") && 
                     (dr.Status == "queued" || dr.Status == "downloading"));


                if (!hasActiveDownloads)
                {
                    Console.WriteLine("No active shows downloads found in database.Skipping sonarr queue check.");
                    return;
                }

                string queueApiUrl = await BuildSonarrQueueUrl();
                string queueResponse = await _httpClientService.GetStringAsync(queueApiUrl);
                SonarrQueueResponse queueData = System.Text.Json.JsonSerializer.Deserialize<SonarrQueueResponse>(queueResponse)!;
             
                if (queueData?.Records == null || !queueData.Records.Any())
                {
                    Console.WriteLine("No items found in Sonarr queue.");
                    return;
                }

                // Check Torrent Health in QBittorent
                List<string> torrentsToDelete = await CheckTorrentHealth(queueData.Records
                    .Where(r => !string.IsNullOrEmpty(r.TorrentId))
                    .Select(r => r.TorrentId!)
                    .ToList());

                if (torrentsToDelete.Any())
                {                 
                    foreach (var torrentId in torrentsToDelete)
                    {
                        // get Id of Sonarr queue item from torrentId
                        var queueItem = queueData.Records.FirstOrDefault(r => 
                            string.Equals(r.TorrentId, torrentId, StringComparison.OrdinalIgnoreCase));

                        if (queueItem != null)
                        {
                            await RemoveMediaItem(queueItem!.Id, false);

                            // Call post to search for episode again
                            string episodeSearchUrl = await _config.GetConfigValueAsync("sonarr_command_endpoint");
                            string url = episodeSearchUrl + _sonarrApiKey;

                            int episodeId = queueItem.EpisodeId!.Value;

                            var payload = new
                            {
                                name = "EpisodeSearch",
                                episodeIds = new List<int> { episodeId }
                            };

                            string payloadJson = JsonSerializer.Serialize(payload);
                            HttpResponseString episodeSearchResponse = await _httpClientService.PostStringAsync(url, payloadJson);

                        }
                    }
                }
      
                Console.WriteLine($"Found {queueData.Records.Count} items in Sonarr queue.");

                await ProcessSonarrQueueItems(queueData.Records);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Sonarr queue: {ex.Message}");
            }
        }
        private async Task<List<string>> CheckTorrentHealth(List<string> torrentIds)
        {
            string qbittorrentCookie = await GetQBitorrentCookie();

            string qbittorrentApiUrl = await _config.GetConfigValueAsync("qbittorrent_torrent_info_endpoint");

            var headers = new Dictionary<string, string>
            {
                { "Cookie", $"SID={qbittorrentCookie}" }
            };

            var response = await _httpClientService.GetStringAsync(qbittorrentApiUrl!, headers);

            List<QBittorrentTorrentInfo> torrentInfos = System.Text.Json.JsonSerializer.Deserialize<List<QBittorrentTorrentInfo>>(response)!;

            List<string> torrentsToCull = new List<string>();

            foreach (string torrentId in torrentIds)
            {
                var torrentInfo = torrentInfos.FirstOrDefault(t =>
                    string.Equals(t.TorrentId, torrentId, StringComparison.OrdinalIgnoreCase));

                if (torrentInfo == null)
                    continue;

                if (ShouldCull(torrentInfo))
                {
                    Console.WriteLine($"Removing stuck torrent '{torrentInfo.Name}' with ID {torrentInfo.TorrentId} from Sonarr queue.");
                    torrentsToCull.Add(torrentInfo.TorrentId);
                }
            }
            return torrentsToCull;

        }
        private async Task<bool> RemoveMediaItem(int queueId, bool isMovie)
        {
            string baseUrl = "";
            if(isMovie)
            {
                baseUrl = await _config.GetConfigValueAsync("radarr_remove_queue_item_endpoint");
                await SetRadarrApiKeyAsync();
                baseUrl = baseUrl!.Replace("{ApiKey}", _radarrApiKey!);
            }
            else
            {
                baseUrl = await _config.GetConfigValueAsync("sonarr_remove_queue_item_endpoint");
                await SetSonarrApiKeyAsync();
                baseUrl = baseUrl!.Replace("{ApiKey}", _sonarrApiKey!);
            }
            string apiUrl = baseUrl.Replace("{id}", queueId.ToString());
            var response = await _httpClientService.DeleteStringAsync(apiUrl);
            if (response == "DELETE request failed.")
            {
                return false;
            }
            return true;
        }
        private async Task<string> GetQBitorrentCookie()
        {
            string apiUrl = await _config.GetConfigValueAsync("qbittorrent_api_cookie_endpoint");

            string username = "admin";
            string password = await _config.GetConfigValueAsync("qbittorrent_api_password");

            // x-www-form-urlencoded body with username and password
            var formParams = new Dictionary<string, string>
            {
                { "username", username ?? string.Empty },
                { "password", password ?? string.Empty }
            };

            var response = await _httpClientService.PostFormUrlEncodedAsync(apiUrl!, formParams);

            return response.SidCookie!;
        }
        private static bool IsDownloadingState(string state) =>
            state is "downloading" or "stalledDL" or "queuedDL" or "metaDL";

        private static bool ShouldCull(QBittorrentTorrentInfo t)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Don’t touch very new torrents
            if (now - t.AddedOn < 180) return false; // 3 minutes

            // If its downloading or seeding, don’t cull
            if (!IsDownloadingState(t.State)) return false;

            // If it has any download speed, don’t cull
            if (t.DownloadSpeed > 0) return false;

            // If it has any upload speed, don’t cull
            var idleSeconds = now - t.LastActivity;
            var nearComplete = t.Progress >= 0.99;
            var idleThreshold = nearComplete ? 3600 : 600;
            if (idleSeconds < idleThreshold) return false;

            // If it has any seeds, don’t cull
            if (t.NumSeeds > 0) return false;

            // If availability of torrent is low, cull
            if (t.Availability > 0.3) return false; // Got to be harsh, if other checks pass

            return true;
        }
        public async Task ScrapeSonarrShows()
        {
            string apiUrl = await BuildScrapeSonarrShowsEndpoint();
            string response = await _httpClientService.GetStringAsync(apiUrl);

            List<SonarrShowsDetailList> showDetails = System.Text.Json.JsonSerializer.Deserialize<List<SonarrShowsDetailList>>(response)!;

            List<DownloadedShows> showParentList = new List<DownloadedShows>();

            foreach (var show in showDetails)
            {
                // amount of seasons equals count of seasons in show.Seasons, excluding season 0 (specials)
                int amountOfSeasons = show.Seasons != null ? show.Seasons.Count(s => s.SeasonNumber != 0) : 0;
                DownloadedShows showParent = new DownloadedShows()
                {
                    Title = show.Title!,
                    Type = "show",
                    Description = show.Description,
                    AmountOfSeasons = amountOfSeasons,
                    DownloadedAt = show.Added,
                    ReleaseDate = show.FirstAired!.Value,
                    ImdbId = show.ImdbId,
                    TvdbId = show.TvdbId,
                    PosterPath = show.Images?.FirstOrDefault()?.Url,
                    SizeOnDiskGB = show.Statistics!.SizeOnDisk > 0 ? Math.Round((double)show.Statistics.SizeOnDisk / (1024.0 * 1024.0 * 1024.0), 2) : 0.0,
                    PhysicalPath = show.Path!,
                    Monitored = show.Monitored,
                    Added = DateTime.Now
                };
                showParentList.Add(showParent);

                if (show.Seasons == null || !show.Seasons.Any())
                {
                    continue;
                }

                List<DownloadedShows> seasonChildList = new List<DownloadedShows>();

                foreach (var season in show.Seasons)
                {
                    if (season.SeasonNumber != 0)
                    {
                        // If size on disk is greater than 0, set episodes downloaded to the episode count
                        // Episode count may be more than 1 if there are errors when downloading (sonarr quirk)
                        int episodesDownloaded = 0;
                        if(season.Statistics!.SizeOnDisk > 0)
                        {                           
                            episodesDownloaded = season.Statistics.EpisodeFileCount;
                        }

                        DownloadedShows seasonChild = new DownloadedShows()
                        {
                            Title = show.Title + " S" + season.SeasonNumber,
                            Type = "season",
                            HasFile = AllShowsInSeasonDownloaded(season),
                            SeasonNumber = season.SeasonNumber,
                            EpisodesInSeason = season.Statistics!.TotalEpisodeCount,
                            EpisodesDownloaded = episodesDownloaded,
                            ImdbId = show.ImdbId,
                            TvdbId = show.TvdbId,
                            SizeOnDiskGB = GetSizeOnDiskGB(season.Statistics.SizeOnDisk),
                            Monitored = season.Monitored,
                            Added = DateTime.Now
                        };
                        seasonChildList.Add(seasonChild);
                    }
                }

                if (seasonChildList.Any(s => s.HasFile == false))
                {
                    showParent.HasFile = false;
                }
                else
                {
                    showParent.HasFile = true;

                }
                // add all seasons to parent show list
                showParentList.AddRange(seasonChildList);
            }
            // add or update all shows and seasons in database
            _db.DownloadedShows.RemoveRange(_db.DownloadedShows);
            _db.DownloadedShows.AddRange(showParentList);
            await _db.SaveChangesAsync();
        }
        private double GetSizeOnDiskGB(long sizeOnDisk)
        {
            return Math.Round((double)sizeOnDisk / (1024.0 * 1024.0 * 1024.0), 2);
        }
        private bool AllShowsInSeasonDownloaded(SonarrSeason season)
        {
            if (season.Statistics.SizeOnDisk == 0)
            {
                // No size on disk means nothing downloaded
                return false;
            }

            if (season!.Statistics!.EpisodeCount > 1)
            {
                // Probably overkill but could also do this -> && season.Statistics.PercentOfEpisodes == 100 
                if (season.Statistics.EpisodeFileCount == season.Statistics.TotalEpisodeCount)
                {
                    // Total episodes in season are equal to episodes downloaded
                    return true;
                }
                else
                {
                    // Episodes downloaded are not equal to the total episodes in season
                    return false;
                }
            }
            else
            {
                // Has 0 episodes in season
                return false;
            }
        }
        private async Task<string> BuildScrapeSonarrShowsEndpoint()
        {
            string baseUrl = await _config.GetConfigValueAsync("sonarr_get_all_shows_endpoint");
            await SetSonarrApiKeyAsync();
            return baseUrl + _sonarrApiKey!;
        }
        public async Task ScrapeRadarrMovies()
        {
            string apiUrl = await BuildScrapeRadarrMoviesEndpoint();
            string response = await _httpClientService.GetStringAsync(apiUrl);

            List<RadarrMovieDetailList> movieDetails = System.Text.Json.JsonSerializer.Deserialize<List<RadarrMovieDetailList>>(response)!;

            // Prepare all new movie entities first
            var newDownloadedMovies = new List<DownloadedMovies>();

            foreach (var movie in movieDetails)
            {
                DownloadedMovies downloadedMovie = new DownloadedMovies
                {
                    Id = movie.Id,
                    Title = movie.Title ?? string.Empty,
                    Description = movie.Description,
                    HasFile = movie.HasFile,
                    DownloadedAt = movie.Added,
                    ReleaseYear = movie.ReleaseYear,
                    ImdbId = movie.ImdbId,
                    TmdbId = movie.TmdbId,
                    PosterPath = movie.Images?.FirstOrDefault()?.Url,
                    SizeOnDiskGB = movie.SizeOnDisk > 0 ? Math.Round((double)movie.SizeOnDisk / (1024.0 * 1024.0 * 1024.0), 2) : 0.0,
                    PhysicalPath = movie.Path ?? string.Empty,
                    Monitored = movie.Monitored,
                    Runtime = movie.Runtime,
                    Added = DateTime.Now
                };
                newDownloadedMovies.Add(downloadedMovie);
            }

            // Clear existing movies and add new ones in quick succession
            _db.DownloadedMovies.RemoveRange(_db.DownloadedMovies);
            _db.DownloadedMovies.AddRange(newDownloadedMovies);

            await _db.SaveChangesAsync();
        }
        private async Task<string> BuildScrapeRadarrMoviesEndpoint()
        {
            string baseUrl = await _config.GetConfigValueAsync("radarr_get_all_movies_endpoint");
            await SetRadarrApiKeyAsync();
            return baseUrl + _radarrApiKey!;
        }
        private async Task ProcessSonarrQueueItems(List<SonarrQueueRecord> queueRecords)
        {
            // Group by series to minimize API calls
            var seriesGroups = queueRecords
                .Where(r => r.SeriesId.HasValue && r.EpisodeId.HasValue)
                .GroupBy(r => r.SeriesId!.Value)
                .ToList();

            foreach (var seriesGroup in seriesGroups)
            {
                int seriesId = seriesGroup.Key;
                var episodesInQueue = seriesGroup.ToList();

                // Get existing download requests for these episodes
                var episodeIds = episodesInQueue.Select(e => e.EpisodeId!.Value).ToList();
                var existingDownloads = await _db.DownloadRequests
                    .Where(dr => dr.MediaType == "Episode" && episodeIds.Contains(dr.EpisodeId!.Value))
                    .ToListAsync();

                // Update existing downloads with current progress
                await UpdateExistingSonarrDownloads(existingDownloads, episodesInQueue);

                // Find missing episodes that need to be added
                var missingEpisodeIds = episodeIds
                    .Where(id => !existingDownloads.Any(ed => ed.EpisodeId == id))
                    .ToList();

                if (missingEpisodeIds.Any())
                {
                    var missingQueueItems = episodesInQueue
                        .Where(e => missingEpisodeIds.Contains(e.EpisodeId!.Value))
                        .ToList();

                    await AddMissingEpisodes(seriesId, missingQueueItems);
                }

                // Update parent series progress after processing all episodes for this series
                await UpdateParentSeriesProgress(seriesId);
            }

            await _db.SaveChangesAsync();
        }
        private int GetDownloadPercentage(long? size, long? sizeLeft)
        {
            if (size.HasValue && size.Value > 0)
            {
                long downloaded = size.Value - (sizeLeft ?? 0);
                return (int)((double)downloaded / size.Value * 100);
            }
            return 0;
        }
        private int GetMinutesLeft(long? size, long? sizeLeft, string? timeLeft)
        {
            if (size.HasValue && size.Value > 0)
            {
                long downloaded = size.Value - (sizeLeft ?? 0);
                double progress = (double)downloaded / size.Value;
                if (progress > 0 && !string.IsNullOrEmpty(timeLeft))
                {
                    int? parsedMinutes = ParseTimeLeftToMinutes(timeLeft);
                    if (parsedMinutes.HasValue)
                    {
                        return (int)(parsedMinutes.Value / progress);
                    }
                }
            }
            return 0;
        }
        private async Task UpdateExistingSonarrDownloads(List<DownloadRequests> existingDownloads, List<SonarrQueueRecord> queueItems)
        {
            foreach (var download in existingDownloads)
            {
                var queueItem = queueItems.FirstOrDefault(q => q.EpisodeId == download.EpisodeId);
                if (queueItem != null)
                {
                    // Calculate download percentage
                    int downloadPercentage = GetDownloadPercentage(queueItem.Size, queueItem.SizeLeft);
                    int? minutesLeft = GetMinutesLeft(queueItem.Size, queueItem.SizeLeft, queueItem.TimeLeft);

                    // Update the download request
                    download.Status = GetDownloadStatus(queueItem.Status);
                    download.DownloadPercentage = downloadPercentage;
                    download.MinutesLeft = minutesLeft;
                    download.UpdatedAt = DateTime.UtcNow;

                    if (download.Status == "completed" && !download.CompletedAt.HasValue)
                    {
                        download.CompletedAt = DateTime.UtcNow;
                        await ScrapeSonarrShows();
                    }

                    Console.WriteLine($"Updated episode {download.EpisodeId}: {downloadPercentage}% - {download.Status}");
                }
            }
        }
        private async Task AddMissingEpisodes(int seriesId, List<SonarrQueueRecord> missingQueueItems)
        {
            try
            {
                // First, get the series info to get the correct TvdbId
                string seriesApiUrl = await BuildSonarrSeriesDataUrl(seriesId.ToString());
                string seriesResponse = await _httpClientService.GetStringAsync(seriesApiUrl);
                SonarrSeries seriesData = System.Text.Json.JsonSerializer.Deserialize<SonarrSeries>(seriesResponse)!;

                if (seriesData == null)
                {
                    Console.WriteLine($"No series data returned for seriesId {seriesId}");
                    return;
                }

                int seriesTvdbId = seriesData.TvdbId;
                Console.WriteLine($"Looking for parent series with TvdbId: {seriesTvdbId}");

                // Find parent series using the series TvdbId
                var parentSeries = await _db.DownloadRequests
                    .Where(dr => (dr.MediaType == "show" || dr.MediaType == "Series") && dr.TvdbId == seriesTvdbId)
                    .FirstOrDefaultAsync();

                if (parentSeries == null)
                {
                    Console.WriteLine($"No parent show found for series TvdbId {seriesTvdbId} (seriesId {seriesId}). Cannot add episodes.");
                    return;
                }

                Console.WriteLine($"Found parent series: '{parentSeries.Title}' with TvdbId {seriesTvdbId}");

                // Now get all episodes for this series from Sonarr
                string episodeApiUrl = await BuildSonarrEpisodeDataUrl(seriesId.ToString());
                string episodeResponse = await _httpClientService.GetStringAsync(episodeApiUrl);
                List<SonarrEpisode> allEpisodes = System.Text.Json.JsonSerializer.Deserialize<List<SonarrEpisode>>(episodeResponse)!;

                if (!allEpisodes.Any())
                {
                    Console.WriteLine($"No episode data returned for seriesId {seriesId}");
                    return;
                }

                // Create new download requests for missing episodes
                var newDownloadRequests = new List<DownloadRequests>();

                foreach (var queueItem in missingQueueItems)
                {
                    var episodeData = allEpisodes.FirstOrDefault(e => e.Id == queueItem.EpisodeId);
                    if (episodeData == null)
                    {
                        Console.WriteLine($"Episode data not found for episodeId {queueItem.EpisodeId}");
                        continue;
                    }

                    // Calculate download progress
                    int downloadPercentage = 0;
                    int? minutesLeft = null;

                    if (queueItem.Size.HasValue && queueItem.Size > 0)
                    {
                        long downloaded = queueItem.Size.Value - (queueItem.SizeLeft ?? 0);
                        downloadPercentage = (int)((double)downloaded / queueItem.Size.Value * 100);
                    }

                    if (!string.IsNullOrEmpty(queueItem.TimeLeft))
                    {
                        minutesLeft = ParseTimeLeftToMinutes(queueItem.TimeLeft);
                    }

                    var newDownloadRequest = new DownloadRequests
                    {
                        MediaId = seriesId,
                        EpisodeId = queueItem.EpisodeId,
                        TvdbId = seriesTvdbId,
                        UserId = parentSeries.UserId,
                        MediaType = "Episode",
                        Title = episodeData.Title,
                        ReleaseYear = !string.IsNullOrEmpty(episodeData.AirDate) ?
                                  DateTime.TryParse(episodeData.AirDate, out var airDateYear) ? airDateYear.Year : null : null,
                        Status = GetDownloadStatus(queueItem.Status),
                        DownloadPercentage = downloadPercentage,
                        MinutesLeft = minutesLeft,
                        RequestedAt = DateTime.UtcNow,
                        SeasonNumber = episodeData.SeasonNumber,
                        EpisodeNumber = episodeData.EpisodeNumber,
                        EpisodeDate = !string.IsNullOrEmpty(episodeData.AirDate) ?
                        DateTime.TryParse(episodeData.AirDate, out var episodeAirDate) ? episodeAirDate : null : null,
                        UpdatedAt = DateTime.UtcNow
                    };

                    newDownloadRequests.Add(newDownloadRequest);
                    Console.WriteLine($"Prepared episode: {episodeData.Title} (S{episodeData.SeasonNumber:D2}E{episodeData.EpisodeNumber:D2}) for series TvdbId {seriesTvdbId}");
                }

                if (newDownloadRequests.Any())
                {
                    _db.DownloadRequests.AddRange(newDownloadRequests);
                    Console.WriteLine($"Added {newDownloadRequests.Count} new episode download requests for series '{parentSeries.Title}' (TvdbId: {seriesTvdbId})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding missing episodes for series {seriesId}: {ex.Message}");
            }
        }
        private async Task UpdateParentSeriesProgress(int seriesId)
        {
            try
            {
                // Get all episodes for this series from the database
                var allSeriesEpisodes = await _db.DownloadRequests
                    .Where(dr => dr.MediaType == "Episode" && dr.MediaId == seriesId)
                    .ToListAsync();

                if (!allSeriesEpisodes.Any())
                {
                    // No episodes found, nothing to update
                    return;
                }

                // Get the parent series record using TvdbId from the first episode
                int seriesTvdbId = allSeriesEpisodes.First().TvdbId!.Value;
                var parentSeries = await _db.DownloadRequests
                    .Where(dr => (dr.MediaType == "show" || dr.MediaType == "Series") && dr.TvdbId == seriesTvdbId)
                    .FirstOrDefaultAsync();

                if (parentSeries == null)
                {
                    Console.WriteLine($"No parent series found for seriesId {seriesId}, TvdbId {seriesTvdbId}");
                    return;
                }

                // Calculate average download percentage across all episodes
                int totalEpisodes = allSeriesEpisodes.Count;
                int totalPercentage = allSeriesEpisodes.Sum(e => e.DownloadPercentage);
                int averagePercentage = totalEpisodes > 0 ? totalPercentage / totalEpisodes : 0;

                // Check if all episodes are completed
                bool allEpisodesCompleted = allSeriesEpisodes.All(e => e.Status == "completed");

                // Update parent series
                parentSeries.DownloadPercentage = averagePercentage;
                parentSeries.UpdatedAt = DateTime.UtcNow;
                parentSeries.Status = "downloading"; // default status

                // Calculate average minutes left (only for non-completed episodes)
                var activeEpisodes = allSeriesEpisodes.Where(e => e.Status != "completed" && e.MinutesLeft.HasValue).ToList();
                if (activeEpisodes.Any())
                {
                    double averageMinutesLeft = activeEpisodes.Average(e => e.MinutesLeft!.Value);
                    parentSeries.MinutesLeft = (int)Math.Round(averageMinutesLeft);
                }
                else
                {
                    parentSeries.MinutesLeft = null;
                }

                // Update status based on episodes
                if (allEpisodesCompleted)
                {
                    parentSeries.Status = "completed";
                    if (!parentSeries.CompletedAt.HasValue)
                    {
                        parentSeries.CompletedAt = DateTime.UtcNow;
                    }
                }
                else if (allSeriesEpisodes.Any(e => e.Status == "downloading"))
                {
                    parentSeries.Status = "downloading";
                }
                else if (allSeriesEpisodes.Any(e => e.Status == "queued"))
                {
                    parentSeries.Status = "queued";
                }
                
                Console.WriteLine($"Updated parent series '{parentSeries.Title}': {averagePercentage}% complete ({totalEpisodes} episodes) - {parentSeries.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating parent series progress for seriesId {seriesId}: {ex.Message}");
            }
        }
        private int? ParseTimeLeftToMinutes(string timeLeft)
        {
            try
            {
                if (string.IsNullOrEmpty(timeLeft))
                    return null;

                // Parse formats like "00:30:45" (HH:mm:ss) or "30:45" (mm:ss)
                var parts = timeLeft.Split(':');
                if (parts.Length == 3)
                {
                    // HH:mm:ss format
                    if (int.TryParse(parts[0], out int hours) &&
                             int.TryParse(parts[1], out int minutes) &&
                             int.TryParse(parts[2], out int seconds))
                    {
                        return hours * 60 + minutes + (seconds > 30 ? 1 : 0);
                    }
                }
                else if (parts.Length == 2)
                {
                    // mm:ss format
                    if (int.TryParse(parts[0], out int minutes) &&
                            int.TryParse(parts[1], out int seconds))
                    {
                        return minutes + (seconds > 30 ? 1 : 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing time left '{timeLeft}': {ex.Message}");
            }

            return null;
        }
        private string GetDownloadStatus(string? status)
        {
            return status?.ToLower() switch
            {
                "downloading" => "downloading",
                "queued" => "queued",
                "paused" => "paused",
                "completed" => "completed",
                "failed" => "failed",
                "importpending" => "importing",
                "warning" => "warning",
                _ => "queued"
            };
        }
        private async Task<string> BuildSonarrQueueUrl()
        {
            string baseUrl = await _config.GetConfigValueAsync("sonarr_download_queue_endpoint");
            await SetSonarrApiKeyAsync();
            return baseUrl!.Replace("{ApiKey}", _sonarrApiKey!);
        }
        private async Task<string> BuildSonarrSeriesDataUrl(string seriesId)
        {
            string baseUrl = await _config.GetConfigValueAsync("sonarr_series_data_endpoint");
            await SetSonarrApiKeyAsync();
            return baseUrl!.Replace("{ApiKey}", _sonarrApiKey!).Replace("{seriesId}", seriesId);
        }
        private async Task<string> BuildSonarrEpisodeDataUrl(string seriesId)
        {
            string baseUrl = await _config.GetConfigValueAsync("sonarr_episode_data_endpoint");
            await SetSonarrApiKeyAsync();
            return baseUrl!.Replace("{ApiKey}", _sonarrApiKey!).Replace("{seriesId}", seriesId);
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
        private async Task<string> BuildRadarrQueueUrl()
        {
            string baseUrl = await _config.GetConfigValueAsync("radarr_download_queue_endpoint");

            await SetRadarrApiKeyAsync();

            string apiUrl = baseUrl!.Replace("{ApiKey}", _radarrApiKey!);

            return apiUrl;
        }
        private async Task SetRadarrApiKeyAsync()
        {
            if (string.IsNullOrEmpty(_radarrApiKey))
            {
                var apiKey = await _config.GetConfigValueAsync("radarr_api_key");
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("radarr_api_key not found in configuration.");
                }
                _radarrApiKey = apiKey;
            }
        }
    }

    public class SonarrQueueResponse
    {
        [JsonPropertyName("records")]
        public List<SonarrQueueRecord>? Records { get; set; }
    }
    public class SonarrQueueRecord
    {
        [JsonPropertyName("seriesId")]
        public int? SeriesId { get; set; }

        [JsonPropertyName("episodeId")]
        public int? EpisodeId { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("sizeleft")]
        public long? SizeLeft { get; set; }

        [JsonPropertyName("timeleft")]
        public string? TimeLeft { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
        [JsonPropertyName("downloadId")]
        public string? TorrentId { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
    public class SonarrEpisode
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("seasonNumber")]
        public int SeasonNumber { get; set; }

        [JsonPropertyName("episodeNumber")]
        public int EpisodeNumber { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("airDate")]
        public string? AirDate { get; set; }
    }
    public class SonarrSeries
    {
        [JsonPropertyName("tvdbId")]
        public int TvdbId { get; set; }
    }
    public class RadarrQueueResponse
    {
        [JsonPropertyName("records")]
        public List<RadarrQueueRecord>? Records { get; set; }
    }

    public class RadarrQueueRecord
    {
        [JsonPropertyName("movieId")]
        public int? MovieId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("size")]
        public long? Size { get; set; }
        
        [JsonPropertyName("sizeleft")]
        public long? SizeLeft { get; set; }

        [JsonPropertyName("timeleft")]
        public string? TimeLeft { get; set; }
        [JsonPropertyName("downloadId")]
        public string? TorrentId { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
    public class RadarrMovieId
    {
        [JsonPropertyName("tmdbId")]
        public int TmbdId { get; set; }
    }
    public class QBittorrentTorrentInfo
    {
        [JsonPropertyName("hash")]
        public string TorrentId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("availability")]
        public double Availability { get; set; }

        [JsonPropertyName("num_seeds")]
        public int NumSeeds { get; set; }

        [JsonPropertyName("num_incomplete")]
        public int NumIncomplete { get; set; }

        [JsonPropertyName("progress")]
        public double Progress { get; set; }

        [JsonPropertyName("time_active")]
        public long TimeActive { get; set; }

        [JsonPropertyName("dlspeed")]
        public long DownloadSpeed { get; set; }

        [JsonPropertyName("upspeed")]
        public long UploadSpeed { get; set; }

        [JsonPropertyName("added_on")]
        public long AddedOn { get; set; }

        [JsonPropertyName("last_activity")]
        public long LastActivity { get; set; }

        [JsonPropertyName("amount_left")]
        public long AmountLeft { get; set; }

        [JsonPropertyName("downloaded")]
        public long Downloaded { get; set; }
    }


}

