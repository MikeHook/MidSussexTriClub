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
	[MemberAuthorize(AllowGroup = "Member,Guest")]
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
		public IEnumerable<EventType> BookableEvents(bool futureEventsOnly, bool withSlotsOnly, bool isAdmin = false)
        {
			try
			{
				var member = _memberProvider.GetLoggedInMember();
				var memberType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
				var btfNumber = member.GetValue<string>(MemberProperty.BTFNumber);
				bool hasBTFNumber = !string.IsNullOrEmpty(btfNumber);
				var isGuest = memberType == MembershipTypeEnum.Guest;

				List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();
				var eventSlots = _eventSlotRepository.GetAll(futureEventsOnly, eventTypes, hasBTFNumber);
				if (!isAdmin)
				{
					eventSlots = eventSlots.Where(es => es.IsGuestEvent == isGuest);
				}

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
				var eventSlots = _eventSlotRepository.GetAll(futureEventsOnly, eventTypes, null).ToList();
				if (onlyBookedByCurrentMember)
				{
					var member = _memberProvider.GetLoggedInMember();
					if (member == null)
					{
						Logger.Warn(typeof(EventController), "Unable to find logged in member record.");
						return new List<EventSlot>();
					}

					eventSlots = eventSlots.Where(es => es.EventParticipants.Any(ep => ep.MemberId == member.Id)).ToList();

					eventSlots.ForEach(es =>
					{
						var currentEventParticipant = es.EventParticipants.First(ep => ep.MemberId == member.Id);
						if (string.IsNullOrEmpty(currentEventParticipant.RaceDistance))
						{
							es.EventTypeName = eventTypes.SingleOrDefault(et => et.Id == es.EventTypeId)?.Name;
						}
						else
						{
							es.EventTypeName = $"{eventTypes.SingleOrDefault(et => et.Id == es.EventTypeId)?.Name} - {currentEventParticipant.RaceDistance}";
						}
					});
				}
				else
				{
					eventSlots.ForEach(es => es.EventTypeName = eventTypes.SingleOrDefault(et => et.Id == es.EventTypeId)?.Name);
				}

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

			var membershipExpiry = member.GetValue<DateTime>(MemberProperty.MembershipExpiry);
			bool isMembershipExpired = membershipExpiry < DateTime.Now;
			if (isMembershipExpired)
			{
				response.Error = "Your membership has expired, please renew your membership before booking any events.";
				return response;
			}

			var memberType = member.GetValue<MembershipTypeEnum>(MemberProperty.membershipType);
			var isGuest = memberType == MembershipTypeEnum.Guest;
			if (string.Equals(model.EventTypeName,"Open Water Swim",StringComparison.OrdinalIgnoreCase) && !isGuest && !member.GetValue<bool>(MemberProperty.OpenWaterIndemnityAcceptance))
			{
				response.Error = "You need to signup for open water swimming (on your member details page) before you can book onto open water swim sessions.";
				return response;
			}

			if (string.Equals(model.EventTypeName, "Pool Swim", StringComparison.OrdinalIgnoreCase) && !CanBookPoolSwim(member, model.EventSlotId))
			{
				response.Error = "You need to signup for swim subs (on your member details page) before you can book onto pool swim sessions.";
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
				MemberId = member.Id,
				RaceDistance = string.IsNullOrEmpty(model.RaceDistance) ? null : model.RaceDistance
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
		public EventResponse CancelEventSlot(CancelEventRequest model)
		{
			var response = new EventResponse();
			IMember loggedInmember = _memberProvider.GetLoggedInMember();
			if (loggedInmember == null)
			{
				Logger.Warn(typeof(EventController), "Unable to find logged in member record.");
				response.Error = "Unable to find logged in member record.";
				return response;
			}			

			var eventSlot = _eventSlotRepository.GetById(model.EventSlotId);	
			if (eventSlot.EventParticipants.Count == 0)
			{		
				response.Error = "There are no participants booked onto the event.";
				return response;
			}
		
			if (eventSlot.Date.Date < DateTime.Now.Date)
			{
				response.Error = $"You can not cancel an event which is in the past.";
				return response;
			}

			foreach (var eventParticipant in eventSlot.EventParticipants)
			{
				var eventMember = Services.MemberService.GetById(eventParticipant.MemberId);
				CancelEventParticipant(eventMember, eventParticipant);				
			}

			List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();
			Logger.Info(typeof(EventController), $"Event slot cancelled for all members - Event: {eventTypes.SingleOrDefault(et => et.Id == eventSlot.EventTypeId)?.Name} on {eventSlot.Date.ToString("dd/MM/yyyy")}.");

			return response;
		}

		[HttpPost]
		public EventResponse CancelEventParticipant(CancelEventRequest model)
		{
			var response = new EventResponse();
			var loggedInmember = _memberProvider.GetLoggedInMember();
			if (loggedInmember == null)
			{
				Logger.Warn(typeof(EventController), "Unable to find logged in member record.");
				response.Error = "Unable to find logged in member record.";
				return response;
			}

			var eventSlot = _eventSlotRepository.GetById(model.EventSlotId);
			if (eventSlot.Date.Date < DateTime.Now.Date)
			{
				response.Error = $"You can not cancel an event which is in the past.";
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
				if (eventSlot.Date.Date <= DateTime.Now.Date)
				{
					response.Error = $"You can not cancel an event on the same day as the event is running.";
					return response;
				}
			}

			
			EventParticipant eventParticipant = eventSlot.EventParticipants.FirstOrDefault(ep => ep.MemberId == eventMember.Id);
			if (eventParticipant == null)
			{
				Logger.Warn(typeof(EventController), $"Member {eventMember.Name} is not currently booked onto event slot {model.EventSlotId}.");
				response.Error = "You are not currently booked onto that event";
				return response;
			}

			CancelEventParticipant(eventMember, eventParticipant);

			List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();
			Logger.Info(typeof(EventController), $"Event slot cancelled - Member: {eventMember.Name} , Event: {eventTypes.SingleOrDefault(et => et.Id == eventSlot.EventTypeId)?.Name} on {eventSlot.Date.ToString("dd/MM/yyyy")} for £{eventParticipant.AmountPaid}.");

			return response;
		}

		private void CancelEventParticipant(IMember eventMember, EventParticipant eventParticipant)
		{
			//Credit cost to members credits
			var credits = eventMember.GetValue<int>(MemberProperty.TrainingCredits);
			credits = credits + eventParticipant.AmountPaid;
			eventMember.SetValue(MemberProperty.TrainingCredits, credits);
			Services.MemberService.Save(eventMember);

			//Remove eventParticipant entry
			_eventParticipantRepository.Delete(eventParticipant.Id);
		}

		private bool CanBookPoolSwim(IMember member, int eventSlotId)
		{
			var eventSlot = _eventSlotRepository.GetById(eventSlotId);
			
			if (eventSlot.Date.Month > 3 && eventSlot.Date.Month < 10)
			{
				bool hasSwimSubs1 = string.IsNullOrEmpty(member.GetValue<string>(MemberProperty.swimSubs1)) == false;
				return hasSwimSubs1;
			} 
			else
			{
				bool hasSwimSubs2 = string.IsNullOrEmpty(member.GetValue<string>(MemberProperty.swimSubs2)) == false;
				return hasSwimSubs2;
			}			
		}
	}
}
