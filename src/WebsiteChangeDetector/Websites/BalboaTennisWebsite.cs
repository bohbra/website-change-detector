using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebsiteChangeDetector.Options;

namespace WebsiteChangeDetector.Websites
{
    public class BalboaTennisWebsite : IWebsite
    {
        private readonly ILogger<BalboaTennisWebsite> _logger;
        private readonly IWebDriver _webDriver;
        private readonly ServiceOptions _options;
        private readonly BalboaSearch _searchOptions;
        private bool _loginNeeded = true;

        private const string LoginUrl = "https://balboatc.tennisbookings.com/loginx.aspx";

        public BalboaTennisWebsite(ILogger<BalboaTennisWebsite> logger, IWebDriver webDriver, IOptions<ServiceOptions> options)
        {
            _logger = logger;
            _webDriver = webDriver;
            _options = options.Value;

            _searchOptions = new BalboaSearch
            {
                GuestName = "Alison",
                DaysOfMonth = new[] {23, 25},
                StartTime = "5:00pm",
                EndTime = "5:30pm",
                Courts = new[] {24, 23, 22, 11, 12, 13, 14, 15, 16, 17}
            };
        }

        public async Task<WebsiteResult> Check()
        {
            // login if needed
            if (_loginNeeded)
            {
                _loginNeeded = false;
                await Login();
            }

            // check all days
            foreach (var day in _searchOptions.DaysOfMonth)
            {
                // switch to schedules frame
                _webDriver.SwitchTo().DefaultContent();
                _webDriver.SwitchTo().Frame("ifMain");

                // select date for the current month
                SelectDate(day);

                // switch to calendar frame
                _webDriver.SwitchTo().Frame("mygridframe");

                // time message
                var timeMessage = $"{DateTime.Now:MMMM} {day} @ {_searchOptions.StartTime}";

                // select times
                var foundTime = SelectTimes();
                if (foundTime)
                {
                    HandleDialog(_searchOptions.GuestName);
                    return new WebsiteResult(true, $"Booked reservation for {timeMessage}");
                }

                _logger.LogDebug($"Couldn't find time for {timeMessage}");
            }

            _logger.LogDebug("No times available");
            return new WebsiteResult(false);
        }

        private async Task Login()
        {
            _logger.LogDebug("Login started");
            _webDriver.Navigate().GoToUrl(LoginUrl);

            // enter email
            var emailInput = _webDriver.FindElement(By.Id("txtUsername"));
            emailInput.SendKeys(_options.BalboaTennisEmail);

            // enter password
            var passwordInput = _webDriver.FindElement(By.Id("txtPassword"));
            passwordInput.SendKeys(_options.BalboaTennisPassword);

            // login
            _webDriver.FindElement(By.Id("btnLogin")).Click();

            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        private void SelectDate(int dayOfMonth)
        {
            var tableElement = _webDriver.FindElement(By.Id("Calendar1"));
            var date = tableElement.FindElement(By.Id($"Q{dayOfMonth}"));
            date.Click();
        }

        private bool SelectTimes()
        {
            var tableElement = _webDriver.FindElement(By.Id("TT"));

            // available start times
            var rowTimeStart = tableElement.FindElement(By.CssSelector($"tr[myTag='{_searchOptions.StartTime}']"));
            var openStartTimes = rowTimeStart.FindElements(By.ClassName("f"));
            var openStartTimesByCourt = openStartTimes.Where(x => _searchOptions.Courts.Contains(CalculateCourtNumber(x)));

            // available end times
            var rowTimeEnd = tableElement.FindElement(By.CssSelector($"tr[myTag='{_searchOptions.EndTime}']"));
            var openEndTimes = rowTimeEnd.FindElements(By.ClassName("f"));
            var openEndTimesByCourt = openEndTimes.Where(x => _searchOptions.Courts.Contains(CalculateCourtNumber(x)));

            // if there are no openings, stop
            if (!openStartTimesByCourt.Any() || !openEndTimesByCourt.Any())
                return false;

            // find column match
            foreach (var startWebElement in openStartTimesByCourt)
            {
                // r19c7
                var startId = startWebElement.GetAttribute("id");
                var columnSplit = startId.Split("c");
                var startRow = Convert.ToInt32(columnSplit.First().Substring(1));
                var startColumn = columnSplit.Last();

                // check if end column exists
                var searchId = $"r{startRow + 1}c{startColumn}";
                var matchingEndTime = openEndTimesByCourt.FirstOrDefault(x => x.GetAttribute("id") == searchId);
                if (matchingEndTime != null)
                {
                    // select both times
                    startWebElement.Click();
                    matchingEndTime.Click();
                    return true;
                }
            }

            return false;
        }

        private int CalculateCourtNumber(IWebElement tdElement)
        {
            var courtNumber = Convert.ToInt32(tdElement.GetAttribute("id").Split('c').Last()) + 3;
            return courtNumber;
        }

        private void HandleDialog(string guestName)
        {
            // switch to schedules frame
            _webDriver.SwitchTo().DefaultContent();
            _webDriver.SwitchTo().Frame("ifMain");

            // click book
            _webDriver.FindElement(By.Id("btnnext")).Click();

            // switch to popup frame
            _webDriver.SwitchTo().DefaultContent();
            _webDriver.SwitchTo().Frame("dialogIF");

            // enter player 2 name
            _webDriver.FindElement(By.Id("n2")).SendKeys(guestName);

            // check radio box for guest
            _webDriver.FindElement(By.Id("y2")).Click();

            // confirm
            _webDriver.FindElement(By.Id("btnConfirmAndPay")).Click();
        }
    }

    public class BalboaSearch
    {
        public string GuestName { get; set; }
        public IEnumerable<int> DaysOfMonth { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public IEnumerable<int> Courts { get; set; }
    }
}
