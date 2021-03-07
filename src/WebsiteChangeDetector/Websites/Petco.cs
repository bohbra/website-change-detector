﻿using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace WebsiteChangeDetector.Websites
{
    public class Petco : IWebsite
    {
        private readonly IWebDriver _webDriver;
        private readonly AppSettings _settings;

        private const string Url = "https://mychart-openscheduling.et0502.epichosted.com/UCSD/SignupAndSchedule/EmbeddedSchedule?dept=9990995&id=99909951,99909952,99909953,99909954,99909955,99909956&vt=3550&payor=1003,2020,1023,1013,1014,1017,1022,1039,1042,1121,1046,1050,1048,1055,1236,1079,1086,1093,2003,1088,-1,-2,-3";
        private const string SearchText = "Sorry, we couldn't find any open appointments";

        public Petco(IWebDriver webDriver, AppSettings settings)
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
            Console.WriteLine($"{DateTime.Now}: Petco search result: {found}");

            // result
            return found;
        }
    }
}
