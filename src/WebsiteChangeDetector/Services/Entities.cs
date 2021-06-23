using System;

namespace WebsiteChangeDetector.Services
{
    public class BlackoutDate
    {
        public BlackoutDate()
        {

        }

        public BlackoutDate(DateTime date)
        {
            Date = date;
        }

        public DateTime Date { get; set; }
    }
}
