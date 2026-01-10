using System.Text.Json;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Media.ExternalServices.Sonarr;
using MediaBridge.Services.Helpers;

namespace MediaBridge.Services.Media.ExternalServices.Sonarr
{
    public interface ISonarrService
    {
        Task<bool> SendShowRequest(int? tvdbId, string title, int[] seasonsRequested);
        Task<ShowDetailsResponse?> GetShowDetails(int? tvdbId);
    }
    public class SonarrService : ISonarrService
    {
        private readonly ISonarrHttp _sonarrHttp;
        private readonly JsonSerializerOptions _jsonOptions;
        private const string SonarrAddShowKey = "sonarr_post_show_endpoint";
        private const string SonarrGetShowKey = "sonarr_get_show_endpoint";
        
        public SonarrService(ISonarrHttp sonarrHttp)
        {
            _sonarrHttp = sonarrHttp;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<bool> SendShowRequest(int? tvdbId, string title, int[] seasonsRequested)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentNullException(nameof(title), "Title is required to download a TV show");
            if (tvdbId == null)
                throw new ArgumentNullException(nameof(tvdbId), "TVDB ID is required to download a TV show");
            if (seasonsRequested == null)
                throw new ArgumentNullException(nameof(seasonsRequested), "No seasons requested");

            // Build season payload
            List<Season> seasonList = BuildSeasonPayload(seasonsRequested);

            string payload = JsonSerializer.Serialize(new AddShowRequest
            {
                TvdbId = tvdbId,
                Title = title,
                Seasons = seasonList
            });

            // Send POST request to Sonarr
            HttpResponseString response = await _sonarrHttp.SonarrHttpPost(SonarrAddShowKey, payload);

            return response.IsSuccess;
        }
        public async Task<ShowDetailsResponse?> GetShowDetails(int? tvdbId)
        {
            // Validate input
            if (tvdbId == null)
                throw new ArgumentNullException(nameof(tvdbId), "TVDB ID is required to retrieve TV show details");

            // Make GET request to Sonarr
            HttpResponseString response = await _sonarrHttp.SonarrHttpGet(SonarrGetShowKey, tvdbId.Value);

            // Deserialize response
            List<ShowDetailsResponse>? showDetailList = JsonSerializer.Deserialize<List<ShowDetailsResponse>>(response.Response, _jsonOptions);

            // Validate response
            if (showDetailList == null || showDetailList.Count == 0)
                throw new InvalidOperationException($"No show details found for TVDB ID: {tvdbId}");

            // Get first show detail
            ShowDetailsResponse? showDetail = showDetailList.FirstOrDefault();

            return showDetail;
        }
        private List<Season> BuildSeasonPayload(int[] seasonsRequested)
        {
            List<Season> seasonList = new List<Season>();
            foreach (var seasonItem in seasonsRequested)
            {
                Season season = new Season()
                {
                    SeasonNumber = seasonItem,
                    Monitored = true
                };
                seasonList.Add(season);
            }
            return seasonList;
        }
    }
}
