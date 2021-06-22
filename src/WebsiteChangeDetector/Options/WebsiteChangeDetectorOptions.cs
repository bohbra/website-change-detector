using System;
using System.Collections.Generic;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector.Options
{
    public class WebsiteChangeDetectorOptions
    {
        public bool Headless { get; set; }
        public int PollDelayInSeconds { get; set; }
        public int PageLoadDelayInSeconds { get; set; }
        public bool PauseOnSuccess { get; set; }
        public string TwilioAccountSid { get; set; }
        public string TwilioAuthToken { get; set; }
        public string TwilioFromPhoneNumber { get; set; }
        public string TwilioToPhoneNumber { get; set; }
        public string SendGridApiKey { get; set; }
        public WebsiteName WebsiteName { get; set; }
        public string SharpEmail { get; set; }
        public string SharpPassword { get; set; }
        public string BalboaTennisUser { get; set; }
        public string BalboaTennisPassword { get; set; }
        public string BalboaTennisGuestName { get; set; }
        public int BalboaTennisNumberOfDays { get; set; }
        public DateTime BalboaTennisStartTime { get; set; }
        public int BalboaTennisLengthInMinutes { get; set; }
        public IEnumerable<DateTime> BalboaTennisBlackoutDates { get; set; }
        public string ExpenseReportEmail { get; set; }
        public string ExpenseReportPassword { get; set; }
        public string ExpenseReportTransactionDate { get; set; }
    }
}
