using System;
using System.Collections.Generic;
using System.Linq;
using Mstc.Core.Providers;
using Umbraco.Web.WebApi;
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
		public BookEventResponse BookEvent(BookEventRequest model)
		{
			var response = new BookEventResponse();			
			var member = _memberProvider.GetLoggedInMember();
			if (member == null)
			{
				response.Error = "Unable to find logged in member record.";
				return response;
			}
			
			if (member.GetValue<int>(MemberProperty.TrainingCredits) < model.Cost)
			{
				response.Error = "You do not have enough training credits to book that event.";
				return response;
			}

			var eventSlot = _eventSlotRepository.GetById(model.EventSlotId);
			if (!eventSlot.HasSpace)
			{
				response.Error = "There is no space remaining on that training slot.";
				return response;
			}

			var eventParticipant = new EventParticipant()
			{
				EventSlotId = model.EventSlotId,
				AmountPaid = model.Cost,
				MemberId = member.Id
			};

			//Debit cost from members credits
			var credits = member.GetValue<int>(MemberProperty.TrainingCredits);
			credits = credits - model.Cost;
			member.SetValue(MemberProperty.TrainingCredits, credits);
			Services.MemberService.Save(member);

			eventParticipant = _eventParticipantRepository.Create(eventParticipant);
			Logger.Info(typeof(EventController), $"New event slot booking - Member: {member.Name} , Event: {model.EventTypeName} on {eventSlot.Date.ToString("dd/MM/yyyy")} for £{model.Cost}.");

			return response;
		}
		
		
	}
}
