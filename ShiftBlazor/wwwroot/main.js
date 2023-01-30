function GetUrl() {
    return window.location.href;
}

function GridAllSelected(id) {
    return document.querySelector(`#${id} .e-checkselectall + .e-check`) != null
}