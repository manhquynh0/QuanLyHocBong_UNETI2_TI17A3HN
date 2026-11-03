/**
 * UNETI Realtime Notification Client (SignalR + WebSocket)
 * Tự động đồng bộ số thông báo chưa đọc, hiển thị toast và cập nhật dropdown theo thời gian thực.
 */
(function () {
    'use strict';

    const badgeEl = document.getElementById('notificationBadge');
    const listEl = document.getElementById('notificationList');
    const markAllBtn = document.getElementById('btnMarkAllRead');
    const toastContainer = document.getElementById('notificationToastContainer');

    // Chỉ khởi tạo nếu người dùng đã đăng nhập (dropdown tồn tại trên layout)
    if (!badgeEl || !listEl) return;

    let unreadCount = 0;

    // 1. CẬP NHẬT BADGE HIỂN THỊ
    function updateBadge(count) {
        unreadCount = Math.max(0, count);
        if (unreadCount > 0) {
            badgeEl.textContent = unreadCount > 99 ? '99+' : unreadCount;
            badgeEl.classList.remove('d-none');
        } else {
            badgeEl.textContent = '0';
            badgeEl.classList.add('d-none');
        }
    }

    // 2. TẠO HTML ELEMENT CHO TỪNG BẢN GHI THÔNG BÁO (Chống XSS: dùng textContent)
    function createNotificationItem(item) {
        const itemLink = document.createElement('a');
        itemLink.href = item.lienKet || 'javascript:void(0);';
        itemLink.className = `dropdown-item p-3 border-bottom text-wrap d-flex align-items-start gap-2 ${item.daDoc ? 'opacity-75' : 'bg-light-subtle'}`;
        itemLink.dataset.id = item.maThongBao;
        itemLink.style.transition = 'background-color 0.15s ease';

        // Icon theo loại thông báo
        let iconClass = 'bi-bell-fill text-primary';
        if (item.loaiThongBao === 'LichPhongVan') iconClass = 'bi-calendar-event-fill text-warning';
        else if (item.loaiThongBao === 'KetQua') iconClass = 'bi-award-fill text-success';
        else if (item.loaiThongBao === 'HoSo') iconClass = 'bi-folder-check text-info';

        const iconCol = document.createElement('div');
        iconCol.className = 'mt-1 flex-shrink-0';
        iconCol.innerHTML = `<i class="bi ${iconClass} fs-5"></i>`;

        const contentCol = document.createElement('div');
        contentCol.className = 'flex-grow-1';

        const titleRow = document.createElement('div');
        titleRow.className = 'd-flex align-items-center justify-content-between mb-1';

        const titleEl = document.createElement('strong');
        titleEl.className = 'fs-6 text-dark';
        titleEl.textContent = item.tieuDe;

        const timeEl = document.createElement('small');
        timeEl.className = 'text-muted ms-2';
        timeEl.textContent = item.thoiGianTuongDoi || item.ngayTao || '';

        titleRow.appendChild(titleEl);
        titleRow.appendChild(timeEl);

        const bodyEl = document.createElement('p');
        bodyEl.className = 'm-0 text-muted small lh-sm';
        bodyEl.textContent = item.noiDung;

        contentCol.appendChild(titleRow);
        contentCol.appendChild(bodyEl);

        if (!item.daDoc) {
            const dotCol = document.createElement('div');
            dotCol.className = 'ms-1 mt-2';
            dotCol.innerHTML = '<span class="badge bg-primary p-1 rounded-circle" style="display:inline-block; width:8px; height:8px;" title="Chưa đọc"></span>';
            itemLink.appendChild(iconCol);
            itemLink.appendChild(contentCol);
            itemLink.appendChild(dotCol);
        } else {
            itemLink.appendChild(iconCol);
            itemLink.appendChild(contentCol);
        }

        // Click để đánh dấu đã đọc
        itemLink.addEventListener('click', function (e) {
            if (!item.daDoc) {
                markAsRead(item.maThongBao);
                item.daDoc = true;
                itemLink.classList.remove('bg-light-subtle');
                itemLink.classList.add('opacity-75');
                const dot = itemLink.querySelector('.badge.bg-primary');
                if (dot) dot.remove();
            }
        });

        return itemLink;
    }

    // 3. TẢI DANH SÁCH THÔNG BÁO BAN ĐẦU
    function loadNotifications() {
        fetch('/ThongBao/GetMyNotifications?top=15')
            .then(res => res.json())
            .then(res => {
                if (res.success) {
                    updateBadge(res.unreadCount);
                    listEl.innerHTML = '';

                    if (!res.data || res.data.length === 0) {
                        listEl.innerHTML = '<div class="p-4 text-center text-muted"><i class="bi bi-inbox fs-3 d-block mb-1"></i>Bạn không có thông báo nào.</div>';
                        return;
                    }

                    res.data.forEach(item => {
                        listEl.appendChild(createNotificationItem(item));
                    });
                }
            })
            .catch(err => {
                listEl.innerHTML = '<div class="p-3 text-center text-danger small">Không thể tải thông báo.</div>';
                console.warn('Lỗi tải thông báo:', err);
            });
    }

    // 4. ĐÁNH DẤU 1 THÔNG BÁO LÀ ĐÃ ĐỌC
    function markAsRead(id) {
        fetch(`/ThongBao/DanhDauDaDoc/${id}`, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(res => res.json())
            .then(res => {
                if (res.success) {
                    updateBadge(res.unreadCount);
                }
            })
            .catch(err => console.warn('Lỗi đánh dấu đã đọc:', err));
    }

    // 5. ĐÁNH DẤU TẤT CẢ ĐÃ ĐỌC
    if (markAllBtn) {
        markAllBtn.addEventListener('click', function (e) {
            e.preventDefault();
            fetch('/ThongBao/DanhDauTatCaDaDoc', {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
                .then(res => res.json())
                .then(res => {
                    if (res.success) {
                        updateBadge(0);
                        // Cập nhật giao diện tất cả items sang đã đọc
                        const dots = listEl.querySelectorAll('.badge.bg-primary');
                        dots.forEach(d => d.remove());
                        const items = listEl.querySelectorAll('.dropdown-item');
                        items.forEach(it => {
                            it.classList.remove('bg-light-subtle');
                            it.classList.add('opacity-75');
                        });
                    }
                })
                .catch(err => console.warn('Lỗi đánh dấu tất cả đã đọc:', err));
        });
    }

    // 6. HIỂN THỊ TOAST THÔNG BÁO POPUP
    function showNotificationToast(item) {
        if (!toastContainer) return;

        const toastId = 'toast_' + Date.now();
        const toastEl = document.createElement('div');
        toastEl.className = 'toast align-items-center text-bg-light border shadow-lg';
        toastEl.id = toastId;
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');

        const toastHeader = document.createElement('div');
        toastHeader.className = 'toast-header bg-primary text-white';
        toastHeader.innerHTML = `
            <i class="bi bi-bell-fill me-2"></i>
            <strong class="me-auto">${escapeHtml(item.tieuDe)}</strong>
            <small class="text-white-50">Vừa xong</small>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
        `;

        const toastBody = document.createElement('div');
        toastBody.className = 'toast-body text-dark';
        toastBody.textContent = item.noiDung;

        if (item.lienKet) {
            const linkBtn = document.createElement('a');
            linkBtn.href = item.lienKet;
            linkBtn.className = 'btn btn-sm btn-outline-primary mt-2 d-block text-start';
            linkBtn.innerHTML = '<i class="bi bi-arrow-right-circle me-1"></i>Xem chi tiết';
            toastBody.appendChild(linkBtn);
        }

        toastEl.appendChild(toastHeader);
        toastEl.appendChild(toastBody);
        toastContainer.appendChild(toastEl);

        const bsToast = new bootstrap.Toast(toastEl, { delay: 6000 });
        bsToast.show();

        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }

    function escapeHtml(str) {
        if (!str) return '';
        const p = document.createElement('p');
        p.textContent = str;
        return p.innerHTML;
    }

    // 7. KHỞI TẠO SIGNALR HUB CONNECTION
    if (typeof signalR !== 'undefined') {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notification')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.on('NhanThongBao', function (data) {
            // Cập nhật số chưa đọc
            if (data.unreadCount !== undefined) {
                updateBadge(data.unreadCount);
            } else {
                updateBadge(unreadCount + 1);
            }

            // Xóa thông báo trống nếu có
            const emptyEl = listEl.querySelector('.text-center');
            if (emptyEl && emptyEl.textContent.includes('không có')) {
                listEl.innerHTML = '';
            }

            // Chèn bản ghi mới lên đầu danh sách
            const newItemEl = createNotificationItem(data);
            listEl.insertBefore(newItemEl, listEl.firstChild);

            // Bật hiệu ứng rung nhẹ chuông
            const btn = document.getElementById('notificationDropdownBtn');
            if (btn) {
                btn.classList.add('animate__animated', 'animate__swing');
                setTimeout(() => btn.classList.remove('animate__animated', 'animate__swing'), 1000);
            }

            // Hiện popup Toast
            showNotificationToast(data);
        });

        connection.start()
            .then(() => {
                // Kết nối thành công
            })
            .catch(err => {
                console.warn('SignalR Hub kết nối thất bại:', err);
            });
    }

    // 8. TẢI DỮ LIỆU BAN ĐẦU KHI TRANG SẴN SÀNG
    document.addEventListener('DOMContentLoaded', loadNotifications);
    // Hoặc tải ngay nếu DOM đã sẵn sàng
    if (document.readyState === 'interactive' || document.readyState === 'complete') {
        loadNotifications();
    }
})();

