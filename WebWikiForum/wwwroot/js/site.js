/**
 * Global Confirmation Modal
 * Usage: showConfirmModal({ title: 'Delete', message: 'Sure?', onConfirm: () => { ... } });
 */
window.showConfirmModal = function(options) {
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
