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
        private readonly ServiceOptions _options;
        private readonly IEnumerable<IWebsite> _websites;
        private readonly ITextClient _textClient;

        public Worker(
            ILogger<Worker> logger, 
            IOptions<ServiceOptions> options, 
            IEnumerable<IWebsite> websites,
            ITextClient textClient)
        {
            _logger = logger;
            _options = options.Value;
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

                    if (_options.PauseOnSuccess)
                    {
                        _logger.LogDebug("Pausing");
                        await Task.Delay(TimeSpan.FromDays(7));
                    }
                }

                _logger.LogDebug($"Pausing for {_options.PollDelayInSeconds} seconds");
                await Task.Delay(TimeSpan.FromSeconds(_options.PollDelayInSeconds), stoppingToken);
            }
        }
    }
}
