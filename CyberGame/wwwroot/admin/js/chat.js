/**
 * CYBERGAME ADMIN - MESSENGER CHAT CONTROLLER (Database-Backed & User-Isolated)
 * Features:
 * 1. Facebook Messenger Dark Mode Experience
 * 2. Real-time Customer & Bot Handover Inbox (Isolated per User Session)
 * 3. Bidirectional Synchronization via REST API & Live Polling
 * 4. Quick Admin Replies & Customer Info Drawer
 */

// ==========================================================================
// 1. DATA STATE & API COMMUNICATION
// ==========================================================================
let cachedConversations = [];
let activeConversationId = null;
let currentFilter = 'all'; // all | waiting | unread | resolved
let searchQuery = '';

// Clear legacy fake data from localStorage
try {
  localStorage.removeItem('cybergame_chat_conversations_v1');
  localStorage.removeItem('cybergame_chat_conversations_v2');
  localStorage.removeItem('cybergame_chat_conversations_v3');
} catch (e) {}

// CyberGame Venue Zone & Seats Mapping
const ZONE_SEATS = {
  vip: Array.from({ length: 20 }, (_, i) => `VIP V${String(i + 1).padStart(2, '0')}`),
  standard: Array.from({ length: 40 }, (_, i) => `Standard S${String(i + 1).padStart(2, '0')}`),
  pro: Array.from({ length: 10 }, (_, i) => `Pro Stage P${String(i + 1).padStart(2, '0')}`),
  stream: Array.from({ length: 5 }, (_, i) => `Stream Room R${String(i + 1).padStart(2, '0')}`)
};

function populateSeatsByZone(zoneKey) {
  const seatSelect = document.getElementById('newChatSeatSelect');
  if (!seatSelect) return;
  const seats = ZONE_SEATS[zoneKey] || ZONE_SEATS.vip;
  seatSelect.innerHTML = seats.map(s => `<option value="${s}">🖥️ ${s}</option>`).join('');
}

function getConversations() {
  return cachedConversations;
}

function saveConversations(conversations) {
  cachedConversations = conversations;
}

async function fetchConversations(isPolling = false) {
  try {
    const res = await fetch('/Admin/GetAdminChatList');
    if (!res.ok) return;
    const data = await res.json();
    if (Array.isArray(data)) {
      cachedConversations = data;
      if (!activeConversationId && cachedConversations.length > 0) {
        activeConversationId = cachedConversations[0].id;
      }
      renderChatList();
      renderActiveChat();
      if (!isPolling) {
        renderCustomerInfoDrawer();
      }
    }
  } catch (err) {
    console.warn('Lỗi tải danh sách chat admin:', err);
  }
}

function formatCurrency(num) {
  return new Intl.NumberFormat('vi-VN').format(num || 0) + ' VNĐ';
}

