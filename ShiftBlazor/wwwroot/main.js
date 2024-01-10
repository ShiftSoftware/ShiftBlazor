window.GetUrl = function () {
    return window.location.href;
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

window.handleKeydown = function (e) {
    if (e.altKey || e.code.includes("Alt")) {
        e.preventDefault();
        document.body.classList.add("show-keys");
    }

    if ((e.altKey && !e.code.includes("Alt")) || e.code == "Escape") {
        DotNet.invokeMethod('ShiftSoftware.ShiftBlazor', 'SendKeys', [e.code]);
    }
}

window.handleKeyup = function (e) {
    if (e.code.includes("Alt")) {
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


window.addEventListener("keydown", handleKeydown);
window.addEventListener("keyup", handleKeyup);
window.addEventListener("blur", handleKeyup);
