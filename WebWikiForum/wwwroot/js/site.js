/**
 * Modal xác nhận toàn cục
 * Cách dùng: showConfirmModal({ title: 'Xóa', message: 'Bạn có chắc không?', onConfirm: () => { ... } });
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

    // Đặt nội dung
    title.textContent = options.title || 'Confirm Action';
    message.textContent = options.message || 'Are you sure you want to proceed?';
    confirmBtn.textContent = options.confirmText || 'Confirm';
    cancelBtn.textContent = options.cancelText || 'Cancel';

    // Hiển thị modal
    modal.classList.remove('hidden');

    // Hiệu ứng hiện modal
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
            // Cập nhật trạng thái listener
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

// Tự ẩn toast toàn cục sau 5 giây
document.addEventListener('DOMContentLoaded', () => {
    const toast = document.getElementById('global-toast');
    if (toast) {
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(120%)';
            setTimeout(() => {
                // Ẩn toast
                toast.style.display = 'none';
            }, 500);
        }, 5000);
    }

    // Logic menu dropdown trên mobile
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
                        // Ẩn dropdown
                        mobileDropdown.classList.add('hidden');
                    }, 300);
                }
            }
        });
    }

    // Custom Select Initialization
    document.querySelectorAll("select.custom-select").forEach(select => {
        select.style.display = 'none';
        
        const wrapper = document.createElement('div');
        wrapper.className = 'relative inline-block w-full h-full';
        
        const btn = document.createElement('button');
        btn.type = 'button';
        const classes = select.className.replace('custom-select', '').trim();
        btn.className = classes + ' flex items-center justify-between text-left relative z-10';
        
        const selectedOpt = select.options[select.selectedIndex];
        btn.innerHTML = `<span class="truncate block pr-6">${selectedOpt ? selectedOpt.text : ''}</span> <span class="material-symbols-outlined absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none">expand_more</span>`;
        
        const menu = document.createElement('div');
        menu.className = 'absolute z-50 left-0 w-full mt-2 py-2 bg-white dark:bg-slate-800 border border-slate-200/50 dark:border-slate-700/50 rounded-xl shadow-xl overflow-hidden hidden flex-col transition-all origin-top max-h-[300px] overflow-y-auto';
        
        Array.from(select.options).forEach(opt => {
            const item = document.createElement('div');
            item.className = 'w-full text-left px-4 py-2.5 text-sm text-slate-700 dark:text-slate-200 hover:bg-primary/10 hover:text-primary transition-colors cursor-pointer ' + (opt.selected ? 'bg-primary/5 text-primary font-bold' : '');
            item.textContent = opt.text;
            item.onclick = () => {
                select.value = opt.value;
                btn.querySelector('span.truncate').textContent = opt.text;
                menu.classList.add('hidden');
                Array.from(menu.children).forEach(c => c.className = 'w-full text-left px-4 py-2.5 text-sm text-slate-700 dark:text-slate-200 hover:bg-primary/10 hover:text-primary transition-colors cursor-pointer');
                item.className = 'w-full text-left px-4 py-2.5 text-sm text-slate-700 dark:text-slate-200 hover:bg-primary/10 hover:text-primary transition-colors cursor-pointer bg-primary/5 text-primary font-bold';
                
                const event = new Event('change', { bubbles: true });
                select.dispatchEvent(event);
            };
            menu.appendChild(item);
        });
        
        btn.onclick = (e) => {
            e.stopPropagation();
            document.querySelectorAll('.custom-select-menu').forEach(m => {
                if (m !== menu) m.classList.add('hidden');
            });
            menu.classList.toggle('hidden');
        };
        
        menu.classList.add('custom-select-menu');
        wrapper.appendChild(btn);
        wrapper.appendChild(menu);
        
        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(select);
    });
    
    document.addEventListener('click', () => {
        document.querySelectorAll('.custom-select-menu').forEach(m => m.classList.add('hidden'));
    });
});
