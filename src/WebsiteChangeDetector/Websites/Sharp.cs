using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                    var searchDates = allDates.Where(x => x.Text.Contains("Saturday") || x.Text.Contains("Sunday")).ToList();
                    var availableDates = searchDates.Where(x => x.Text.Contains("More info")).ToList();
                    if (availableDates.Any())
                    {
                        Console.WriteLine($"{DateTime.Now}: Sharp search result: True [{url}]");
                        var firstDate = availableDates.First();
                        firstDate.FindElement(By.CssSelector(".section-more-info.button.storm.full-width.text-center")).Click();
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now}: Sharp search result: False [{url}]");
                        continue;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    _webDriver.FindElement(By.Id("add-to-cart")).Click();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    _webDriver.Navigate().GoToUrl("https://www.sharp.com/cart/checkout/");

                    // fill in custom fields
                    var jobTitle = _webDriver.FindElement(By.Id("customField_39_47389_1"));
                    jobTitle.SendKeys("Sales");

                    var departmentName = _webDriver.FindElement(By.Id("customField_40_47389_1"));
                    departmentName.SendKeys("None");

                    var entityName = _webDriver.FindElement(By.Name("customField_41_47389_1"));
                    var selectElement = new SelectElement(entityName);
                    selectElement.SelectByText("N/A (Not a Sharp Employee)");
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
