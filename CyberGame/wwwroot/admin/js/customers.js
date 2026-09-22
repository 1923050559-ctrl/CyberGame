/**
 * CYBERGAME ADMIN - CUSTOMER MANAGEMENT & FNB ORDER PROCESSING CONTROLLER
 * Features:
 * 1. Customer CRUD (Create Account, Edit, Delete, Top Up Balance)
 * 2. Custom Right-Click Context Menu
 * 3. FnB Live Order Queue & Workflow Processing
 * 4. LocalStorage State Persistence
 */

// ==========================================================================
// 1. DATA STORAGE & INITIAL SEEDING (CLEAN ZERO FAKE DATA)
// ==========================================================================
const STORAGE_KEY_CUSTOMERS = 'cybergame_customers_v3';
const STORAGE_KEY_ORDERS = 'cybergame_fnb_orders_v3';

// Clear legacy fake customer and order data
try {
  localStorage.removeItem('cybergame_customers_v1');
  localStorage.removeItem('cybergame_customers_v2');
  localStorage.removeItem('cybergame_fnb_orders_v1');
} catch (e) {}

// Initial customer accounts (Clean empty array, ready for user creation or DB)
const DEFAULT_CUSTOMERS = [];

// Initial orders for kitchen queue (Clean empty array, ready for real live orders)
const DEFAULT_ORDERS = [];

function getCustomers() {
  const saved = localStorage.getItem(STORAGE_KEY_CUSTOMERS);
  if (!saved) {
    saveCustomers(DEFAULT_CUSTOMERS);
    return DEFAULT_CUSTOMERS;
  }
  try {
    return JSON.parse(saved);
  } catch (e) {
    return DEFAULT_CUSTOMERS;
  }
}

function saveCustomers(customers) {
  localStorage.setItem(STORAGE_KEY_CUSTOMERS, JSON.stringify(customers));
}

function getOrders() {
  const saved = localStorage.getItem(STORAGE_KEY_ORDERS);
  if (!saved) {
    saveOrders(DEFAULT_ORDERS);
    return DEFAULT_ORDERS;
  }
  try {
    return JSON.parse(saved);
  } catch (e) {
    return DEFAULT_ORDERS;
  }
}

function saveOrders(orders) {
  localStorage.setItem(STORAGE_KEY_ORDERS, JSON.stringify(orders));
}

