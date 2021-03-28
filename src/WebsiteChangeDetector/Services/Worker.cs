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
            _logger.LogInformation("Worker is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // check each website
                    foreach (var website in _websites)
                    {
                        var result = await website.Check();
                        if (!result.Success)
                            continue;

                        // log success message
                        _logger.LogDebug(result.Message);

                        // send text
                        _textClient.Send(result.Message);

                        if (_options.PauseOnSuccess)
                        {
                            _logger.LogDebug("Pausing because check was successful");
                            await Task.Delay(TimeSpan.FromDays(7));
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occurred executing website check");
                    _textClient.Send("Error occurred");
                    throw;
                }
                
                _logger.LogDebug($"Pausing for {_options.PollDelayInSeconds} seconds");
                await Task.Delay(TimeSpan.FromSeconds(_options.PollDelayInSeconds), stoppingToken);
            }

            _logger.LogInformation("Worker is stopping");
        }
    }
}
