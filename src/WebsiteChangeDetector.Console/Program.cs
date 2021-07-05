using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;
using System;
using System.Threading.Tasks;
using OpenQA.Selenium.Remote;
using WebsiteChangeDetector.Console.Logging;
using WebsiteChangeDetector.Console.Options;
using WebsiteChangeDetector.Console.Services;
using WebsiteChangeDetector.Notifications;
using WebsiteChangeDetector.Options;
using WebsiteChangeDetector.Services;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.overrides.json", true);
                })
                .ConfigureServices((context, services) =>
                {
                    // configure
                    services.Configure<ServiceOptions>(context.Configuration.GetSection(nameof(ServiceOptions)));
                    services.Configure<WebsiteChangeDetectorOptions>(context.Configuration.GetSection(nameof(WebsiteChangeDetectorOptions)));

                    // options
                    var options = context.Configuration.GetSection(nameof(WebsiteChangeDetectorOptions)).Get<WebsiteChangeDetectorOptions>();

                    // web driver
                    services.AddSingleton<IWebDriver>(provider =>
                    {
                        var chromeOptions = new ChromeOptions();
                        chromeOptions.AddArgument("--window-size=1024,768");
                        chromeOptions.AddArgument("--disable-logging");
                        chromeOptions.AddArgument("--log-level=3");
                        chromeOptions.AddArgument("--disable-background-timer-throttling");
                        chromeOptions.AddArgument("--disable-backgrounding-occluded-windows");
                        chromeOptions.AddArgument("--disable-breakpad");
                        chromeOptions.AddArgument("--disable-component-extensions-with-background-pages");
                        chromeOptions.AddArgument("--disable-dev-shm-usage");
                        chromeOptions.AddArgument("--disable-extensions");
                        chromeOptions.AddArgument("--disable-features=TranslateUI,BlinkGenPropertyTrees");
                        chromeOptions.AddArgument("--disable-ipc-flooding-protection");
                        chromeOptions.AddArgument("--disable-renderer-backgrounding");
                        chromeOptions.AddArgument("--enable-features=NetworkService,NetworkServiceInProcess");
                        chromeOptions.AddArgument("--force-color-profile=srgb");
                        chromeOptions.AddArgument("--hide-scrollbars");
                        chromeOptions.AddArgument("--metrics-recording-only");
                        chromeOptions.AddArgument("--mute-audio");
                        chromeOptions.AddArgument("--no-sandbox");

                        if (options.Headless)
                        {
                            chromeOptions.AddArguments("headless");
                        }
                        return new ChromeDriver(chromeOptions);
                    });

                    // services
                    services.AddHostedService<Service>();
                    services.AddSingleton<IEmailClient, EmailClient>();
                    services.AddSingleton<IWebsiteChangeDetector, WebsiteChangeDetector>();
                    services.AddSingleton<IGoogleCalendarService, GoogleCalendarService>();
                    services.AddSingleton<IBalboaTennisService, BalboaTennisService>();

                    // websites
                    switch (options.WebsiteName)
                    {
                        case WebsiteName.Balboa:
                            services.AddScoped<IWebsite, BalboaTennisWebsite>();
                            break;
                        case WebsiteName.Petco:
                            services.AddScoped<IWebsite, PetcoWebsite>();
                            break;
                        case WebsiteName.Sharp:
                            services.AddScoped<IWebsite, SharpWebsite>();
                            break;
                        case WebsiteName.Expense:
                            services.AddScoped<IWebsite, ExpenseReportWebsite>();
                            break;
                    }
                })
                .UseSerilog((context, configureLogger) =>
                {
                    configureLogger
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.With<ClassNameEnricher>()
                        .Enrich.WithProperty("EnvironmentName", context.HostingEnvironment.EnvironmentName)
                        .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
                        .Enrich.WithProperty("HostName", Environment.MachineName);
                });
    }
}