// Helpers
function formatCurrency(num) {
  return new Intl.NumberFormat('vi-VN').format(num) + ' VNĐ';
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
// 2. CUSTOMER LIST RENDERING & ACTIONS
// ==========================================================================
let currentSelectedCustomer = null;

function renderCustomersTable(searchQuery = '') {
  const tbody = document.getElementById('customersTableBody');
  if (!tbody) return;

  const customers = getCustomers();
  const query = searchQuery.toLowerCase().trim();

  const filtered = customers.filter(c => 
    !query ||
    c.name.toLowerCase().includes(query) ||
    (c.email && c.email.toLowerCase().includes(query)) ||
    c.id.toLowerCase().includes(query)
  );

  // Update counters
  const badge = document.getElementById('customerCountBadge');
  if (badge) badge.textContent = customers.length;

  const totalBalanceEl = document.getElementById('totalCustomerBalance');
  if (totalBalanceEl) {
    const sum = customers.reduce((acc, c) => acc + (c.balance || 0), 0);
    totalBalanceEl.textContent = formatCurrency(sum);
  }

  if (filtered.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="7" style="text-align: center; padding: 60px 20px; color: var(--text-muted);">
          <div style="font-size: 36px; margin-bottom: 8px;">👤</div>
          <div style="font-weight: 700; font-size: 15px; color: #fff;">Không tìm thấy khách hàng nào</div>
          <div style="font-size: 12px; margin-top: 4px;">Nhấn nút "+ Tạo Tài Khoản Cho Khách" ở góc phải để thêm mới</div>
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = filtered.map(c => {
    const isPositive = c.balance > 0;
    const isLocked = c.status === 'locked';

    return `
      <tr class="customer-row" data-id="${c.id}">
        <td><strong style="color: var(--admin-blue-light); font-family: monospace;">${c.id}</strong></td>
        <td>
          <div style="font-weight: 700; color: #fff; font-size: 14px;">${c.name}</div>
        </td>
        <td>
          <span style="font-weight: 500; color: var(--text-main); font-family: monospace;">${c.email || '—'}</span>
        </td>
        <td>
          <span class="balance-pill ${isPositive ? 'positive' : 'zero'}">
            <span>${isPositive ? '💰' : '⭕'}</span>
            <span>${formatCurrency(c.balance)}</span>
          </span>
        </td>
        <td>
          <span class="badge ${isLocked ? 'danger' : 'success'}">
            ${isLocked ? 'Đang khóa' : 'Hoạt động'}
          </span>
        </td>
        <td>
          <span style="color: var(--text-muted); font-size: 12px;">${c.createdAt}</span>
        </td>
        <td style="text-align: right;">
          <div class="action-btn-group" style="justify-content: flex-end;">
            <button class="btn-icon-action btn-topup" data-id="${c.id}" title="Nạp tiền nhanh">💵</button>
            <button class="btn-icon-action btn-edit" data-id="${c.id}" title="Sửa thông tin">✏️</button>
            <button class="btn-icon-action danger btn-delete" data-id="${c.id}" title="Xóa tài khoản">🗑️</button>
          </div>
        </td>
      </tr>
    `;
  }).join('');

  // Attach Event Listeners to rows
  tbody.querySelectorAll('.customer-row').forEach(row => {
    const id = row.dataset.id;
    const customer = customers.find(c => c.id === id);

    // RIGHT-CLICK CONTEXT MENU
    row.addEventListener('contextmenu', (e) => {
      e.preventDefault();
      openContextMenu(e.clientX, e.clientY, customer);
    });

    // Quick action buttons
    row.querySelector('.btn-topup')?.addEventListener('click', (e) => {
      e.stopPropagation();
      openTopUpModal(customer);
    });

    row.querySelector('.btn-edit')?.addEventListener('click', (e) => {
      e.stopPropagation();
      openEditCustomerModal(customer);
    });

    row.querySelector('.btn-delete')?.addEventListener('click', (e) => {
      e.stopPropagation();
      openDeleteCustomerModal(customer);
    });
  });
}

// ==========================================================================
// 3. RIGHT-CLICK CONTEXT MENU
// ==========================================================================
const contextMenu = document.getElementById('customerContextMenu');

function openContextMenu(x, y, customer) {
  if (!contextMenu || !customer) return;

  currentSelectedCustomer = customer;

  // Highlight selected row
  document.querySelectorAll('.customer-row').forEach(r => r.classList.remove('selected'));
  const activeRow = document.querySelector(`.customer-row[data-id="${customer.id}"]`);
  if (activeRow) activeRow.classList.add('selected');

  // Update menu title
  const titleEl = document.getElementById('ctxCustomerTitle');
  if (titleEl) {
    titleEl.textContent = `KH: ${customer.name} (${customer.id})`;
  }

  // Position context menu
  const menuWidth = 230;
  const menuHeight = 150;
  let posX = x;
  let posY = y;

  if (posX + menuWidth > window.innerWidth) {
    posX = window.innerWidth - menuWidth - 10;
  }
  if (posY + menuHeight > window.innerHeight) {
    posY = window.innerHeight - menuHeight - 10;
  }

  contextMenu.style.left = `${posX}px`;
  contextMenu.style.top = `${posY}px`;
  contextMenu.classList.add('show');
}

function closeContextMenu() {
  if (contextMenu) {
    contextMenu.classList.remove('show');
  }
  document.querySelectorAll('.customer-row').forEach(r => r.classList.remove('selected'));
}

// ==========================================================================
// 4. MODALS MANAGEMENT
// ==========================================================================
function openModal(modalId) {
  closeContextMenu();
  const modal = document.getElementById(modalId);
  if (modal) modal.classList.add('show');
}

function closeModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) modal.classList.remove('show');
}

