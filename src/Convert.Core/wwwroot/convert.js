// Core's JavaScript, loaded as an ES module by BrowserInterop: nothing lands in the global scope and the
// app needs no <script> tag for it.

// Bytes arrive as a Uint8Array: Blazor marshals a byte[] argument natively, with no base64 round trip.
function toBlob(contentType, bytes) {
    return new Blob([bytes], { type: contentType });
}

export function download(fileName, contentType, bytes) {
    const url = URL.createObjectURL(toBlob(contentType, bytes));
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    // Revoked on the next turn: Safari has not always started the download when click() returns.
    setTimeout(() => URL.revokeObjectURL(url), 0);
}

export function createObjectUrl(contentType, bytes) {
    return URL.createObjectURL(toBlob(contentType, bytes));
}

export function revokeObjectUrl(url) {
    URL.revokeObjectURL(url);
}

export function userAgent() {
    return navigator.userAgent;
}

// A file dropped anywhere on the page opens through input, as if picked there: the InputFile then reads
// it like any other. The body carries "dragging" while a file is held over the page. Only the first of
// several files is handed on, since InputFileChangeEventArgs.File throws on more. The listeners stay for
// the page's life but do nothing once input has left the document (the wizard moved past Empty).
let current = null;
let attached = false;

export function acceptDrops(input) {
    current = input;
    if (attached) {
        return;
    }

    attached = true;
    const root = document.body;
    const holdsFiles = event => event.dataTransfer?.types?.includes('Files');
    const target = () => current?.isConnected ? current : null;
    const leave = () => root.classList.remove('dragging');

    root.addEventListener('dragover', event => {
        if (holdsFiles(event) && target()) {
            event.preventDefault();
            event.dataTransfer.dropEffect = 'copy';
            root.classList.add('dragging');
        }
    });
    root.addEventListener('dragleave', event => {
        if (!root.contains(event.relatedTarget)) {
            leave();
        }
    });
    root.addEventListener('drop', event => {
        leave();
        const input = target();
        const file = event.dataTransfer?.files?.[0];
        if (!input || !file) {
            return;
        }

        event.preventDefault();
        const transfer = new DataTransfer();
        transfer.items.add(file);
        input.files = transfer.files;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    });
}

// Sets the URL fragment without adding a history entry or a navigation. Empty clears it.
export function replaceHash(fragment) {
    const url = location.pathname + location.search + (fragment ? '#' + fragment : '');
    history.replaceState(history.state, '', url);
}
