using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebsiteChangeDetector.Options;

namespace WebsiteChangeDetector.Services
{
    public class GoogleCalendarService : IGoogleCalendarService
    {
        private readonly WebsiteChangeDetectorOptions _options;
        private const string ApplicationName = "Website Change Detector";
        private const string ServiceAccountEmail = "wcd-service-account@website-change-detector.iam.gserviceaccount.com";

        public GoogleCalendarService(IOptions<WebsiteChangeDetectorOptions> options)
        {
            _options = options.Value;
        }

        public CalendarService CreateService()
        {
            // create credential
            var credential = GoogleCredential
                .FromJson(JsonConvert.SerializeObject(_options.GoogleServiceAccountCredential))
                .CreateScoped(CalendarService.Scope.CalendarEvents)
                .CreateWithUser(ServiceAccountEmail);

            // create calendar service
            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }
    }
}
