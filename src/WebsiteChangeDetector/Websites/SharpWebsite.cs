using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebsiteChangeDetector.Options;

namespace WebsiteChangeDetector.Websites
{
    public class SharpWebsite : IWebsite
    {
        private readonly ILogger<SharpWebsite> _logger;
        private readonly IWebDriver _webDriver;
        private readonly WebsiteChangeDetectorOptions _options;
        private readonly List<string> _urls = new();

        private bool _loginNeeded = false;

        public SharpWebsite(ILogger<SharpWebsite> logger, IWebDriver webDriver, IOptions<WebsiteChangeDetectorOptions> options)
        {
            _logger = logger;
            _webDriver = webDriver;
            _options = options.Value;

            // Vaccine volunteer
            //_urls.Add("https://www.sharp.com/health-classes/volunteer-registration-grossmont-center-covid-19-vaccine-clinic-2558");
            //_urls.Add("https://www.sharp.com/health-classes/volunteer-registration-chula-vista-center-covid-19-vaccine-clinic-2554");
            //_urls.Add("https://www.sharp.com/health-classes/volunteer-registration-sharp-metro-campus-covid-19-vaccine-clinic-2564");

            // Vaccine appointments
            //_urls.Add("https://www.calvax.org/clinic/search?q%5Bage_groups_name_in%5D%5B%5D=All+Ages&location=92116&search_radius=All&q%5Bvenue_search_name_or_venue_name_i_cont%5D=&clinic_date_eq%5Byear%5D=&clinic_date_eq%5Bmonth%5D=&clinic_date_eq%5Bday%5D=&q%5Bvaccinations_name_i_cont%5D=&commit=Search#search_results");

            // grossmont
            _urls.Add("https://www.calvax.org/reg/2635358109");
            _urls.Add("https://www.calvax.org/reg/3616629048");
            _urls.Add("https://www.calvax.org/reg/8469350621");
            _urls.Add("https://www.calvax.org/reg/3618229068");

            // coronado
            _urls.Add("https://www.calvax.org/reg/1360422935");
            _urls.Add("https://www.calvax.org/reg/8962136780");

            // south bay
            _urls.Add("https://www.calvax.org/reg/0213962395");
            _urls.Add("https://www.calvax.org/reg/1862932604");
            _urls.Add("https://www.calvax.org/reg/4012648369");
            _urls.Add("https://www.calvax.org/reg/7612608369");

            // knollwood
            _urls.Add("https://www.calvax.org/reg/3901650942");
            _urls.Add("https://www.calvax.org/reg/9238801664");
            _urls.Add("https://www.calvax.org/reg/8314296032");

            // csu san marcos
            _urls.Add("https://www.calvax.org/reg/1552937603");
            _urls.Add("https://www.calvax.org/reg/3619329048");
            _urls.Add("https://www.calvax.org/reg/8669390021");
            _urls.Add("https://www.calvax.org/reg/6073129648");

            // Testing
            //_urls.Add("https://www.sharp.com/health-classes/vaccinator-registration-sharp--county-of-san-diego-covid-19-vaccination-clinic-2555");
        }

        public async Task<WebsiteResult> Check()
        {
            // login first if needed
            if (_loginNeeded)
            {
                _logger.LogDebug($"Login first");
                _webDriver.Navigate().GoToUrl("https://account.sharp.com");

                // enter email
                var emailInput = _webDriver.FindElement(By.Id("email"));
                emailInput.SendKeys(_options.SharpEmail);
                _webDriver.FindElement(By.Id("pre-login-submit-btn")).Click();
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                // enter password
                var passwordInput = _webDriver.FindElement(By.Id("password"));
                passwordInput.SendKeys(_options.SharpPassword);
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
                await Task.Delay(TimeSpan.FromSeconds(_options.PageLoadDelayInSeconds));

              
                // click through pages
                try
                {
                    //var found = await SearchVaccineVolunteer(url);
                    var found = SearchVaccineAppointments(url);
                    if (!found)
                        continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                    
                return new WebsiteResult(true, true, $"Found at {url.Split("/volunteer-registration-").Last()}");
            }

            return new WebsiteResult(false);
        }

        private bool SearchVaccineAppointments(string url)
        {
            if (_webDriver.PageSource.Contains("Clinic does not have any appointment slots available."))
                return false;

            var tableElement = _webDriver.FindElement(By.CssSelector("table.mb-6"));

            // get all rows except the header row
            var tableRow = tableElement.FindElements(By.TagName("tr")).Skip(1);

            foreach (var row in tableRow)
            {
                if (row.Text.Contains("No appointments available"))
                    continue;

                _logger.LogDebug($"Found! {row.Text}");

                // click radio element
                row.FindElement(By.ClassName("form-radio")).Click();

                // save and continue
                _webDriver.FindElement(By.Id("submitButton")).Click();

                return true;
            }

            _logger.LogDebug($"No appointments found for {url}");
            return false;
        }

        private async Task<bool> SearchVaccineVolunteer(string url)
        {
            var allDates = _webDriver.FindElements(By.CssSelector(".section-date.row"));
            var availableDates = allDates.Where(x => x.Text.Contains("More info")).ToList();

            if (availableDates.Any())
                _logger.LogDebug($"Some dates found! Total is {availableDates.Count} for {url}");

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
                _logger.LogDebug($"Sharp search result: True [{url}]");
                var firstDate = searchDates.First();
                firstDate.FindElement(By.CssSelector(".section-more-info.button.storm.full-width.text-center")).Click();
            }
            else
            {
                _logger.LogDebug($"Sharp search result: False [{url}]");
                return false;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(600));
            _webDriver.FindElement(By.Id("add-to-cart")).Click();
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            _webDriver.Navigate().GoToUrl("https://www.sharp.com/cart/checkout/");

            // fill inputs
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            var customFieldsDiv = _webDriver.FindElement(By.ClassName("custom-fields"));
            customFieldsDiv.FindElement(By.XPath("//input[@type='text']")).SendKeys("1111");
            customFieldsDiv.SendKeys(Keys.Tab);
            customFieldsDiv.SendKeys(Keys.Tab);
            customFieldsDiv.SendKeys("Sales Manager");

            return true;
        }
    }
}
