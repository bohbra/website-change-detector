using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebsiteChangeDetector.Websites
{
    public class Sharp : IWebsite
    {
        private readonly IWebDriver _webDriver;
        private readonly AppSettings _settings;
        private readonly List<string> _urls = new();

        private const string SearchText = "More info";

        public Sharp(IWebDriver webDriver, AppSettings settings)
        {
            _webDriver = webDriver;
            _settings = settings;
            _urls.Add("https://www.sharp.com/health-classes/volunteer-registration-grossmont-center-covid-19-vaccine-clinic-2558");
            _urls.Add("https://www.sharp.com/health-classes/volunteer-registration-chula-vista-center-covid-19-vaccine-clinic-2554");
        }

        public async Task<bool> Check()
        {
            foreach (var url in _urls)
            {
                // navigate to page
                _webDriver.Navigate().GoToUrl(url);

                // sleep after load
                await Task.Delay(TimeSpan.FromSeconds(_settings.PageLoadDelayInSeconds));

                // search for text
                var found = _webDriver.PageSource.Contains(SearchText);

                // log result
                Console.WriteLine($"{DateTime.Now}: Sharp search result: {found} [{url}]");

                // result
                if (found)
                    return true;
            }

            return false;
        }
    }
}
