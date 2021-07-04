using Google.Apis.Calendar.v3;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebsiteChangeDetector.Entities;

namespace WebsiteChangeDetector.Services
{
    public class BalboaTennisService : IBalboaTennisService
    {
        private const string BalboaTennisCalendarId = "crnpk6qltpq5nca7uo4lrfddbg@group.calendar.google.com";
        private const string BookedSummary = "🟢 Tennis: Booked";

        private readonly ILogger<BalboaTennisService> _logger;

        private readonly CalendarService _calendarService;

        public BalboaTennisService(ILogger<BalboaTennisService> logger, IGoogleCalendarService calendarService)
        {
            _logger = logger;
            _calendarService = calendarService.CreateService();
        }

        public async Task<IEnumerable<BalboaTennisEvent>> GetWantedEvents()
        {
            // define parameters
            var request = _calendarService.Events.List(BalboaTennisCalendarId);
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 30;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // execute
            var events = await request.ExecuteAsync();

            // create events
            var tennisEvents = new List<BalboaTennisEvent>();

            // check for issue with events
            if (events.Items == null || events.Items.Count == 0)
                throw new Exception("Couldn't find any available tennis events");

            // populate tennis event data
            foreach (var eventItem in events.Items)
            {
                // ignore events with no time
                if (eventItem.Start.DateTime == null)
                {
                    _logger.LogWarning($"Missing time, ignoring {eventItem.Start.DateTime}");
                    continue;
                }

                // balboa tennis only supports 8 days in future
                if (eventItem.Start.DateTime > DateTime.Now.Date.AddDays(8))
                    continue;

                // ignore any other event type that isn't wanted
                if (!eventItem.Summary.Contains("Wanted"))
                    continue;

                var startDateTime = eventItem.Start.DateTime.Value;
                var startTime = startDateTime.ToString("h:mm") + "pm";
                var endTime = startDateTime.AddMinutes(30).ToString("h:mm") + "pm";
                tennisEvents.Add(new BalboaTennisEvent(eventItem.Id, eventItem.Start.DateTime.Value, startTime, endTime));
            }
            return tennisEvents;
        }

        public async Task BookEvent(BalboaTennisEvent tennisEvent)
        {
            _logger.LogDebug($"{nameof(BookEvent)} called");
            // retrieve event from api
            var calendarEvent = await _calendarService.Events.Get(BalboaTennisCalendarId, tennisEvent.Id).ExecuteAsync();

            // make a change
            calendarEvent.Summary = BookedSummary;

            // update the event
            await _calendarService.Events.Update(calendarEvent, BalboaTennisCalendarId, calendarEvent.Id).ExecuteAsync();
        }
    }
}
