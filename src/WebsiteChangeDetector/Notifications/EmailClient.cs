using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using WebsiteChangeDetector.Options;

namespace WebsiteChangeDetector.Notifications
{
    public class EmailClient : IEmailClient
    {
        private readonly SendGridClient _client;

        public EmailClient(IOptions<WebsiteChangeDetectorOptions> options)
        {
            _client = new SendGridClient(options.Value.SendGridApiKey);
        }

        public async Task Send(string message)
        {
            var from = new EmailAddress("detector@bohbra.com", "Detector");
            var subject = "Website Change Detector";
            var to = new EmailAddress("bobgaede@gmail.com", "Robert Gaede");
            var htmlContent = message;
            var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);
            await _client.SendEmailAsync(msg);
        }
    }
}
