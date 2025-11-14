using System.Text.Json;
using MediaBridge.Models.Dashboard;
using MediaBridge.Models.Search;
using MediaBridge.Services.Helpers;

namespace MediaBridge.Services.Media
{
    public class SearchService : ISearchService
    {
        private readonly IGetConfig _config;
        private readonly IHttpClientService _httpClientService;
        private readonly JsonSerializerOptions _jsonOptions;
        private const string SEARCH_ENDPOINT_KEY = "mdblist_search_endpont";
        public SearchService(IGetConfig config, IHttpClientService httpClientService)
        {
            _config = config;
            _httpClientService = httpClientService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }
        public async Task<MdbListMediaSearchResponse> MdbListSearch(string media, string query)
        {
            MdbListMediaSearchResponse response = new MdbListMediaSearchResponse();

            // Get Endpoint
            string configUrl = await _config.GetConfigValueAsync(SEARCH_ENDPOINT_KEY);
            if (String.IsNullOrEmpty(configUrl))
            {
                response.IsSuccess = false;
                response.Reason = "Endpoint URL cannot be found";
                return response;
            }

            // Build apiUrl
            string apiUrl = configUrl
                .Replace("{mediaType}", media)
                .Replace("{searchQuery}", query);

            string apiKey = await _config.GetConfigValueAsync("mdblist_api_key");
            var fullUrl = $"{apiUrl}{apiKey}";

            string httpResponse = await _httpClientService.GetStringAsync(fullUrl);
            MbListSearchResult searchResult = JsonSerializer.Deserialize<MbListSearchResult>(httpResponse, _jsonOptions);

            List<int> traktIds = searchResult?.Search?
                    .Where(s => s.Ids != null)
                    .Select(s => s.Ids!.Traktid)
                    .ToList();

            if (traktIds == null)
            {
                response.IsSuccess = false;
                response.Reason = "Cannot get media info, failed to extract traktIds";
                return response;
            }

            var something = GetMediaInfo(traktIds, media);


            return null;
        }
        private async Task<string> GetMediaInfo(List<int> traktIds, string mediaType)
        {




            return "";
        }
    }
    public class MbListSearchResult
    {
        public List<Search>? Search { get; set; }
    }
    public class Search
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
    }

    //{
    //        "title": "Natsume's Book of Friends",
    //        "year": 2008,
    //        "score": 76,
    //        "score_average": 81,
    //        "type": "show",
    //        "ids": {
    //            "imdbid": "tt1352421",
    //            "tmdbid": 69500,
    //            "traktid": 57330,
    //            "malid": 4081,
    //            "tvdbid": null
    //        }
    //    },
}

// https://api.mdblist.com/search/{mediaType}?query={searchQuery}&limit_by_score=30&sort_by_score=True&limit=10&apikey=