function closeAllModals() {
  document.querySelectorAll('.admin-modal').forEach(m => m.classList.remove('show'));
}

// A. Create Customer Modal
function openCreateCustomerModal() {
  const form = document.getElementById('formCreateCustomer');
  if (form) form.reset();
  document.getElementById('newCustInitialBalance').value = 50000;
  openModal('createCustomerModal');
}

// B. Top Up Modal
function openTopUpModal(customer) {
  if (!customer) return;
  currentSelectedCustomer = customer;

  document.getElementById('topUpCustomerId').value = customer.id;
  document.getElementById('topUpCustomerName').textContent = `${customer.name} (${customer.email || customer.id})`;
  document.getElementById('topUpCurrentBalance').textContent = formatCurrency(customer.balance);
  document.getElementById('topUpAmount').value = 50000;

  openModal('topUpModal');
}

// C. Edit Customer Modal
function openEditCustomerModal(customer) {
  if (!customer) return;
  currentSelectedCustomer = customer;

  document.getElementById('editCustomerId').value = customer.id;
  document.getElementById('editCustName').value = customer.name;
  document.getElementById('editCustEmail').value = customer.email || '';
  document.getElementById('editCustStatus').value = customer.status || 'active';

  openModal('editCustomerModal');
}

// D. Delete Customer Modal
function openDeleteCustomerModal(customer) {
  if (!customer) return;
  currentSelectedCustomer = customer;

  const label = document.getElementById('deleteCustomerName');
  if (label) label.textContent = `${customer.name} (${customer.id})`;

  openModal('deleteCustomerModal');
}

// ==========================================================================
// 5. FNB LIVE ORDERS QUEUE & WORKFLOW
// ==========================================================================
let currentOrderFilter = 'all';

