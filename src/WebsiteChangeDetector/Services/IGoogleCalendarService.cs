using Google.Apis.Calendar.v3;

namespace WebsiteChangeDetector.Services
{
    public interface IGoogleCalendarService
    {
        CalendarService CreateService();
    }
}
