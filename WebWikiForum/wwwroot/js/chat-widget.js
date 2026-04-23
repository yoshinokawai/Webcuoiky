/* =====================================================
   VTWiki Chat Widget — JavaScript
   ===================================================== */
(function () {
    'use strict';

    // ── State
    let isOpen = false;
    let isLoading = false;
    let firstOpen = true;
    let userAvatarUrl = '';
    let userName = '';
    const STORAGE_KEY = 'vtwiki_chat_history';
    const MAX_HISTORY = 30;

    // ── Session ID (persisted in localStorage per browser)
    const SESSION_KEY = 'vtwiki_chat_session';
    let chatSessionId = localStorage.getItem(SESSION_KEY) || '';
    let lastPolledId = 0;    // ID của tin nhắn admin cuối cùng đã nhận
    let pollTimer = null;

    // ── DOM references (populated after DOMContentLoaded)
    let toggleBtn, panel, messagesEl, input, sendBtn, suggestionsEl, unreadBadge;

    const currentLang = (document.documentElement.lang || 'en').substring(0, 2).toLowerCase();
    const translations = {
        vi: {
            title: 'Trợ lý Yoshi',
            welcome: '👋 Chào mừng bạn đến với **VTWiki**! Mình là **Yoshi** ✨\n\nTôi có thể giúp bạn:\n• Tìm hiểu về VTubers và các agency\n• Hướng dẫn sử dụng VTWiki\n• Giải thích thuật ngữ cộng đồng VTuber\n\nBạn muốn hỏi gì? 😊',
            placeholder: 'Hỏi Yoshi về VTubers, wiki, ...',
            error: 'Lỗi kết nối. Vui lòng thử lại.',
            busy: 'Đang tìm kiếm...',
            suggestions: [
                '🌟 VTuber là gì?',
                '🏢 Agency là gì?',
                '📺 Hololive là gì?',
                '🗺️ Cách dùng VTWiki?',
                '🎤 Tin tức mới nhất?'
            ]
        },
        en: {
            title: 'Yoshi Assistant',
            welcome: '👋 Welcome to **VTWiki**! I am **Yoshi** ✨\n\nI can help you with:\n• Finding VTubers and Agencies\n• How to use VTWiki\n• Explaining VTuber community terms\n\nWhat would you like to know? 😊',
            placeholder: 'Ask Yoshi about VTubers, wiki, ...',
            error: 'Connection error. Please try again.',
            busy: 'Searching...',
            suggestions: [
                '🌟 What is a VTuber?',
                '🏢 What is an Agency?',
                '📺 About Hololive?',
                '🗺️ How to use VTWiki?',
                '🎤 Latest news?'
            ]
        }
    };
    const t = translations[currentLang] || translations.en;

    // ── Antiforgery token — not needed for this JSON API endpoint

    // ── Simple markdown-like renderer (bold, italic, bullet, code, links)
    function renderMarkdown(text) {
        if (!text) return '';
        // First escape all, but we will allow <a> tags later
        return text
            .replace(/&/g, '&amp;')
            .replace(/<(?!\/?a(?=>|\s.*>))/g, '&lt;') // Escape all < except <a> tags
            .replace(/(?<!<a.*)>(?<!<\/a>)/g, '&gt;') // Escape all > except in <a> tags
            // Code blocks / inline code
            .replace(/```[\s\S]*?```/g, m => `<code>${m.slice(3, -3).trim()}</code>`)
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            // Bold & Italic
            .replace(/\*\*\*(.+?)\*\*\*/g, '<strong><em>$1</em></strong>')
            .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.+?)\*/g, '<em>$1</em>')
            // Bullet lists
            .replace(/^[\*\-] (.+)$/gm, '<li>$1</li>')
            .replace(/(<li>.*<\/li>)/s, '<ul>$1</ul>')
            // Numbered lists
            .replace(/^\d+\. (.+)$/gm, '<li>$1</li>')
            // Paragraphs (double newline)
            .replace(/\n{2,}/g, '</p><p>')
            // Single newline → <br>
            .replace(/\n/g, '<br>')
            // Wrap in paragraph
            .replace(/^(.*)$/, '<p>$1</p>')
            // Clean empty paragraphs
            .replace(/<p><\/p>/g, '');
    }

    // ── Append a message bubble
    function appendMessage(role, text) {
        const isAI = role === 'ai';
        const wrapper = document.createElement('div');
        wrapper.className = `chat-msg ${role}`;

        const avatar = document.createElement('div');
        avatar.className = 'chat-msg-avatar';
        if (isAI) {
            avatar.innerHTML = '<img src="/images/chat-avatar.png" style="width:100%;height:100%;object-fit:cover;border-radius:999px;" alt="AI" />';
            avatar.style.padding = '0';
            avatar.style.overflow = 'hidden';
        } else if (userAvatarUrl) {
            // Đăng nhập, có avatar thực
            avatar.innerHTML = `<img src="${userAvatarUrl}" style="width:100%;height:100%;object-fit:cover;border-radius:999px;" alt="${userName}" onerror="this.parentElement.innerHTML='${(userName||'U')[0].toUpperCase()}';this.parentElement.style.paddingTop='0';" />`;
            avatar.style.padding = '0';
            avatar.style.overflow = 'hidden';
        } else if (userName) {
            // Đăng nhập nhưng chưa có avatar — hiển thị chữ cái
            avatar.textContent = (userName[0] || 'U').toUpperCase();
            avatar.style.background = 'linear-gradient(135deg, #64748b, #475569)';
            avatar.style.fontSize = '13px';
            avatar.style.fontWeight = '800';
            avatar.style.color = '#fff';
        } else {
            avatar.textContent = '👤';
        }

        const bubble = document.createElement('div');
        bubble.className = 'chat-bubble';
        if (isAI) {
            bubble.innerHTML = renderMarkdown(text);
        } else {
            bubble.textContent = text;
        }

        wrapper.appendChild(avatar);
        wrapper.appendChild(bubble);
        messagesEl.appendChild(wrapper);
        scrollToBottom();
        return wrapper;
    }

    // ── Typing indicator
    function showTyping() {
        const wrapper = document.createElement('div');
        wrapper.className = 'chat-msg ai';
        wrapper.id = 'chat-typing-indicator';

        const avatar = document.createElement('div');
        avatar.className = 'chat-msg-avatar';
        avatar.textContent = '✨';

        const bubble = document.createElement('div');
        bubble.className = 'chat-bubble';
        bubble.innerHTML = '<div class="chat-typing"><span></span><span></span><span></span></div>';

        wrapper.appendChild(avatar);
        wrapper.appendChild(bubble);
        messagesEl.appendChild(wrapper);
        scrollToBottom();
    }

    function hideTyping() {
        document.getElementById('chat-typing-indicator')?.remove();
    }

    // ── Scroll to bottom
    function scrollToBottom() {
        messagesEl.scrollTop = messagesEl.scrollHeight;
    }

    // ── Show suggestions
    function showSuggestions(show) {
        if (!suggestionsEl) return;
        suggestionsEl.style.display = show ? 'flex' : 'none';
    }

    // ── Send message
    async function sendMessage(text) {
        const msg = (text || input.value).trim();
        if (!msg || isLoading) return;

        input.value = '';
        input.style.height = 'auto';
        sendBtn.disabled = true;
        isLoading = true;
        showSuggestions(false);

        appendMessage('user', msg);
        showTyping();

        try {
            const res = await fetch('/Chat/Ask', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ 
                    message: msg,
                    lang: currentLang,
                    sessionId: chatSessionId || null
                })
            });

            hideTyping();
            const data = await res.json();

            // Lưu sessionId do server trả về (lần đầu sẽ được tạo mới)
            if (data.sessionId && !chatSessionId) {
                chatSessionId = data.sessionId;
                localStorage.setItem(SESSION_KEY, chatSessionId);
                startPolling(); // Bắt đầu polling sau lần chat đầu tiên
            }

            // Nhận diện 'response' từ Backend và hiển thị
            if (res.ok && data.response) {
                appendMessage('ai', data.response);
            } else {
                appendMessage('ai', `⚠️ ${data.error || t.error}`);
            }
        } catch {
            hideTyping();
            appendMessage('ai', '⚠️ Không thể kết nối. Kiểm tra lại mạng và thử lại.');
        } finally {
            isLoading = false;
            sendBtn.disabled = false;
            input.focus();
        }
    }

    // ── Admin reply polling (mỗi 5 giây)
    function startPolling() {
        if (pollTimer) return;
        pollTimer = setInterval(async () => {
            try {
                // Nếu chưa có sessionId thì chưa poll (hoặc poll theo user nếu đã load avatar/info)
                const url = `/Chat/Poll?sessionId=${encodeURIComponent(chatSessionId || '')}&after=${lastPolledId}`;
                const res = await fetch(url);
                if (!res.ok) return;
                const data = await res.json();
                if (data.messages && data.messages.length > 0) {
                    data.messages.forEach(m => {
                        lastPolledId = Math.max(lastPolledId, m.id);
                        appendAdminMessage(m.username, m.message);
                    });
                    // Hiện badge nếu panel đang đóng
                    if (!isOpen) {
                        unreadBadge.textContent = '!';
                        unreadBadge.classList.add('show');
                    }
                }
            } catch { /* ignore network errors */ }
        }, 5000);
    }

    // ── Append admin reply bubble
    function appendAdminMessage(adminName, text) {
        const wrapper = document.createElement('div');
        wrapper.className = 'chat-msg ai'; // same side as bot
        wrapper.style.cssText = '--bubble-bg: #fef3c7; --bubble-color: #92400e;';

        const avatar = document.createElement('div');
        avatar.className = 'chat-msg-avatar';
        avatar.textContent = '👑';
        avatar.style.cssText = 'background:linear-gradient(135deg,#f59e0b,#d97706);color:#fff;font-size:14px;';

        const bubble = document.createElement('div');
        bubble.className = 'chat-bubble';
        bubble.style.cssText = 'background:#fef3c7;color:#78350f;border:1px solid #fcd34d;';

        const label = document.createElement('div');
        label.style.cssText = 'font-size:10px;font-weight:800;color:#d97706;margin-bottom:4px;';
        label.textContent = adminName || '👑 Admin';

        bubble.appendChild(label);
        const contentDiv = document.createElement('div');
        contentDiv.innerHTML = renderMarkdown(text);
        bubble.appendChild(contentDiv);
        wrapper.appendChild(avatar);
        wrapper.appendChild(bubble);
        messagesEl.appendChild(wrapper);
        scrollToBottom();
    }

    // ── Load history from server
    async function loadHistory() {
        try {
            const res = await fetch(`/Chat/History?sessionId=${encodeURIComponent(chatSessionId)}`);
            if (!res.ok) return;
            const data = await res.json();
            if (data.messages && data.messages.length > 0) {
                messagesEl.innerHTML = ''; // Clear welcome or old state
                data.messages.forEach(m => {
                    if (m.role === 'admin') {
                        appendAdminMessage(m.username, m.message);
                        lastPolledId = Math.max(lastPolledId, m.id);
                    } else {
                        // role 'user', 'bot' (ai)
                        appendMessage(m.role === 'user' ? 'user' : 'ai', m.message);
                    }
                });
                scrollToBottom();
                firstOpen = false;
            } else {
                showWelcome();
            }
        } catch {
            showWelcome();
        }
    }

    // ── Welcome greeting
    function showWelcome() {
        if (messagesEl && messagesEl.children.length > 0) return;
        appendMessage('ai', t.welcome);
    }

    // ── Toggle panel
    function togglePanel() {
        isOpen = !isOpen;
        toggleBtn.classList.toggle('open', isOpen);
        panel.classList.toggle('open', isOpen);

        if (isOpen) {
            // Hide unread badge
            unreadBadge.classList.remove('show');

            if (firstOpen) {
                // Chỉ hiện welcome nếu sau khi load history (nếu có) mà vẫn trống
                if (messagesEl.children.length === 0) {
                    showWelcome();
                }
                firstOpen = false;
            }

            setTimeout(() => input.focus(), 300);
        }
    }

    // ── Reset chat (Start a new session without deleting DB history)
    async function resetChat() {
        const title = currentLang === 'vi' ? 'Làm mới đoạn chat' : 'Reset chat';
        const msg = currentLang === 'vi' ? 'Bạn có muốn bắt đầu một cuộc trò chuyện mới không? (Lịch sử cũ vẫn được lưu lại)' : 'Do you want to start a new chat? (Previous history will be saved)';
        const confirmTxt = currentLang === 'vi' ? 'Làm mới' : 'Reset';
        const cancelTxt = currentLang === 'vi' ? 'Hủy' : 'Cancel';

        const performReset = () => {
            messagesEl.innerHTML = '';
            chatSessionId = '';
            localStorage.removeItem(SESSION_KEY);
            lastPolledId = 0;
            showWelcome();
            showSuggestions(true);
        };

        if (window.showConfirmModal) {
            window.showConfirmModal({
                title: title,
                message: msg,
                confirmText: confirmTxt,
                cancelText: cancelTxt,
                onConfirm: performReset
            });
        } else {
            if (confirm(msg)) {
                performReset();
            }
        }
    }


    // ── Build the widget HTML dynamically to avoid Razor conflicts
    function buildWidget() {
        // ... (toggleBtn part remains same)
        toggleBtn = document.createElement('button');
        toggleBtn.id = 'chat-toggle-btn';
        toggleBtn.setAttribute('aria-label', 'Toggle Yoshi chat');
        toggleBtn.innerHTML = `
            <img src="/images/chat-avatar.png" class="chat-icon" style="width:42px;height:42px;border-radius:999px;object-fit:cover;border:2px solid rgba(255,255,255,0.4);" alt="Yoshi AI" />
            <span class="material-symbols-outlined close-icon">close</span>
            <span id="chat-unread-badge">1</span>
        `;

        // Panel
        panel = document.createElement('div');
        panel.id = 'chat-panel';
        panel.setAttribute('role', 'dialog');
        panel.setAttribute('aria-label', 'Yoshi AI Chat');
        panel.innerHTML = `
            <div id="chat-header">
                <div id="chat-header-avatar" style="padding:0;overflow:hidden;"><img src="/images/chat-avatar.png" style="width:100%;height:100%;object-fit:cover;border-radius:999px;" alt="Yoshi AI" /></div>
                <div id="chat-header-info">
                    <div id="chat-header-name">${t.title}</div>
                    <div id="chat-header-status">${currentLang === 'vi' ? 'Trực tuyến' : 'Online'}</div>
                </div>
                <button id="chat-reset-btn" title="${currentLang === 'vi' ? 'Làm mới cuộc trò chuyện' : 'Reset chat'}">
                    <span class="material-symbols-outlined" style="font-size:14px;">restart_alt</span>
                    ${currentLang === 'vi' ? 'Làm mới' : 'Reset'}
                </button>
            </div>
            <div id="chat-messages"></div>
            <div id="chat-suggestions"></div>
            <div id="chat-input-area">
                <textarea
                    id="chat-input"
                    placeholder="${t.placeholder}"
                    rows="1"
                    maxlength="500"
                    autocomplete="off"
                ></textarea>
                <button id="chat-send-btn" title="${currentLang === 'vi' ? 'Gửi' : 'Send'}">
                    <span class="material-symbols-outlined">send</span>
                </button>
            </div>
        `;

        document.body.appendChild(toggleBtn);
        document.body.appendChild(panel);

        // Resolve element refs
        messagesEl    = panel.querySelector('#chat-messages');
        input         = panel.querySelector('#chat-input');
        sendBtn       = panel.querySelector('#chat-send-btn');
        suggestionsEl = panel.querySelector('#chat-suggestions');
        unreadBadge   = toggleBtn.querySelector('#chat-unread-badge');

        // Build suggestion chips
        t.suggestions.forEach(s => {
            const chip = document.createElement('button');
            chip.className = 'chat-chip';
            chip.textContent = s;
            chip.type = 'button';
            // Extract the text part (after emoji)
            const query = s.replace(/^[^\s]+\s/, '');
            chip.addEventListener('click', () => sendMessage(query));
            suggestionsEl.appendChild(chip);
        });

        // Show unread badge after 3s to draw attention
        setTimeout(() => unreadBadge.classList.add('show'), 3000);

        // ── Event listeners
        toggleBtn.addEventListener('click', togglePanel);

        document.getElementById('chat-reset-btn').addEventListener('click', (e) => {
            e.stopPropagation();
            resetChat();
        });

        sendBtn.addEventListener('click', () => sendMessage());

        input.addEventListener('keydown', e => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        // Auto-resize textarea
        input.addEventListener('input', () => {
            input.style.height = 'auto';
            input.style.height = Math.min(input.scrollHeight, 100) + 'px';
        });

        // Close panel on Escape
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape' && isOpen) togglePanel();
        });

        // Close when clicking outside
        document.addEventListener('click', e => {
            if (isOpen && !panel.contains(e.target) && !toggleBtn.contains(e.target)) {
                togglePanel();
            }
        });
    }

    // ── Load user avatar on init
    async function loadUserAvatar() {
        try {
            const res = await fetch('/Chat/GetUserAvatar');
            if (res.ok) {
                const data = await res.json();
                userAvatarUrl = data.avatarUrl || '';
                userName = data.username || '';
            }
        } catch { /* Không ăng nhập hoặc lỗi mạng — dùng fallback */ }
    }

    // ── Init
    async function init() {
        await loadUserAvatar();
        buildWidget();
        
        // Luôn thử tải lịch sử nếu có sessionId hoặc user đã login
        if (chatSessionId) {
            await loadHistory();
            startPolling();
        } else {
            showWelcome();
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
