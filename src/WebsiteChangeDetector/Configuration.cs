using Microsoft.Extensions.Configuration;

namespace WebsiteChangeDetector
{
    public class Configuration
    {
        private static readonly AppSettings _appSettings;

        static Configuration()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.overrides.json", true)
                .Build();

            _appSettings = configuration
                .GetSection("appSettings")
                .Get<AppSettings>();
        }

        public static AppSettings GetAppSettings()
        {
            return _appSettings;
        }
    }

    public class AppSettings
    {
        public string Url { get; set; }
        public bool Headless { get; set; }
        public int PollDelayInSeconds { get; set; }
        public int PageLoadDelayInSeconds { get; set; }
        public string TwilioAccountSid { get; set; }
        public string TwilioAuthToken { get; set; }
        public string TwilioFromPhoneNumber { get; set; }
        public string TwilioToPhoneNumber { get; set; }
        public string SharpEmail { get; set; }
        public string SharpPassword { get; set; }
    }
}
