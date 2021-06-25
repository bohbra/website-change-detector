using System;

namespace WebsiteChangeDetector.Services
{
    public class BlackoutDate
    {
        public BlackoutDate()
        {

        }

        public BlackoutDate(DateTime blackoutDateTime, bool reservation)
        {
            BlackoutDateTime = blackoutDateTime;
            Reservation = reservation;
        }

        public DateTime BlackoutDateTime { get; set; }

        public bool Reservation { get; set; }
    }
}
