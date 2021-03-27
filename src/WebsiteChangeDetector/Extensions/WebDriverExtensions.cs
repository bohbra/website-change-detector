using System.Diagnostics;
using OpenQA.Selenium;

namespace WebsiteChangeDetector.Extensions
{
    public static class WebDriverExtensions
    {
        [DebuggerNonUserCode]
        public static bool FindOptionalElement(this IWebDriver driver, By by, out IWebElement result)
        {
            try
            {
                result = driver.FindElement(by);
                return true;
            }
            catch
            {
                result = null;
            }

            return false;
        }
    }
}
