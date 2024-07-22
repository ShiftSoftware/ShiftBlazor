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

function fixAllStickyColumns() {
    document.querySelectorAll("[id^='Grid-']").forEach(x => fixStickyColumn(x.id));
}

function fixAutocompleteIndent(inputId) {
    // calculate visible tags size
    var input = document.getElementById(inputId);
    var tags = input.getElementsByClassName("autocomplete-tags")[0];
    var tagsWidth = tags.getBoundingClientRect().width;
    input.querySelector("input").style.textIndent = tagsWidth + "px";

    // calculate total tags size
    var inputWidth = input.getBoundingClientRect().width;
    var hiddenTags = input.getElementsByClassName("autocomplete-tags")[1];
    var hiddenTagsWidth = hiddenTags.getBoundingClientRect().width;

    return hiddenTagsWidth / inputWidth;
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
window.addEventListener("keyup", releaseAltKey);
window.addEventListener("blur", releaseAltKey);
window.addEventListener("resize", fixAllStickyColumns);