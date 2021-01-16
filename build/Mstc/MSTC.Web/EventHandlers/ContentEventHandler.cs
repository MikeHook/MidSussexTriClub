using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mstc.Core;
using Mstc.Core.DataAccess;
using Mstc.Core.Domain;
using Mstc.Core.Providers;
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
            /*
             *-	Add any slots which do not exist
                -	Check if any slots should be removed
	                - If slots have participants then cancel saving with message
	                - If slots are empty then remove the slots
                -	Update cost and max participants on existing slots
             */

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

                        foreach(var date in eventDates)
                        {
                            var eventSlot = new EventSlot()
                            {
                                EventTypeId = eventTypeId.Value,
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
                            EventTypeId = eventTypeId.Value,
                            Date = eventPage.StartDate,
                            Cost = eventPage.Cost,
                            MaxParticipants = eventPage.MaximumParticipants
                        };
                        _eventSlotRepository.Create(eventSlot);
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
    }
}
