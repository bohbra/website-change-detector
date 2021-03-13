using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using WebsiteChangeDetector.Options;

namespace WebsiteChangeDetector.Common
{
    public class TextClient : ITextClient
    {
        private readonly ServiceOptions _options;

        public TextClient(IOptions<ServiceOptions> options)
        {
            _options = options.Value;

            // setup twilio client
            TwilioClient.Init(_options.TwilioAccountSid, _options.TwilioAuthToken);
        }

        public void Send(string message)
        {
            MessageResource.Create(
                body: message,
                @from: new PhoneNumber(_options.TwilioFromPhoneNumber),
                to: new PhoneNumber(_options.TwilioToPhoneNumber)
            );
        }
    }
}
