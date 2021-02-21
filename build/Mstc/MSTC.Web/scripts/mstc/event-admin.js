var eventAdmin = (function () {
	var eventTypes = [];
	var eventType = undefined;

	var eventTypeChanged = function (field) {
		if (!field.value) {
			return;
		}
		
		var eventTypeId = parseInt(field.value, 10);
		eventType = eventTypes.find(b => b.id === eventTypeId);
		if (eventType === undefined) {
			return;
		}

		bindEventDetails(null);
		bindEventSlots();
		
	};

	var bindEventSlots = function () {
		$('#event-slots-table').DataTable().clear().destroy();

		var eventSlotsTable = $('#event-slots-table').DataTable({
			"lengthMenu": [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
			data: eventType.eventSlots,
			dom: 'Brtip',
			select: true,
			buttons: ['csv', 'excel', 'print'],
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

		eventSlotsTable.off('select');
		eventSlotsTable.on('select', function (e, dt, type, indexes) {
			if (type === 'row') {
				var eventSlotRow = eventSlotsTable.rows(indexes).data()[0];
				//var eventSlot = eventType.eventSlots.find(es => es.id == eventSlotRow.id);
				bindEventDetails(eventSlotRow);
			}
		});
	};

	var bindEventDetails = function (eventSlot) {
		$('#event-participants-table').DataTable().clear().destroy();		

		var title = !eventSlot ? '' : `Participants for ${eventSlot.eventTypeName} on ${eventSlot.dateDisplay}`;
		$('#eventTitle').html(title);

		var eventParticipantsTable = $('#event-participants-table').DataTable({
			"lengthMenu": [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
			data: !eventSlot ? [] : eventSlot.eventParticipants,
			dom: 'Brtip',
			select: true,
			buttons: ['csv', 'excel', {
				extend: 'print',
				title: title,
				autoPrint: false
			}],
			columns: [
				{ data: 'name' },
				{ data: 'email' },
				{ data: 'phone' }
			],
			'order': [[0, 'asc']]
		});
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