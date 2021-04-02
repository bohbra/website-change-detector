using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebsiteChangeDetector.Extensions;
using WebsiteChangeDetector.Options;
using WindowsInput;
using WindowsInput.Native;

namespace WebsiteChangeDetector.Websites
{
    public class ExpenseReportWebsite : IWebsite
    {
        private readonly ILogger<ExpenseReportWebsite> _logger;
        private readonly IWebDriver _webDriver;
        private readonly ServiceOptions _options;
        private readonly IKeyboardSimulator _keyboard;

        public ExpenseReportWebsite(ILogger<ExpenseReportWebsite> logger, IWebDriver webDriver, IOptions<ServiceOptions> options)
        {
            _logger = logger;
            _webDriver = webDriver;
            _options = options.Value;
            _keyboard = new InputSimulator().Keyboard;
        }

        public async Task<WebsiteResult> Check()
        {
            await CreateReport();
            return new WebsiteResult(true);
        }

        private async Task CreateReport()
        {
            var concurUrl = "https://sso.bd.com/idp/startSSO.ping?PartnerSpId=Concur_Consolidated";
            _webDriver.Navigate().GoToUrl(concurUrl);

            // login to http auth popup
            _logger.LogDebug("Logging in");
            _keyboard.TextEntry(_options.ExpenseReportEmail);
            _keyboard.KeyPress(VirtualKeyCode.TAB);
            _keyboard.TextEntry(_options.ExpenseReportPassword);
            _keyboard.KeyPress(VirtualKeyCode.TAB);
            _keyboard.KeyPress(VirtualKeyCode.RETURN);

            // wait for login to complete
            _logger.LogDebug("Wait for login to complete");
            await Task.Delay(TimeSpan.FromSeconds(4));

            // dismiss any popup
            _logger.LogDebug("Dismissing any popups");
            new Actions(_webDriver).SendKeys(Keys.Escape).Perform();

            // click start report
            var quickStartItems = _webDriver.FindSlowElements(By.ClassName("cnqr-quicktask"));
            var startReport = quickStartItems.First(x => x.Text.Contains("Start a Report"));
            startReport.Click();

            // enter report name
            var reportNameInput = _webDriver.FindSlowElement(By.Name("Report_1207_TRAVELER_ISNEW_Name"));
            reportNameInput.SendKeys($"Home internet ({_options.ExpenseReportTransactionDate})");

            // enter business purpose
            var businessPurposeInput = _webDriver.FindElement(By.Name("Report_1207_TRAVELER_ISNEW_Purpose"));
            businessPurposeInput.SendKeys("Home internet");

            // click save
            _webDriver.FindElement(By.CssSelector(".x-btn-text.menu_save")).Click();

            // click "Internet (home)"
            _webDriver.FindSlowElement(By.Id("etListSearchItem_2_0")).Click();

            // enter transaction date
            _webDriver.FindSlowElement(By.Id("Expense_1409_TRAVELER_P1054_ADJAMT_HD_TransactionDate"))
                .SendKeys(_options.ExpenseReportTransactionDate);

            // enter business purpose
            _webDriver.FindElement(By.Id("Expense_1409_TRAVELER_P1054_ADJAMT_HD_Description"))
                .SendKeys("Home Internet");

            // enter vendor name
            _webDriver.FindElement(By.Id("Expense_1409_TRAVELER_P1054_ADJAMT_HD_VendorDescription"))
                .SendKeys("Cox");

            // enter city of purchase
            _webDriver.FindElement(By.Id("Expense_1409_TRAVELER_P1054_ADJAMT_HD_LocName"))
                .SendKeys("San Diego, California");

            // enter amount
            _webDriver.FindElement(By.Id("Expense_1409_TRAVELER_P1054_ADJAMT_HD_TransactionAmount"))
                .SendKeys("30");

            // click attach receipt
            _webDriver.FindElement(By.XPath("//button[text()='Attach Receipt']")).Click();

            _logger.LogDebug("Please fill in the rest manually");

            // wait
            await Task.Delay(TimeSpan.FromMinutes(100));
        }
    }
}
