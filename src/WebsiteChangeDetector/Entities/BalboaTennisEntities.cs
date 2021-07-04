using OpenQA.Selenium;
using System;

namespace WebsiteChangeDetector.Entities
{
    public record BalboaTennisEvent(string Id, DateTime StartDateTime, string StartTime, string EndTime);
    public record BalboaTennisTimeSlot(IWebElement WebElement, int CourtNumber);
}