function renderOrders(filter = currentOrderFilter) {
  currentOrderFilter = filter;
  const container = document.getElementById('ordersContainer');
  if (!container) return;

  const orders = getOrders();
  const pendingCount = orders.filter(o => o.status === 'pending').length;

  // Update badge
  const pendingBadge = document.getElementById('pendingOrdersBadge');
  if (pendingBadge) {
    pendingBadge.textContent = `${pendingCount} Chờ`;
    pendingBadge.className = `badge ${pendingCount > 0 ? 'yellow' : 'info'}`;
  }

  const filtered = orders.filter(o => filter === 'all' || o.status === filter);

  if (filtered.length === 0) {
    container.innerHTML = `
      <div style="text-align: center; padding: 60px 20px; color: var(--text-muted);">
        <div style="font-size: 36px; margin-bottom: 8px;">🍔</div>
        <div style="font-weight: 700; font-size: 15px; color: #fff;">Không có đơn gọi món nào trong mục này</div>
        <div style="font-size: 12px; margin-top: 4px;">Các đơn đặt từ khách hoặc tạo tại quầy sẽ hiển thị tự động tại đây</div>
      </div>
    `;
    return;
  }

  container.innerHTML = `
    <div class="orders-grid">
      ${filtered.map(order => {
        let statusBadge = '';
        let actionButtons = '';

        if (order.status === 'pending') {
          statusBadge = '<span class="badge yellow">⏳ Chờ nhận đơn</span>';
          actionButtons = `
            <button class="btn-order-action" style="background: var(--admin-blue); color: #fff;" data-action="prepare" data-id="${order.id}">👨‍🍳 Nhận đơn & Làm</button>
            <button class="btn-order-action" style="background: #232830; color: #ef4444;" data-action="cancel" data-id="${order.id}">Hủy</button>
          `;
        } else if (order.status === 'preparing') {
          statusBadge = '<span class="badge info">🔥 Đang chuẩn bị</span>';
          actionButtons = `
            <button class="btn-order-action" style="background: var(--admin-purple); color: #fff;" data-action="serve" data-id="${order.id}">🚚 Đã phục vụ máy</button>
            <button class="btn-order-action" style="background: #232830; color: #ef4444;" data-action="cancel" data-id="${order.id}">Hủy</button>
          `;
        } else if (order.status === 'served') {
          statusBadge = '<span class="badge purple">✅ Đã mang lên máy</span>';
          actionButtons = `
            <button class="btn-order-action" style="background: var(--admin-green); color: #fff;" data-action="complete" data-id="${order.id}">💵 Hoàn tất & Thu tiền</button>
          `;
        } else if (order.status === 'completed') {
          statusBadge = '<span class="badge success">🎉 Đã hoàn tất</span>';
          actionButtons = `<div style="font-size: 12px; color: var(--admin-green); font-weight: 700; text-align: center; width: 100%;">✓ Đã thanh toán đầy đủ</div>`;
        } else if (order.status === 'cancelled') {
          statusBadge = '<span class="badge danger">✕ Đã hủy đơn</span>';
          actionButtons = `<div style="font-size: 12px; color: #ef4444; text-align: center; width: 100%;">Đã hủy đơn</div>`;
        }

        return `
          <div class="order-card ${order.status}">
            <div class="order-card-header">
              <div>
                <strong style="color: #fff; font-size: 15px;">${order.customer}</strong>
                <div style="font-size: 12px; color: var(--text-muted); margin-top: 2px;">Mã: ${order.id} • ${order.time}</div>
              </div>
              <span class="order-seat-badge">🖥️ ${order.seat}</span>
            </div>

            <div style="margin-top: 2px;">
              ${statusBadge}
            </div>

            <div class="order-items-box">
              ${order.items.map(it => `
                <div class="order-item-row">
                  <span>${it.name} <strong style="color: var(--admin-cyan);">x${it.qty}</strong></span>
                  <span style="color: var(--text-muted);">${formatCurrency(it.price * it.qty)}</span>
                </div>
              `).join('')}
            </div>

            <div style="display: flex; justify-content: space-between; align-items: center; padding-top: 6px; border-top: 1px dashed var(--admin-border);">
              <span style="font-size: 12px; color: var(--text-muted); font-weight: 700;">TỔNG TIỀN:</span>
              <strong style="font-size: 16px; color: var(--admin-yellow);">${formatCurrency(order.total)}</strong>
            </div>

            <div class="order-actions">
              ${actionButtons}
            </div>
          </div>
        `;
      }).join('')}
    </div>
  `;

  // Attach action button events
  container.querySelectorAll('[data-action]').forEach(btn => {
    btn.addEventListener('click', () => {
      const orderId = btn.dataset.id;
      const action = btn.dataset.action;
      updateOrderStatus(orderId, action);
    });
  });
}

function updateOrderStatus(orderId, action) {
  const orders = getOrders();
  const order = orders.find(o => o.id === orderId);
  if (!order) return;

  if (action === 'prepare') {
    order.status = 'preparing';
    showToast(`Đã nhận đơn #${orderId}, bếp đang chuẩn bị!`);
  } else if (action === 'serve') {
    order.status = 'served';
    showToast(`Đã phục vụ đơn #${orderId} tại ${order.seat}!`);
  } else if (action === 'complete') {
    order.status = 'completed';
    showToast(`Đơn #${orderId} đã thanh toán hoàn tất: ${formatCurrency(order.total)}!`);
  } else if (action === 'cancel') {
    if (confirm(`Bạn chắc chắn muốn hủy đơn hàng #${orderId}?`)) {
      order.status = 'cancelled';
      showToast(`Đã hủy đơn hàng #${orderId}`, 'danger');
    }
  }

  saveOrders(orders);
  renderOrders();
  fetch('/Admin/UpdateOrderStatus', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ orderId: parseInt(orderId), status: order.status })
  }).catch(e => console.log(e));
}

