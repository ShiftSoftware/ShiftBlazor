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
            const { isSuccess, message, csvURL, fileName, alertTitle } = e.data

            if (isSuccess) {
                window.downloadFileFromUrl(fileName, csvURL)
                setTimeout(() => URL.revokeObjectURL(csvURL), 1000)
            }

            dotNetObjectRef.invokeMethodAsync("OnExportProcessed", isSuccess, message, payload.name, alertTitle)

            worker.terminate()
        } else if (e.data.messageType === "export processing"){
            dotNetObjectRef.invokeMethodAsync("OnExportProcessing", payload.name)
        }
    }

    worker.postMessage({
        payload, headers: getHeaders(), origin: `${window.location.origin}/api` })
}

window.getQueryParam = function(key) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(key);
}

window.updateQueryParams = function (newParams) {
    const url = new URL(window.location);
    Object.entries(newParams).forEach(([key, value]) => {
        if (value) {
            url.searchParams.set(key, value);
        } else {
            url.searchParams.delete(key);
        }
    });
    history.pushState({}, '', url);
}

window.getWindowDimensions = function () {
    return {
        width: window.innerWidth,
        height: window.innerHeight,
    };
};

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

window.SetAsSortable = function (id, counter = 0) {
    // Wait for Sortable to be loaded
    if (typeof window["Sortable"] != "function") {
        if (counter < 10) {
            setTimeout(() => SetAsSortable(id, ++counter), 1000);
        }
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
    if (!grid) return;
    var headRow = grid.querySelector(".mud-table-container .mud-table-head .mud-table-row");
    if (!headRow) return;
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

function resizeMenuItems() {
    var menus = document.getElementsByClassName("menu-items-container");
    var offset = 5;

    for (var i = 0; i < menus.length; i++) {
        var menu = menus[i];
        var parent = menu.parentElement;
        var parentStyles = window.getComputedStyle(parent);
        var parentInnerWidth = parent.offsetWidth - parseFloat(parentStyles.paddingLeft) - parseFloat(parentStyles.paddingRight);
        var siblings = Array.from(parent.children).filter(x => x !== menu && !x.classList.contains("flex-grow-1"));
        var siblingsWidth = siblings.reduce((total, sib) => total + (sib.offsetWidth ?? sib.clientWidth), 0);

        var itemsContainer = menu.getElementsByClassName("menu-items")[0];
        var dropdown = menu.getElementsByClassName("menu-dropdown")[0];


        if (siblingsWidth + itemsContainer.offsetWidth + offset >= parentInnerWidth) {
            itemsContainer.classList.add("hide");
            dropdown.classList.remove("hide");
        } else {
            itemsContainer.classList.remove("hide");
            dropdown.classList.add("hide");
        }
    }
}

window.registerSlideDownEvent = function (element, dotNetObj) {
    element.ontransitionend = () => dotNetObj.invokeMethodAsync("TransitionEndHandler");
    element.children[0].ontransitionend = (e) => e.stopPropagation();
    }

window.removeSlideDownEvent = function (element) {
    element.ontransitionend = null;
    element.children[0].ontransitionend = null;
}

// Single-line chip overflow (see ShiftChipDisplay.razor): hide whole chips that don't fit and
// reveal a trailing "+N" indicator. Measurement-driven so chips are never sliced. One ResizeObserver
// per host element, disposed by the component to avoid leaking observers as grid rows recycle.
window.shiftChipOverflow = (function () {
    const observers = new WeakMap();

    function layout(el) {
        if (!el || !el.isConnected) return;
        const available = el.clientWidth;
        if (available <= 0) return;

        const items = Array.from(el.querySelectorAll(":scope > .shift-chip-display__item"));
        const overflow = el.querySelector(":scope > .shift-chip-display__overflow");
        if (items.length === 0) {
            if (overflow) overflow.style.display = "none";
            return;
        }

        // Reset to "everything visible, no +N".
        items.forEach(i => i.classList.remove("shift-chip-hidden"));
        if (overflow) overflow.style.display = "none";

        const left = el.getBoundingClientRect().left;
        const lastRight = items[items.length - 1].getBoundingClientRect().right - left;
        if (lastRight <= available + 0.5) return; // all chips fit, nothing to collapse

        // Overflow: reveal the +N indicator to measure its width, then reserve room for it.
        if (overflow) overflow.style.display = "";
        const reserve = overflow ? overflow.getBoundingClientRect().width + 4 : 0;
        const budget = available - reserve;

        // Record positions before hiding anything (hiding reflows and would invalidate later reads).
        const rights = items.map(i => i.getBoundingClientRect().right - left);
        let hidden = 0;
        for (let i = 0; i < items.length; i++) {
            // Never hide the first chip — it ellipsizes via CSS so at least one chip is always shown.
            if (i > 0 && rights[i] > budget) {
                items[i].classList.add("shift-chip-hidden");
                hidden++;
            }
        }

        if (hidden === 0) {
            // Only the first chip itself was too wide; it truncates via CSS — no +N needed.
            if (overflow) overflow.style.display = "none";
            return;
        }

        const countEl = overflow ? overflow.querySelector(".shift-chip-display__count") : null;
        if (countEl) countEl.textContent = String(hidden);
    }

    return {
        init(el) {
            if (!el || observers.has(el)) { layout(el); return; }
            const ro = new ResizeObserver(() => layout(el));
            ro.observe(el);
            observers.set(el, ro);
            layout(el);
        },
        layout: layout,
        dispose(el) {
            const ro = observers.get(el);
            if (ro) { ro.disconnect(); observers.delete(el); }
        }
    };
})();

function responsiveFix() {
    fixAllStickyColumns();
    resizeMenuItems();
}

window.addEventListener("keydown", handleKeydown);
window.addEventListener("keyup", releaseAltKey);
window.addEventListener("blur", releaseAltKey);
window.addEventListener("resize", responsiveFix);

// Prevent browser auto-translation.
// Browser translation tools (e.g. Google Translate) modify the DOM directly, which conflicts
// with frameworks that manage their own DOM (Blazor, React, etc.). This can cause rendering
// errors, broken event handlers, and hydration mismatches.
document.documentElement.setAttribute('translate', 'no');
if (!document.head.querySelector('meta[name="google"][content="notranslate"]')) {
    document.head.insertAdjacentHTML('beforeend', '<meta name="google" content="notranslate">');
}