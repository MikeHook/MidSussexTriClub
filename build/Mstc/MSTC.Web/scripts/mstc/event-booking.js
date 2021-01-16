var eventBooking = (function() {
	var eventTypes = [];

	var eventTypeChanged = function (field) {
		$("#eventDate").empty();

		if (!field.value) {
			return;
		}
		
		var eventTypeId = parseInt(field.value, 10);
		var eventType = eventTypes.find(b => b.id === eventTypeId);
		if (!eventType) {
			return;
		}
		var eventDates = eventType.eventSlots.map(es => es.date);	
		var eventDateDropDown = document.getElementById('eventDate');
		eventDates.forEach(item => {
			//TODO - Parse date into better format
			eventDateDropDown.options[eventDateDropDown.options.length] = new Option(item, item);
		});		
		
	};

	var getEvents = function() {
		$.ajax({
			url: '/umbraco/api/event/BookableEvents?futureEventsOnly=true&withSlotsOnly=false',
			method: 'GET',// jQuery > 1.9
			type: 'GET', //jQuery < 1.9
			success: function (response) {
				eventTypes = response;
				var eventTypeDropDown = document.getElementById('eventType');

				eventTypes.forEach(item => {
					eventTypeDropDown.options[eventTypeDropDown.options.length] = new Option(item.name, item.id);
				});
			},
			error: function() { }
		});
	};


	var bindFunctions = function() {
		$('#eventType').off('change');
		$('#eventType').on('change', function() {
			eventTypeChanged(this);
		});
	};

	var init = function() {
		bindFunctions();
		getEvents();
	};

	return {		
		init: init
		//anything else you want available
		//through eventBooking.function()
		//or expose variables here too
	};
})();

$(document).ready(function() {
	eventBooking.init();
});