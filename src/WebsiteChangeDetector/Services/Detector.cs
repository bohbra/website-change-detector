using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using WebsiteChangeDetector.Options;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector.Services
{
    public class Detector : IDetector
    {
        private readonly ILogger<Detector> _logger;
        private readonly ServiceOptions _options;
        private readonly IEnumerable<IWebsite> _websites;

        public Detector(ILogger<Detector> logger, IOptions<ServiceOptions> options, IEnumerable<IWebsite> websites)
        {
            _logger = logger;
            _options = options.Value;
            _websites = websites;

            // setup twilio client
            TwilioClient.Init(_options.TwilioAccountSid, _options.TwilioAuthToken);
        }

        public async Task Scan()
        {
            _logger.LogDebug("Starting scan");

            // check all websites
            foreach (var website in _websites)
            {
                var result = await website.Check();
                if (!result.Success) 
                    continue;
                SendText(result.Message);
                _logger.LogDebug("Pausing");
                await Task.Delay(TimeSpan.FromHours(4));
            }
        }

        private void SendText(string message)
        {
            MessageResource.Create(
                body: message,
                @from: new PhoneNumber(_options.TwilioFromPhoneNumber),
                to: new PhoneNumber(_options.TwilioToPhoneNumber)
            );
        }
    }
}
