using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebsiteChangeDetector.Common;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector.Services
{
    public class Detector : IDetector
    {
        private readonly ILogger<Detector> _logger;
        private readonly IEnumerable<IWebsite> _websites;
        private readonly ITextClient _textClient;

        public Detector(ILogger<Detector> logger, IEnumerable<IWebsite> websites, ITextClient textClient)
        {
            _logger = logger;
            _websites = websites;
            _textClient = textClient;
        }

        public async Task Scan()
        {
            // check all websites
            foreach (var website in _websites)
            {
                var result = await website.Check();
                if (!result.Success) 
                    continue;
                _textClient.Send(result.Message);
                _logger.LogDebug("Pausing");
                await Task.Delay(TimeSpan.FromHours(4));
            }
        }
    }
}