function showToast(message, type = 'success') {
  const container = document.getElementById('adminToastContainer');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `admin-toast ${type}`;
  toast.innerHTML = `
    <span>${type === 'success' ? '✅' : '⚠️'}</span>
    <span>${message}</span>
  `;
  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateY(-10px)';
    toast.style.transition = 'all 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}

// ==========================================================================
// 2. UI RENDERING
// ==========================================================================

function updatePendingBadges() {
  const conversations = getConversations();
  const waitingCount = conversations.filter(c => c.status === 'waiting_admin').length;

  // Header Nav Badge
  const navBadge = document.getElementById('navChatPendingBadge');
  if (navBadge) {
    if (waitingCount > 0) {
      navBadge.textContent = waitingCount;
      navBadge.style.display = 'inline-flex';
    } else {
      navBadge.style.display = 'none';
    }
  }

  // Filter Pill Badge
  const filterBadge = document.getElementById('filterWaitingBadge');
  if (filterBadge) {
    if (waitingCount > 0) {
      filterBadge.textContent = waitingCount;
      filterBadge.style.display = 'inline-flex';
    } else {
      filterBadge.style.display = 'none';
    }
  }
}

function renderChatList() {
  const container = document.getElementById('chatListContainer');
  if (!container) return;

  const conversations = getConversations();
  const query = searchQuery.toLowerCase().trim();

  const filtered = conversations.filter(c => {
    // Search filter
    const matchesSearch = !query ||
      c.customerName.toLowerCase().includes(query) ||
      (c.email && c.email.toLowerCase().includes(query)) ||
      (c.seat && c.seat.toLowerCase().includes(query)) ||
      (c.messages && c.messages.some(m => m.text.toLowerCase().includes(query)));

    if (!matchesSearch) return false;

    // Category filter
    if (currentFilter === 'waiting') return c.status === 'waiting_admin';
    if (currentFilter === 'unread') return !!c.unread;
    if (currentFilter === 'resolved') return c.status === 'resolved';
    return true; // 'all'
  });

  updatePendingBadges();

  if (filtered.length === 0) {
    container.innerHTML = `
      <div class="chat-empty-state">
        <div class="chat-empty-icon">💬</div>
        <div class="chat-empty-title">Không có cuộc trò chuyện nào</div>
        <div style="font-size: 13px; color: #787f8d;">Thử tìm từ khóa khác hoặc chuyển bộ lọc</div>
      </div>
    `;
    return;
  }

  container.innerHTML = filtered.map(c => {
    const isSelected = c.id == activeConversationId;
    const lastMsg = c.messages && c.messages.length > 0 ? c.messages[c.messages.length - 1] : null;
    let previewText = lastMsg ? lastMsg.text.replace(/\n/g, ' ') : 'Đoạn chat mới';
    if (lastMsg && lastMsg.sender === 'admin') previewText = `Bạn: ${previewText}`;
    if (lastMsg && lastMsg.sender === 'bot') previewText = `🤖 Bot: ${previewText}`;

    // Tag badge
    let tagHtml = '';
    if (c.status === 'waiting_admin') {
      tagHtml = `<span class="tag-waiting-admin">⚠️ Chờ Admin</span>`;
    } else if (c.status === 'bot_active') {
      tagHtml = `<span class="tag-bot-active">🤖 Bot</span>`;
    } else if (c.status === 'resolved') {
      tagHtml = `<span class="tag-resolved">✓ Đã xong</span>`;
    }

    return `
      <button class="chat-item ${isSelected ? 'active' : ''} ${c.unread ? 'unread' : ''}" data-conv-id="${c.id}">
        <div class="chat-avatar-wrap">
          <div class="chat-avatar">${c.avatarText || c.customerName.charAt(0)}</div>
          <span class="avatar-online-dot ${c.isOnline ? '' : 'offline'}"></span>
        </div>
        <div class="chat-item-content">
          <div class="chat-item-top">
            <span class="chat-item-name">${c.customerName}</span>
            <span class="chat-item-time">${c.updatedAt || ''}</span>
          </div>
          <div class="chat-item-bottom">
            <span class="chat-item-preview">${previewText}</span>
            ${c.unread ? '<span class="unread-dot"></span>' : ''}
          </div>
          <div class="chat-item-tags">
            ${tagHtml}
            ${c.seat ? `<span style="font-size: 10px; color: #9ca3af; background: #1c2027; padding: 1px 5px; border-radius: 4px;">${c.seat}</span>` : ''}
          </div>
        </div>
      </button>
    `;
  }).join('');

  // Event Listeners for chat items
  container.querySelectorAll('.chat-item').forEach(item => {
    item.addEventListener('click', () => {
      const convId = item.dataset.convId;
      selectConversation(convId);
    });
  });
}

function selectConversation(convId) {
  activeConversationId = parseInt(convId) || convId;
  const conversations = getConversations();
  const conv = conversations.find(c => c.id == activeConversationId);

  if (conv) {
    conv.unread = false;
  }

  renderChatList();
  renderActiveChat();
  renderCustomerInfoDrawer();
}

function renderActiveChat() {
  const messagesContainer = document.getElementById('chatMessagesStream');
  const toggleResolveBtn = document.getElementById('btnToggleResolve');
  if (!messagesContainer) return;

  const conversations = getConversations();
  const conv = conversations.find(c => c.id == activeConversationId);

  if (!conv) {
    if (conversations.length > 0) {
      selectConversation(conversations[0].id);
      return;
    }
    const avatarEl = document.getElementById('activeUserAvatar');
    if (avatarEl) avatarEl.textContent = '💬';
    const nameEl = document.getElementById('activeUserName');
    if (nameEl) nameEl.textContent = 'Hộp Thư Phản Hồi Khách Hàng';
    const subEl = document.getElementById('activeUserSubtitle');
    if (subEl) subEl.textContent = 'Chưa chọn máy trạm • Nhấn ✏️ để bắt đầu';
    const onlineDot = document.getElementById('activeUserOnlineDot');
    if (onlineDot) onlineDot.style.display = 'none';
    if (toggleResolveBtn) toggleResolveBtn.style.display = 'none';

    messagesContainer.innerHTML = `
      <div class="chat-empty-state" style="margin: auto; text-align: center; padding: 60px 20px;">
        <div class="chat-empty-icon" style="font-size: 52px; margin-bottom: 12px;">💬</div>
        <div class="chat-empty-title" style="font-size: 18px; font-weight: 700; color: #fff; margin-bottom: 6px;">Chưa có tin nhắn nào</div>
        <div style="font-size: 14px; color: #8b929f; max-width: 440px; margin: 0 auto; line-height: 1.5;">
          Hệ thống sẵn sàng. Nhấn nút <strong>✏️</strong> ở góc trên bên trái để chọn khu & máy trạm gửi tin nhắn, hoặc chờ tiếp nhận tự động khi khách hàng nhắn tin từ Website.
        </div>
      </div>
    `;
    return;
  }

  const onlineDot = document.getElementById('activeUserOnlineDot');
  if (onlineDot) {
    onlineDot.style.display = 'block';
    onlineDot.className = `avatar-online-dot ${conv.isOnline ? '' : 'offline'}`;
  }
  if (toggleResolveBtn) toggleResolveBtn.style.display = 'inline-flex';

  // Update Header
  document.getElementById('activeUserAvatar').textContent = conv.avatarText || conv.customerName.charAt(0);
  document.getElementById('activeUserName').textContent = conv.customerName;
  document.getElementById('activeUserSubtitle').textContent = `${conv.isOnline ? 'Đang trực tuyến' : 'Ngoại tuyến'} • ${conv.seat || 'Khách vãng lai'} • ${conv.email || ''}`;

  // Resolve button status
  if (toggleResolveBtn) {
    if (conv.status === 'waiting_admin') {
      toggleResolveBtn.className = 'btn-resolve-toggle waiting';
      toggleResolveBtn.innerHTML = `<span>⚠️ Chờ Admin xử lý</span>`;
    } else {
      toggleResolveBtn.className = 'btn-resolve-toggle resolved';
      toggleResolveBtn.innerHTML = `<span>✅ Đã giải quyết</span>`;
    }
  }

  // Render Messages
  let streamHtml = `
    <div class="msg-time-separator">
      <span>ĐOẠN CHAT VỚI KHÁCH HÀNG (PHIÊN #${conv.id})</span>
    </div>
  `;

  // If waiting admin handover exists, show warning banner
  if (conv.status === 'waiting_admin') {
    streamHtml += `
      <div class="handover-alert-banner">
        <span class="icon">⚠️</span>
        <div>
          <strong>BOT ĐÃ CHUYỂN TIẾP TICKET CHO ADMIN:</strong>
          <div style="margin-top: 2px;">${conv.waitingReason || 'Khách hàng có thắc mắc cần Quản Trị Viên giải đáp trực tiếp.'}</div>
        </div>
      </div>
    `;
  }

  // Message bubbles
  (conv.messages || []).forEach(m => {
    const isIncoming = m.sender === 'customer';
    const isBot = m.sender === 'bot';
    const isOutgoing = m.sender === 'admin';

    let rowClass = 'msg-row incoming';
    let senderLabel = conv.customerName;

    if (isBot) {
      rowClass = 'msg-row bot';
      senderLabel = '🤖 Trợ Lý CyberGame (AI Auto)';
    } else if (isOutgoing) {
      rowClass = 'msg-row outgoing';
      senderLabel = '👑 Quản Trị Viên (Bạn)';
    }

    const formattedText = m.text.replace(/\n/g, '<br>');

    streamHtml += `
      <div class="${rowClass}">
        ${isIncoming ? `<div class="msg-mini-avatar">${conv.avatarText || conv.customerName.charAt(0)}</div>` : ''}
        ${isBot ? `<div class="msg-mini-avatar" style="background: #1e3a8a;">🤖</div>` : ''}
        
        <div class="msg-bubble-wrap">
          <span class="msg-sender-label">${senderLabel}</span>
          <div class="msg-bubble">
            ${formattedText}
          </div>
          <div class="msg-meta-row">
            <span>${m.time || ''}</span>
            ${isOutgoing ? '<span>• ✓✓ Đã gửi</span>' : ''}
          </div>
        </div>
      </div>
    `;
  });

  messagesContainer.innerHTML = streamHtml;
  messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

function renderCustomerInfoDrawer() {
  const drawer = document.getElementById('drawerBodyContent');
  if (!drawer) return;

  const conversations = getConversations();
  const conv = conversations.find(c => c.id == activeConversationId);
  if (!conv) {
    drawer.innerHTML = `
      <div class="chat-empty-state" style="padding: 30px 10px; text-align: center;">
        <div class="chat-empty-icon" style="font-size: 36px; margin-bottom: 8px;">ℹ️</div>
        <div class="chat-empty-title" style="font-size: 15px; color: #fff; font-weight: 700;">Thông tin máy trạm</div>
        <div style="font-size: 12px; color: #787f8d; margin-top: 4px;">Chọn một máy trạm để xem chi tiết</div>
      </div>
    `;
    return;
  }

  const balance = conv.balance || 0;
  const custId = conv.userId ? `USER-#${conv.userId}` : `GUEST-#${conv.id}`;

  drawer.innerHTML = `
    <div class="drawer-profile-card">
      <div class="drawer-avatar">${conv.avatarText || conv.customerName.charAt(0)}</div>
      <div class="drawer-name">${conv.customerName}</div>
      <div class="drawer-email">${conv.email || 'Khách chưa liên kết Gmail'}</div>
      <div class="drawer-seat-badge">🖥️ Vị trí: ${conv.seat || 'Chưa gắn máy'}</div>
    </div>

    <div class="drawer-section">
      <div class="drawer-section-title">
        <span>💰</span>
        <span>Tài Khoản Hội Viên</span>
      </div>
      <div class="drawer-data-row">
        <span class="drawer-data-label">Mã tài khoản:</span>
        <span class="drawer-data-val">${custId}</span>
      </div>
      <div class="drawer-data-row">
        <span class="drawer-data-label">Số dư hiện tại:</span>
        <span class="drawer-data-val" style="color: #22c55e; font-size: 14px;">${formatCurrency(balance)}</span>
      </div>
      <div class="drawer-data-row">
        <span class="drawer-data-label">Trạng thái:</span>
        <span class="drawer-data-val" style="color: #60a5fa;">${conv.isOnline ? 'Đang hoạt động' : 'Ngoại tuyến'}</span>
      </div>
      <a href="/Admin/Customers" class="drawer-topup-btn" id="btnDrawerQuickTopUp" style="text-decoration:none;display:inline-flex;align-items:center;justify-content:center;gap:6px">
        <span>💵</span>
        <span>Quản Lý Hội Viên</span>
      </a>
    </div>

    <div class="drawer-section">
      <div class="drawer-section-title">
        <span>⚙️</span>
        <span>Thông Tin Hỗ Trợ</span>
      </div>
      <div class="drawer-data-row">
        <span class="drawer-data-label">Trạng thái xử lý:</span>
        <span class="drawer-data-val" style="color: ${conv.status === 'waiting_admin' ? '#ef4444' : '#22c55e'};">
          ${conv.status === 'waiting_admin' ? '⚠️ Cần Admin hỗ trợ' : '✅ Đã hoàn tất'}
        </span>
      </div>
      ${conv.waitingReason ? `
        <div style="font-size: 12px; color: #fca5a5; background: rgba(239, 68, 68, 0.1); padding: 8px; border-radius: 6px; margin-top: 6px; border: 1px solid rgba(239, 68, 68, 0.2);">
          <strong>Lý do chuyển tiếp:</strong> ${conv.waitingReason}
        </div>
      ` : ''}
    </div>
  `;
}

// ==========================================================================
// 3. ADMIN ACTIONS (SEND MESSAGE, QUICK REPLIES, TOGGLE RESOLVE)
// ==========================================================================

async function sendAdminMessage(text) {
  const cleanText = text.trim();
  if (!cleanText || !activeConversationId) return;

  try {
    const res = await fetch('/Admin/SendAdminReply', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        chatSessionId: parseInt(activeConversationId),
        content: cleanText
      })
    });
    const data = await res.json();
    if (data && data.success) {
      showToast('Đã gửi phản hồi đến khách hàng!');
      await fetchConversations();
    } else {
      showToast(data.message || 'Lỗi khi gửi phản hồi!', 'danger');
    }
  } catch (err) {
    console.error('sendAdminMessage error:', err);
    showToast('Lỗi mạng hoặc kết nối máy chủ!', 'danger');
  }
}

