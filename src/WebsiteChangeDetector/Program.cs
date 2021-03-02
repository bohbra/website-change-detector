using OpenQA.Selenium.Chrome;
using System;
using System.Threading.Tasks;

namespace WebsiteChangeDetector
{
    class Program
    {
        static async Task Main()
        {
            var chromeOptions = new ChromeOptions();
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
