var intervalId;

function stop() {
    clearInterval(intervalId);
}

var waiting = false;

function takeSnapshot(video, elCanvas, passed, sendDataCallback) {
    if (waiting) return;
    waiting = true;
    elCanvas.getContext('2d').drawImage(video, 0, 0, elCanvas.width, elCanvas.height);
    elCanvas.toBlob(blob => {
        sendDataCallback(blob, name => {
                if (name === null) {
                    msgFn('who is this?!');
                } else {
                    video.pause();
                    video.srcObject.getTracks()[0].stop();
                    stop();
                    passed(name);
                }
                waiting = false;
            },
            error => {
                switch (error.statusCode) {
                    case 400:
                        msgFn('No face found, adjust position or lightning');
                        break;
                    case 401:
                        msgFn('Too many faces! Make sure you\'re the only one.');
                        break;
                }
                // console.log('Some error occurred');
                // console.log(this.responseText);
                // stop();
                waiting = false;
            })
    }, 'image/jpeg', 1);
}

function msgFn(msg) {
    const h1 = document.getElementById('msg');
    h1.innerText = `${msg}`;
}

function passed(name) {
    msgFn(`Welcome ${name}`)
}

async function start() {
    let camera_button = document.querySelector("#start-camera");
    let video = document.querySelector("#video");
    let addFaceButton = document.querySelector("#add-face");
    let canvas_add = document.querySelector("#canvas-add");
    let verifyFaceButton = document.querySelector("#verify-face");
    let snapshotCanvas = document.querySelector("#snapshot");
    const add_face_name = document.getElementById("name")

    let stream = await navigator.mediaDevices.getUserMedia({video: true, audio: false});
    video.srcObject = stream;
    intervalId = setInterval(takeSnapshot, 2000, video, snapshotCanvas, passed, sendDataCallback);
}



