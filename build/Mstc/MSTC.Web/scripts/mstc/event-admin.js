var eventAdmin = (function () {
	var eventTypes = [];
	var eventType = undefined;
	var eventSlots = [];


	var eventTypeChanged = function (field) {
		if (!field.value) {
			return;
		}
		
		var eventTypeId = parseInt(field.value, 10);
		eventType = eventTypes.find(b => b.id === eventTypeId);
		if (eventType === undefined) {
			return;
		}

		$('#event-slots-table').DataTable().clear().destroy();

		$('#event-slots-table').DataTable({
			//pageLength: 50,
			"lengthMenu": [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
			data: eventType.eventSlots,
			dom: 'Brtip',
			select: true,
			buttons: [
				'csv', 'excel', 'print'
			],		
			columns: [
				{
					data: 'date',
					render: function (data, type, row) {
						if (type === "sort" || type === "type") {
							return data;
						}
						return moment(data).format("dddd DD MMM YYYY");
					}
				},
				{ data: 'participants' }	
			],
			'order': [[0, 'asc']]			
		});

		/*
		if (eventType.eventSlots.length > 0) {
			for (i = 0; i < eventType.eventSlots.length; i++) {
				var html = `<tr data-id="${eventType.eventSlots[i].id}"><td>${eventType.eventSlots[i].dateDisplay}</td><td>${eventType.eventSlots[i].participants}</td>` +
					'<td><button name="cancelEvent" class="cancel-bookings-button" type="button" class="btn btn-grey">Cancel bookings</button></td></tr>';

				$("#eventSlots").append(html);
				$(".cancel-bookings-button").off('click');
				$(".cancel-bookings-button").on('click', function () {

					var tr = $(this).closest("tr");   // Finds the closest row <tr> 
					var slotId = tr.data("id");     // Gets a descendent with class="slot-id"
					cancelSlotId = slotId;
					console.log(slotId);
					var eventSlot = eventType.eventSlots.find(es => es.id === slotId);
					eventTypeCancel$.html(eventSlot.eventTypeName);
					eventDateCancel$.html(eventSlot.dateDisplay);
					$("#dialog-cancel").dialog("open");
				});
			}
		}
		*/
	};	

	var getEvents = function() {
		$.ajax({
			url: '/umbraco/api/event/BookableEvents?futureEventsOnly=false&withSlotsOnly=false',
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
				JL().error('Call to /umbraco/api/event/BookableEvents?futureEventsOnly=false&withSlotsOnly=false returned error');
				JL().error(message);
			}
		});
	};

	var cancelEvent = function () {
		var cancelModel = {			
			eventSlotId: cancelSlotId		
		};

		$.ajax({
			url: '/umbraco/api/event/CancelEvent',
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
				JL().error('Call to /umbraco/api/event/CancelEvent returned error');
				JL().error(message);
				cancelSlotId = null;
			}
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
	eventAdmin.init();
});