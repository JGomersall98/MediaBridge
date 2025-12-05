using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.DownloadRequests;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Services.Media.Downloads
{
    public class RequestDownloadStatusService : IRequestDownloadStatusService
    {
        private readonly MediaBridgeDbContext _db;
        public RequestDownloadStatusService(MediaBridgeDbContext db)
        {
            _db = db;
        }
        public async Task<DownloadRequestsResponse> GetDownloadRequestsStatus(int userId)
        {
            DownloadRequestsResponse downloadRequestsResponse = new DownloadRequestsResponse();

            DateTime sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

            List<DownloadRequests> downloadRequests =
                await _db.DownloadRequests
                    .Where(dr => dr.UserId == userId && dr.RequestedAt >= sixMonthsAgo)
                    .ToListAsync();

            if (downloadRequests == null || downloadRequests.Count == 0)
            {
                downloadRequestsResponse.IsSuccess = false;
                downloadRequestsResponse.Reason = "No download requests found for the user.";
                return downloadRequestsResponse;
            }

            List<MediaRequestStatus> requests = new List<MediaRequestStatus>();
            foreach (var request in downloadRequests)
            {
                if (request.MediaType == "movie")
                {
                    MediaRequestStatus movieRequestStatus = BuildMovieMediaRequestStatus(request);
                    requests.Add(movieRequestStatus);
                }
                else if (request.MediaType == "show")
                {
                    int? tvdvId = request.TvdbId;

                    List<DownloadRequests> episodeRequests = downloadRequests
                        .Where(dr => dr.TvdbId == tvdvId && dr.EpisodeId != null)
                        .ToList();

                    MediaRequestStatus showRequestStatus = BuildShowMediaRequestStatus(request, episodeRequests);
                    requests.Add(showRequestStatus);
                }
            }     
            downloadRequestsResponse.IsSuccess = true;
            downloadRequestsResponse.Requests = requests;
            return downloadRequestsResponse;
        }
        private MediaRequestStatus BuildMovieMediaRequestStatus(DownloadRequests request)
        {
            return new MediaRequestStatus
            {
                Id = request.Id,
                MediaType = request.MediaType,
                Title = request.Title,
                Description = request.Description,
                ReleaseYear = request.ReleaseYear?.ToString(),
                PosterUrl = request.PosterUrl,
                Status = request.Status,
                DownloadPercentage = request.DownloadPercentage,
                RequestedAt = request.RequestedAt,
                CompletedAt = request.CompletedAt,
                MinutesLeft = request.MinutesLeft,
            };
        }
        private MediaRequestStatus BuildShowMediaRequestStatus(DownloadRequests show, List<DownloadRequests>? episodes)
        {

            MediaRequestStatus mediaRequestStatus = new MediaRequestStatus
            {
                Id = show.Id,
                MediaType = show.MediaType,
                Title = show.Title,
                Description = show.Description,
                ReleaseYear = show.ReleaseYear?.ToString(),
                PosterUrl = show.PosterUrl,
                Status = show.Status,
                DownloadPercentage = show.DownloadPercentage,
                RequestedAt = show.RequestedAt,
                CompletedAt = show.CompletedAt,
                MinutesLeft = show.MinutesLeft,
            };

            List<ShowEpisodesStatus > episodesStatus = new List<ShowEpisodesStatus>();

            if (episodes != null && episodes.Count > 0)
            {
                foreach (var episode in episodes)
                {
                    ShowEpisodesStatus showEpisodesStatus = new ShowEpisodesStatus
                    {
                        Id = episode.Id,
                        SeasonNumber = episode.SeasonNumber,
                        EpisodeNumber = episode.EpisodeNumber,
                        Title = episode.Title,
                        EpisodeDate = episode.EpisodeDate,
                        Status = episode.Status,
                        DownloadPercentage = episode.DownloadPercentage,
                        MinutesLeft = episode.MinutesLeft
                    };
                    episodesStatus.Add(showEpisodesStatus);
                }
                mediaRequestStatus.EpisodesStatus = episodesStatus;
            }
            return mediaRequestStatus;
        }
    }
}