﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebsiteChangeDetector.Options;
using WebsiteChangeDetector.Services;
using WebsiteChangeDetector.Websites;

namespace WebsiteChangeDetector.Common
{
    public static class Extensions
    {
        public static void AddServices(this IServiceCollection services, HostBuilderContext hostContext)
        {
            // configure options
            services.Configure<ServiceOptions>(hostContext.Configuration.GetSection("Service"));

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

            // websites
            //services.AddScoped<IWebsite, PetcoWebsite>();
            services.AddScoped<IWebsite, SharpWebsite>();

            // text client
            services.AddSingleton<ITextClient, TextClient>();
        }
    }
}