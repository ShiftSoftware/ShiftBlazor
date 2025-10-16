const getHeaders = () => ({
    cookie: document.cookie,
    "Content-Type": "application/json",
    authorization: "Bearer " + JSON.parse(localStorage.token).Token,
});
   
window.tableExport = (payload, dotNetObjectRef) => {
    const worker = new Worker(
        "_content/ShiftSoftware.ShiftBlazor/workers/table-export.js"
    )

    worker.onmessage = (e) => {

        if (e.data.messageType === "export ended") {
            const { isSuccess, message, csvURL, fileName } = e.data

            if (isSuccess) {
                window.downloadFileFromUrl(fileName, csvURL)
                setTimeout(() => URL.revokeObjectURL(csvURL), 1000)
            }

            dotNetObjectRef.invokeMethodAsync("OnExportProcessed", isSuccess, message, payload.name)

            worker.terminate()
        } else if (e.data.messageType === "export processing"){
            dotNetObjectRef.invokeMethodAsync("OnExportProcessing", payload.name)
        }
    }

    worker.postMessage({
        payload, headers: getHeaders(), origin: `${window.location.origin}/api` })
}

window.GetUrl = function () {
    return window.location.href;
};

window.getQueryParam = function(key) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(key);
}

window.updateQueryParams= function (newParams) {
    const url = new URL(window.location);
    Object.entries(newParams).forEach(([key, value]) => {
        url.searchParams.set(key, value);
    });
    history.pushState({}, '', url);
}

function dispatchUrlChange() {
    window.dispatchEvent(new CustomEvent('urlchange', { detail: window.location }));
}

// Overwrite pushState and ReplaceState to be included in urlchange event
const originalPushState = history.pushState;
history.pushState = function (...args) {
    originalPushState.apply(history, args);
    dispatchUrlChange();
};

const originalReplaceState = history.replaceState;
history.replaceState = function (...args) {
    originalReplaceState.apply(history, args);
    dispatchUrlChange();
};

window.addEventListener('popstate', dispatchUrlChange);

const customListeners = {};

window.addCustomUrlChangeListener = function (dotNetRef, listenerId) {
    const listener = (e) => {
        dotNetRef.invokeMethodAsync("OnUrlChanged", e.detail.href);
    };

    customListeners[listenerId] = listener;
    window.addEventListener("urlchange", listener);
};

window.removeCustomUrlChangeListener = function (listenerId) {
    const listener = customListeners[listenerId];
    if (listener) {
        window.removeEventListener("urlchange", listener);
        delete customListeners[listenerId];
    }
};


window.getWindowDimensions = function () {
    return {
        width: window.innerWidth,
        height: window.innerHeight,
    };
};

window.reloadPage = function () {
    window.location.reload();
}

window.ClickElementById = function (id) {
    document.getElementById(id)?.click();
}

window.CloseFileExplorerDialogs = function (id) {
    document.getElementById(id)?.querySelectorAll(".e-dialog .e-dlg-closeicon-btn").forEach(x => x.click && x.click())
}

window.handleKeydown = function (e) {
    if (e.altKey || (e.code != null && e.code.includes("Alt"))) {
        e.preventDefault();
        document.body.classList.add("show-keys");
    }

    if ((e.altKey && (e.code != null && !e.code.includes("Alt"))) || e.code == "Escape") {
        DotNet.invokeMethod('ShiftSoftware.ShiftBlazor', 'SendKeys', [e.code]);
    }
}

window.releaseAltKey = function (e) {
    if (e.type === "blur" || e.code.includes("Alt")) {
        document.body.classList.remove("show-keys");
    }
}

window.SetAsSortable = function (id) {
    if (typeof window["Sortable"] != "function") {
        return;
    }
    let addButtonClass = ".add-file";

    let element = document.getElementById(id);
    let grid = element.querySelector(".mud-grid");

    if (element && grid.children[0].getAttribute("draggable") == null) {
        new Sortable(grid, {
            animation: 150,
            filter: addButtonClass,
            dataIdAttr: "data-guid",
            onMove: function (e) {
                if (element.classList.contains("readonly") || e.related.classList.contains(addButtonClass.slice(1))) {
                    return false;
                }
            },
            onEnd: function (e) {
                let order = [...grid.querySelectorAll(`.mud-grid-item:not(${addButtonClass})`)].map(x => x.dataset.guid);
                DotNet.invokeMethod("ShiftSoftware.ShiftBlazor", "ReorderGrid", { Key: id, Value: order });
            }
        });
    }
}

