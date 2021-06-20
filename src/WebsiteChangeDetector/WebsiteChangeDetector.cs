using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebsiteChangeDetector.Notifications;
using WebsiteChangeDetector.Options;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector
{
    public class WebsiteChangeDetector : IWebsiteChangeDetector
    {
        private readonly ILogger<WebsiteChangeDetector> _logger;
        private readonly WebsiteChangeDetectorOptions _options;
        private readonly IEnumerable<IWebsite> _websites;
        private readonly IEmailClient _emailClient;


        public WebsiteChangeDetector(
            ILogger<WebsiteChangeDetector> logger,
            IOptions<WebsiteChangeDetectorOptions> options,
            IEnumerable<IWebsite> websites,
            IEmailClient emailClient)
        {
            _logger = logger;
            _options = options.Value;
            _websites = websites;
            _emailClient = emailClient;
        }

        public async Task Scan()
        {
            // check each website
            foreach (var website in _websites)
            {
                var result = await website.Check();
                if (!result.Success)
                    continue;

                // log success message
                _logger.LogDebug(result.Message);

                // send message
                await _emailClient.Send(result.Message);

                if (_options.PauseOnSuccess)
                {
                    _logger.LogDebug("Pausing because check was successful");
                    await Task.Delay(Timeout.Infinite);
                }
            }
        }
    }
}
