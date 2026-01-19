namespace MediaBridge.Services.Media.Downloads
{
    public interface IDownloadProcessorService
    {
        Task ProcessSonarrQueue();
        Task ProcessRadarrQueue();
        Task ProcessStuckMedia();
        Task ScrapeRadarrMovies();
        Task ScrapeSonarrShows();
    }
}
