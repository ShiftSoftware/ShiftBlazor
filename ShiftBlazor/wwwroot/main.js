window.GetUrl = function () {
    return window.location.href;
};

window.GridAllSelected = function (id) {
    return document.querySelector(`#${id} .e-checkselectall + .e-check`) != null
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