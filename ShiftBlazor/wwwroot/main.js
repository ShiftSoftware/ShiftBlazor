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