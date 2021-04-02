using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Linq;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using WebsiteChangeDetector.Extensions;
using WebsiteChangeDetector.Options;

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
            await Task.Delay(TimeSpan.FromSeconds(6));

            // dismiss any popup
            _logger.LogDebug("Dismissing any popups");
            new Actions(_webDriver).SendKeys(Keys.Escape).Perform();

            // wait for main to finish loading after any popups
            _logger.LogDebug("Wait for landing page load to complete");
            await Task.Delay(TimeSpan.FromSeconds(5));

            // click start report
            var quickStartItems = _webDriver.FindElements(By.ClassName("cnqr-quicktask"));
            var startReport = quickStartItems.First(x => x.Text.Contains("Start a Report"));
            startReport.Click();

            // first page details
            var businessPurposeInput = _webDriver.FindSlowElement(By.Id("Report_1207_TRAVELER_ISNEW_Purpose"));
            businessPurposeInput.SendKeys("Work from home expense");

            var reportNameInput = _webDriver.FindElement(By.Id("Report_1207_TRAVELER_ISNEW_Name"));
            reportNameInput.SendKeys("Work from home expense");

            // click save
            _webDriver.FindElement(By.ClassName(" x-btn-text menu_save")).Click();

            // click new expense
            _webDriver.FindSlowElement(By.ClassName(" x-btn-text menu_newexpense2")).Click();

            // click "Internet (home)"
            _webDriver.FindSlowElement(By.ClassName("etListSearchItem_3_0")).Click();

            _logger.LogDebug("Done");

            // wait
            await Task.Delay(TimeSpan.FromMinutes(100));
        }
    }
}
