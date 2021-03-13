using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebsiteChangeDetector.Options;

namespace WebsiteChangeDetector.Services
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IOptions<ServiceOptions> _settings;
        private readonly IDetector _detector;

        public Worker(ILogger<Worker> logger, IOptions<ServiceOptions> settings, IDetector detector)
        {
            _logger = logger;
            _settings = settings;
            _detector = detector;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (true)
                {
                    await _detector.Scan();
                    _logger.LogDebug($"Pausing for {_settings.Value.PollDelayInSeconds} seconds");
                    await Task.Delay(TimeSpan.FromSeconds(_settings.Value.PollDelayInSeconds));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred", ex);
            }
        }
    }
}
