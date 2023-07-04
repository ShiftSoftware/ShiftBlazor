// modified version of https://www.codingnepalweb.com/drag-and-drop-sortable-list-html-javascript/

const sortableList = document.querySelector(".mud-grid");
const addButton = document.querySelector(".mud-grid-item.add-file");
const items = sortableList.querySelectorAll(".mud-grid-item");

const initSortableList = (e) => {
    e.preventDefault();
    const draggingItem = document.querySelector(".dragging");
    // Getting all items except currently dragging and making array of them
    let siblings = [...sortableList.querySelectorAll(".mud-grid-item:not(.dragging)")];
    // Finding the sibling after which the dragging item should be placed
    let nextSibling = siblings.find(sibling => {
        var rect = sibling.getBoundingClientRect();
        var row = e.clientX <= rect.left + sibling.offsetWidth / 2;
        var col = e.clientY <= rect.top + rect.height;
        return row && col;
    });

    // Inserting the dragging item before the found sibling
    sortableList.insertBefore(draggingItem, nextSibling);
    sortableList.appendChild(addButton);
}
sortableList.addEventListener("dragover", initSortableList);
sortableList.addEventListener("dragenter", e => e.preventDefault());

sortableList.addEventListener("dragstart", function (e) {
    if (e.target.classList.contains("mud-grid-item") && e.target.draggable) {
        e.target.classList.add("dragging");
    }
});

sortableList.addEventListener("dragend", (e) => {
    if (e.target.classList.contains("mud-grid-item") && e.target.draggable) {
        e.target.classList.remove("dragging");
    }
});