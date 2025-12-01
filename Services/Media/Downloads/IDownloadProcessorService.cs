namespace MediaBridge.Services.Media.Downloads
{
    public interface IDownloadProcessorService
    {
        Task ProcessSonarrQueue();
    }
}
