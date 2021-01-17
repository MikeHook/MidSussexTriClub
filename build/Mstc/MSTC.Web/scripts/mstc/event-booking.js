var eventBooking = (function () {
	var eventDateDropDown = document.getElementById('eventDate');
	var spacesEl = document.getElementById('spaces');
	var bookEventButton$ = $('#bookEventButton');

	var eventTypes = [];
	var eventType = undefined;

	var eventTypeChanged = function (field) {
		$("#eventDate").empty();
		eventSlotChanged(undefined);

		if (!field.value) {
			return;
		}
		
		var eventTypeId = parseInt(field.value, 10);
		eventType = eventTypes.find(b => b.id === eventTypeId);
		if (eventType === undefined) {
			return;
		}		
		
		eventDateDropDown.options[eventDateDropDown.options.length] = new Option('Select a date', '-1');
		eventType.eventSlots.forEach(eventSlot => {			
			var eventDate = luxon.DateTime.fromISO(eventSlot.date);
			eventDateDropDown.options[eventDateDropDown.options.length] = new Option(eventDate.toLocaleString(luxon.DateTime.DATE_MED_WITH_WEEKDAY), eventSlot.id);			
		});		
	};

	var eventDateChanged = function (field) {
		if (!field.value || eventType === undefined) {
			return;
		}

		var eventSlotId = parseInt(field.value, 10);
		var eventSlot = eventType.eventSlots.find(es => es.id === eventSlotId);
		eventSlotChanged(eventSlot);
	};

	var eventSlotChanged = function (eventSlot) {		
		spacesEl.innerText = eventSlot === undefined ? '' : eventSlot.spacesRemaining;		
		if (eventSlot === undefined || !eventSlot.hasSpace) {
			bookEventButton$.prop('disabled', true);
			bookEventButton$.html('Book Event - No slots available');
		} else {
			bookEventButton$.prop('disabled', false);
			var buttonText = eventSlot.cost > 0 ? 'Book Event for £' + eventSlot.cost : 'Book Event - No Cost';
			bookEventButton$.html(buttonText);
		}
	};

	var getEvents = function() {
		$.ajax({
			url: '/umbraco/api/event/BookableEvents?futureEventsOnly=true&withSlotsOnly=true',
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

		$('#eventDate').off('change');
		$('#eventDate').on('change', function () {
			eventDateChanged(this);
		});
	};

	var init = function() {
		bindFunctions();
		getEvents();
		eventSlotChanged(undefined);
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