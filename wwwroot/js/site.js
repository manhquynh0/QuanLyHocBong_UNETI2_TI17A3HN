window.showLoading = function () {
    document.getElementById("loadingOverlay")?.classList.add("show");
};

window.hideLoading = function () {
    document.getElementById("loadingOverlay")?.classList.remove("show");
};

window.unetiConfirm = function (options) {
    var title = options.title || "Xác nhận?";
    var body = options.body || "Hành động này không thể hoàn tác.";
    var okText = options.okText || "Xác nhận";
    var onOk = options.onOk;

    var titleEl = document.getElementById("confirmModalTitle");
    var bodyEl = document.getElementById("confirmModalBody");
    var okBtn = document.getElementById("confirmModalOk");
    var modalEl = document.getElementById("confirmModal");
    if (!modalEl || !window.bootstrap) {
        if (window.confirm(title + "\n" + body)) onOk && onOk();
        return;
    }

    titleEl.textContent = title;
    bodyEl.innerHTML = body;
    okBtn.innerHTML = '<i class="bi bi-trash me-1"></i> ' + okText;

    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    var handler = function () {
        okBtn.removeEventListener("click", handler);
        modal.hide();
        onOk && onOk();
    };
    okBtn.addEventListener("click", handler);
    modalEl.addEventListener("hidden.bs.modal", function () {
        okBtn.removeEventListener("click", handler);
    }, { once: true });
    modal.show();
};

document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("form[data-loading]").forEach(function (form) {
        form.addEventListener("submit", function () {
            if (form.checkValidity()) window.showLoading();
        });
    });

    document.querySelectorAll("form.js-confirm").forEach(function (form) {
        form.addEventListener("submit", function (e) {
            if (form.dataset.confirmed === "1") return;
            e.preventDefault();
            window.unetiConfirm({
                title: form.dataset.confirmTitle || "Xác nhận?",
                body: form.dataset.confirmBody || "Hành động này không thể hoàn tác.",
                okText: form.dataset.confirmOk || "Xác nhận",
                onOk: function () {
                    form.dataset.confirmed = "1";
                    form.submit();
                }
            });
        });
    });
});
