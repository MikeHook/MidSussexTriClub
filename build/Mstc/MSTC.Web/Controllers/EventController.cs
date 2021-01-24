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
using MSTC.Web.Model;

namespace MSTC.Web.Controllers
{
	[MemberAuthorize(AllowGroup = "Member")]
	public class EventController : UmbracoApiController
    {
		DataTypeProvider _dataTypeProvider;
		IEventSlotRepository _eventSlotRepository;
		IEventParticipantRepository _eventParticipantRepository;
		MemberProvider _memberProvider;

		public EventController()
		{
			_dataTypeProvider = new DataTypeProvider(Services.DataTypeService);
			var dataConnection = new DataConnection();
			_eventSlotRepository = new EventSlotRepository(dataConnection);
			_eventParticipantRepository = new EventParticipantRepository(dataConnection);
			_memberProvider = new MemberProvider(Services);
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

		[HttpPost]
		public bool BookEvent(BookEventModel model)
		{
			var member = _memberProvider.GetLoggedInMember();
			if (member == null)
			{
				return false;
			}

			var eventParticipant = new EventParticipant()
			{
				EventSlotId = model.EventSlotId,
				AmountPaid = model.Cost,
				MemberId = member.Id
			};

			eventParticipant = _eventParticipantRepository.Create(eventParticipant);

			return true;
		}
		
		
	}
}
