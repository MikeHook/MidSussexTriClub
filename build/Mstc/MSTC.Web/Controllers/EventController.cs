using System;
using System.Collections.Generic;
using System.Linq;
using Mstc.Core.Dto;
using Mstc.Core.Providers;
using Umbraco.Web.WebApi;
using Umbraco.Web.PublishedContentModels;
using System.Web.Http;

namespace MSTC.Web.Controllers
{
	[MemberAuthorize(AllowGroup = "Member")]
	public class EventController : UmbracoApiController
    {
		DataTypeProvider _dataTypeProvider;

		public EventController()
		{
			_dataTypeProvider = new DataTypeProvider(Services.DataTypeService);
		}

		[HttpGet]
		public IEnumerable<BookableEvent> BookableEvents()
        {
			try
			{
				//TODO - Return Domain EventTypes instead
				var events = Umbraco.TypedContentAtXPath("//event").Cast<Event>();
				var eventTypeNames = events.Select(e => e.EventType).Distinct();

				var eventTypesCollection = _dataTypeProvider.GetDataTypeOptions("Event - Event Type - Dropdown");				
				var eventTypes = eventTypesCollection.PreValuesAsDictionary.Where(p => eventTypeNames.Contains(p.Value.Value));
				return eventTypes.Select(e => new BookableEvent() {
					eventTypeId = e.Value.Id,
					eventTypeName = e.Value.Value,
					eventSlots = GetEventSlots()					
					});			
			}
			catch (Exception ex)
			{
				Logger.Error(typeof(EventController), string.Format("Exception calling GetEventTypes: {0}", ex.ToString()), ex);
				return new List<BookableEvent>();
			}
        }
		
		private List<EventSlot> GetEventSlots()
		{
			return new List<EventSlot>();
		}
	}
}