window.fixStickyColumn = function (gridId) {

    var grid = document.getElementById(gridId);
    var headRow = grid.querySelector(".mud-table-container .mud-table-head .mud-table-row");
    var leftCells = [...headRow.getElementsByClassName("sticky-left")];
    var rightCells = [...headRow.getElementsByClassName("sticky-right")];
    var offset = 0;
    var css = [];

    for (var cell of leftCells) {
        css.push(`#${gridId} .mud-table-container .mud-table-row .mud-table-cell.sticky-left:nth-child(${cell.cellIndex + 1}) { left: ${offset}px}`);
        var cellWidth = cell.getBoundingClientRect().width;
        offset += cellWidth;
    }

    offset = 0;
    for (var cell of rightCells.reverse()) {
        css.push(`#${gridId} .mud-table-container .mud-table-row .mud-table-cell.sticky-right:nth-child(${cell.cellIndex + 1}) { right: ${offset}px}`);
        var cellWidth = cell.getBoundingClientRect().width;
        offset += cellWidth;
    }

    var cssId = "css-" + gridId;
    document.getElementById(cssId)?.remove();
    var style = document.createElement("style");
    style.id = cssId;
    style.innerHTML = css.join(" ");

    grid.appendChild(style);
}

window.fixAllStickyColumns = function () {
    document.querySelectorAll("[id^='Grid-']").forEach(x => fixStickyColumn(x.id));
}

window.scrollToFirstError = function (id) {
    var form = document.getElementById(id);
    var element = form?.getElementsByClassName("mud-input-error")[0];
    element?.scrollIntoView();
}

window.setDropZone = function (UploaderId, dropZoneSelector) {

    let input = document.querySelector(`${UploaderId} [id^="Input"]`);
    let dropZone = document.querySelector(dropZoneSelector ?? UploaderId);

    input.addEventListener("drop", () => endDrop(input, dropZone));
    input.addEventListener("dragleave", () => endDrop(input, dropZone));
    input.addEventListener("dragend", () => endDrop(input, dropZone));
    input.addEventListener("mouseleave", () => endDrop(input, dropZone));
    dropZone.addEventListener("dragenter", () => startDrop(input, dropZone));

    if (dropZone.style.position === "static" || dropZone.style.position === "") {
        dropZone.style.position = "relative";
    }

    if (dropZoneSelector != null) {
        dropZone.appendChild(input);
    }
};

function endDrop(input, dropZone) {
    input.style.display = "none";
    dropZone.classList.remove("drop-area-active");
}

function startDrop(input, dropZone) {
    input.style.display = "block";
    dropZone.classList.add("drop-area-active");
}

window.openInput = function (id, dirUpload) {
    var input = document.getElementById(id);

    if (dirUpload) {
        input.setAttribute("webkitdirectory", "");
    }
    else {
        input.removeAttribute("webkitdirectory");
    }

    input?.click();
}

window.getFileList = (inputFile) => {
    const fileList = [];

    for (let i = 0; i < inputFile.files.length; i++) {
        fileList.push(inputFile.files[i].webkitRelativePath);
    }

    return fileList;
};

window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    window.downloadFileFromUrl(fileName, url);
    URL.revokeObjectURL(url);
}

window.downloadFileFromUrl = function (fileName, url) {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.target = "_blank";
    anchorElement.click();
    anchorElement.remove();
}

window.getCursorPosition = function (inputId) {
    const input = document.getElementById(inputId);
    if (input && input.selectionStart !== undefined) {
        return [input.selectionStart, input.selectionEnd];
    }

    return [-1, -1]; // Return -1 if the input is not found or selectionStart is not supported
}

window.GetFocusableElementCount = function (element)
{
    return getTabbableElements(element).length;
}

window.addEventListener("keydown", handleKeydown);
window.addEventListener("keyup", releaseAltKey);
window.addEventListener("blur", releaseAltKey);
window.addEventListener("resize", fixAllStickyColumns);