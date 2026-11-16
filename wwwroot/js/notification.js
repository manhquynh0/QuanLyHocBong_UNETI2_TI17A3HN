/**
 * UNETI Realtime Notification Client (SignalR + WebSocket)
 * Đồng bộ số thông báo chưa đọc, hiển thị toast và cập nhật dropdown theo thời gian thực.
 */
(function () {
    'use strict';

    const badgeEl = document.getElementById('notificationBadge');
    const listEl = document.getElementById('notificationList');
    const markAllBtn = document.getElementById('btnMarkAllRead');
    const toastContainer = document.getElementById('notificationToastContainer');

    // Chỉ khởi tạo khi đã đăng nhập (có chuông thông báo trên layout)
    if (!badgeEl || !listEl) return;

    let unreadCount = 0;

    const ICON_MAP = {
        LichPhongVan: 'bi-calendar-event-fill text-warning',
        KetQua: 'bi-award-fill text-success',
        HoSo: 'bi-folder-check text-info'
    };

    function escapeHtml(str) {
        if (!str) return '';
        const p = document.createElement('p');
        p.textContent = str;
        return p.innerHTML;
    }

    function updateBadge(count) {
        unreadCount = Math.max(0, count);
        badgeEl.textContent = unreadCount > 99 ? '99+' : unreadCount;
        badgeEl.classList.toggle('d-none', unreadCount === 0);
    }

    function createNotificationItem(item) {
        const iconClass = ICON_MAP[item.loaiThongBao] || 'bi-bell-fill text-primary';
        const link = document.createElement('a');
        link.href = item.lienKet || 'javascript:void(0);';
        link.dataset.id = item.maThongBao;
        link.className = `dropdown-item p-3 border-bottom text-wrap d-flex align-items-start gap-2 ${item.daDoc ? 'opacity-75' : 'bg-light-subtle'}`;

        link.innerHTML = `
            <div class="mt-1 flex-shrink-0"><i class="bi ${iconClass} fs-5"></i></div>
            <div class="flex-grow-1">
                <div class="d-flex align-items-center justify-content-between mb-1">
                    <strong class="fs-6 text-dark">${escapeHtml(item.tieuDe)}</strong>
                    <small class="text-muted ms-2">${escapeHtml(item.thoiGianTuongDoi || item.ngayTao || '')}</small>
                </div>
                <p class="m-0 text-muted small lh-sm">${escapeHtml(item.noiDung)}</p>
            </div>
            ${item.daDoc ? '' : '<div class="ms-1 mt-2"><span class="badge bg-primary p-1 rounded-circle" style="display:inline-block; width:8px; height:8px;" title="Chưa đọc"></span></div>'}
        `;

        link.addEventListener('click', () => {
            if (item.daDoc) return;
            item.daDoc = true;
            markAsRead(item.maThongBao);
            link.classList.remove('bg-light-subtle');
            link.classList.add('opacity-75');
            link.querySelector('.badge.bg-primary')?.remove();
        });

        return link;
    }

    function loadNotifications() {
        fetch('/ThongBao/GetMyNotifications?top=15')
            .then(res => res.json())
            .then(res => {
                if (!res.success) return;
                updateBadge(res.unreadCount);
                listEl.innerHTML = '';

                if (!res.data || res.data.length === 0) {
                    listEl.innerHTML = '<div class="p-4 text-center text-muted"><i class="bi bi-inbox fs-3 d-block mb-1"></i>Bạn không có thông báo nào.</div>';
                    return;
                }

                res.data.forEach(item => listEl.appendChild(createNotificationItem(item)));
            })
            .catch(err => {
                listEl.innerHTML = '<div class="p-3 text-center text-danger small">Không thể tải thông báo.</div>';
                console.warn('Lỗi tải thông báo:', err);
            });
    }

    function markAsRead(id) {
        fetch(`/ThongBao/DanhDauDaDoc/${id}`, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(res => res.json())
            .then(res => { if (res.success) updateBadge(res.unreadCount); })
            .catch(err => console.warn('Lỗi đánh dấu đã đọc:', err));
    }

    if (markAllBtn) {
        markAllBtn.addEventListener('click', (e) => {
            e.preventDefault();
            fetch('/ThongBao/DanhDauTatCaDaDoc', {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
                .then(res => res.json())
                .then(res => {
                    if (!res.success) return;
                    updateBadge(0);
                    listEl.querySelectorAll('.badge.bg-primary').forEach(d => d.remove());
                    listEl.querySelectorAll('.dropdown-item').forEach(it => {
                        it.classList.remove('bg-light-subtle');
                        it.classList.add('opacity-75');
                    });
                })
                .catch(err => console.warn('Lỗi đánh dấu tất cả đã đọc:', err));
        });
    }

    function showNotificationToast(item) {
        if (!toastContainer) return;

        const toastEl = document.createElement('div');
        toastEl.className = 'toast align-items-center text-bg-light border shadow-lg';
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');

        const linkHtml = item.lienKet
            ? `<a href="${escapeHtml(item.lienKet)}" class="btn btn-sm btn-outline-primary mt-2 d-block text-start"><i class="bi bi-arrow-right-circle me-1"></i>Xem chi tiết</a>`
            : '';

        toastEl.innerHTML = `
            <div class="toast-header bg-primary text-white">
                <i class="bi bi-bell-fill me-2"></i>
                <strong class="me-auto">${escapeHtml(item.tieuDe)}</strong>
                <small class="text-white-50">Vừa xong</small>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body text-dark">
                ${escapeHtml(item.noiDung)}
                ${linkHtml}
            </div>
        `;

        toastContainer.appendChild(toastEl);
        const bsToast = new bootstrap.Toast(toastEl, { delay: 6000 });
        bsToast.show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }

    // Kết nối SignalR nhận thông báo Realtime
    if (typeof signalR !== 'undefined') {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notification')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.on('NhanThongBao', (data) => {
            updateBadge(data.unreadCount !== undefined ? data.unreadCount : unreadCount + 1);

            const emptyEl = listEl.querySelector('.text-center');
            if (emptyEl && emptyEl.textContent.includes('không có')) {
                listEl.innerHTML = '';
            }

            listEl.insertBefore(createNotificationItem(data), listEl.firstChild);

            const btn = document.getElementById('notificationDropdownBtn');
            if (btn) {
                btn.classList.add('animate__animated', 'animate__swing');
                setTimeout(() => btn.classList.remove('animate__animated', 'animate__swing'), 1000);
            }

            showNotificationToast(data);
        });

        connection.start().catch(err => console.warn('SignalR Hub kết nối thất bại:', err));
    }

    function initServerToasts() {
        if (!toastContainer) return;
        toastContainer.querySelectorAll('.toast').forEach(el => {
            new bootstrap.Toast(el).show();
            el.addEventListener('hidden.bs.toast', () => el.remove());
        });
    }

    // Hàm Toast toàn cục cho JS
    window.showToast = function (message, type = 'success', title = '') {
        if (!toastContainer) return;

        const STYLES = {
            danger: { bg: 'text-bg-danger', icon: 'bi-exclamation-triangle-fill', title: 'Thông báo lỗi', delay: 6500 },
            error: { bg: 'text-bg-danger', icon: 'bi-exclamation-triangle-fill', title: 'Thông báo lỗi', delay: 6500 },
            warning: { bg: 'text-bg-warning text-dark', icon: 'bi-exclamation-circle-fill', title: 'Cảnh báo', delay: 4500, darkClose: true },
            info: { bg: 'text-bg-primary', icon: 'bi-info-circle-fill', title: 'Thông tin', delay: 4500 },
            success: { bg: 'text-bg-success', icon: 'bi-check-circle-fill', title: 'Thành công', delay: 4500 }
        };
        const cfg = STYLES[type] || STYLES.success;

        const toastEl = document.createElement('div');
        toastEl.className = `toast align-items-center ${cfg.bg} border-0 shadow-lg mb-2`;
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');
        toastEl.innerHTML = `
            <div class="d-flex">
                <div class="toast-body d-flex align-items-center py-2 px-3">
                    <i class="bi ${cfg.icon} fs-5 me-2 flex-shrink-0"></i>
                    <div>
                        <div class="fw-semibold">${escapeHtml(title || cfg.title)}</div>
                        <div class="small">${escapeHtml(message)}</div>
                    </div>
                </div>
                <button type="button" class="btn-close ${cfg.darkClose ? '' : 'btn-close-white'} me-2 m-auto" data-bs-dismiss="toast" aria-label="Đóng"></button>
            </div>
        `;

        toastContainer.appendChild(toastEl);
        new bootstrap.Toast(toastEl, { delay: cfg.delay }).show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    };

    function initAll() {
        loadNotifications();
        initServerToasts();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }
})();
