using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;
using WebsiteChangeDetector.Console.Logging;
using WebsiteChangeDetector.Console.Options;
using WebsiteChangeDetector.Console.Services;
using WebsiteChangeDetector.Notifications;
using WebsiteChangeDetector.Options;
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
                    config.AddJsonFile("appsettings.overrides.json", true);
                })
                .ConfigureServices((context, services) =>
                {
                    // configure
                    services.Configure<ServiceOptions>(context.Configuration.GetSection(nameof(ServiceOptions)));
                    services.Configure<WebsiteChangeDetectorOptions>(context.Configuration.GetSection(nameof(WebsiteChangeDetectorOptions)));

                    // web driver
                    services.AddSingleton<IWebDriver>(provider =>
                    {
                        var options = provider.GetRequiredService<IOptions<WebsiteChangeDetectorOptions>>().Value;
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

                    // services
                    services.AddHostedService<Service>();
                    services.AddSingleton<IEmailClient, EmailClient>();
                    services.AddSingleton<IWebsiteChangeDetector, WebsiteChangeDetector>();

                    // websites
                    //services.AddScoped<IWebsite, PetcoWebsite>();
                    //services.AddScoped<IWebsite, SharpWebsite>();
                    services.AddScoped<IWebsite, BalboaTennisWebsite>();
                    //services.AddScoped<IWebsite, ExpenseReportWebsite>();
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
