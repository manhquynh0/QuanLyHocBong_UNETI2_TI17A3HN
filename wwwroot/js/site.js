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
    bodyEl.textContent = body;
    okBtn.innerHTML = '<i class="bi bi-check2-circle me-1"></i> ' + okText;

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

    document.querySelectorAll(".js-confirm").forEach(function (btn) {
        btn.addEventListener("click", function (e) {
            e.preventDefault();
            var form = btn.closest("form");
            window.unetiConfirm({
                title: btn.dataset.confirmTitle || "Xác nhận thao tác?",
                body: btn.dataset.confirmBody || "Bạn có chắc chắn muốn thực hiện hành động này?",
                okText: btn.dataset.confirmOk || "Xác nhận",
                onOk: function () {
                    if (form) {
                        window.showLoading();
                        form.submit();
                    } else if (btn.href) {
                        window.location.href = btn.href;
                    }
                }
            });
        });
    });
});
