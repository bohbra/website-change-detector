using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace WebsiteChangeDetector.Websites
{
    public class Sharp : IWebsite
    {
        private readonly IWebDriver _webDriver;
        private readonly AppSettings _settings;
        private readonly List<string> _urls = new();

        private bool _loginNeeded = true;

        public Sharp(IWebDriver webDriver, AppSettings settings)
        {
            _webDriver = webDriver;
            _settings = settings;

            _urls.Add("https://www.sharp.com/health-classes/volunteer-registration-grossmont-center-covid-19-vaccine-clinic-2558");
            _urls.Add("https://www.sharp.com/health-classes/volunteer-registration-chula-vista-center-covid-19-vaccine-clinic-2554");
            _urls.Add("https://www.sharp.com/health-classes/volunteer-registration-sharp-metro-campus-covid-19-vaccine-clinic-2564");

            // Testing
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

                // click through pages
                try
                {
                    var allDates = _webDriver.FindElements(By.CssSelector(".section-date.row"));
                    var availableDates = allDates.Where(x => x.Text.Contains("More info")).ToList();

                    if (availableDates.Any())
                        Console.WriteLine($"{DateTime.Now}: Some dates found! Total is {availableDates.Count} for {url}");

                    var searchDatesExcluded = availableDates.Where(x => !x.Text.Contains("March 23")).ToList();
                    //var searchDates =  searchDatesExcluded.Where(x => 
                    //    x.Text.Contains("am to") || (x.Text.Contains("Saturday") || x.Text.Contains("Sunday"))).ToList();
                    var searchDates = searchDatesExcluded.Where(x =>
                       (!x.Text.Contains("am to") && x.Text.Contains("March 16")) ||
                       (!x.Text.Contains("am to") && x.Text.Contains("March 17")) ||
                       (!x.Text.Contains("am to") && x.Text.Contains("March 18")) ||
                       (x.Text.Contains("March 20") || x.Text.Contains("March 21"))).ToList();

                    if (searchDates.Any())
                    {
                        Console.WriteLine($"{DateTime.Now}: Sharp search result: True [{url}]");
                        var firstDate = searchDates.First();
                        firstDate.FindElement(By.CssSelector(".section-more-info.button.storm.full-width.text-center")).Click();
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now}: Sharp search result: False [{url}]");
                        continue;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(600));
                    _webDriver.FindElement(By.Id("add-to-cart")).Click();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    _webDriver.Navigate().GoToUrl("https://www.sharp.com/cart/checkout/");

                    // fill inputs
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    var customFieldsDiv = _webDriver.FindElement(By.ClassName("custom-fields"));

                    customFieldsDiv.FindElement(By.XPath("//input[@type='text']")).SendKeys("Sales");
                    customFieldsDiv.SendKeys(Keys.Tab);
                    customFieldsDiv.SendKeys("Norton");
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
