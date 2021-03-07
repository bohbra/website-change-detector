using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace WebsiteChangeDetector.Websites
{
    public class Sharp : IWebsite
    {
        private readonly IWebDriver _webDriver;
        private readonly AppSettings _settings;

        private const string Url = "https://www.sharp.com/coronavirus/vaccine-volunteers.cfm";
        private const string SearchText = "Non-medical opportunities are filled. We will add future opportunities as spots open.";

        public Sharp(IWebDriver webDriver, AppSettings settings)
        {
            _webDriver = webDriver;
            _settings = settings;
        }

        public async Task<bool> Check()
        {
            // navigate to page
            _webDriver.Navigate().GoToUrl(Url);

            // sleep after load
            await Task.Delay(TimeSpan.FromSeconds(_settings.PageLoadDelayInSeconds));

            // search for text
            var found = !_webDriver.PageSource.Contains(SearchText);

            // log result
            Console.WriteLine($"{DateTime.Now}: Sharp search result: {found}");

            // result
            return found;
        }
    }
}
