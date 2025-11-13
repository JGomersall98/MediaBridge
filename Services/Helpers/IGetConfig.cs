namespace MediaBridge.Services.Helpers
{
    public interface IGetConfig
    {
        Task<string?> GetConfigValueAsync(string key);
    }
}
