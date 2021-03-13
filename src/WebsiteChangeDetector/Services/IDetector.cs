using System.Threading.Tasks;

namespace WebsiteChangeDetector.Services
{
    public interface IDetector
    {
        Task Scan();
    }
}
