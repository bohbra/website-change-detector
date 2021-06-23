using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebsiteChangeDetector.Services
{
    public interface IBalboaTennisService
    {
        Task<IEnumerable<BlackoutDate>> GetAllBlackoutDatesAsync();
        Task<int> AddBlackoutDateAsync(BlackoutDate entity);
    }
}
