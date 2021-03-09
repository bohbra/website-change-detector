using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector
{
    public class Detector
    {
        private readonly AppSettings _settings;
        private readonly List<IWebsite> _websites = new();

        public Detector(IWebDriver webDriver, AppSettings settings)
        {
            _settings = settings;

            // setup twilio client
            TwilioClient.Init(_settings.TwilioAccountSid, _settings.TwilioAuthToken);

            // setup websites
            //_websites.Add(new Petco(webDriver, settings));
            _websites.Add(new Sharp(webDriver, settings));
        }

        public async Task Scan()
        {
            // check all websites
            foreach (var website in _websites)
            {
                var result = await website.Check();
                if (!result.Success) 
                    continue;
                SendText(result.Message);
                Console.WriteLine("Pausing");
                await Task.Delay(TimeSpan.FromHours(4));
            }
        }

        private void SendText(string message)
        {
            MessageResource.Create(
                body: message,
                from: new PhoneNumber(_settings.TwilioFromPhoneNumber),
                to: new PhoneNumber(_settings.TwilioToPhoneNumber)
            );
        }
    }
}
