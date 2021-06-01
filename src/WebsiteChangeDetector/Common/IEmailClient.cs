using System.Threading.Tasks;

namespace WebsiteChangeDetector.Common
{
    public interface IEmailClient
    {
        Task Send(string message);
    }
}
