using System;

namespace WebsiteChangeDetector.Services
{
    public class BlackoutDate
    {
        public BlackoutDate()
        {

        }

        public BlackoutDate(DateTime blackoutDateTime)
        {
            BlackoutDateTime = blackoutDateTime;
        }

        public DateTime BlackoutDateTime { get; set; }
    }
}
