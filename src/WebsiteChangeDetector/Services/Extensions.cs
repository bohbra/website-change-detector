using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebsiteChangeDetector.Common;
using WebsiteChangeDetector.Options;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector.Services
{
    public static class Extensions
    {
        public static void AddServices(this IServiceCollection services, HostBuilderContext hostContext)
        {
            // configure options
            services.Configure<ServiceOptions>(hostContext.Configuration.GetSection(ServiceOptions.Section));

            // worker
            services.AddHostedService<Worker>();

            // web driver
            services.AddSingleton<IWebDriver>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<ServiceOptions>>().Value;
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddArgument("--window-size=1024,768");
                chromeOptions.AddArgument("--disable-logging");
                chromeOptions.AddArgument("--log-level=3");
                if (options.Headless)
                {
                    chromeOptions.AddArguments("headless");
                }
                return new ChromeDriver(chromeOptions);
            });

            // detector
            services.AddSingleton<IDetector, Detector>();

            // websites
            services.AddScoped<IWebsite, Petco>();
            //services.AddScoped<IWebsite, Sharp>();

            // text client
            services.AddSingleton<ITextClient, TextClient>();
        }
    }
}
