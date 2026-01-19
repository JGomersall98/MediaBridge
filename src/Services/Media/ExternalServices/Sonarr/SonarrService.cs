using System.Text.Json;
using MediaBridge.Configuration;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Media.ExternalServices.Sonarr;
using MediaBridge.Services.Helpers;
using Microsoft.Extensions.Options;

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
        private readonly SonarrOptions _sonarrOptions;

        public SonarrService(ISonarrHttp sonarrHttp, IOptions<SonarrOptions> sonarrOptions)
        {
            _sonarrHttp = sonarrHttp;
            _sonarrOptions = sonarrOptions.Value;
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

            // Get endpoint and replace any placeholders if needed
            var endpoint = _sonarrOptions.Endpoints.AddSeries;

            // Send POST request to Sonarr
            HttpResponseString response = await _sonarrHttp.SonarrHttpPost(endpoint, payload);

            return response.IsSuccess;
        }

        public async Task<ShowDetailsResponse?> GetShowDetails(int? tvdbId)
        {
            // Validate input
            if (tvdbId == null)
                throw new ArgumentNullException(nameof(tvdbId), "TVDB ID is required to retrieve TV show details");

            // Get endpoint and replace ID placeholder
            var endpoint = _sonarrOptions.Endpoints.GetSeries.Replace("{id}", tvdbId.Value.ToString());

            // Send GET request to Sonarr with the prepared endpoint
            HttpResponseString response = await _sonarrHttp.SonarrHttpGet(endpoint, tvdbId.Value);

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
