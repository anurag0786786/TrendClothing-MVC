// ================================================================
//  TrendClothing — Global Toast Notification System
// ================================================================

(function () {

    // Create container once
    function getContainer() {
        var c = document.getElementById('tc-toast-container');
        if (!c) {
            c = document.createElement('div');
            c.id = 'tc-toast-container';
            document.body.appendChild(c);
        }
        return c;
    }

    // Icon map per type
    var icons = {
        success : '✅',
        error   : '❌',
        warning : '⚠️',
        info    : 'ℹ️',
        cart    : '🛒'
    };

    // Title map per type
    var titles = {
        success : 'Success',
        error   : 'Oops!',
        warning : 'Warning',
        info    : 'Info',
        cart    : 'Cart'
    };

    /**
     * showToast(message, type, duration)
     *  type     : 'success' | 'error' | 'warning' | 'info' | 'cart'
     *  duration : ms (default 4000)
     */
    window.showToast = function (message, type, duration) {
        if (!message) return;
        type     = type     || 'info';
        duration = duration || 4000;

        var container = getContainer();

        var item = document.createElement('div');
        item.className = 'tc-toast-item tc-toast--' + type;

        item.innerHTML =
            '<span class="tc-toast-icon">' + (icons[type] || 'ℹ️') + '</span>' +
            '<div class="tc-toast-text">' +
                '<div class="tc-toast-title">' + (titles[type] || 'Notice') + '</div>' +
                '<div class="tc-toast-msg">'   + message + '</div>' +
            '</div>' +
            '<span class="tc-toast-close">✕</span>';

        // Reset progress bar animation to match duration
        item.style.setProperty('--tc-toast-dur', duration + 'ms');

        container.appendChild(item);

        // Animate in (next tick)
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                item.classList.add('tc-toast--show');
            });
        });

        // Auto dismiss
        var timer = setTimeout(function () { dismiss(item); }, duration);

        // Manual close
        item.querySelector('.tc-toast-close').addEventListener('click', function () {
            clearTimeout(timer);
            dismiss(item);
        });

        // Click anywhere on toast to dismiss
        item.addEventListener('click', function (e) {
            if (e.target.classList.contains('tc-toast-close')) return;
            clearTimeout(timer);
            dismiss(item);
        });
    };

    function dismiss(item) {
        item.classList.remove('tc-toast--show');
        item.classList.add('tc-toast--hide');
        item.addEventListener('transitionend', function () {
            if (item.parentNode) item.parentNode.removeChild(item);
        }, { once: true });
    }

})();
