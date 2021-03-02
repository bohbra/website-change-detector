using OpenQA.Selenium.Chrome;
using System;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebsiteChangeDetector
{
    class Program
    {
        static async Task Main()
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--window-size=1024,768");
            chromeOptions.AddArgument("--disable-logging");
            chromeOptions.AddArgument("--log-level=3");

            var settings = Configuration.GetAppSettings();
            if (settings.Headless)
            {
                chromeOptions.AddArguments("headless");
            }
            var driver = new ChromeDriver(chromeOptions);
            var detector = new Detector(driver, settings);

            try
            {
                while (true)
                {
                    await detector.Scan();
                    await Task.Delay(TimeSpan.FromSeconds(settings.PollDelayInSeconds));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred. Message: {ex.Message}. Stack trace: {ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine("Closing driver");
                driver.Close();
            }
        }
    }
}