// ==========================================================================
// 6. INITIALIZATION & EVENT LISTENERS
// ==========================================================================
document.addEventListener('DOMContentLoaded', () => {
  // 1. Initial Render
  renderCustomersTable();
  renderOrders();

  // 2. Tab Navigation
  const tabBtns = document.querySelectorAll('.tab-btn[data-tab]');
  tabBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      tabBtns.forEach(b => b.classList.remove('active'));
      document.querySelectorAll('.report-tab-pane').forEach(p => p.classList.remove('active'));

      btn.classList.add('active');
      const target = document.getElementById(btn.dataset.tab);
      if (target) target.classList.add('active');
    });
  });

  // 3. Search Bar
  const searchInput = document.getElementById('mainSearchInput');
  if (searchInput) {
    searchInput.addEventListener('input', (e) => {
      renderCustomersTable(e.target.value);
    });
  }

  // 4. Order Filter Buttons
  const orderFilterBtns = document.querySelectorAll('.order-filter-btn');
  orderFilterBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      orderFilterBtns.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      renderOrders(btn.dataset.filter);
    });
  });

  // 5. Open Modals
  document.getElementById('btnOpenCreateCustomerModal')?.addEventListener('click', openCreateCustomerModal);

  // 6. Close Modals Buttons
  document.querySelectorAll('[data-close-modal]').forEach(btn => {
    btn.addEventListener('click', closeAllModals);
  });

  // Click outside to close modals and context menu
  window.addEventListener('click', (e) => {
    closeContextMenu();
    if (e.target.classList.contains('admin-modal')) {
      closeAllModals();
    }
  });

  window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
      closeContextMenu();
      closeAllModals();
    }
  });

  // 7. Context Menu Action Handlers
  document.getElementById('ctxBtnTopUp')?.addEventListener('click', () => {
    if (currentSelectedCustomer) openTopUpModal(currentSelectedCustomer);
  });

  document.getElementById('ctxBtnEdit')?.addEventListener('click', () => {
    if (currentSelectedCustomer) openEditCustomerModal(currentSelectedCustomer);
  });

  document.getElementById('ctxBtnDelete')?.addEventListener('click', () => {
    if (currentSelectedCustomer) openDeleteCustomerModal(currentSelectedCustomer);
  });

  // 8. Create Customer Form Submit
  const formCreate = document.getElementById('formCreateCustomer');
  if (formCreate) {
    formCreate.addEventListener('submit', (e) => {
      e.preventDefault();
      const name = document.getElementById('newCustName').value.trim();
      const email = document.getElementById('newCustEmail').value.trim();
      const initialBalance = parseInt(document.getElementById('newCustInitialBalance').value, 10) || 0;

      const customers = getCustomers();

      // Check duplicate email
      if (customers.some(c => c.email && c.email.toLowerCase() === email.toLowerCase())) {
        showToast('Địa chỉ Gmail này đã được đăng ký tài khoản!', 'danger');
        return;
      }

      const nextNum = customers.length + 1;
      const newId = `KH-${String(nextNum).padStart(3, '0')}`;
      const now = new Date();
      const dateStr = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')} ${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

      const newCustomer = {
        id: newId,
        name: name,
        email: email,
        balance: initialBalance,
        status: 'active',
        createdAt: dateStr
      };

      customers.unshift(newCustomer);
      saveCustomers(customers);
      fetch('/Admin/CreateCustomer', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: name, email: email, initialBalance: initialBalance })
      }).catch(e => console.log(e));
      renderCustomersTable();
      closeAllModals();
      showToast(`Đã tạo tài khoản thành công cho khách hàng ${name} (${newId})!`);
    });
  }

  // Quick initial balance buttons in create modal
  document.querySelectorAll('[data-add-balance]').forEach(btn => {
    btn.addEventListener('click', () => {
      const input = document.getElementById('newCustInitialBalance');
      const addVal = parseInt(btn.dataset.addBalance, 10);
      const current = parseInt(input.value, 10) || 0;
      input.value = current + addVal;
    });
  });

  document.querySelectorAll('[data-set-balance]').forEach(btn => {
    btn.addEventListener('click', () => {
      document.getElementById('newCustInitialBalance').value = btn.dataset.setBalance;
    });
  });

  // 9. Top Up Form Submit
  const formTopUp = document.getElementById('formTopUp');
  if (formTopUp) {
    formTopUp.addEventListener('submit', (e) => {
      e.preventDefault();
      const id = document.getElementById('topUpCustomerId').value;
      const amount = parseInt(document.getElementById('topUpAmount').value, 10) || 0;

      if (amount <= 0) {
        showToast('Số tiền nạp phải lớn hơn 0 VNĐ!', 'danger');
        return;
      }

      const customers = getCustomers();
      const customer = customers.find(c => c.id === id);
      if (!customer) return;

      customer.balance = (customer.balance || 0) + amount;
      saveCustomers(customers);
      renderCustomersTable();
      closeAllModals();
      showToast(`Đã nạp thành công ${formatCurrency(amount)} cho khách hàng ${customer.name}!`);
    });
  }

  // Quick top up amount buttons
  document.querySelectorAll('[data-topup-val]').forEach(btn => {
    btn.addEventListener('click', () => {
      document.getElementById('topUpAmount').value = btn.dataset.topupVal;
    });
  });

  // 10. Edit Customer Form Submit
  const formEdit = document.getElementById('formEditCustomer');
  if (formEdit) {
    formEdit.addEventListener('submit', (e) => {
      e.preventDefault();
      const id = document.getElementById('editCustomerId').value;
      const name = document.getElementById('editCustName').value.trim();
      const email = document.getElementById('editCustEmail').value.trim();
      const status = document.getElementById('editCustStatus').value;

      const customers = getCustomers();
      const customer = customers.find(c => c.id === id);
      if (!customer) return;

      // Check duplicate email with other accounts
      if (customers.some(c => c.id !== id && c.email && c.email.toLowerCase() === email.toLowerCase())) {
        showToast('Địa chỉ Gmail này đã trùng với tài khoản khác!', 'danger');
        return;
      }

      customer.name = name;
      customer.email = email;
      customer.status = status;

      saveCustomers(customers);
      renderCustomersTable();
      closeAllModals();
      showToast(`Đã cập nhật thông tin tài khoản ${customer.name}!`);
    });
  }

  // 11. Delete Confirmation
  document.getElementById('btnConfirmDelete')?.addEventListener('click', () => {
    if (!currentSelectedCustomer) return;
    const id = currentSelectedCustomer.id;
    const name = currentSelectedCustomer.name;

    let customers = getCustomers();
    customers = customers.filter(c => c.id !== id);
    saveCustomers(customers);
    renderCustomersTable();
    closeAllModals();
    showToast(`Đã xóa tài khoản của khách hàng ${name}`, 'danger');
  });

  updateChatNavBadge();
    // Sync live database customers & orders
    fetch('/Admin/GetCustomersData')
      .then(r => r.json())
      .then(data => {
        if (data.success && data.customers && data.customers.length > 0) {
          const mapped = data.customers.map(c => ({
            id: c.id,
            name: c.username,
            email: c.email || (c.username + '@cybergame.vn'),
            balance: c.balance,
            status: c.status === 'active' ? 'active' : 'locked',
            createdAt: c.createdAt || 'Hôm nay'
          }));
          saveCustomers(mapped);
          renderCustomersTable();
        }
        if (data.success && data.orders && data.orders.length > 0) {
          const mappedOrders = data.orders.map(o => ({
            id: o.id,
            customerName: o.customerName,
            seat: o.seatNumber,
            status: o.status,
            total: o.total,
            time: o.createdAt,
            items: (o.items || []).map(i => `${i.qty}x ${i.name}`)
          }));
          saveOrders(mappedOrders);
          renderOrders();
        }
      })
      .catch(e => console.log('Local customers ready'));

  setInterval(updateChatNavBadge, 2500);
});

function updateChatNavBadge() {
  const badge = document.getElementById('navChatPendingBadge');
  if (!badge) return;
  const raw = localStorage.getItem('cybergame_chat_conversations_v1');
  if (!raw) return;
  try {
    const convs = JSON.parse(raw);
    const count = convs.filter(c => c.status === 'waiting_admin').length;
    if (count > 0) {
      badge.textContent = count;
      badge.style.display = 'inline-flex';
    } else {
      badge.style.display = 'none';
    }
  } catch (e) {}
}
