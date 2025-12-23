using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Morgan.LogBackup.Application;

namespace Morgan.LogBackup
{
    /// <summary>
    /// Background worker responsible for periodically executing the log backup routine.
    /// Uses LogBackupRunner to process log files. 
    /// Ensures the application runs reliably without interruption.
    /// </summary>
    /// <remarks>
    /// - Runs continuously until the host stops  
    /// - Execution interval is configurable in configuration  
    /// - Automatically resumes processing in each cycle  
    /// - Catches and logs unexpected exceptions without stopping the worker  
    /// </remarks>
    public class Worker : BackgroundService
    {
        private readonly LogBackupRunner _runner;
        private readonly ILogger<Worker> _logger;
        private readonly int _intervalSeconds;

        public Worker(LogBackupRunner runner, ILogger<Worker> logger, IConfiguration config)
        {
            _runner = runner;
            _logger = logger;
            _intervalSeconds = config.GetValue<int>("Worker:IntervalSeconds", 15);
        }

        /// <summary>
        /// Main execution loop of the background worker.  
        /// Executes the backup cycle at a configurable interval until cancellation is requested.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Log Backup Worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _runner.RunAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker encountered an unexpected error.");
                }
                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
            }

            _logger.LogInformation("Log Backup Worker stopping");
        }
    }

}
