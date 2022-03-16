document.addEventListener("DOMContentLoaded", start);

var intervalId;
function stop() {
    clearInterval(intervalId);
}

function takeSnapshot(video, elCanvas, passed) {
    elCanvas.getContext('2d').drawImage(video, 0, 0, elCanvas.width, elCanvas.height);
    elCanvas.toBlob(blob => {
        const xhr = new XMLHttpRequest();
        xhr.open('POST', '/api/verify', true);
        xhr.setRequestHeader('content-type', 'image/jpg');
        xhr.onload = function () {
            if (this.status === 200) {
                // TODO: approved face
                console.log(this.responseText);
                // stop()
            } else {
                console.log('Some error occurred');
                console.log(this.responseText);
                stop();
            }
        }
        xhr.onreadystatechange = function() {
            if (xhr.readyState === 4) {
                if (xhr.response == null) {
                    console.log('some error occurred!');
                    stop()
                } else {
                    let res = JSON.parse(xhr.response);
                    if (res.error !== null && res.error.statusCode === 400) {
                        console.log('no face found in image');
                    } else if (res.data.identified == true) {
                        stop();
                        passed(res.data.name);
                    } else {
                        // not identified
                        console.log('who is this?!')
                    } 
                    console.log(res);
                }
                
            }
        }
        xhr.send(blob);
    }, 'image/jpeg', 1);
}

function passed(name) {
    const h1 = document.getElementById('msg');
    h1.innerText = `Welcome ${name}`;
}

async function start() {
    let camera_button = document.querySelector("#start-camera");
    let video = document.querySelector("#video");
    let addFaceButton = document.querySelector("#add-face");
    let canvas_add = document.querySelector("#canvas-add");
    let verifyFaceButton = document.querySelector("#verify-face");
    let snapshotCanvas = document.querySelector("#snapshot");
    const add_face_name = document.getElementById("name")

    let stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
    video.srcObject = stream;
    intervalId = setInterval(takeSnapshot, 2000, video, snapshotCanvas, passed);
    
    // addFaceButton.addEventListener('click', function () {
    //     canvas_add.getContext('2d').drawImage(video, 0, 0, canvas_add.width, canvas_add.height);
    //     canvas_add.toBlob(blob => {
    //         const xhr = new XMLHttpRequest();
    //         xhr.open('POST', `/api/upload/${add_face_name.value}`, true);
    //         xhr.setRequestHeader('content-type', 'image/jpg');
    //         xhr.onload = function () {
    //             if (this.status === 200) {
    //                 console.log(this.responseText);
    //             } else {
    //                 console.log('Some error occurred');
    //             }
    //         }
    //         xhr.send(blob);
    //     }, 'image/jpeg', 1);
    // });
    // verifyFaceButton.addEventListener('click', function () {
    //     canvas_verify.getContext('2d').drawImage(video, 0, 0, canvas_verify.width, canvas_verify.height);
    //     canvas_verify.toBlob(blob => {
    //         const xhr = new XMLHttpRequest();
    //         xhr.open('POST', '/api/verify', true);
    //         xhr.setRequestHeader('content-type', 'image/jpg');
    //         xhr.onload = function () {
    //             if (this.status === 200) {
    //                 console.log(this.responseText);
    //             } else {
    //                 console.log('Some error occurred');
    //             }
    //         }
    //         xhr.send(blob);
    //     }, 'image/jpeg', 1);
    // });
}



