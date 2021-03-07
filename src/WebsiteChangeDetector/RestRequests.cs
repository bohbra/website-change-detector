using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WebsiteChangeDetector
{
    public class RestRequests
    {
        public void GetWalgreens()
        {
            var client = new HttpClient();
            var url = "https://www.walgreens.com/hcschedulersvc/svc/v1/immunizationLocations/availability";
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("authority", "www.walgreens.com");

            var response = client.GetStringAsync(new Uri(url)).Result;
        }
    }
}
