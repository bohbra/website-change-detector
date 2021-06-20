using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebsiteChangeDetector.Console.Options;
using WebsiteChangeDetector.Notifications;

namespace WebsiteChangeDetector.Console.Services
{
    public class Service : BackgroundService
    {
        private readonly ILogger<Service> _logger;
        private readonly IWebsiteChangeDetector _detector;
        private readonly IEmailClient _emailClient;
        private readonly ServiceOptions _options;

        public Service(
            ILogger<Service> logger,
            IOptions<ServiceOptions> options,
            IWebsiteChangeDetector detector,
            IEmailClient emailClient)
        {
            _logger = logger;
            _detector = detector;
            _emailClient = emailClient;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _detector.Scan();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred");
                    var errorMessage = new StringBuilder();
                    errorMessage.AppendLine($"Error occurred @ {DateTime.Now}");
                    errorMessage.AppendLine($"Exception: {e}");
                    await _emailClient.Send(errorMessage.ToString());
                    throw;
                }

                _logger.LogDebug($"Pausing for {_options.PollDelayInSeconds} seconds");
                await Task.Delay(TimeSpan.FromSeconds(_options.PollDelayInSeconds), stoppingToken);
            }

            _logger.LogInformation("Worker is stopping");
        }
    }
}
