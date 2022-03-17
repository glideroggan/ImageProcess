function debugImg(blob) {
    const urlCreator = window.URL || window.webkitURL;
    const imageUrl = urlCreator.createObjectURL(blob);
    document.querySelector('#test-image').src = imageUrl;
}

function sendRequestVerifyImage(blob, successCallback, errorCallback) {
    const xhr = new XMLHttpRequest();
    console.log('data sent:', blob.size);
    xhr.open('POST', '/api/verify', true);
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

function takeSnapshot(video, elCanvas, passed) {
    elCanvas.getContext('2d').drawImage(video, 0, 0, elCanvas.width, elCanvas.height);
    elCanvas.toBlob(blob => {
        sendRequestVerifyImage(blob, response => {
            let json = JSON.parse(response);
            if (json.error !== null && json.error.statusCode === 400) {
                console.log('no face found in image');
            } else if (json.data.identified === true) {
                passed(json.data.name);
            } else {
                // not identified
                console.log('who is this?!')
            }

        }, error => {
            console.log('Some error occurred');
            console.log(this.responseText);
        })
    }, 'image/jpeg', 1);
}

function start() {
    let camera_button = document.querySelector("#start-camera");
    let video = document.querySelector("#video");
    let addFaceButton = document.querySelector("#add-face");
    let canvas_add = document.querySelector("#canvas-add");
    let verifyFaceButton = document.querySelector("#verify-face");
    let canvas_verify = document.querySelector("#canvas-verify");
    const add_face_name = document.getElementById("name")
    

    camera_button.addEventListener('click', async function () {
        let stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
        video.srcObject = stream;

        // TODO: set up a setInterval that will take a capture
    });

    addFaceButton.addEventListener('click', function () {
        canvas_add.getContext('2d').drawImage(video, 0, 0, canvas_add.width, canvas_add.height);
        canvas_add.toBlob(blob => {
            debugImg(blob);
            const xhr = new XMLHttpRequest();
            xhr.open('POST', `/api/upload/${add_face_name.value}`, true);
            xhr.setRequestHeader('content-type', 'image/jpg');
            xhr.onload = function () {
                if (this.status === 200) {
                    console.log(this.responseText);
                } else {
                    console.log('Some error occurred');
                }
            }
            xhr.send(blob);    
        }, 'image/jpeg', 1);
    });
    verifyFaceButton.addEventListener('click', function () {
        takeSnapshot(video, canvas_verify, (name) => {
            console.log('verified: ', name);
        });
    });
}



