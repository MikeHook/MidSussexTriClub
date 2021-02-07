using System;
using System.Collections.Generic;
using System.Linq;
using Mstc.Core.Providers;
using Umbraco.Web.WebApi;
using System.Web.Http;
using Mstc.Core.DataAccess;
using Mstc.Core.Domain;
using MSTC.Web.Model;
using Umbraco.Core.Models;

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

		[HttpGet]
		public IEnumerable<EventSlot> EventSlots(bool futureEventsOnly, bool onlyBookedByCurrentMember)
		{
			try
			{
				List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();

				var eventSlots = _eventSlotRepository.GetAll(futureEventsOnly).ToList();
				if (onlyBookedByCurrentMember)
				{
					var member = _memberProvider.GetLoggedInMember();
					if (member == null)
					{
						Logger.Warn(typeof(EventController), "Unable to find logged in member record.");				
						return new List<EventSlot>();
					}

					eventSlots = eventSlots.Where(es => es.EventParticipants.Any(ep => ep.MemberId == member.Id)).ToList();
				}
				eventSlots.ForEach(es => es.EventTypeName = eventTypes.SingleOrDefault(et => et.Id == es.EventTypeId)?.Name);

				return eventSlots;
			}
			catch (Exception ex)
			{
				Logger.Error(typeof(EventController), string.Format("Exception calling EventSlots: {0}", ex.ToString()), ex);
				return new List<EventSlot>();
			}
		}

		[HttpPost]
		public EventResponse BookEvent(BookEventRequest model)
		{
			var response = new EventResponse();			
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

			if (eventSlot.EventParticipants.Any(ep => ep.MemberId == member.Id))
			{
				response.Error = "You are already booked onto that event.";
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

		[HttpPost]
		public EventResponse CancelEvent(CancelEventRequest model)
		{
			var response = new EventResponse();
			var loggedInmember = _memberProvider.GetLoggedInMember();
			if (loggedInmember == null)
			{
				Logger.Warn(typeof(EventController), "Unable to find logged in member record.");
				response.Error = "Unable to find logged in member record.";
				return response;
			}

			IMember eventMember;
			if (model.MemberId.HasValue)
			{
				eventMember = Services.MemberService.GetById(model.MemberId.Value);
				if (eventMember == null)
				{
					Logger.Warn(typeof(EventController), $"Unable to find member with id {model.MemberId} for event cancellation.");
					response.Error = $"Unable to find member with id {model.MemberId} for event cancellation.";
					return response;
				}
			} 
			else
			{
				eventMember = loggedInmember;
			}

			var eventSlot = _eventSlotRepository.GetById(model.EventSlotId);
			EventParticipant eventParticipant = eventSlot.EventParticipants.FirstOrDefault(ep => ep.MemberId == eventMember.Id);
			if (eventParticipant == null)
			{
				Logger.Warn(typeof(EventController), $"Member {eventMember.Name} is not currently booked onto event slot {model.EventSlotId}.");
				response.Error = "You are not currently booked onto that event";
				return response;
			}			

			//Credit cost to members credits
			var credits = eventMember.GetValue<int>(MemberProperty.TrainingCredits);
			credits = credits + eventParticipant.AmountPaid;
			eventMember.SetValue(MemberProperty.TrainingCredits, credits);
			Services.MemberService.Save(eventMember);

			_eventParticipantRepository.Delete(eventParticipant.Id);

			List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();
			Logger.Info(typeof(EventController), $"Event slot cancelled - Member: {eventMember.Name} , Event: {eventTypes.SingleOrDefault(et => et.Id == eventSlot.EventTypeId)?.Name} on {eventSlot.Date.ToString("dd/MM/yyyy")} for £{eventParticipant.AmountPaid}.");

			return response;
		}


	}
}
