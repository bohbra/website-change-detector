using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebsiteChangeDetector.Websites
{
    public class Sharp : IWebsite
    {
        private readonly IWebDriver _webDriver;
        private readonly AppSettings _settings;
        private readonly List<string> _urls = new();

        private const string SearchText = "More info";

        private bool _loginNeeded = true;

        public Sharp(IWebDriver webDriver, AppSettings settings)
        {
            _webDriver = webDriver;
            _settings = settings;
            _urls.Add("https://www.sharp.com/health-classes/volunteer-registration-grossmont-center-covid-19-vaccine-clinic-2558");
            _urls.Add("https://www.sharp.com/health-classes/volunteer-registration-chula-vista-center-covid-19-vaccine-clinic-2554");
            _urls.Add("https://www.sharp.com/health-classes/volunteer-registration-sharp-metro-campus-covid-19-vaccine-clinic-2564");
            //_urls.Add("https://www.sharp.com/health-classes/vaccinator-registration-sharp--county-of-san-diego-covid-19-vaccination-clinic-2555");
        }

        public async Task<WebsiteResult> Check()
        {
            // login first if needed
            if (_loginNeeded)
            {
                Console.WriteLine($"{DateTime.Now}: Login first");
                _webDriver.Navigate().GoToUrl("https://account.sharp.com");

                // enter email
                var emailInput = _webDriver.FindElement(By.Id("email"));
                emailInput.SendKeys(_settings.SharpEmail);
                _webDriver.FindElement(By.Id("pre-login-submit-btn")).Click();

                // enter password
                var passwordInput = _webDriver.FindElement(By.Id("password"));
                passwordInput.SendKeys(_settings.SharpPassword);
                _webDriver.FindElement(By.Id("btn-sign-in")).Click();

                await Task.Delay(TimeSpan.FromSeconds(3));
                _loginNeeded = false;
            }

            // check site
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

                if (!found) 
                    continue;

                // click through pages
                try
                {
                    //_webDriver.FindElements(By.CssSelector(".section-more-info.button.storm.full-width.text-center"))[1].Click();
                    _webDriver.FindElement(By.CssSelector(".section-more-info.button.storm.full-width.text-center")).Click();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    _webDriver.FindElement(By.Id("add-to-cart")).Click();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    _webDriver.Navigate().GoToUrl("https://www.sharp.com/cart/checkout/");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                    
                return new WebsiteResult(true, $"Found at {url.Split("/volunteer-registration-").Last()}");
            }

            return new WebsiteResult(false);
        }
    }
}
