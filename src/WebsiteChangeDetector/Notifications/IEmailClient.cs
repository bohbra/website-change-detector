using System.Threading.Tasks;

namespace WebsiteChangeDetector.Notifications
{
    public interface IEmailClient
    {
        Task Send(string message);
    }
}
