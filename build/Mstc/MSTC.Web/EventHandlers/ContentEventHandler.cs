using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mstc.Core;
using Mstc.Core.DataAccess;
using Mstc.Core.Domain;
using Mstc.Core.Providers;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.PublishedContentModels;

namespace MSTC.Web.EventHandlers
{
    public class ContentEventHandler
    {
        UmbracoHelper _umbracoHelper;
        DataTypeProvider _dataTypeProvider;
        IEventSlotRepository _eventSlotRepository;

        public ContentEventHandler()
        {
            _umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            _dataTypeProvider = new DataTypeProvider(_umbracoHelper.DataTypeService);
            _eventSlotRepository = new EventSlotRepository(new DataConnection());
        }

        public void ContentService_Saving(Umbraco.Core.Services.IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            List<EventType> eventTypes = _dataTypeProvider.GetEventTypes();            
            foreach (IContent entity in e.SavedEntities)
            {
                if (entity.ContentType.Alias == "event")
                {
                    IPublishedContent eventPageContent = entity.ToPublishedContent();
                    var eventPage = new Event(eventPageContent);  
                    
                    int? eventTypeId = eventTypes.SingleOrDefault(et => et.Name == eventPage.EventType)?.Id;
                    if (!eventTypeId.HasValue)
                    {
                        //Log and move on
                        continue;
                    }

                    List<EventSlot> existingEventSlots = _eventSlotRepository.GetAll(true).ToList();
                    if (existingEventSlots.Any())
                    {
                        //If slots have participants then cancel saving with message
                        if (existingEventSlots.Any(es => es.EventParticipants.Count > 0))
                        {
                            e.Messages.Add(new EventMessage("Error", "Unable to save changes - existing bookings for event slots, please cancel the bookings first.", EventMessageType.Error));
                            e.Cancel = true;
                            return;
                        }

                        UpdateEventSlots(existingEventSlots, eventTypeId.Value, eventPage);
                    }
                    else
                    {
                        AddEventSlots(eventTypeId.Value, eventPage);
                    }
                }
            }   
        }

        public void ContentService_Deleting(Umbraco.Core.Services.IContentService sender, Umbraco.Core.Events.DeleteEventArgs<Umbraco.Core.Models.IContent> e)
        {
            /*
             * -	Check if any slots should be removed
	                - If slots have participants then cancel deleting with message
	                - If slots are empty then remove the slots
             */
            //throw new NotImplementedException();
        }   
        
        private void AddEventSlots(int eventTypeId, Event eventPage)
        {
            if (eventPage.Recurring)
            {
                var eventDates = new List<DateTime>();
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

                foreach (var date in eventDates.Where(d => d > DateTime.Now)) //Only add events in the future
                {
                    var eventSlot = new EventSlot()
                    {
                        EventTypeId = eventTypeId,
                        Date = date,
                        Cost = eventPage.Cost,
                        MaxParticipants = eventPage.MaximumParticipants
                    };
                    _eventSlotRepository.Create(eventSlot);
                }
            }
            else
            {
                var eventSlot = new EventSlot()
                {
                    EventTypeId = eventTypeId,
                    Date = eventPage.StartDate,
                    Cost = eventPage.Cost,
                    MaxParticipants = eventPage.MaximumParticipants
                };
                _eventSlotRepository.Create(eventSlot);
            }        
        }

        private void UpdateEventSlots(List<EventSlot> existingEventSlots, int eventTypeId, Event eventPage)
        {
            //TODO - Implement this
            //Add any slots which do not exist
            //Remove any slots which are no longer needed
            //Update existing slots max participants and cost
        }
    }
}
