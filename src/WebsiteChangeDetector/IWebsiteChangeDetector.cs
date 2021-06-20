using System.Threading.Tasks;

namespace WebsiteChangeDetector
{
    public interface IWebsiteChangeDetector
    {
        Task Scan();
    }
}
