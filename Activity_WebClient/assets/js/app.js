function init() {
    websocket = new WebSocket('ws://' + SERVER + ':8000/');

    websocket.onopen = function(obj) {
        onOpen(obj)
    };

    websocket.onclose = function(obj) {
        onClose(obj)
    };

    websocket.onerror = function(obj) {
        onError(obj)
    };

    websocket.onmessage = function(obj) {
        onMessage(obj)
    };
}

function onOpen(obj) {
    console.log('Sikeres csatlakozás a szerverhez');
}

function onClose(obj) {
    console.log('Lecsatlakozva a szerverről');
}

function onError(obj) {
    console.log('Hiba: ' + obj.data);
}

function onMessage(obj) {
	var data = JSON.parse(obj.data);

	if(data.action == 'refreshTime') {
		var time = moment.duration(data.value, 'second').format('mm:ss');
		document.getElementById('counter').innerHTML = time;
	}
}

window.addEventListener('load', init, false);