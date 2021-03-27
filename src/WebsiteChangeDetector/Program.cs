﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Serilog;
using WebsiteChangeDetector.Common;

namespace WebsiteChangeDetector
{
    class Program
    {
        public const string DefaultConsoleTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {ClassName} - {Message:lj} {NewLine}{Exception}";

        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configHost =>
                {
                    configHost.AddJsonFile("appsettings.overrides.json", true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddServices(hostContext);
                })
                .UseSerilog((context, configureLogger) =>
                {
                    configureLogger
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.WithProperty("EnvironmentName", context.HostingEnvironment.EnvironmentName)
                        .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
                        .Enrich.WithProperty("HostName", Environment.MachineName)
                        .WriteTo.Console(outputTemplate: DefaultConsoleTemplate);
                });
    }
}
