function getEvents() {
	$.ajax({
		url: '/umbraco/api/event/BookableEvents',
		method: 'GET',// jQuery > 1.9
		type: 'GET', //jQuery < 1.9
		success: function (response) {
			var eventType = document.getElementById('eventType');			

			response.forEach(item => {
				eventType.options[eventType.options.length] = new Option(item.eventTypeName, item.eventTypeId);
			});			
		},
		error: function () { }
	});
}

$(document).ready(function () {
	getEvents();
});