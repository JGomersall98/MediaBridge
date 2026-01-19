using System.Runtime.CompilerServices;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Services.Dashboard;
using MediaBridge.Services.Media.Downloads;

namespace MediaBridge.Services.Background
{
    public class DownloadQueueBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DownloadQueueBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromSeconds(15);
        private readonly TimeSpan _sixHourPeriod = TimeSpan.FromHours(6);

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

            // Start the 6-hour timer task
            var sixHourTask = RunSixHourTimerAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var downloadProcessorService = scope.ServiceProvider
                    .GetRequiredService<IDownloadProcessorService>();

                    await downloadProcessorService.ProcessSonarrQueue();
                    await downloadProcessorService.ProcessRadarrQueue();
                    await downloadProcessorService.ProcessStuckMedia();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing Sonarr/Radarr queues or stuck media.");
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

            // Wait for the 6-hour task to complete
            try
            {
                await sixHourTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }

            _logger.LogInformation("Download Queue Background Service stopping.");
        }

        private async Task RunSixHourTimerAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running 6-hour maintenance task...");

                    using var scope = _serviceProvider.CreateScope();
                    var downloadProcessorService = scope.ServiceProvider.GetRequiredService<IDownloadProcessorService>();

                    // Run each maintenance task independently so one failure doesn't stop others
                    try
                    {
                        _logger.LogInformation("Starting ScrapeRadarrMovies...");
                        await downloadProcessorService.ScrapeRadarrMovies();
                        _logger.LogInformation("Completed ScrapeRadarrMovies.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ScrapeRadarrMovies failed.");
                    }

                    try
                    {
                        _logger.LogInformation("Starting ScrapeSonarrShows...");
                        await downloadProcessorService.ScrapeSonarrShows();
                        _logger.LogInformation("Completed ScrapeSonarrShows.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ScrapeSonarrShows failed.");
                    }

                    try
                    {
                        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
                        _logger.LogInformation("Starting RefreshCaches...");
                        await dashboardService.RefreshCaches();
                        _logger.LogInformation("Completed RefreshCaches.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "RefreshCaches failed.");
                    }

                    _logger.LogInformation("6-hour maintenance task completed.");
                    await Task.Delay(_sixHourPeriod, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during 6-hour maintenance task.");
                }
            }
        }
    }
}