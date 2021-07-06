using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WebsiteChangeDetector.Entities;
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
        private bool _loginNeeded = true;
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

            // clear any dialogs
            new Actions(_webDriver).SendKeys(Keys.Enter).Perform();
            new Actions(_webDriver).SendKeys(Keys.Escape).Perform();

            // refresh page to fix any memory leaks
            _webDriver.Navigate().Refresh();

            // retrieve all events
            var allEvents = await _service.GetWantedEvents();

            // log all search dates
            _logger.LogDebug("Starting search for these dates:");
            var tennisEvents = allEvents.ToList();
            foreach (var tennisEvent in tennisEvents)
            {
                _logger.LogDebug($" {tennisEvent.StartDateTime:MM/dd/yyyy hh:mm:ss tt}");
            }

            // check all days
            foreach (var tennisEvent in tennisEvents)
            {
                // time message
                var timeMessage = $"{tennisEvent.StartDateTime:MM/dd/yyyy} @ {tennisEvent.StartTime}";
                _logger.LogDebug($"Searching for {timeMessage}");

                // select date for the current month
                var foundDate = SelectDate(tennisEvent.StartDateTime);
                if (!foundDate)
                    return new WebsiteResult(false);

                // select time
                var (foundTime, courtNumber) = await SelectTime(tennisEvent);
                if (!foundTime)
                {
                    _logger.LogDebug($"Couldn't find time for {timeMessage}");
                    continue;
                }

                // book time
                var success = BookTime(_options.BalboaTennisGuestName);
                if (success)
                {
                    await _service.BookEvent(tennisEvent, courtNumber);
                    return new WebsiteResult(true, false, $"Booked reservation for {timeMessage}, Court {courtNumber}");
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

        private async Task<(bool, int)> SelectTime(BalboaTennisEvent tennisEvent)
        {
            // switch to calendar frame
            _webDriver.SwitchTo().Frame("mygridframe");

            // get calendar table
            var tableElement = _webDriver.FindElement(By.Id("TT"));

            // check for existing bookings
            var allDivs = tableElement.FindElements(By.CssSelector("div"));
            var bookings = new List<IWebElement>();
            foreach (var div in allDivs)
            {
                if (div.Text == "My Booking")
                    bookings.Add(div);
            }
            if (bookings.Any())
            {
                _logger.LogDebug("Skipping, already have a booking for this day");
                return (false, 0);
            }

            // create list for wanted courts to manipulate data
            var wantedCourts = _options.BalboaTennisCourts.ToList();

            // available start times
            var rowTimeStart = tableElement.FindElement(By.CssSelector($"tr[myTag='{tennisEvent.StartTime}']"));
            var openStartTimes = rowTimeStart.FindElements(By.ClassName("f"));
            var openStartTimeSlots = openStartTimes
                .Select(x => new BalboaTennisTimeSlot(x, CalculateCourtNumber(x)))
                .Where(x => wantedCourts.Contains(x.CourtNumber))
                .OrderBy(x => wantedCourts.IndexOf(x.CourtNumber))
                .ToList();

            // available end times
            var rowTimeEnd = tableElement.FindElement(By.CssSelector($"tr[myTag='{tennisEvent.EndTime}']"));
            var openEndTimes = rowTimeEnd.FindElements(By.ClassName("f"));
            var openEndTimeSlots = openEndTimes
                .Select(x => new BalboaTennisTimeSlot(x, CalculateCourtNumber(x)))
                .Where(x => wantedCourts.Contains(x.CourtNumber))
                .OrderBy(x => wantedCourts.IndexOf(x.CourtNumber))
                .ToList();

            // if there are no openings, stop
            if (!openStartTimeSlots.Any() || !openEndTimeSlots.Any())
                return (false, 0);

            // find column match
            foreach (var startTimeSlot in openStartTimeSlots)
            {
                if (!startTimeSlot.WebElement.Displayed || !startTimeSlot.WebElement.Enabled)
                {
                    _logger.LogTrace("Can't select time because element is either disabled or not displayed");
                    continue;
                }

                // r19c7
                var startId = startTimeSlot.WebElement.GetAttribute("id");
                var columnSplit = startId.Split("c");
                var startRow = Convert.ToInt32(columnSplit.First().Substring(1));
                var startColumn = columnSplit.Last();

                // check if end column exists
                var searchId = $"r{startRow + 1}c{startColumn}";
                var matchingEndTime = openEndTimeSlots.FirstOrDefault(x => x.WebElement.GetAttribute("id") == searchId);
                if (matchingEndTime == null) 
                    continue;

                _logger.LogDebug("Found available start and end times");

                // scroll element into view
                _logger.LogDebug("Scrolling start time element into view");
                ((IJavaScriptExecutor)_webDriver).ExecuteScript("arguments[0].scrollIntoView(true);", startTimeSlot.WebElement);
                await Task.Delay(1000);

                _logger.LogDebug($"Clicking {tennisEvent.StartTime} for court {startTimeSlot.CourtNumber}");
                startTimeSlot.WebElement.Click();
                _logger.LogDebug($"Clicking {tennisEvent.EndTime} for court {startTimeSlot.CourtNumber}");
                matchingEndTime.WebElement.Click();
                return (true, startTimeSlot.CourtNumber);
            }

            return (false, 0);
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

            // click email booking
            _webDriver.FindElement(By.Id("btnEmail")).Click();
            _webDriver.FindElement(By.Id("btnSendEmail")).Click();

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
}
