using System;
using System.Collections.Generic;
using System.Linq;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using Umbraco.Web.WebApi;
using Umbraco.Web.PublishedContentModels;
using System.Web.Http;
using Mstc.Core.DataAccess;
using Mstc.Core.Domain;

namespace MSTC.Web.Controllers
{
	[MemberAuthorize(AllowGroup = "Member")]
	public class EventController : UmbracoApiController
    {
		DataTypeProvider _dataTypeProvider;
		IEventSlotRepository _eventSlotRepository;

		public EventController()
		{
			_dataTypeProvider = new DataTypeProvider(Services.DataTypeService);
			_eventSlotRepository = new EventSlotRepository(new DataConnection());
		}

		[HttpGet]
		public IEnumerable<EventType> BookableEvents(bool futureEventsOnly, bool withSlotsOnly)
        {
			try
			{
				List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();
				var eventSlots = _eventSlotRepository.GetAll(futureEventsOnly);
				foreach(var eventType in eventTypes)
				{
					eventType.EventSlots = eventSlots.Where(es => es.EventTypeId == eventType.Id).ToList();
				}

				return withSlotsOnly ?  eventTypes.Where(e => e.EventSlots.Any()) : eventTypes;			
			}
			catch (Exception ex)
			{
				Logger.Error(typeof(EventController), string.Format("Exception calling GetEventTypes: {0}", ex.ToString()), ex);
				return new List<EventType>();
			}
        }
		
		
	}
}
