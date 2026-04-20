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

    // ── DOM references (populated after DOMContentLoaded)
    let toggleBtn, panel, messagesEl, input, sendBtn, suggestionsEl, unreadBadge;

    const currentLang = document.documentElement.lang || 'en';
    const translations = {
        vi: {
            title: 'Trợ lý VTWiki',
            welcome: '👋 Chào mừng bạn đến với **VTWiki**!\n\nTôi có thể giúp bạn:\n• Tìm hiểu về VTubers và các agency\n• Hướng dẫn sử dụng VTWiki\n• Giải thích thuật ngữ cộng đồng VTuber\n\nBạn muốn hỏi gì? 😊',
            placeholder: 'Hỏi về VTubers, wiki, ...',
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
            title: 'VTWiki Assistant',
            welcome: '👋 Welcome to **VTWiki**!\n\nI can help you with:\n• Finding VTubers and Agencies\n• How to use VTWiki\n• Explaining VTuber community terms\n\nWhat would you like to know? 😊',
            placeholder: 'Ask about VTubers, wiki, ...',
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
                body: JSON.stringify({ message: msg })
            });

            hideTyping();
            const data = await res.json();

            if (res.ok && data.reply) {
                appendMessage('ai', data.reply);
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

    // ── Welcome greeting
    function showWelcome() {
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
                showWelcome();
                firstOpen = false;
            }

            setTimeout(() => input.focus(), 300);
        }
    }

    // ── Clear history
    function clearHistory() {
        messagesEl.innerHTML = '';
        showWelcome();
        showSuggestions(true);
    }


    // ── Build the widget HTML dynamically to avoid Razor conflicts
    function buildWidget() {
        // Toggle button
        toggleBtn = document.createElement('button');
        toggleBtn.id = 'chat-toggle-btn';
        toggleBtn.setAttribute('aria-label', 'Toggle AI chat');
        toggleBtn.innerHTML = `
            <img src="/images/chat-avatar.png" class="chat-icon" style="width:42px;height:42px;border-radius:999px;object-fit:cover;border:2px solid rgba(255,255,255,0.4);" alt="VTWiki AI" />
            <span class="material-symbols-outlined close-icon">close</span>
            <span id="chat-unread-badge">1</span>
        `;

        // Panel
        panel = document.createElement('div');
        panel.id = 'chat-panel';
        panel.setAttribute('role', 'dialog');
        panel.setAttribute('aria-label', 'VTWiki AI Chat');
        panel.innerHTML = `
            <div id="chat-header">
                <div id="chat-header-avatar" style="padding:0;overflow:hidden;"><img src="/images/chat-avatar.png" style="width:100%;height:100%;object-fit:cover;border-radius:999px;" alt="VTWiki AI" /></div>
                <div id="chat-header-info">
                    <div id="chat-header-name">${t.title}</div>
                    <div id="chat-header-status">${currentLang === 'vi' ? 'Trực tuyến' : 'Online'}</div>
                </div>
                <button id="chat-clear-btn" title="${currentLang === 'vi' ? 'Xóa lịch sử chat' : 'Clear history'}">
                    <span class="material-symbols-outlined" style="font-size:14px;">refresh</span>
                    ${currentLang === 'vi' ? 'Làm mới' : 'Refresh'}
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

        document.getElementById('chat-clear-btn').addEventListener('click', (e) => {
            e.stopPropagation();
            clearHistory();
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
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
