using System;
using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

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

        public static IWebElement FindSlowElement(this IWebDriver driver, By by)
        {
            var wait = new WebDriverWait(driver, new TimeSpan(0, 0, 10));
            return wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(by));
        }
    }
}
