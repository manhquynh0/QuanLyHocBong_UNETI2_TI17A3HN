/* Việt hóa thông báo mặc định (tiếng Anh) của jQuery Validation.
   Cách dùng: nạp SAU jquery.validate (tức là ngay sau <partial name="_ValidationScriptsPartial" />). */
(function ($) {
    if (!$ || !$.validator) { return; }

    $.extend($.validator.messages, {
        required: "Trường này là bắt buộc.",
        remote: "Vui lòng sửa trường này.",
        email: "Email không đúng định dạng.",
        url: "Địa chỉ web không hợp lệ.",
        date: "Ngày không hợp lệ.",
        dateISO: "Ngày không hợp lệ (định dạng yyyy-MM-dd).",
        number: "Vui lòng nhập một số hợp lệ.",
        digits: "Vui lòng chỉ nhập chữ số.",
        equalTo: "Vui lòng nhập lại cùng một giá trị.",
        maxlength: $.validator.format("Vui lòng nhập tối đa {0} ký tự."),
        minlength: $.validator.format("Vui lòng nhập ít nhất {0} ký tự."),
        rangelength: $.validator.format("Vui lòng nhập từ {0} đến {1} ký tự."),
        range: $.validator.format("Vui lòng nhập giá trị từ {0} đến {1}."),
        max: $.validator.format("Vui lòng nhập giá trị nhỏ hơn hoặc bằng {0}."),
        min: $.validator.format("Vui lòng nhập giá trị lớn hơn hoặc bằng {0}."),
        // step="0.01" (ô điểm) -> nhắc tối đa 2 chữ số thập phân; step khác -> thông báo chung
        step: function (param) {
            if (Number(param) === 0.01) {
                return "Chỉ nhập tối đa 2 chữ số thập phân (ví dụ 3.25).";
            }
            return $.validator.format("Vui lòng nhập bội số của {0}.")(param);
        }
    });
})(window.jQuery);
