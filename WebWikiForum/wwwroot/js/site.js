/**
 * Global Confirmation Modal
 * Usage: showConfirmModal({ title: 'Delete', message: 'Sure?', onConfirm: () => { ... } });
 */
window.showConfirmModal = function (options) {
    const modal = document.getElementById('confirmModal');
    const overlay = document.getElementById('modalOverlay');
    const content = document.getElementById('modalContent');
    const title = document.getElementById('modal-title');
    const message = document.getElementById('modal-message');
    const confirmBtn = document.getElementById('modalConfirmBtn');
    const cancelBtn = document.getElementById('modalCancelBtn');

    if (!modal) return;

    // Set content
    title.textContent = options.title || 'Confirm Action';
    message.textContent = options.message || 'Are you sure you want to proceed?';
    confirmBtn.textContent = options.confirmText || 'Confirm';
    cancelBtn.textContent = options.cancelText || 'Cancel';

    // Show modal
    modal.classList.remove('hidden');

    // Animate in
    setTimeout(() => {
        overlay.classList.replace('opacity-0', 'opacity-100');
        content.classList.replace('opacity-0', 'opacity-100');
        content.classList.replace('translate-y-4', 'translate-y-0');
        content.classList.replace('sm:scale-95', 'sm:scale-100');
    }, 10);

    const closeModal = () => {
        overlay.classList.replace('opacity-100', 'opacity-0');
        content.classList.replace('opacity-100', 'opacity-0');
        content.classList.replace('translate-y-0', 'translate-y-4');
        content.classList.replace('sm:scale-100', 'sm:scale-95');

        setTimeout(() => {
            modal.classList.add('hidden');
            // Reset listeners
            confirmBtn.onclick = null;
            cancelBtn.onclick = null;
            overlay.onclick = null;
        }, 300);
    };

    confirmBtn.onclick = () => {
        if (options.onConfirm) options.onConfirm();
        closeModal();
    };

    cancelBtn.onclick = closeModal;
    overlay.onclick = closeModal;
};

// Auto-hide global toast after 5 seconds
document.addEventListener('DOMContentLoaded', () => {
    const toast = document.getElementById('global-toast');
    if (toast) {
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(120%)';
            setTimeout(() => {
                toast.style.display = 'none';
            }, 500);
        }, 5000);
    }

    // Mobile Dropdown Logic
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    const mobileDropdown = document.getElementById('mobileDropdown');
    const menuIcon = document.getElementById('menuIcon');

    if (mobileMenuBtn && mobileDropdown) {
        mobileMenuBtn.onclick = () => {
            const isHidden = mobileDropdown.classList.contains('hidden');
            
            if (isHidden) {
                mobileDropdown.classList.remove('hidden');
                menuIcon.innerText = 'close';
                setTimeout(() => {
                    mobileDropdown.style.maxHeight = mobileDropdown.scrollHeight + 'px';
                }, 10);
            } else {
                mobileDropdown.style.maxHeight = '0px';
                menuIcon.innerText = 'menu';
                setTimeout(() => {
                    mobileDropdown.classList.add('hidden');
                }, 300);
            }
        };

        document.addEventListener('click', (e) => {
            if (!mobileMenuBtn.contains(e.target) && !mobileDropdown.contains(e.target)) {
                if (!mobileDropdown.classList.contains('hidden')) {
                    mobileDropdown.style.maxHeight = '0px';
                    menuIcon.innerText = 'menu';
                    setTimeout(() => {
                        mobileDropdown.classList.add('hidden');
                    }, 300);
                }
            }
        });
    }
});
