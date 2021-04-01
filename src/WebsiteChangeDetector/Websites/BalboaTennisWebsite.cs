using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium.Interactions;
using WebsiteChangeDetector.Extensions;
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
                DelaySearchTime = null,
                Dates = new[]
                {
                    new DateTime(2021, 4, 5),
                    new DateTime(2021, 4, 6),
                    new DateTime(2021, 4, 7),
                    new DateTime(2021, 4, 8),
                    new DateTime(2021, 4, 9)
                },
                StartTime = "5:00pm",
                EndTime = "5:30pm",
                Courts = new[] {24, 23, 22, 11, 12, 13, 14, 15, 16, 17}
            };
        }

        public async Task<WebsiteResult> Check()
        {
            // check if there's a delay to start searching
            if (DateTime.Now.TimeOfDay <= _searchOptions.DelaySearchTime?.TimeOfDay)
            {
                _logger.LogDebug($"Delaying start until {_searchOptions.DelaySearchTime?.TimeOfDay}");
                return new WebsiteResult(false);
            }

            // login if needed
            if (_loginNeeded)
            {
                _loginNeeded = false;
                await Login();
            }

            // refresh page to fix any memory leaks
            _webDriver.Navigate().Refresh();

            // check all days
            foreach (var searchDate in _searchOptions.Dates)
            {
                // time message
                var timeMessage = $"{searchDate:MM/dd/yyyy} @ {_searchOptions.StartTime}";
                _logger.LogDebug($"Searching for {timeMessage}");

                // can't book a date in the past
                if (searchDate.Date < DateTime.Now.Date)
                {
                    _logger.LogWarning("Can't book date in the past");
                    continue;
                }

                // select date for the current month
                var foundDate = SelectDate(searchDate);
                if (!foundDate)
                    return new WebsiteResult(false);

                // select time
                var foundTime = SelectTime();
                if (!foundTime)
                {
                    _logger.LogDebug($"Couldn't find time for {timeMessage}");
                    continue;
                }

                // book time
                var success = BookTime(_searchOptions.GuestName);
                if (success) 
                    return new WebsiteResult(true, $"Booked reservation for {timeMessage}");

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

        private bool SelectDate(DateTime searchDate)
        {
            // switch to schedules frame
            _webDriver.SwitchTo().DefaultContent();
            _webDriver.SwitchTo().Frame("ifMain");

            // select the month
            var prevMonthLink = _webDriver.FindElement(By.CssSelector("a[title='Go to the previous month']"));
            var nextMonthLink = _webDriver.FindElement(By.CssSelector("a[title='Go to the next month']"));
            if (searchDate.Month == DateTime.ParseExact(prevMonthLink.Text, "MMM", CultureInfo.CurrentCulture).Month)
            {
                _logger.LogTrace("Selecting previous month on the calendar");
                prevMonthLink.Click();
            } 
            else if (searchDate.Month == DateTime.ParseExact(nextMonthLink.Text, "MMM", CultureInfo.CurrentCulture).Month)
            {
                _logger.LogTrace("Selecting next month on the calendar");
                nextMonthLink.Click();
            }

            // select date in calendar
            var tableElement = _webDriver.FindElement(By.Id("Calendar1"));

            // search dates for the currently selected month (ignores calendarna)
            var selectedDates = tableElement.FindElements(By.ClassName("calendarseldate"));
            var unselectedDates = tableElement.FindElements(By.ClassName("calendarunsel"));
            var searchableDates = selectedDates.Concat(unselectedDates);

            var date = searchableDates.FirstOrDefault(x => Convert.ToInt32(x.Text) == searchDate.Day);
            if (date == null)
            {
                _logger.LogWarning($"Couldn't find date {searchDate}");
                return false;
            }

            try
            {
                // A popup occurs right when the click does
                date.Click();
            }
            catch (ElementClickInterceptedException e)
            {
                _logger.LogWarning("Click intercepted by dialog when trying to select date, skipping", e);
                return false;
            }

            // check if popup dialog occurred after selecting date
            if (DetectAlertDialog())
                return false;

            return true;
        }

        private bool SelectTime()
        {
            // switch to calendar frame
            _webDriver.SwitchTo().Frame("mygridframe");

            // get calendar table
            var tableElement = _webDriver.FindElement(By.Id("TT"));

            // available start times
            var rowTimeStart = tableElement.FindElement(By.CssSelector($"tr[myTag='{_searchOptions.StartTime}']"));
            var openStartTimes = rowTimeStart.FindElements(By.ClassName("f"));
            var openStartTimesByCourt = openStartTimes.Where(x => _searchOptions.Courts.Contains(CalculateCourtNumber(x))).ToList();

            // available end times
            var rowTimeEnd = tableElement.FindElement(By.CssSelector($"tr[myTag='{_searchOptions.EndTime}']"));
            var openEndTimes = rowTimeEnd.FindElements(By.ClassName("f"));
            var openEndTimesByCourt = openEndTimes.Where(x => _searchOptions.Courts.Contains(CalculateCourtNumber(x))).ToList();

            // if there are no openings, stop
            if (!openStartTimesByCourt.Any() || !openEndTimesByCourt.Any())
                return false;

            // find column match
            foreach (var startWebElement in openStartTimesByCourt)
            {
                if (!startWebElement.Displayed || !startWebElement.Enabled)
                {
                    _logger.LogTrace("Can't select time because element is either disabled or not displayed");
                    continue;
                }

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
            var courtElement = tdElement.GetAttribute("id").Split("c").Last();
            var courtNumber = Convert.ToInt32(courtElement) + 3;
            return courtNumber;
        }

        private bool BookTime(string guestName)
        {
            // switch to schedules frame
            _webDriver.SwitchTo().DefaultContent();
            _webDriver.SwitchTo().Frame("ifMain");

            // click book
            _webDriver.FindElement(By.Id("btnnext")).Click();
            _logger.LogDebug("Clicking book on main page");

            // check if alert dialog occurred after selecting date
            if (DetectAlertDialog())
                return false;

            // switch to popup frame
            _webDriver.SwitchTo().DefaultContent();
            _webDriver.SwitchTo().Frame("dialogIF");

            // enter player 2 name
            _webDriver.FindElement(By.Id("n2")).SendKeys(guestName);

            // check radio box for guest
            _webDriver.FindElement(By.Id("y2")).Click();

            // confirm
            _webDriver.FindElement(By.Id("btnConfirmAndPay")).Click();
            _logger.LogDebug("Clicking book on dialog");

            // check if alert dialog occurred after selecting book
            if (DetectAlertDialog())
                return false;

            return true;
        }

        private bool DetectAlertDialog()
        {
            if (!_webDriver.FindOptionalElement(By.ClassName("tbalertmodal"), out var dialog)) 
                return false;

            _logger.LogDebug($"Found dialog, skipping. Text = {dialog.Text}");
            new Actions(_webDriver).SendKeys(Keys.Escape).Perform();
            return true;
        }
    }

    public class BalboaSearch
    {
        public string GuestName { get; set; }
        public DateTime? DelaySearchTime { get; set; }
        public IEnumerable<DateTime> Dates { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public IEnumerable<int> Courts { get; set; }
    }
}
