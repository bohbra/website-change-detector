using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebsiteChangeDetector.Options;

namespace WebsiteChangeDetector.Websites
{
    public class PetcoWebsite : IWebsite
    {
        private readonly ILogger<PetcoWebsite> _logger;
        private readonly IWebDriver _webDriver;
        private readonly ServiceOptions _options;

        private const string Url = "https://mychart-openscheduling.et0502.epichosted.com/UCSD/SignupAndSchedule/EmbeddedSchedule?dept=9990995&id=99909951,99909952,99909953,99909954,99909955,99909956&vt=3550&payor=1003,2020,1023,1013,1014,1017,1022,1039,1042,1121,1046,1050,1048,1055,1236,1079,1086,1093,2003,1088,-1,-2,-3";
        private const string SearchText = "Sorry, we couldn't find any open appointments";

        public PetcoWebsite(ILogger<PetcoWebsite> logger, IWebDriver webDriver, IOptions<ServiceOptions> options)
        {
            _logger = logger;
            _webDriver = webDriver;
            _options = options.Value;
        }

        public async Task<WebsiteResult> Check()
        {
            // navigate to page
            _webDriver.Navigate().GoToUrl(Url);

            // sleep after load
            await Task.Delay(TimeSpan.FromSeconds(_options.PageLoadDelayInSeconds));

            // search for text
            var found = !_webDriver.PageSource.Contains(SearchText);

            // log result
            _logger.LogDebug($"Search result: {found}");

            // result
            return new WebsiteResult(found);
        }
    }
}
