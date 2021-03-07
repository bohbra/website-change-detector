using System.Threading.Tasks;

namespace WebsiteChangeDetector.Websites
{
    public interface IWebsite
    {
        public Task<bool> Check();
    }
}
