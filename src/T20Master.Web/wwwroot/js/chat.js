function scrollToBottom(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text);
}
