using System.Threading.Tasks;

namespace WebsiteChangeDetector.Websites
{
    public interface IWebsite
    {
        public Task<WebsiteResult> Check();
    }

    public class WebsiteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public WebsiteResult(bool success, string message = "New appointment found!")
        {
            Success = success;
            Message = message;
        }
    }
}
