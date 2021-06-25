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
using WebsiteChangeDetector.Services;

namespace WebsiteChangeDetector.Websites
{
    public class BalboaTennisWebsite : IWebsite
    {
        private readonly ILogger<BalboaTennisWebsite> _logger;
        private readonly IWebDriver _webDriver;
        private readonly IBalboaTennisService _service;
        private readonly WebsiteChangeDetectorOptions _options;
        private readonly BalboaSearch _searchOptions;
        private bool _loginNeeded = true;
        private IEnumerable<BlackoutDate> _blackoutDates;

        private const string LoginUrl = "https://balboatc.tennisbookings.com/loginx.aspx";

        public BalboaTennisWebsite(
            ILogger<BalboaTennisWebsite> logger, 
            IWebDriver webDriver, 
            IOptions<WebsiteChangeDetectorOptions> options,
            IBalboaTennisService service)
        {
            _logger = logger;
            _webDriver = webDriver;
            _service = service;
            _options = options.Value;

            // save search options
            _searchOptions = new BalboaSearch(
                _options.BalboaTennisGuestName,
                _options.BalboaTennisStartTime.ToString("h:mm") + "pm",
                _options.BalboaTennisStartTime.AddMinutes(_options.BalboaTennisLengthInMinutes).ToString("h:mm") + "pm",
                new[] {24, 23, 22, 11, 12, 13, 14, 15, 16, 17}
            );
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

            // get blackout dates
            if (_blackoutDates == null)
            {
                _logger.LogDebug("Getting all blackout dates");
                _blackoutDates = await _service.GetAllBlackoutDatesAsync();

                // sync config blackout dates with persisted ones
                var persistedBlackoutDates = _blackoutDates.Select(x => x.BlackoutDateTime);
                var configBlackOutDates = _options.BalboaTennisBlackoutDates;
                var newBlackOutDates = configBlackOutDates
                    .Except(persistedBlackoutDates)
                    .Select(x => new BlackoutDate(x, false))
                    .ToList();

                // add new blackout dates
                foreach (var newBlackOutDate in newBlackOutDates)
                {
                    _logger.LogDebug($"Adding new blackout date: {newBlackOutDate.BlackoutDateTime}");
                    await _service.AddBlackoutDateAsync(newBlackOutDate);
                }

                // update blackout dates
                _blackoutDates = _blackoutDates.Union(newBlackOutDates);
            }

            // login if needed
            if (_loginNeeded)
            {
                _loginNeeded = false;
                await Login();
            }

            // refresh page to fix any memory leaks
            _webDriver.Navigate().Refresh();

            // get all days based on number of days from now
            var searchDates = GetDateRange(DateTime.Now.Date, TimeSpan.FromDays(_options.BalboaTennisNumberOfDays)).Where(x => 
                    x.DayOfWeek != DayOfWeek.Saturday && 
                    x.DayOfWeek != DayOfWeek.Sunday &&
                    _blackoutDates.All(item => item.BlackoutDateTime != x));

            // log all search dates
            _logger.LogDebug("Starting search for these dates:");
            foreach (var searchDate in searchDates)
            {
                _logger.LogDebug($" {searchDate:MM/dd/yyyy}");
            }

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
                    await _service.AddBlackoutDateAsync(new BlackoutDate(searchDate, true));
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
            var openStartTimeSlots = openStartTimes
                .Select(x => new BalboaTimeSlot(x, CalculateCourtNumber(x)))
                .Where(x => _searchOptions.Courts.Contains(x.CourtNumber))
                .ToList();

            // available end times
            var rowTimeEnd = tableElement.FindElement(By.CssSelector($"tr[myTag='{_searchOptions.EndTime}']"));
            var openEndTimes = rowTimeEnd.FindElements(By.ClassName("f"));
            var openEndTimeSlots = openEndTimes
                .Select(x => new BalboaTimeSlot(x, CalculateCourtNumber(x)))
                .Where(x => _searchOptions.Courts.Contains(x.CourtNumber))
                .ToList();

            // if there are no openings, stop
            if (!openStartTimeSlots.Any() || !openEndTimeSlots.Any())
                return false;

            // find column match
            foreach (var startTime in openStartTimeSlots)
            {
                if (!startTime.WebElement.Displayed || !startTime.WebElement.Enabled)
                {
                    _logger.LogTrace("Can't select time because element is either disabled or not displayed");
                    continue;
                }

                // r19c7
                var startId = startTime.WebElement.GetAttribute("id");
                var columnSplit = startId.Split("c");
                var startRow = Convert.ToInt32(columnSplit.First().Substring(1));
                var startColumn = columnSplit.Last();

                // check if end column exists
                var searchId = $"r{startRow + 1}c{startColumn}";
                var matchingEndTime = openEndTimeSlots.FirstOrDefault(x => x.WebElement.GetAttribute("id") == searchId);
                if (matchingEndTime == null) 
                    continue;

                _logger.LogDebug("Found available start and end times");
                _logger.LogDebug($"Clicking {_searchOptions.StartTime} for court {startTime.CourtNumber}");
                new Actions(_webDriver).Click(startTime.WebElement).Perform();
                _logger.LogDebug($"Clicking {_searchOptions.EndTime} for court {startTime.CourtNumber}");
                new Actions(_webDriver).Click(matchingEndTime.WebElement).Perform();
                return true;
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

    public record BalboaSearch(string GuestName, string StartTime, string EndTime, IEnumerable<int> Courts);

    public record BalboaTimeSlot(IWebElement WebElement, int CourtNumber);
}
