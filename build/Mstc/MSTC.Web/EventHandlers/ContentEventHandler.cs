using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mstc.Core;
using Mstc.Core.DataAccess;
using Mstc.Core.Domain;
using Mstc.Core.Providers;
using Mstc.Core.ContentModels;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace MSTC.Web.EventHandlers
{
    public class ContentEventHandler
    {
        UmbracoHelper _umbracoHelper;
        DataTypeProvider _dataTypeProvider;
        IEventSlotRepository _eventSlotRepository;
        ILogger _logger;

        public ContentEventHandler()
        {
            _umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            
            _dataTypeProvider = new DataTypeProvider(_umbracoHelper.DataTypeService);
            _eventSlotRepository = new EventSlotRepository(new DataConnection());
            _logger = ApplicationContext.Current.ProfilingLogger.Logger;
        }                    

        public void ContentService_Saving(Umbraco.Core.Services.IContentService sender, SaveEventArgs<IContent> e)
        {
            List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();            
            
            foreach (IContent entity in e.SavedEntities)
            {
                if (entity.ContentType.Alias == "event")
                {
                    IPublishedContent eventPageContent = entity.ToPublishedContent();
                    var eventPage = new Event(eventPageContent);                
                    if (eventPage.Id == 0)
                    {
                        //No need to check this as must be creating a new event page
                        continue;
                    }

                    var eventType = eventTypes.SingleOrDefault(et => et.Name == eventPage.EventType);
                    if (eventType == null)
                    {
                        //Log and move on
                        _logger.Warn(typeof(ContentEventHandler), $"No event type found for name '{eventPage.EventType}'.");                        
                        continue;
                    }              

                    var eventDates = GetEventDates(eventPage, true);                    
                    List<EventSlot> existingEventSlots = _eventSlotRepository.GetAll(true, eventTypes).ToList();
                    existingEventSlots = existingEventSlots.Where(es => es.EventTypeId == eventType.Id && es.EventPageId == eventPage.Id).ToList();
                    if (existingEventSlots.Any(es => eventDates.Contains(es.Date) == false && es.EventParticipants.Count > 0))
                    {
                        string message = $"Unable to save changes - existing bookings for event slots of type {eventType.Name}, please cancel the bookings through the Event Booking Admin page first.";
                        _logger.Warn(typeof(ContentEventHandler), message);
                        //If any slots are no longer needed and have participants then cancel saving with message                    
                        e.Messages.Add(new EventMessage("Error", message, EventMessageType.Error));
                        e.Cancel = true;
                        return;
                    }
                }
            }   
        }

        public void ContentService_Saved(Umbraco.Core.Services.IContentService sender, SaveEventArgs<IContent> e)
        {
            List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();

            foreach (IContent entity in e.SavedEntities)
            {
                if (entity.ContentType.Alias == "event")
                {
                    IPublishedContent eventPageContent = entity.ToPublishedContent();
                    var eventPage = new Event(eventPageContent);
                   
                    var eventType = eventTypes.SingleOrDefault(et => et.Name == eventPage.EventType);
                    if (eventType == null)
                    {
                        //Log and move on
                        _logger.Warn(typeof(ContentEventHandler), $"No event type found for name '{eventPage.EventType}', event slots will not be created/updated.");
                        continue;
                    }

                    var eventDates = GetEventDates(eventPage, true);
                    List<EventSlot> existingEventSlots = _eventSlotRepository.GetAll(true, eventTypes).ToList();
                    existingEventSlots = existingEventSlots.Where(es => es.EventTypeId == eventType.Id && es.EventPageId == eventPage.Id).ToList();                

                    var response = CreateUpdateEventSlots(existingEventSlots, eventType.Id, eventPage);
                    _logger.Info(typeof(ContentEventHandler),
                        $"Updated event slots for event type '{eventPage.EventType}'. Created {response.SlotsCreated}, updated {response.SlotsUpdated} and deleted {response.SlotsDeleted} slots.");
                }
            }
        }

        public void ContentService_Deleting(Umbraco.Core.Services.IContentService sender, DeleteEventArgs<Umbraco.Core.Models.IContent> e)
        {
            List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();
            
            /*
             - Check if any slots should be removed
	         - If slots have participants then cancel deleting with message
	         - If slots are empty then remove the slots
             */
            foreach (IContent entity in e.DeletedEntities)
            {
                if (entity.ContentType.Alias == "event")
                {
                    IPublishedContent eventPageContent = entity.ToPublishedContent();
                    var eventPage = new Event(eventPageContent);
                    
                    List<EventSlot> existingEventSlots = _eventSlotRepository.GetAll(true, eventTypes).Where(es => es.EventPageId == eventPage.Id).ToList();
                    if (existingEventSlots.Any(es => es.EventParticipants.Count > 0))
                    {
                        string message = $"Unable to delete event page - existing bookings for event slots of type {eventPage.EventType}, please cancel the bookings through the Event Booking Admin page first.";
                        _logger.Warn(typeof(ContentEventHandler), message);
                        //If any future slots have participants then cancel deleting with message                    
                        e.Messages.Add(new EventMessage("Error", message, EventMessageType.Error));
                        e.Cancel = true;
                        return;
                    }

                    //Remove future event slots              
                    foreach (var slot in existingEventSlots)
                    {
                        _eventSlotRepository.Delete(slot.Id);
                    }
                    _logger.Info(typeof(ContentEventHandler),
                     $"Deleted {existingEventSlots.Count} event slots for page {eventPage.Name}, event type '{eventPage.EventType}'.");
                }
            }
        }

        private UpdateEventSlotResponse CreateUpdateEventSlots(List<EventSlot> existingFutureEventSlots, int eventTypeId, Event eventPage)
        {
            var response = new UpdateEventSlotResponse();
            var eventDates = GetEventDates(eventPage, true);
            //Add any slots which do not exist
            var newDates = eventDates.Where(ed => existingFutureEventSlots.Any(es => es.Date == ed) == false).ToList();
            foreach(var date in newDates)
            {
                AddEventSlot(eventTypeId, date, eventPage);
            }
            response.SlotsCreated = newDates.Count;

            //Remove any slots which are no longer needed
            var redundantEventSlots = existingFutureEventSlots.Where(es => eventDates.Contains(es.Date) == false).ToList();
            foreach (var slot in redundantEventSlots)
            {
                _eventSlotRepository.Delete(slot.Id);
            }
            response.SlotsDeleted = redundantEventSlots.Count;

            //Update existing slots max participants and cost
            var existingEventSlots = existingFutureEventSlots.Where(es => eventDates.Contains(es.Date)).ToList();
            foreach (var slot in existingEventSlots)
            {
                slot.MaxParticipants = eventPage.MaximumParticipants;
                slot.Cost = eventPage.Cost;
                slot.IndemnityWaiverDocumentLink = eventPage.IndemnityWaiver?.Url;
                slot.CovidDocumentLink = eventPage.CovidHealthDeclaration?.Url;
                slot.IsGuestEvent = eventPage.IsGuestEvent;
                slot.SetDisances(eventPage.RaceDistances);
                _eventSlotRepository.Update(slot);
            }   
            response.SlotsUpdated = existingEventSlots.Count;
            return response;
        }

        private List<DateTime> GetEventDates(Event eventPage, bool futureOnly)
        {
            var eventDates = new List<DateTime>();
            if (!eventPage.Recurring)
            { 
                eventDates.Add(eventPage.StartDate);  
            }
            else
            {
                var eventDate = eventPage.StartDate;
                while (eventDate <= eventPage.EndDate)
                {
                    eventDates.Add(eventDate);
                    if (eventPage.RecurringFrequency == (int)RecurringFrequencyEnum.Weekly)
                    {
                        eventDate = eventDate.AddDays(7);
                    }
                    else
                    {
                        eventDate = eventDate.AddMonths(1);
                    }
                }
            }
            return futureOnly ? eventDates.Where(d => d > DateTime.Now).ToList() : eventDates;
        }

        private void AddEventSlot(int eventTypeId, DateTime eventDate, Event eventPage)
        {            
            var eventSlot = new EventSlot()
            {
                EventTypeId = eventTypeId,
                EventPageId = eventPage.Id,
                Date = eventDate,
                Cost = eventPage.Cost,
                MaxParticipants = eventPage.MaximumParticipants,
                IndemnityWaiverDocumentLink = eventPage.IndemnityWaiver?.Url,
                CovidDocumentLink = eventPage.CovidHealthDeclaration?.Url,
                IsGuestEvent = eventPage.IsGuestEvent
            };
            eventSlot.SetDisances(eventPage.RaceDistances);
            _eventSlotRepository.Create(eventSlot);            
        }        
    }
}
