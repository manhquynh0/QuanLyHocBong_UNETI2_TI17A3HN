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

    searchForm.querySelectorAll("select").forEach((select) => {
      select.addEventListener("change", () => {
        if (searchForm.reportValidity()) {
          searchForm.requestSubmit();
        }
      });
    });
  }

  const bulkForm = document.getElementById("scholarship-bulk-form");
  if (!bulkForm) {
    return;
  }

  const selectAll = document.getElementById("select-all-scholarships");
  const rowCheckboxes = Array.from(bulkForm.querySelectorAll(".scholarship-row-checkbox"));
  const selectedCount = document.getElementById("selected-scholarship-count");
  const bulkActions = document.getElementById("scholarship-bulk-actions");
  const statusSelect = document.getElementById("bulk-scholarship-status");
  const updateStatusButton = document.getElementById("bulk-update-status");

  const updateSelection = () => {
    const checkedCount = rowCheckboxes.filter((checkbox) => checkbox.checked).length;
    if (selectedCount) {
      selectedCount.textContent = checkedCount === 1
        ? "Đã chọn 1 học bổng"
        : checkedCount > 1
          ? `Đã chọn ${checkedCount} học bổng`
          : "Chưa chọn học bổng";
    }

    if (bulkActions) {
      bulkActions.hidden = checkedCount === 0;
    }

    if (updateStatusButton) {
      updateStatusButton.disabled = checkedCount === 0 || !statusSelect?.value;
    }

    if (selectAll) {
      selectAll.checked = checkedCount > 0 && checkedCount === rowCheckboxes.length;
      selectAll.indeterminate = checkedCount > 0 && checkedCount < rowCheckboxes.length;
    }
  };

  selectAll?.addEventListener("change", () => {
    rowCheckboxes.forEach((checkbox) => {
      checkbox.checked = selectAll.checked;
    });
    updateSelection();
  });

  rowCheckboxes.forEach((checkbox) => checkbox.addEventListener("change", updateSelection));

  statusSelect?.addEventListener("change", updateSelection);

  bulkForm.addEventListener("submit", (event) => {
    const submitter = event.submitter;
    const checkedCount = rowCheckboxes.filter((checkbox) => checkbox.checked).length;
    if (checkedCount === 0) {
      event.preventDefault();
      window.alert("Hãy chọn ít nhất một học bổng trước khi thực hiện thao tác.");
      return;
    }

    if (submitter?.value === "status" && !statusSelect?.value) {
      event.preventDefault();
      window.alert("Hãy chọn trạng thái mới trước khi cập nhật.");
      return;
    }

    const confirmation = submitter?.value === "status"
      ? `Xác nhận đổi trạng thái thành "${statusSelect.value}" cho ${checkedCount} học bổng đã chọn?`
      : submitter?.dataset.confirm;

    if (confirmation && !window.confirm(confirmation)) {
      event.preventDefault();
    }
  });

  updateSelection();
})();
