using System.Threading.Tasks;

namespace WebsiteChangeDetector.Websites
{
    public interface IWebsite
    {
        public Task<WebsiteResult> Check();
    }

    public record WebsiteResult(bool Success, bool EmailOnSuccess = true, string Message = "New appointment found!");
}
