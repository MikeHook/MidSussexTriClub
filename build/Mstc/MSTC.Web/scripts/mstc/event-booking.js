var eventBooking = (function () {
	var eventDateDropDown = document.getElementById('eventDate');
	var raceDistanceDropDown = document.getElementById('raceDistance');
	var spacesEl = document.getElementById('spaces');
	var bookEventButton$ = $('#bookEventButton');
	var eventTypeConfirm$ = $('#eventTypeConfirm');
	var eventDateConfirm$ = $('#eventDateConfirm');
	var eventCostConfirm$ = $('#eventCostConfirm');	
	var btfLicenseSummary$ = $('#btfLicenseSummary');	
	var btfMemberCost$ = $('#btfMemberCost');	
	var btfNonMemberCost$ = $('#btfNonMemberCost');	

	var eventTypeCancel$ = $('#eventTypeCancel');
	var eventDateCancel$ = $('#eventDateCancel');

	var raceDistanceDiv$ = $('#raceDistanceDiv');
	var waiverDiv$ = $('#waiverDiv');
	var covidDiv$ = $('#covidDiv');

	var eventTypes = [];
	var eventType = undefined;
	var eventSlot = undefined;
	var raceDistance = '';
	var bookedEventSlots = [];
	var cancelSlotId = null;

	var limitBooking = document.currentScript.getAttribute('limitBooking');

	var owsGuestDiv$ = $('#owsGuestDiv');
	var owsGuestNameDiv$ = $('#owsGuestNameDiv');
	var owsGuestName = '';

	// PAYG
	var memberHasSubscription = false;

	var resetProps = function () {
		eventTypes = [];
		eventType = undefined;
		eventSlot = undefined;
		raceDistance = '';
		bookedEventSlots = [];
		cancelSlotId = null;

		$("#eventType").empty();
		$("#eventDate").empty();
		$("#raceDistance").empty();
		spacesEl.innerText = '';
		btfLicenseSummary$.addClass('hide');
		btfMemberCost$.text('');
		btfNonMemberCost$.text('');

		owsGuestDiv$.addClass('hide');
		owsGuestNameDiv$.addClass('hide');

		$('#OWSAddGuest').text = '';
		owsGuestName = '';
	};

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

	var raceDistanceChanged = function (field) {
		if (!field.value || eventType === undefined) {
			return;
		}
		raceDistance = field.value;
	};

	var eventSlotChanged = function (slot) {		
		spacesEl.innerText = slot === undefined ? '' : slot.spacesRemaining;		
		if (slot === undefined || !slot.hasSpace) {
			bookEventButton$.prop('disabled', true);
			bookEventButton$.html('Book Event - No slots available');
			raceDistanceDiv$.addClass('hide');
			waiverDiv$.addClass('hide');
			covidDiv$.addClass('hide');
			$('#checkboxWaiver').prop('checked', false);
			$('#checkboxCovid').prop('checked', false);

			$('#groupCyclingDiv').addClass('hide');
			$('#checkboxGroupCycling1').prop('checked', false);
			$('#checkboxGroupCycling2').prop('checked', false);

			owsGuestDiv$.addClass('hide');
			owsGuestNameDiv$.addClass('hide');

			$('#checkboxOWSAddGuest').prop('checked', false);

		} else {
			bookEventButton$.prop('disabled', false);
			// PAYG
			var buttonText = ''
			if (memberHasSubscription) {
				slot.memberCost = slot.subsCost;
				
			}
			buttonText = slot.memberCost > 0 ? 'Book Event for £' + slot.memberCost : 'Book Event - No Cost';
			eventCostConfirm$.html(slot.memberCost);

			bookEventButton$.html(buttonText);
		
			eventDateConfirm$.html(slot.dateDisplay);
			

			if (slot.raceDistances.length > 0) {
				raceDistanceDiv$.removeClass('hide');
				$("#raceDistance").empty();
				raceDistanceDropDown.options[raceDistanceDropDown.options.length] = new Option('Select a race distance', '');
				slot.raceDistances.forEach(raceDistance => {
					raceDistanceDropDown.options[raceDistanceDropDown.options.length] = new Option(raceDistance, raceDistance);
				});		
			} else {
				raceDistanceDiv$.addClass('hide');
			}

			if (slot.indemnityWaiverDocumentLink) {
				waiverDiv$.removeClass('hide');
				$('#waiverLink').attr('href', slot.indemnityWaiverDocumentLink);
			} else {
				waiverDiv$.addClass('hide');
			}
			if (slot.covidDocumentLink) {
				covidDiv$.removeClass('hide');
				$('#covidLink').attr('href', slot.covidDocumentLink);
			} else {
				covidDiv$.addClass('hide');
			}

			if (slot.requiresBTFLicense) {
				btfLicenseSummary$.removeClass('hide');
				btfMemberCost$.text(slot.btfCost);
				btfNonMemberCost$.text(slot.cost);
			} else {
				btfLicenseSummary$.addClass('hide');
			}

			// New for group cycling
			if (eventSlot.eventTypeName === "Group Cycling" ) {
				$('#groupCyclingDiv').removeClass('hide');
			}

			// New for OWS
			if (eventSlot.eventTypeName === "Open Water Swim") {
				owsGuestDiv$.removeClass('hide');
			}
		}
	};

	var getEvents = function () {
		resetProps();
		$.ajax({
			url: `/umbraco/api/event/BookableEvents?futureEventsOnly=true&withSlotsOnly=true&limitBooking=${limitBooking}`,
			method: 'GET',// jQuery > 1.9
			type: 'GET', //jQuery < 1.9
			success: function (response) {
				eventTypes = response;
				var events = [];

				$("#eventType").empty();
				var eventTypeDropDown = document.getElementById('eventType');
				eventTypeDropDown.options[eventTypeDropDown.options.length] = new Option('Select an event', null);
				eventTypes.forEach(item => {

					// PAYG
					if (item.memberHasSubscription) {
						memberHasSubscription = true
					}

					eventTypeDropDown.options[eventTypeDropDown.options.length] = new Option(item.name, item.id);

					item.eventSlots.forEach(slot => {
						events.push({
							id: slot.id,
							title: slot.eventTypeName,
							start: slot.date,
							extendedProps: {
								type: item.id,
								dateDisplay: slot.dateDisplay,
							}
                    })
                    })
					
				});

				var calendarEl = document.getElementById('calendar');

				var calendar = new FullCalendar.Calendar(calendarEl, {
					eventClick: function (info) {
						var eventObj = info.event;

						eventTypeDropDown.value = eventObj.extendedProps.type;
						eventTypeChanged({ value: eventObj.extendedProps.type });

						eventDateDropDown.value = eventObj.id;
						eventDateChanged({ value: eventObj.id })
	
					},
					initialView: 'listWeek',
					headerToolbar: {
						left: 'prev,today,next',
						right: 'title'
						
					},
					footerToolbar: {
						right: 'listWeek dayGridMonth,timeGridWeek,timeGridDay'
					},
					events
				});

				calendar.render();
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
					getEvents();
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
			cost: owsGuestName.length < 6 ? eventSlot.memberCost : 2 * eventSlot.memberCost,
			raceDistance: raceDistance,
			owsGuestName: owsGuestName
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
					getEvents();
					$("#eventBookingForm")[0].reset();					
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

	var validateForm = function () {
		if (eventSlot.raceDistances.length > 0 && raceDistance === '') {
			toastr.error('Please select a race distance');
			return false;
		} 

		if (eventSlot.indemnityWaiverDocumentLink && $('#checkboxWaiver').is(":checked") === false) {
			toastr.error('Please acccept the indemnity waiver document.');
			return false;
		}
		if (eventSlot.covidDocumentLink && $('#checkboxCovid').is(":checked") === false) {
			toastr.error('Please acccept the covid health declaration.');
			return false;
		}

		// New for group cycling
		if (eventSlot.eventTypeName === "Group Cycling" && ($('#checkboxGroupCycling1').is(":checked") === false || $('#checkboxGroupCycling2').is(":checked") === false )) {
			toastr.error('Please acccept the Group Cycling rules.');
			return false;
		}

		// New for OWS
		if (eventSlot.eventTypeName === "Open Water Swim" && $('#checkboxOWSAddGuest').is(":checked") === true && owsGuestName.length < 6) {
			toastr.error('Please write the full name of your guest.');
			return false;
		}

		return true;
	};

	var OWSAddGuestChanged = function (field) {
		if (!field.value || eventType === undefined) {
			return;
		}
		owsGuestName = field.value;
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

		$('#raceDistance').off('change');
		$('#raceDistance').on('change', function () {
			raceDistanceChanged(this);
		});

		$("#eventBookingForm").on("submit", function (event) {
			if (!event.isDefaultPrevented()) {
				event.preventDefault();

				if (validateForm()) {
					$("#dialog-confirm").dialog("open");
				}
			}
		});

		// New for OWS
		$('#OWSAddGuest').off('change');
		$('#OWSAddGuest').on('change', function () {
			OWSAddGuestChanged(this);
		});

		$('#checkboxOWSAddGuest').off('change');
		$('#checkboxOWSAddGuest').on('change', function () {
			if (this.checked) {
				owsGuestNameDiv$.removeClass('hide');
				var buttonText = eventSlot.memberCost > 0 ? 'Book Event for £' + (2 * eventSlot.memberCost) : 'Book Event - No Cost';
				bookEventButton$.html(buttonText);
				eventCostConfirm$.html(2 * eventSlot.memberCost);
			} else {
				owsGuestNameDiv$.addClass('hide');
				var buttonText = eventSlot.memberCost > 0 ? 'Book Event for £' + eventSlot.memberCost : 'Book Event - No Cost';
				bookEventButton$.html(buttonText);
				eventCostConfirm$.html(eventSlot.memberCost);
			}
			
		});

		
	};



	var init = function () {
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