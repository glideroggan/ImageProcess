document.addEventListener("DOMContentLoaded", start);

function sendRequestVerifyImage(blob, successCallback, errorCallback) {
    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/api/faces/verify', true);
    xhr.setRequestHeader('content-type', 'image/jpg');
    xhr.onload = function () {
        if (this.status === 200) {
            successCallback(this.response);
        } else {
            errorCallback(this.response);
        }
    }
    xhr.send(blob);
}

function sendDataCallback(blob, success, error) {
    sendRequestVerifyImage(blob, response => {
        let json = JSON.parse(response);
        if (json.error !== null) {
            error(json.error);
        } else {
            success(json.data.name);
        }
    }, error);
}
