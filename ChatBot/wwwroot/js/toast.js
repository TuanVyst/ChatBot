function showToast(message, type) {
    type = type || 'info';
    var container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    var icons = {
        success: 'ph-check-circle',
        error: 'ph-x-circle',
        warning: 'ph-warning-circle',
        info: 'ph-bell'
    };

    var toast = document.createElement('div');
    toast.className = 'toast toast-' + type;
    toast.innerHTML =
        '<i class="ph ' + (icons[type] || icons.info) + '"></i>' +
        '<span>' + message + '</span>' +
        '<button class="toast-close" onclick="dismissToast(this.parentElement)">&times;</button>';

    container.appendChild(toast);

    setTimeout(function () {
        dismissToast(toast);
    }, 5000);
}

function dismissToast(toast) {
    if (!toast) return;
    toast.classList.add('toast-out');
    setTimeout(function () {
        if (toast.parentElement) {
            toast.parentElement.removeChild(toast);
        }
    }, 300);
}

function showConfirm(message, options) {
    options = options || {};
    var title = options.title || 'Xác nhận';
    var confirmText = options.confirmText || 'Xóa';
    var cancelText = options.cancelText || 'Hủy';
    var type = options.type || 'danger';

    return new Promise(function (resolve) {
        var overlay = document.createElement('div');
        overlay.className = 'confirm-overlay';

        var okClass = 'confirm-btn confirm-btn-ok' + (type === 'success' ? ' btn-success' : '');

        overlay.innerHTML =
            '<div class="confirm-modal">' +
            '  <div class="confirm-icon"><i class="ph ph-warning"></i></div>' +
            '  <p class="confirm-title">' + title + '</p>' +
            '  <p class="confirm-message">' + message + '</p>' +
            '  <div class="confirm-actions">' +
            '    <button class="confirm-btn confirm-btn-cancel">' + cancelText + '</button>' +
            '    <button class="' + okClass + '">' + confirmText + '</button>' +
            '  </div>' +
            '</div>';

        var btnCancel = overlay.querySelector('.confirm-btn-cancel');
        var btnOk = overlay.querySelector('.confirm-btn-ok');

        function close(result) {
            overlay.style.animation = 'confirmFadeIn 0.15s ease reverse forwards';
            setTimeout(function () {
                if (overlay.parentElement) overlay.parentElement.removeChild(overlay);
            }, 150);
            resolve(result);
        }

        btnCancel.addEventListener('click', function () { close(false); });
        btnOk.addEventListener('click', function () { close(true); });
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) close(false);
        });

        document.body.appendChild(overlay);
    });
}

function confirmDelete(form, message) {
    showConfirm(message || 'Bạn có chắc chắn muốn xóa?').then(function (confirmed) {
        if (confirmed) form.submit();
    });
    return false;
}