namespace MediaBridge.Services.Helpers
{
    public interface IHttpClientService
    {
        Task<string> GetStringAsync(string url);
    }
}
