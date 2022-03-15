document.addEventListener("DOMContentLoaded", start);

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
        canvas_verify.getContext('2d').drawImage(video, 0, 0, canvas_verify.width, canvas_verify.height);
        canvas_verify.toBlob(blob => {
            const xhr = new XMLHttpRequest();
            xhr.open('POST', '/api/image', true);
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
}



