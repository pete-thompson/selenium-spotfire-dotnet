window.addEventListener("message", function (event) {
    // We only accept messages from ourselves
    if (event.source != window)
        return;

    if (event.data.type && (event.data.type == "SET_CREDENTIALS")) {
        console.log('Sending credentials to extension')
        let port = chrome.runtime.sendMessage({username: event.data.username, password: event.data.password})
    }
}, false)