async function toggleResolveStatus() {
  if (!activeConversationId) return;

  try {
    const res = await fetch('/Admin/ToggleTicketStatus', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        chatSessionId: parseInt(activeConversationId)
      })
    });
    const data = await res.json();
    if (data && data.success) {
      showToast(data.message);
      await fetchConversations();
    }
  } catch (err) {
    console.error('toggleResolveStatus error:', err);
  }
}

// ==========================================================================
// 4. INITIALIZATION & EVENT BINDINGS
// ==========================================================================

document.addEventListener('DOMContentLoaded', async () => {
  await fetchConversations();

  // Search Input
  const searchInput = document.getElementById('searchChatInput');
  if (searchInput) {
    searchInput.addEventListener('input', (e) => {
      searchQuery = e.target.value;
      renderChatList();
    });
  }

  // Filter Pills
  const filtersWrap = document.querySelector('.sidebar-filters-wrap');
  if (filtersWrap) {
    filtersWrap.addEventListener('wheel', (e) => {
      if (e.deltaY !== 0) {
        e.preventDefault();
        filtersWrap.scrollLeft += e.deltaY;
      }
    }, { passive: false });
  }

  document.querySelectorAll('.filter-pill').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.filter-pill').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      btn.scrollIntoView({ behavior: 'smooth', inline: 'nearest', block: 'nearest' });
      currentFilter = btn.dataset.filter;
      renderChatList();
    });
  });

  // Admin Chat Input Form
  const chatForm = document.getElementById('adminChatForm');
  const chatInput = document.getElementById('adminChatInput');
  const sendBtn = document.getElementById('btnSendAdminMsg');

  if (chatInput && sendBtn) {
    chatInput.addEventListener('input', () => {
      if (chatInput.value.trim().length > 0) {
        sendBtn.className = 'btn-send-message';
        sendBtn.innerHTML = '➤';
        sendBtn.title = 'Gửi tin nhắn';
      } else {
        sendBtn.className = 'btn-send-message thumbs-up';
        sendBtn.innerHTML = '👍';
        sendBtn.title = 'Gửi biểu tượng thích';
      }
    });
  }

  if (chatForm && chatInput) {
    chatForm.addEventListener('submit', (e) => {
      e.preventDefault();
      const txt = chatInput.value.trim();
      if (txt) {
        sendAdminMessage(txt);
        chatInput.value = '';
        if (sendBtn) {
          sendBtn.className = 'btn-send-message thumbs-up';
          sendBtn.innerHTML = '👍';
        }
      } else {
        sendAdminMessage('👍');
      }
    });
  }

  // Quick Reply Chips
  const quickBar = document.querySelector('.quick-replies-bar');
  if (quickBar) {
    quickBar.addEventListener('wheel', (e) => {
      if (e.deltaY !== 0) {
        e.preventDefault();
        quickBar.scrollLeft += e.deltaY;
      }
    }, { passive: false });
  }

  document.querySelectorAll('.quick-reply-chip').forEach(chip => {
    chip.addEventListener('click', () => {
      const text = chip.dataset.quickText;
      if (text) {
        sendAdminMessage(text);
      }
    });
  });

  // Emoji Picker Click
  document.getElementById('btnEmojiPicker')?.addEventListener('click', () => {
    if (chatInput) {
      chatInput.value += ' 😊 ';
      chatInput.focus();
      chatInput.dispatchEvent(new Event('input'));
    }
  });

  // Toggle Resolve Button
  document.getElementById('btnToggleResolve')?.addEventListener('click', toggleResolveStatus);

  // Toggle Info Drawer
  const infoDrawer = document.getElementById('messengerInfoDrawer');
  document.getElementById('btnToggleInfoDrawer')?.addEventListener('click', () => {
    if (infoDrawer) infoDrawer.classList.toggle('hidden');
  });
  document.getElementById('btnCloseDrawer')?.addEventListener('click', () => {
    if (infoDrawer) infoDrawer.classList.add('hidden');
  });

  // Refresh Chat
  document.getElementById('btnRefreshChat')?.addEventListener('click', async () => {
    await fetchConversations();
    showToast('Đã làm mới danh sách tin nhắn!');
  });

  // New Chat Modal: Zone & Seat Selection
  const modalNewChat = document.getElementById('modalNewChat');
  const zoneSelect = document.getElementById('newChatZoneSelect');

  if (zoneSelect) {
    zoneSelect.addEventListener('change', (e) => {
      populateSeatsByZone(e.target.value);
    });
  }

  document.getElementById('btnNewChat')?.addEventListener('click', () => {
    if (zoneSelect) {
      populateSeatsByZone(zoneSelect.value);
    }
    const initialInput = document.getElementById('newChatInitialMsg');
    if (initialInput) initialInput.value = '';
    if (modalNewChat) modalNewChat.classList.add('show');
  });

  // Close modals
  document.querySelectorAll('[data-close-modal]').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.admin-modal').forEach(m => m.classList.remove('show'));
    });
  });

  // Form Create New Chat (Send message directly to selected machine)
  const formNewChat = document.getElementById('formCreateNewChat');
  if (formNewChat) {
    formNewChat.addEventListener('submit', async (e) => {
      e.preventDefault();
      const selectedSeat = document.getElementById('newChatSeatSelect').value;
      const initialMsg = document.getElementById('newChatInitialMsg').value.trim();
      if (!initialMsg) return;

      try {
        const res = await fetch('/Admin/CreateDirectChat', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            seat: selectedSeat,
            content: initialMsg
          })
        });

        const data = await res.json();
        if (data && data.success) {
          if (data.sessionId) activeConversationId = data.sessionId;
          await fetchConversations();
          if (modalNewChat) modalNewChat.classList.remove('show');
          showToast(`Đã gửi tin nhắn tới ${selectedSeat}!`);
        } else {
          showToast(data.message || 'Lỗi khi tạo tin nhắn!', 'danger');
        }
      } catch (err) {
        console.error('CreateDirectChat error:', err);
        showToast('Lỗi kết nối máy chủ!', 'danger');
      }
    });
  }

  // Periodic polling check every 3 seconds for real-time ticket & message synchronization
  setInterval(() => {
    fetchConversations(true);
  }, 3000);
});
