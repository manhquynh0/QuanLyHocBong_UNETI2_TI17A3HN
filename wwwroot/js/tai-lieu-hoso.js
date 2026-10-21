/* Họ và tên: Nguyễn Phi Hùng
   Mã sinh viên: 23103100148
   Nội dung thực hiện: Module 3 - chọn tệp đính kèm hồ sơ: kiểm tra định dạng/dung lượng/số lượng phía trình duyệt,
   hiện danh sách tệp, kéo-thả, bỏ chọn từng tệp. (Server vẫn kiểm tra lại toàn bộ - đây chỉ để báo lỗi sớm.) */
(function () {
    "use strict";

    var DUOI = [".pdf", ".jpg", ".jpeg", ".png"];

    function duoi(ten) {
        var i = ten.lastIndexOf(".");
        return i < 0 ? "" : ten.slice(i).toLowerCase();
    }

    function dinhDang(byte) {
        return byte >= 1048576
            ? (byte / 1048576).toFixed(1) + " MB"
            : Math.max(1, Math.round(byte / 1024)) + " KB";
    }

    function khoiTao(input) {
        var toiDaByte = parseInt(input.dataset.maxBytes, 10) || 10485760;
        var toiDaTep = parseInt(input.dataset.maxFiles, 10) || 5;
        var dsEl = document.getElementById(input.dataset.list);
        var loiEl = document.getElementById(input.dataset.error);
        var vung = document.querySelector('label.dropzone[for="' + input.id + '"]');
        var daChon = [];
        var coDataTransfer = typeof DataTransfer !== "undefined";

        function hienLoi(ds) {
            if (!loiEl) return;
            loiEl.innerHTML = "";
            ds.forEach(function (m) {
                var d = document.createElement("div");
                d.textContent = m;
                loiEl.appendChild(d);
            });
        }

        function veDanhSach() {
            if (!dsEl) return;
            dsEl.innerHTML = "";
            daChon.forEach(function (f, i) {
                var li = document.createElement("li");
                li.className = "upload-item";

                var icon = document.createElement("i");
                icon.className = "bi " + (duoi(f.name) === ".pdf" ? "bi-file-earmark-pdf doc-icon" : "bi-file-earmark-image doc-icon-img");

                var ten = document.createElement("span");
                ten.className = "upload-name";
                ten.textContent = f.name;
                ten.title = f.name;

                var kt = document.createElement("span");
                kt.className = "upload-size";
                kt.textContent = dinhDang(f.size);

                var bo = document.createElement("button");
                bo.type = "button";
                bo.className = "btn-remove";
                bo.setAttribute("aria-label", "Bỏ chọn " + f.name);
                bo.innerHTML = '<i class="bi bi-x-lg"></i>';
                bo.addEventListener("click", function () {
                    daChon.splice(i, 1);
                    hienLoi([]);
                    dongBo();
                });

                li.appendChild(icon);
                li.appendChild(ten);
                li.appendChild(kt);
                li.appendChild(bo);
                dsEl.appendChild(li);
            });
        }

        // Ghi lại danh sách đã chọn vào <input> để gửi lên server.
        function dongBo() {
            if (coDataTransfer) {
                try {
                    var dt = new DataTransfer();
                    daChon.forEach(function (f) { dt.items.add(f); });
                    input.files = dt.files;
                } catch (e) { /* trình duyệt cũ: server vẫn kiểm tra */ }
            }
            veDanhSach();
        }

        function them(files) {
            var loi = [];
            Array.prototype.forEach.call(files, function (f) {
                if (DUOI.indexOf(duoi(f.name)) < 0) {
                    loi.push("Tệp \"" + f.name + "\" không đúng định dạng (chỉ chấp nhận PDF, JPG, PNG).");
                    return;
                }
                if (f.size === 0) {
                    loi.push("Tệp \"" + f.name + "\" rỗng (0 KB).");
                    return;
                }
                if (f.size > toiDaByte) {
                    loi.push("Tệp \"" + f.name + "\" (" + dinhDang(f.size) + ") vượt quá " + Math.round(toiDaByte / 1048576) + " MB.");
                    return;
                }
                var trung = daChon.some(function (c) { return c.name === f.name && c.size === f.size; });
                if (trung) return;
                if (daChon.length >= toiDaTep) {
                    loi.push("Chỉ được chọn tối đa " + toiDaTep + " tệp.");
                    return;
                }
                daChon.push(f);
            });
            hienLoi(loi);
            dongBo();
        }

        input.addEventListener("change", function () {
            var moiChon = Array.prototype.slice.call(input.files);
            if (!coDataTransfer) daChon = [];   // không gộp được danh sách -> mỗi lần chọn thay thế lần trước
            them(moiChon);
        });

        if (vung) {
            ["dragenter", "dragover"].forEach(function (ev) {
                vung.addEventListener(ev, function (e) { e.preventDefault(); vung.classList.add("dragover"); });
            });
            ["dragleave", "drop"].forEach(function (ev) {
                vung.addEventListener(ev, function (e) { e.preventDefault(); vung.classList.remove("dragover"); });
            });
            vung.addEventListener("drop", function (e) {
                if (e.dataTransfer && e.dataTransfer.files) them(e.dataTransfer.files);
            });
        }
    }

    function khoiTaoTatCa() {
        document.querySelectorAll('input[type="file"][data-uploader]').forEach(khoiTao);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", khoiTaoTatCa);
    } else {
        khoiTaoTatCa();
    }
})();
