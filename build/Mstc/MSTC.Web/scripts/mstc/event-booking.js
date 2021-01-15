var eventBooking = (function() {
	var bookableEvents = [];

	var eventTypeChanged = function (field) {
		if (!field.value) {
			return;
		}
		var eventTypeId = parseInt(field.value, 10);
		var event = bookableEvents.find(b => b.eventTypeId === eventTypeId);
		var eventDates = event.eventSlots.map(es => es.eventDate);
		var eventDateDropDown = document.getElementById('eventDate');
		eventDates.forEach(item => {
			eventDateDropDown.options[eventDateDropDown.options.length] = new Option(item, item);
		});		
		
	};

	var getEvents = function() {
		$.ajax({
			url: '/umbraco/api/event/BookableEvents',
			method: 'GET',// jQuery > 1.9
			type: 'GET', //jQuery < 1.9
			success: function (response) {
				bookableEvents = response;
				var eventTypeDropDown = document.getElementById('eventType');

				bookableEvents.forEach(item => {
					eventTypeDropDown.options[eventTypeDropDown.options.length] = new Option(item.eventTypeName, item.eventTypeId);
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