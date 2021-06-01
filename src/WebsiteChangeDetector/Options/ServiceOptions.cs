﻿using System;
using System.Collections.Generic;

namespace WebsiteChangeDetector.Options
{
    public class ServiceOptions
    {
        public bool Headless { get; set; }
        public int PollDelayInSeconds { get; set; }
        public int PageLoadDelayInSeconds { get; set; }
        public bool PauseOnSuccess { get; set; }
        public string TwilioAccountSid { get; set; }
        public string TwilioAuthToken { get; set; }
        public string TwilioFromPhoneNumber { get; set; }
        public string TwilioToPhoneNumber { get; set; }
        public string SharpEmail { get; set; }
        public string SharpPassword { get; set; }
        public string BalboaTennisUser { get; set; }
        public string BalboaTennisPassword { get; set; }
        public IEnumerable<DateTime> BalboaTennisBlackoutDates { get; set; }
        public string ExpenseReportEmail { get; set; }
        public string ExpenseReportPassword { get; set; }
        public string ExpenseReportTransactionDate { get; set; }
    }
}
