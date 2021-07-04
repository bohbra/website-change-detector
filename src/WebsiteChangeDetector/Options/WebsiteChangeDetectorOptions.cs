using System.Collections.Generic;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector.Options
{
    public class WebsiteChangeDetectorOptions
    {
        public bool Headless { get; set; }
        public int PageLoadDelayInSeconds { get; set; }
        public bool PauseOnSuccess { get; set; }
        public string SendGridApiKey { get; set; }
        public WebsiteName WebsiteName { get; set; }
        public string SharpEmail { get; set; }
        public string SharpPassword { get; set; }
        public string BalboaTennisUser { get; set; }
        public string BalboaTennisPassword { get; set; }
        public string BalboaTennisGuestName { get; set; }
        public IEnumerable<int> BalboaTennisCourts { get; set; }
        public string ExpenseReportEmail { get; set; }
        public string ExpenseReportPassword { get; set; }
        public string ExpenseReportTransactionDate { get; set; }
        public GoogleJsonCredential GoogleServiceAccountCredential { get; set; }
    }

    public class GoogleJsonCredential
    {
        public string type { get; set; }

        public string project_id { get; set; }

        public string private_key_id { get; set; }

        public string private_key { get; set; }

        public string client_email { get; set; }

        public string client_id { get; set; }

        public string auth_uri { get; set; }

        public string token_uri { get; set; }

        public string auth_provider_x509_cert_url { get; set; }

        public string client_x509_cert_url { get; set; }
    }

}
