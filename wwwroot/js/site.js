// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.querySelectorAll(".side-nav .nav-item").forEach((item) => {
    item.addEventListener("click", (event) => {
        const currentItem = event.currentTarget;
        const isPlaceholderLink = currentItem.getAttribute("href") === "#";

        if (isPlaceholderLink) {
            event.preventDefault();
        }

        document.querySelectorAll(".side-nav .nav-item").forEach((navItem) => {
            navItem.classList.remove("active");
            navItem.removeAttribute("aria-current");
        });

        currentItem.classList.add("active");
        currentItem.setAttribute("aria-current", "page");
    });
});
