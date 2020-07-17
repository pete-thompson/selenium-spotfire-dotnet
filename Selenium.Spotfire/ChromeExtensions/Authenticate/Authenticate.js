// An extension to handle authentication prompts when opening Spotfire
// Note that extensions aren't supported when running Headless, so this functionality won't work for Headless

const bkg = chrome.extension.getBackgroundPage()
bkg.console.log('Initialising Authentication extension')

let username = ''
let password = ''

chrome.runtime.onMessage.addListener(
    function (request, sender) {
        console.log('Received credentials for ' + request.username)
        username = request.username
        password = request.password
    }
)

chrome.webRequest.onAuthRequired.addListener(
    function handler(details) {
        if (username !== '') {
            bkg.console.log('Handling authentication request - user ', username)
            return { 'authCredentials': { username: username, password: password } };
        } else {
            bkg.console.log('Unable to handle authentication request - credentials not set')
        }
    },
    { urls: ["<all_urls>"] },
    ['blocking']
)