using System.Collections.Generic;
using System.Threading.Tasks;
using WebsiteChangeDetector.Entities;

namespace WebsiteChangeDetector.Services
{
    public interface IBalboaTennisService
    {
        Task<IEnumerable<BalboaTennisEvent>> GetWantedEvents();
        Task BookEvent(BalboaTennisEvent tennisEvent, int courtNumber);
    }
}
