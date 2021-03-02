using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace WebsiteChangeDetector
{
    public class Detector
    {
        private readonly IWebDriver _webDriver;
        private readonly AppSettings _settings;

        public Detector(IWebDriver webDriver, AppSettings settings)
        {
            _webDriver = webDriver;
            _settings = settings;

            // setup twilio client
            TwilioClient.Init(_settings.TwilioAccountSid, _settings.TwilioAuthToken);
        }

        public async Task Scan()
        {
            _webDriver
                .Navigate()
                .GoToUrl(_settings.Url);

            // sleep after load
            await Task.Delay(TimeSpan.FromSeconds(_settings.PageLoadDelayInSeconds));

            var found = !_webDriver.PageSource.Contains("Sorry, we couldn't find any open appointments");
            Console.WriteLine($"{DateTime.Now}: Appointment search result: {found}");

            if (found)
            {
                SendText();
                throw new Exception("Stop running, found appointment");
            }
        }

        private void SendText()
        {
            MessageResource.Create(
                body: "New appointment found!",
                from: new PhoneNumber(_settings.TwilioFromPhoneNumber),
                to: new PhoneNumber(_settings.TwilioToPhoneNumber)
            );
        }
    }
}
