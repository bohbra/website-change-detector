using JsonFlatFileDataStore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IDocumentCollection<BlackoutDate> _blackoutDatesCollection;
        private bool _loginNeeded = true;

        private const string LoginUrl = "https://balboatc.tennisbookings.com/loginx.aspx";

        public BalboaTennisWebsite(ILogger<BalboaTennisWebsite> logger, IWebDriver webDriver, IOptions<ServiceOptions> options)
        {
            _logger = logger;
            _webDriver = webDriver;
            _options = options.Value;

            // initialize data store
            var dataStore = new DataStore("balboa_tennis.json");
            _blackoutDatesCollection = dataStore.GetCollection<BlackoutDate>();
            var blackoutDates = _options.BalboaTennisBlackoutDates.Select(x => new BlackoutDate(x));
            foreach (var blackoutDate in blackoutDates)
            {
                _blackoutDatesCollection.ReplaceOne(x => x.Date == blackoutDate.Date, blackoutDate, true);
            }

            // save search options
            _searchOptions = new BalboaSearch
            {
                GuestName = "Alison",
                StartTime = "5:00pm",
                EndTime = "5:30pm",
                Courts = new[] {24, 23, 22, 11, 12, 13, 14, 15, 16, 17}
            };
        }

        public async Task<WebsiteResult> Check()
        {
            if (DateTime.Now.TimeOfDay > new TimeSpan(0, 0, 0) && 
                DateTime.Now.TimeOfDay < new TimeSpan(7, 30, 0))
            {
                _logger.LogDebug("Search disabled for blackout hours");
                _loginNeeded = true;
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

            // get all days one week from now
            var searchDates = GetDateRange(DateTime.Now.Date, TimeSpan.FromDays(14)).Where(x => 
                    x.DayOfWeek != DayOfWeek.Saturday && 
                    x.DayOfWeek != DayOfWeek.Sunday &&
                    _blackoutDatesCollection.AsQueryable().All(item => item.Date != x));

            // check all days
            foreach (var searchDate in searchDates)
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
                {
                    await _blackoutDatesCollection.ReplaceOneAsync(x => x.Date == searchDate, new BlackoutDate(searchDate), true);
                    return new WebsiteResult(true, $"Booked reservation for {timeMessage}");
                }

                _logger.LogDebug($"Couldn't find time for {timeMessage}");
            }

            _logger.LogDebug("No times available");
            return new WebsiteResult(false);
        }

        private async Task Login()
        {
            _logger.LogDebug($"{nameof(Login)} started");
            _webDriver.Navigate().GoToUrl(LoginUrl);

            // enter email
            var emailInput = _webDriver.FindElement(By.Id("txtUsername"));
            emailInput.Clear();
            emailInput.SendKeys(_options.BalboaTennisUser);

            // enter password
            var passwordInput = _webDriver.FindElement(By.Id("txtPassword"));
            passwordInput.SendKeys(_options.BalboaTennisPassword);

            // login
            _webDriver.FindElement(By.Id("btnLogin")).Click();

            await Task.Delay(TimeSpan.FromSeconds(3));

            _logger.LogDebug($"{nameof(Login)} ended");
        }

        private bool SelectDate(DateTime searchDate)
        {
            // switch to schedules frame
            _webDriver.SwitchTo().DefaultContent();
            try
            {
                _webDriver.SwitchTo().Frame("ifMain");
            }
            catch (Exception e)
            {
                _logger.LogError("Frame ifMain doesn't exist", e);
                return false;
            }

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

            // check for existing bookings
            var allDivs = tableElement.FindElements(By.CssSelector("div"));
            var bookings = allDivs.Where(x => x.Text == "My Booking");
            if (bookings.Any())
            {
                _logger.LogDebug("Skipping, already have a booking for this day");
                return false;
            }

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
                    new Actions(_webDriver).Click(startWebElement).Perform();
                    new Actions(_webDriver).Click(matchingEndTime).Perform();
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

        private IEnumerable<DateTime> GetDateRange(DateTime startDate, TimeSpan offset)
        {
            var endDate = startDate.Add(offset);
            while (startDate <= endDate)
            {
                yield return startDate.Date;
                startDate = startDate.AddDays(1);
            }
        }
    }

    public class BalboaSearch
    {
        public string GuestName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public IEnumerable<int> Courts { get; set; }
    }

    public class BlackoutDate
    {
        public BlackoutDate()
        {
            
        }

        public BlackoutDate(DateTime date)
        {
            Date = date;
        }

        public DateTime Date { get; set; }
    }

}
