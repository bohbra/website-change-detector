using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebsiteChangeDetector.Common;
using WebsiteChangeDetector.Options;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector.Services
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IOptions<ServiceOptions> _settings;
        private readonly IEnumerable<IWebsite> _websites;
        private readonly ITextClient _textClient;

        public Worker(
            ILogger<Worker> logger, 
            IOptions<ServiceOptions> settings, 
            IEnumerable<IWebsite> websites,
            ITextClient textClient)
        {
            _logger = logger;
            _settings = settings;
            _websites = websites;
            _textClient = textClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // check each website
                foreach (var website in _websites)
                {
                    var result = await website.Check();
                    if (!result.Success)
                        continue;

                    // send text when successful
                    _textClient.Send(result.Message);

                    _logger.LogDebug("Pausing");
                    await Task.Delay(TimeSpan.FromHours(4));
                }

                _logger.LogDebug($"Pausing for {_settings.Value.PollDelayInSeconds} seconds");
                await Task.Delay(TimeSpan.FromSeconds(_settings.Value.PollDelayInSeconds));
            }
        }
    }
}
