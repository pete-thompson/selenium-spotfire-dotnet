// A simple Chrome extension that hides the downloads shelf whenever it appears
// We want this so that we know that any images we capture from pages will be the same size regardless of whether there have been previous downloads or not

chrome.downloads.onChanged.addListener(function (e) {
    if ((typeof e.state !== "undefined") && (e.state.current === "complete")) {
        chrome.downloads.setShelfEnabled(false);
        chrome.downloads.erase({ state: "complete" });
    }
});
