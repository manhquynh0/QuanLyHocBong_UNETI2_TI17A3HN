(() => {
  const searchForm = document.getElementById("scholarship-search-form");
  if (searchForm) {
    let searchTimer;

    searchForm.querySelectorAll('input[type="search"], input[type="number"]').forEach((input) => {
      input.addEventListener("input", () => {
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(() => {
          if (searchForm.reportValidity()) {
            searchForm.requestSubmit();
          }
        }, 500);
      });
    });

    searchForm.querySelectorAll("select").forEach((control) => {
      control.addEventListener("change", () => {
        if (searchForm.reportValidity()) {
          searchForm.requestSubmit();
        }
      });
    });
  }

})();
