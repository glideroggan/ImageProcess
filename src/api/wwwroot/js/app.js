document.addEventListener("DOMContentLoaded", start);

function start() {
    let camera_button = document.querySelector("#start-camera");
    let video = document.querySelector("#video");
    let click_button = document.querySelector("#click-photo");
    let canvas = document.querySelector("#canvas");

    camera_button.addEventListener('click', async function () {
        let stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
        video.srcObject = stream;

        // TODO: set up a setInterval that will take a capture
    });

    click_button.addEventListener('click', function () {
        canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height);
        // let image_data_url = canvas.toDataURL('image/jpeg');
        canvas.toBlob(blob => {
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

        // data url of the image
        // console.log(image_data_url);

        
    });
}



