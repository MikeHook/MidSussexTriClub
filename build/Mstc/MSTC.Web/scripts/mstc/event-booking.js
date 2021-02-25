var eventBooking = (function () {
	var eventDateDropDown = document.getElementById('eventDate');
	var spacesEl = document.getElementById('spaces');
	var bookEventButton$ = $('#bookEventButton');
	var eventTypeConfirm$ = $('#eventTypeConfirm');
	var eventDateConfirm$ = $('#eventDateConfirm');
	var eventCostConfirm$ = $('#eventCostConfirm');	

	var eventTypeCancel$ = $('#eventTypeCancel');
	var eventDateCancel$ = $('#eventDateCancel');

	var eventTypes = [];
	var eventType = undefined;
	var eventSlot = undefined;
	var bookedEventSlots = [];
	var cancelSlotId = null;

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

		eventTypeConfirm$.html(eventType.name);
		
		eventDateDropDown.options[eventDateDropDown.options.length] = new Option('Select a date', '-1');
		eventType.eventSlots.forEach(eventSlot => {
			eventDateDropDown.options[eventDateDropDown.options.length] = new Option(eventSlot.dateDisplay, eventSlot.id);			
		});		
	};

	var eventDateChanged = function (field) {
		if (!field.value || eventType === undefined) {
			return;
		}

		var eventSlotId = parseInt(field.value, 10);
		eventSlot = eventType.eventSlots.find(es => es.id === eventSlotId);
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
		
			eventDateConfirm$.html(eventSlot.dateDisplay);
			eventCostConfirm$.html(eventSlot.cost);
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
			error: function (message) {
				//Log error
				JL().error('Call to /umbraco/api/event/BookableEvents?futureEventsOnly=true&withSlotsOnly=true returned error');
				JL().error(message);
			}
		});
	};

	var cancelEvent = function () {
		var cancelModel = {			
			eventSlotId: cancelSlotId		
		};

		$.ajax({
			url: '/umbraco/api/event/CancelEventParticipant',
			data: cancelModel,
			method: 'POST',// jQuery > 1.9
			type: 'POST', //jQuery < 1.9
			success: function (bookingResponse) {
				if (bookingResponse.hasError) {
					toastr.error(bookingResponse.error);
				} else {
					toastr.success('Event slot cancelation complete.');
					getBookedEventSlots();
				}
				cancelSlotId = null;
			},
			error: function (message) {
				toastr.error(message);
				//Log error
				JL().error('Call to /umbraco/api/event/CancelEventParticipant returned error');
				JL().error(message);
				cancelSlotId = null;
			}
		});
	};

	var getBookedEventSlots = function () {
		$.ajax({
			url: '/umbraco/api/event/EventSlots?futureEventsOnly=true&onlyBookedByCurrentMember=true',
			method: 'GET',// jQuery > 1.9
			type: 'GET', //jQuery < 1.9
			success: function (response) {		
				bookedEventSlots = response;
				$("#upcomingEvents").empty();
				if (bookedEventSlots.length > 0) {
					for (i = 0; i < bookedEventSlots.length; i++) {
						var html = `<tr data-id="${bookedEventSlots[i].id}"><td>${bookedEventSlots[i].eventTypeName}</td><td>${bookedEventSlots[i].dateDisplay}</td>` +							
							'<td><button name="cancelEvent" class="cancel-button" type="button" class="btn btn-grey">Cancel event</button></td></tr>';

						$("#upcomingEvents").append(html);
						$(".cancel-button").off('click');
						$(".cancel-button").on('click', function () {

							var tr = $(this).closest("tr");   // Finds the closest row <tr> 
							var slotId = tr.data("id");     // Gets a descendent with class="slot-id"
							cancelSlotId = slotId;
							var eventSlot = bookedEventSlots.find(es => es.id === slotId);
							eventTypeCancel$.html(eventSlot.eventTypeName);
							eventDateCancel$.html(eventSlot.dateDisplay);
							$("#dialog-cancel").dialog("open");
						});								
					}
				}  

			},
			error: function (message) {
				//Log error
				JL().error('Call to /umbraco/api/event/EventSlots?futureEventsOnly=true&onlyBookedByCurrentMember=true returned error');
				JL().error(message);
			}
		});
	};

	var bookEvent = function () {

		var bookingModel = {
			eventTypeName: eventType.name,
			eventSlotId: eventSlot.id,
			cost: eventSlot.cost
		};

		$.ajax({
			url: '/umbraco/api/event/BookEvent',
			data: bookingModel,
			method: 'POST',// jQuery > 1.9
			type: 'POST', //jQuery < 1.9
			success: function (bookingResponse) {
				if (bookingResponse.hasError) {
					toastr.error(bookingResponse.error);
				} else {
					toastr.success('Event slot booking complete.');
					getBookedEventSlots();
				}		
			},
			error: function (message) {
				toastr.error(message);
				//Log error
				JL().error('Call to /umbraco/api/event/BookEvent returned error');
				JL().error(message);
			}
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

		$("#eventBookingForm").on("submit", function (event) {
			if (!event.isDefaultPrevented()) {
				event.preventDefault();

				$("#dialog-confirm").dialog("open");	
			}
		});

		
	};

	var init = function() {
		bindFunctions();
		getEvents();
		eventSlotChanged(undefined);
		getBookedEventSlots();

		$("#dialog-confirm").dialog({
			autoOpen: false,
			resizable: false,
			height: "auto",
			width: 400,
			modal: true,
			buttons: {
				Yes: function () {
					bookEvent();
					$(this).dialog("close");
				},
				Cancel: function () {
					$(this).dialog("close");
				}
			}
		});

		$("#dialog-cancel").dialog({
			autoOpen: false,
			resizable: false,
			height: "auto",
			width: 400,
			modal: true,
			buttons: {
				Yes: function () {
					cancelEvent();
					$(this).dialog("close");
				},
				Cancel: function () {
					$(this).dialog("close");
				}
			}
		});

		toastr.options = {
			"positionClass": "toast-top-center"
		};
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