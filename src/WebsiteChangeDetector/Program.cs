using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using WebsiteChangeDetector.Services;

namespace WebsiteChangeDetector
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.AddJsonFile("appsettings.overrides.json", true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddServices(hostContext);
                });
    }
}
