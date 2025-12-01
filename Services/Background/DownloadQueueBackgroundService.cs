using MediaBridge.Services.Media.Downloads;

namespace MediaBridge.Services.Background
{
    public class DownloadQueueBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DownloadQueueBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromSeconds(30);

        public DownloadQueueBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DownloadQueueBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Download Queue Background Service starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var downloadProcessorService = scope.ServiceProvider
                    .GetRequiredService<IDownloadProcessorService>();

                    await downloadProcessorService.ProcessSonarrQueue();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing Sonarr queue.");
                }

                try
                {
                    await Task.Delay(_period, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
            }

            _logger.LogInformation("Download Queue Background Service stopping.");
        }
    }
}