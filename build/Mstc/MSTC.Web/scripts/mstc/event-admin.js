var eventAdmin = (function () {
	var eventTypes = [];
	var eventType = undefined;
	var eventSlot = null;
	var eventParticipant = null;

	var eventMemberCancel$ = $('#eventMemberCancel');
	var eventCancel$ = $('#eventCancel');

	var eventTypeChanged = function (field) {
		if (!field.value) {
			return;
		}
		
		var eventTypeId = parseInt(field.value, 10);
		eventType = eventTypes.find(b => b.id === eventTypeId);
		if (eventType === undefined) {
			return;
		}

		eventSlot = null;
		bindEventDetails();
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
				eventSlot = eventSlotsTable.rows(indexes).data()[0];
				bindEventDetails();
			}
		});
	};

	var bindEventDetails = function () {
		$('#event-participants-table').DataTable().clear().destroy();		

		var title = !eventSlot ? '' : `Participants for ${eventSlot.eventTypeName} on ${eventSlot.dateDisplay}`;
		$('#eventTitle').html(title);

		var eventParticipantsTable = $('#event-participants-table').DataTable({
			"lengthMenu": [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
			data: !eventSlot ? [] : eventSlot.eventParticipants,
			dom: 'Brtip',
			select: true,
			buttons: [
				'csv',
				'excel',
				{
				extend: 'print',
				title: title,
				autoPrint: false
				},
				{
					text: 'Cancel Member Booking',
					action: function (e, dt, node, config) {
						if (eventParticipant === null) {
							toastr.warning('Please select a member in the below table first.');
							return;
						}
						var msg = `${eventParticipant.name} for ${eventSlot.eventTypeName} on ${eventSlot.dateDisplay}`;
						eventMemberCancel$.html(msg);					
						$("#dialog-cancel").dialog("open");
					}
				},
				{
					text: 'Cancel All Bookings',
					action: function (e, dt, node, config) {
						var msg = `${eventSlot.eventTypeName} on ${eventSlot.dateDisplay}`;
						eventCancel$.html(msg);
						$("#dialog-cancel-all").dialog("open");
					}
				}

			],
			columns: [
				{ data: 'name' },
				{ data: 'email' },
				{ data: 'phone' }
			],
			'order': [[0, 'asc']]
		});

		eventParticipantsTable.off('select');
		eventParticipantsTable.on('select', function (e, dt, type, indexes) {
			if (type === 'row') {
				eventParticipant = eventParticipantsTable.rows(indexes).data()[0];
			}
		});
		eventParticipantsTable.off('deselect');
		eventParticipantsTable.on('deselect', function (e, dt, type, indexes) {
			eventParticipant = null;			
		});
	};

	var refreshEventSlot = function () {
		var eventTypeId = eventType.id;
		var eventSlotId = eventSlot.id;
		getEvents(() => {
			eventType = eventTypes.find(et => et.id === eventTypeId);
			bindEventSlots();
			eventSlot = eventType.eventSlots.find(es => es.id === eventSlotId);
			bindEventDetails();
		});
	};

	var getEvents = function(callback) {
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
				if (callback) {
					callback();
				}
			},
			error: function (message) {
				//Log error
				JL().error('Call to /umbraco/api/event/BookableEvents?futureEventsOnly=false&withSlotsOnly=false returned error');
				JL().error(message);
			}
		});
	};

	var cancelEventParticipant = function () {
		var cancelModel = {			
			eventSlotId: eventSlot.id,
			memberId: eventParticipant.memberId
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
					toastr.success('Event booking cancelation complete.');
					refreshEventSlot();
				}
			},
			error: function (message) {
				toastr.error(message);
				//Log error
				JL().error('Call to /umbraco/api/event/CancelEventParticipant returned error');
				JL().error(message);
			}
		});
	};	

	var cancelEventSlot = function () {
		var cancelModel = {
			eventSlotId: eventSlot.id
		};

		$.ajax({
			url: '/umbraco/api/event/CancelEventSlot',
			data: cancelModel,
			method: 'POST',// jQuery > 1.9
			type: 'POST', //jQuery < 1.9
			success: function (bookingResponse) {
				if (bookingResponse.hasError) {
					toastr.error(bookingResponse.error);
				} else {
					toastr.success('Event booking cancelations complete.');
					refreshEventSlot();
				}
			},
			error: function (message) {
				toastr.error(message);
				//Log error
				JL().error('Call to /umbraco/api/event/CancelEventSlot returned error');
				JL().error(message);
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

		$("#dialog-cancel-all").dialog({
			autoOpen: false,
			resizable: false,
			height: "auto",
			width: 400,
			modal: true,
			buttons: {
				Yes: function () {
					cancelEventSlot();
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
					cancelEventParticipant();
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
		//anything else you want available through eventAdmin.function() or expose variables here too
	};
})();

$(document).ready(function() {
	eventAdmin.init();
});