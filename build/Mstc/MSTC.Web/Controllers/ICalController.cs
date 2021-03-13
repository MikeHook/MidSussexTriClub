using System;
using System.Linq;
using System.Web.Mvc;
using Mstc.Core.Providers;
using Umbraco.Web.Mvc;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using System.Text;
using Mstc.Core.DataAccess;

namespace MSTC.Web.Controllers
{
    [MemberAuthorize(AllowType = "Member")]
    public class ICalController : SurfaceController
    {
        protected DataTypeProvider _dataTypeProvider;
        protected IEventSlotRepository _eventSlotRepository;

        public ICalController()
        {
            _dataTypeProvider = new DataTypeProvider(Services.DataTypeService);
            var dataConnection = new DataConnection();
            _eventSlotRepository = new EventSlotRepository(dataConnection);
        }

        //A link to this method can be added in a view like this: @Html.ActionLink("Add event to your calendar", "DownloadiCal", "ICal", new { eventSlotId = 188 }, null)
        //It creates a link like this: <a href="/umbraco/Surface/ICal/DownloadiCal?eventSlotId=188">Add event to your calendar</a>
        [HttpGet]
        public ActionResult DownloadiCal(int eventSlotId)
        {
            var eventTypes = _dataTypeProvider.GetEventTypes();
            var eventSlot = _eventSlotRepository.GetById(eventSlotId);
            eventSlot.EventTypeName = eventTypes.SingleOrDefault(et => et.Id == eventSlot.EventTypeId)?.Name;

            var calendar = new Calendar();
            calendar.Events.Add(new CalendarEvent
            {
                Class = "PUBLIC",
                Summary = eventSlot.EventTypeName, //TODO - Make this a better summary
                Created = new CalDateTime(DateTime.Now),
                //Description = res.Details,
                Start = new CalDateTime(eventSlot.Date),
                //End = new CalDateTime(Convert.ToDateTime(res.EndDate)),
                Sequence = 0,
                Uid = Guid.NewGuid().ToString(),
                //Location = res.Location,
            });

            var serializer = new CalendarSerializer(new SerializationContext());
            var serializedCalendar = serializer.SerializeToString(calendar);
            var bytesCalendar = Encoding.UTF8.GetBytes(serializedCalendar);
            return File(bytesCalendar, "text/calendar", "event.ics");
        }
    }
}