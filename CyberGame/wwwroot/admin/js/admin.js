/**
 * CYBERGAME ADMIN DASHBOARD - SINGLE VENUE CONTROLLER
 * Clean UI Structure (Ready for Real Database / API Integration)
 * NO Fake Mock Records / Clean Empty States for:
 * 1. Booking Revenue
 * 2. FnB (Food & Beverage) Revenue
 * 3. Gear & Equipment Rental Revenue
 * 4. Maintenance Operations & Costs
 */

// ==========================================================================
// 1. DATA REPOSITORY (INITIAL CLEAN STATE - NO FAKE DATA)
// ==========================================================================
const STORE_DATA = {
  venueName: 'CyberGame Stadium',

  // Current Metrics (Clean 0 values, ready for database sync)
  analytics: {
    bookingRevenue: 0,
    fnbRevenue: 0,
    gearRevenue: 0,
    totalRevenue: 0,
    maintenanceCost: 0,
    maintenanceCount: 0,
    trendRev: '0%',
    trendBooking: '0%',
    trendFnb: '0%',
    trendGear: '0%'
  },

  // Collections (Clean empty arrays, ready to be populated from DB/API)
  gearRentals: [],
  maintenanceLogs: [],
  topFoodItems: [],
  transactions: []
};

// ==========================================================================
// 3. CHART INSTANCES & CONFIGURATION
// ==========================================================================
let revenueTrendChart = null;
let categoryDoughnutChart = null;
let weekdayRevenueChart = null;
let maintenanceChart = null;

function formatCurrency(amount) {
  return new Intl.NumberFormat('vi-VN').format(amount) + ' VNĐ';
}

function getChartDataByPeriod(period) {
  if (STORE_DATA.charts && STORE_DATA.charts.trend && STORE_DATA.charts.trend[period]) {
    const t = STORE_DATA.charts.trend[period];
    return {
      labels: t.labels,
      bookingData: t.bookingData,
      fnbData: t.fnbData,
      gearData: t.rechargesData || t.gearData || t.labels.map(() => 0)
    };
  }

  let labels = ['08:00', '10:00', '12:00', '14:00', '16:00', '18:00', '20:00', '22:00', '24:00'];
  if (period === 'week') labels = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ Nhật'];
  else if (period === 'month') labels = ['Tuần 1', 'Tuần 2', 'Tuần 3', 'Tuần 4'];
  else if (period === 'quarter') labels = ['Quý 1', 'Quý 2', 'Quý 3', 'Quý 4'];
  else if (period === 'year') labels = ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10', 'T11', 'T12'];

  const zeros = labels.map(() => 0);
  return {
    labels: labels,
    bookingData: zeros,
    fnbData: zeros,
    gearData: zeros
  };
}

function initCharts() {
  const chartOptionsBase = {
    responsive: true,
    maintainAspectRatio: false,
    color: '#848e9c',
    plugins: {
      legend: { display: false },
      tooltip: {
        backgroundColor: '#13171f',
        titleColor: '#fff',
        bodyColor: '#54a9ff',
        borderColor: 'rgba(255,255,255,0.1)',
        borderWidth: 1,
        padding: 12,
        boxPadding: 6,
        usePointStyle: true,
        callbacks: {
          label: (context) => `${context.dataset.label}: ${formatCurrency(context.raw)}`
        }
      }
    },
    scales: {
      x: {
        grid: { color: 'rgba(255, 255, 255, 0.04)', borderColor: 'transparent' },
        ticks: { color: '#848e9c', font: { family: 'Inter', size: 11 } }
      },
      y: {
        beginAtZero: true,
        suggestedMin: 0,
        suggestedMax: 1000000,
        grid: { color: 'rgba(255, 255, 255, 0.04)', borderColor: 'transparent' },
        ticks: {
          color: '#848e9c',
          font: { family: 'Inter', size: 11 },
          precision: 0,
          stepSize: 200000,
          callback: (value) => formatCurrency(value)
        }
      }
    }
  };

  // 1. Revenue Trend Line Chart
  const trendCtx = document.getElementById('revenueTrendChart')?.getContext('2d');
  if (trendCtx) {
    const trendData = getChartDataByPeriod('today');
    revenueTrendChart = new Chart(trendCtx, {
      type: 'line',
      data: {
        labels: trendData.labels,
        datasets: [
          {
            label: 'Doanh thu Booking',
            data: trendData.bookingData,
            borderColor: '#10b981',
            backgroundColor: 'rgba(16, 185, 129, 0.08)',
            fill: true,
            tension: 0.35,
            borderWidth: 2,
            pointBackgroundColor: '#10b981',
            pointRadius: 2
          },
          {
            label: 'Doanh thu FnB',
            data: trendData.fnbData,
            borderColor: '#f59e0b',
            backgroundColor: 'rgba(245, 158, 11, 0.08)',
            fill: true,
            tension: 0.35,
            borderWidth: 2,
            pointBackgroundColor: '#f59e0b',
            pointRadius: 2
          },
          {
            label: 'Nạp tiền ví & Dịch vụ',
            data: trendData.gearData,
            borderColor: '#00d2ff',
            backgroundColor: 'rgba(0, 210, 255, 0.08)',
            fill: true,
            tension: 0.35,
            borderWidth: 2,
            pointBackgroundColor: '#00d2ff',
            pointRadius: 2
          }
        ]
      },
      options: chartOptionsBase
    });
  }

  // 2. Revenue Doughnut Chart (Placeholder ring when empty)
  const doughnutCtx = document.getElementById('categoryDoughnutChart')?.getContext('2d');
  if (doughnutCtx) {
    categoryDoughnutChart = new Chart(doughnutCtx, {
      type: 'doughnut',
      data: {
        labels: ['Chưa có dữ liệu'],
        datasets: [{
          data: [1],
          backgroundColor: ['#1c222c'],
          borderColor: '#13171f',
          borderWidth: 2,
          hoverOffset: 0
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '72%',
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#13171f',
            borderColor: 'rgba(255,255,255,0.1)',
            borderWidth: 1,
            padding: 10,
            callbacks: {
              label: () => ' Chưa có dữ liệu phát sinh'
            }
          }
        }
      }
    });
  }

  // 3. Weekday Revenue Bar Chart
  const weekdayCtx = document.getElementById('weekdayRevenueChart')?.getContext('2d');
  if (weekdayCtx) {
    weekdayRevenueChart = new Chart(weekdayCtx, {
      type: 'bar',
      data: {
        labels: ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ Nhật'],
        datasets: [
          {
            label: 'Booking',
            data: [0, 0, 0, 0, 0, 0, 0],
            backgroundColor: '#10b981',
            borderRadius: 4
          },
          {
            label: 'FnB',
            data: [0, 0, 0, 0, 0, 0, 0],
            backgroundColor: '#f59e0b',
            borderRadius: 4
          },
          {
            label: 'Nạp tiền ví',
            data: [0, 0, 0, 0, 0, 0, 0],
            backgroundColor: '#00d2ff',
            borderRadius: 4
          }
        ]
      },
      options: chartOptionsBase
    });
  }

  // 4. Maintenance Operations Chart
  const maintCtx = document.getElementById('maintenanceChart')?.getContext('2d');
  if (maintCtx) {
    maintenanceChart = new Chart(maintCtx, {
      type: 'bar',
      data: {
        labels: ['Chuột gaming', 'Bàn phím cơ', 'Tai nghe & Mút', 'PC & Tản nhiệt', 'Màn hình & Cáp', 'Ghế Gaming'],
        datasets: [
          {
            label: 'Chi phí sửa chữa',
            data: [0, 0, 0, 0, 0, 0],
            backgroundColor: '#ef4444',
            borderRadius: 4,
            yAxisID: 'y'
          },
          {
            label: 'Số sự vụ',
            data: [0, 0, 0, 0, 0, 0],
            backgroundColor: '#00d2ff',
            borderRadius: 4,
            yAxisID: 'y1'
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#13171f',
            titleColor: '#fff',
            borderColor: 'rgba(255,255,255,0.1)',
            borderWidth: 1,
            padding: 12,
            callbacks: {
              label: (context) => {
                if (context.dataset.yAxisID === 'y1') {
                  return `${context.dataset.label}: ${context.raw} vụ`;
                }
                return `${context.dataset.label}: ${formatCurrency(context.raw)}`;
              }
            }
          }
        },
        scales: {
          x: chartOptionsBase.scales.x,
          y: {
            beginAtZero: true,
            suggestedMin: 0,
            suggestedMax: 1000000,
            grid: { color: 'rgba(255, 255, 255, 0.04)' },
            ticks: {
              color: '#ef4444',
              precision: 0,
              stepSize: 200000,
              callback: (v) => formatCurrency(v)
            }
          },
          y1: {
            beginAtZero: true,
            suggestedMin: 0,
            suggestedMax: 5,
            position: 'right',
            grid: { display: false },
            ticks: {
              color: '#00d2ff',
              stepSize: 1,
              precision: 0,
              callback: (v) => Number.isInteger(v) ? v + ' vụ' : ''
            }
          }
        }
      }
    });
  }
}

// ==========================================================================
// 4. UI RENDERERS FOR TABLES AND KPIS (EMPTY STATES)
// ==========================================================================
function renderKPIs() {
  const data = STORE_DATA.analytics;

  const elTotalRev = document.getElementById('kpiTotalRevenue');
  if (elTotalRev) elTotalRev.textContent = formatCurrency(data.totalRevenue);
  const elTotalRevTrend = document.getElementById('kpiTotalRevenueTrend');
  if (elTotalRevTrend) elTotalRevTrend.textContent = data.trendRev || '0%';

  const elBookingRev = document.getElementById('kpiBookingRevenue');
  if (elBookingRev) elBookingRev.textContent = formatCurrency(data.bookingRevenue);
  const elBookingTrend = document.getElementById('kpiBookingTrend');
  if (elBookingTrend) elBookingTrend.textContent = data.trendBooking || '0%';
  const elBookingPct = document.getElementById('kpiBookingPct');
  if (elBookingPct) {
    const pct = data.totalRevenue > 0 ? ((data.bookingRevenue / data.totalRevenue) * 100).toFixed(1) : '0';
    elBookingPct.textContent = `Chiếm ${pct}% tổng thu`;
  }

  const elFnbRev = document.getElementById('kpiFnbRevenue');
  if (elFnbRev) elFnbRev.textContent = formatCurrency(data.fnbRevenue);
  const elFnbTrend = document.getElementById('kpiFnbTrend');
  if (elFnbTrend) elFnbTrend.textContent = data.trendFnb || '0%';
  const elFnbPct = document.getElementById('kpiFnbPct');
  if (elFnbPct) {
    const pct = data.totalRevenue > 0 ? ((data.fnbRevenue / data.totalRevenue) * 100).toFixed(1) : '0';
    elFnbPct.textContent = `Chiếm ${pct}% tổng thu`;
  }

  const elGearRev = document.getElementById('kpiGearRevenue');
  if (elGearRev) elGearRev.textContent = formatCurrency(data.gearRevenue);
  const elGearTrend = document.getElementById('kpiGearTrend');
  if (elGearTrend) elGearTrend.textContent = data.trendGear || '0%';
  const elGearPct = document.getElementById('kpiGearPct');
  if (elGearPct) {
    const pct = data.totalRevenue > 0 ? ((data.gearRevenue / data.totalRevenue) * 100).toFixed(1) : '0';
    elGearPct.textContent = `Chiếm ${pct}% tổng thu`;
  }

  const elMaintCost = document.getElementById('kpiMaintenanceCost');
  if (elMaintCost) elMaintCost.textContent = formatCurrency(data.maintenanceCost);
  const elMaintCount = document.getElementById('kpiMaintenanceCount');
  if (elMaintCount) elMaintCount.textContent = `${data.maintenanceCount} sự vụ ghi nhận`;

  const pBooking = document.getElementById('piePctBooking');
  const pFnb = document.getElementById('piePctFnb');
  const pGear = document.getElementById('piePctGear');
  if (pBooking && pFnb && pGear) {
    pBooking.textContent = data.totalRevenue > 0 ? `${((data.bookingRevenue / data.totalRevenue) * 100).toFixed(1)}%` : '0%';
    pFnb.textContent = data.totalRevenue > 0 ? `${((data.fnbRevenue / data.totalRevenue) * 100).toFixed(1)}%` : '0%';
    pGear.textContent = data.totalRevenue > 0 ? `${((data.gearRevenue / data.totalRevenue) * 100).toFixed(1)}%` : '0%';
  }
}

function renderTransactionsTable(filterText = '') {
  const tbody = document.getElementById('transactionsTableBody');
  if (!tbody) return;

  const query = filterText.toLowerCase().trim();
  const filtered = STORE_DATA.transactions.filter(t => 
    !query || 
    t.code.toLowerCase().includes(query) ||
    t.customer.toLowerCase().includes(query) ||
    t.phone.includes(query)
  );

  if (filtered.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="9" style="text-align:center;padding:50px 20px;color:var(--text-muted)">
          <div style="font-size:32px;margin-bottom:8px">📭</div>
          <div style="font-weight:700;font-size:14px;color:#fff">Chưa có dữ liệu đơn Booking & FnB</div>
          <div style="font-size:12px;color:var(--text-dim);margin-top:4px">Các đơn đặt chỗ và gọi món mới sẽ tự động hiển thị tại đây khi phát sinh</div>
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = filtered.map(t => `
    <tr>
      <td><strong style="color:var(--admin-blue-light)">${t.code}</strong></td>
      <td><div style="font-weight:700">${t.customer}</div></td>
      <td><div style="font-size:12px;color:var(--text-muted)">${t.phone}</div></td>
      <td><strong style="color:var(--admin-green)">${formatCurrency(t.bookingFee)}</strong></td>
      <td><strong style="color:var(--admin-yellow)">${formatCurrency(t.fnbFee)}</strong></td>
      <td><strong style="color:var(--admin-purple)">${formatCurrency(t.gearFee)}</strong></td>
      <td><strong style="color:#fff">${formatCurrency(t.total)}</strong></td>
      <td><span class="badge ${t.method === 'transfer' ? 'purple' : 'info'}">${t.method === 'transfer' ? '⚡ VietQR' : '💵 Tiền mặt'}</span></td>
      <td><span class="badge ${t.status === 'completed' ? 'success' : (t.status === 'cancelled' ? 'danger' : 'info')}">${t.status}</span></td>
    </tr>
  `).join('');
}

function renderGearTable() {
  const tbody = document.getElementById('gearTableBody');
  if (!tbody) return;

  if (STORE_DATA.gearRentals.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="7" style="text-align:center;padding:50px 20px;color:var(--text-muted)">
          <div style="font-size:32px;margin-bottom:8px">🎧</div>
          <div style="font-weight:700;font-size:14px;color:#fff">Chưa có dữ liệu thuê Gear & Dụng cụ</div>
          <div style="font-size:12px;color:var(--text-dim);margin-top:4px">Danh sách thiết bị và lượt thuê sẽ hiển thị tại đây khi phát sinh</div>
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = STORE_DATA.gearRentals.map(g => `
    <tr>
      <td><strong style="color:var(--admin-purple)">${g.code}</strong></td>
      <td><strong style="color:#fff">${g.name}</strong></td>
      <td><span class="badge purple">${g.category}</span></td>
      <td><span style="color:var(--admin-cyan);font-weight:700">${formatCurrency(g.price)}/h</span></td>
      <td><strong>${g.rentals} lượt</strong></td>
      <td><strong style="color:var(--admin-green)">${formatCurrency(g.revenue)}</strong></td>
      <td><span class="badge success">${g.status}</span></td>
    </tr>
  `).join('');
}

function renderMaintenanceTable() {
  const tbody = document.getElementById('maintenanceTableBody');
  if (!tbody) return;

  if (STORE_DATA.maintenanceLogs.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="8" style="text-align:center;padding:50px 20px;color:var(--text-muted)">
          <div style="font-size:32px;margin-bottom:8px">🔧</div>
          <div style="font-weight:700;font-size:14px;color:#fff">Chưa có nhật ký bảo trì thiết bị</div>
          <div style="font-size:12px;color:var(--text-dim);margin-top:4px">Các phiếu ghi nhận sửa chữa, thay thế linh kiện phòng máy sẽ hiển thị tại đây khi phát sinh</div>
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = STORE_DATA.maintenanceLogs.map(m => `
    <tr>
      <td><strong style="color:var(--admin-red)">${m.code}</strong></td>
      <td><span style="font-weight:700;color:#fff">${m.location}</span></td>
      <td><strong style="color:var(--admin-cyan)">${m.item}</strong></td>
      <td><span style="color:var(--text-muted);font-size:12px">${m.detail}</span></td>
      <td><strong style="color:#ef4444">${formatCurrency(m.cost)}</strong></td>
      <td>${m.tech}</td>
      <td>${m.date}</td>
      <td><span class="badge success">${m.status}</span></td>
    </tr>
  `).join('');
}

function renderTopFoodTable() {
  const tbody = document.getElementById('topFoodTableBody');
  if (!tbody) return;

  if (STORE_DATA.topFoodItems.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="7" style="text-align:center;padding:50px 20px;color:var(--text-muted)">
          <div style="font-size:32px;margin-bottom:8px">🍔</div>
          <div style="font-weight:700;font-size:14px;color:#fff">Chưa có dữ liệu món ăn & thức uống</div>
          <div style="font-size:12px;color:var(--text-dim);margin-top:4px">Thống kê món bán chạy từ bếp F&B sẽ hiển thị tại đây khi có đơn hàng</div>
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = STORE_DATA.topFoodItems.map(item => `
    <tr>
      <td><span class="badge info">#${item.rank}</span></td>
      <td><strong style="color:#fff">${item.name}</strong></td>
      <td><span class="badge yellow">${item.category}</span></td>
      <td>${formatCurrency(item.price)}</td>
      <td><strong style="color:var(--admin-cyan)">${item.qty} suất</strong></td>
      <td><strong style="color:var(--admin-green)">${formatCurrency(item.revenue)}</strong></td>
      <td><span>${item.contribution}</span></td>
    </tr>
  `).join('');
}

// ==========================================================================
// 5. EVENT LISTENERS & CONTROLLER
// ==========================================================================

// ==========================================================================
// DYNAMIC LIVE DATA SYNC WITH SQL SERVER
// ==========================================================================
function applyLiveDashboardData(data) {
  if (!data || !data.success) return;

  if (data.analytics) {
    Object.assign(STORE_DATA.analytics, data.analytics);
  }
  if (data.charts) {
    STORE_DATA.charts = data.charts;
  }
  if (data.transactions && data.transactions.length > 0) {
    STORE_DATA.transactions = data.transactions;
  }
  if (data.topFoodItems && data.topFoodItems.length > 0) {
    STORE_DATA.topFoodItems = data.topFoodItems;
  }
  if (data.maintenanceLogs && data.maintenanceLogs.length > 0) {
    STORE_DATA.maintenanceLogs = data.maintenanceLogs;
  }

  // 1. Render updated KPI cards
  renderKPIs();

  // 2. Render Doughnut Chart (Category Breakdown)
  if (categoryDoughnutChart) {
    const cat = data.charts?.category;
    if (cat && cat.data && cat.data.some(v => v > 0)) {
      categoryDoughnutChart.data.labels = cat.labels;
      categoryDoughnutChart.data.datasets[0].data = cat.data;
      categoryDoughnutChart.data.datasets[0].backgroundColor = ['#10b981', '#f59e0b', '#00d2ff'];
      categoryDoughnutChart.data.datasets[0].borderColor = ['#13171f', '#13171f', '#13171f'];
      categoryDoughnutChart.options.plugins.tooltip.callbacks.label = (context) => {
        const total = context.dataset.data.reduce((a, b) => a + b, 0);
        const pct = total > 0 ? ((context.raw / total) * 100).toFixed(1) : 0;
        return ` ${context.label}: ${formatCurrency(context.raw)} (${pct}%)`;
      };
    } else {
      categoryDoughnutChart.data.labels = ['Chưa có dữ liệu'];
      categoryDoughnutChart.data.datasets[0].data = [1];
      categoryDoughnutChart.data.datasets[0].backgroundColor = ['#1c222c'];
      categoryDoughnutChart.data.datasets[0].borderColor = ['#13171f'];
    }
    categoryDoughnutChart.update();
  }

  // Update Doughnut percentage text labels below the chart
  const totRev = STORE_DATA.analytics.totalRevenue || 0;
  const elPieBk = document.getElementById('piePctBooking');
  if (elPieBk) elPieBk.textContent = totRev > 0 ? ((STORE_DATA.analytics.bookingRevenue / totRev) * 100).toFixed(1) + '%' : '0%';
  const elPieFb = document.getElementById('piePctFnb');
  if (elPieFb) elPieFb.textContent = totRev > 0 ? ((STORE_DATA.analytics.fnbRevenue / totRev) * 100).toFixed(1) + '%' : '0%';
  const elPieGear = document.getElementById('piePctGear');
  if (elPieGear) {
    const rcRev = STORE_DATA.analytics.rechargesRevenue || 0;
    elPieGear.textContent = totRev > 0 ? ((rcRev / totRev) * 100).toFixed(1) + '%' : '0%';
  }

  // 3. Render Revenue Trend Line Chart
  if (revenueTrendChart && data.charts?.trend) {
    const period = document.getElementById('filterPeriod')?.value || 'today';
    const trendObj = data.charts.trend[period] || data.charts.trend['today'];
    if (trendObj) {
      revenueTrendChart.data.labels = trendObj.labels;
      revenueTrendChart.data.datasets[0].data = trendObj.bookingData;
      revenueTrendChart.data.datasets[1].data = trendObj.fnbData;
      revenueTrendChart.data.datasets[2].data = trendObj.rechargesData || trendObj.gearData || [];
      const allVals = [...trendObj.bookingData, ...trendObj.fnbData, ...(trendObj.rechargesData || [0])];
      const maxVal = Math.max(...allVals, 50000);
      revenueTrendChart.options.scales.y.suggestedMax = Math.ceil(maxVal * 1.3);
      revenueTrendChart.update();
    }
  }

  // 4. Render Weekday Revenue Bar Chart
  if (weekdayRevenueChart && data.charts?.weekday) {
    const wd = data.charts.weekday;
    weekdayRevenueChart.data.labels = wd.labels;
    weekdayRevenueChart.data.datasets[0].data = wd.bookingData;
    weekdayRevenueChart.data.datasets[1].data = wd.fnbData;
    weekdayRevenueChart.data.datasets[2].data = wd.rechargesData || [];
    const allVals = [...wd.bookingData, ...wd.fnbData, ...(wd.rechargesData || [0])];
    const maxVal = Math.max(...allVals, 50000);
    weekdayRevenueChart.options.scales.y.suggestedMax = Math.ceil(maxVal * 1.3);
    weekdayRevenueChart.update();
  }

  // 5. Render Maintenance Operations Chart
  if (maintenanceChart && data.charts?.maintenance) {
    const mc = data.charts.maintenance;
    maintenanceChart.data.labels = mc.labels;
    maintenanceChart.data.datasets[0].data = mc.costData;
    maintenanceChart.data.datasets[1].data = mc.countData;
    const maxCost = Math.max(...mc.costData, 500000);
    const maxCount = Math.max(...mc.countData, 5);
    maintenanceChart.options.scales.y.suggestedMax = Math.ceil(maxCost * 1.3);
    maintenanceChart.options.scales.y1.suggestedMax = maxCount + 1;
    maintenanceChart.update();
  }

  // 6. Render Tables
  renderTransactionsTable();
  renderGearTable();
  renderMaintenanceTable();
  renderTopFoodTable();
}

function fetchLiveDashboardStats() {
  return fetch('/Admin/GetDashboardStats')
    .then(r => r.json())
    .then(data => {
      applyLiveDashboardData(data);
    })
    .catch(e => {
      console.log('Using initial dashboard state:', e);
    });
}


function setupEventListeners() {
  const periodSelect = document.getElementById('filterPeriod');

  function updateDashboardData() {
    const period = periodSelect ? periodSelect.value : 'today';
    renderKPIs();

    if (revenueTrendChart) {
      const newChartData = getChartDataByPeriod(period);
      revenueTrendChart.data.labels = newChartData.labels;
      revenueTrendChart.data.datasets[0].data = newChartData.bookingData;
      revenueTrendChart.data.datasets[1].data = newChartData.fnbData;
      revenueTrendChart.data.datasets[2].data = newChartData.gearData;
      const allVals = [...newChartData.bookingData, ...newChartData.fnbData, ...newChartData.gearData];
      const maxVal = Math.max(...allVals, 50000);
      revenueTrendChart.options.scales.y.suggestedMax = Math.ceil(maxVal * 1.3);
      revenueTrendChart.update();
    }
  }

  if (periodSelect) periodSelect.addEventListener('change', updateDashboardData);

  // Tabs Controller
  const tabBtns = document.querySelectorAll('.tab-btn');
  const tabPanes = document.querySelectorAll('.report-tab-pane');

  tabBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      tabBtns.forEach(b => b.classList.remove('active'));
      tabPanes.forEach(p => p.classList.remove('active'));

      btn.classList.add('active');
      const targetId = btn.dataset.tab;
      const targetPane = document.getElementById(targetId);
      if (targetPane) targetPane.classList.add('active');
    });
  });

  // Search in Transactions Table
  const searchInput = document.getElementById('txSearchInput');
  if (searchInput) {
    searchInput.addEventListener('input', (e) => {
      renderTransactionsTable(e.target.value);
    });
  }

  // Refresh Button - Live fetch from DB
  const btnRefresh = document.getElementById('btnRefreshData');
  if (btnRefresh) {
    btnRefresh.addEventListener('click', () => {
      btnRefresh.innerHTML = '⏳ Đang làm mới...';
      fetchLiveDashboardStats().finally(() => {
        btnRefresh.innerHTML = '🔄 Làm mới';
      });
    });
  }

  // Export CSV Report
  const btnExport = document.getElementById('btnExportReport');
  if (btnExport) {
    btnExport.addEventListener('click', exportCSVReport);
  }
}

// Export CSV File
function exportCSVReport() {
  const period = document.getElementById('filterPeriod')?.value || 'today';
  let csvContent = "data:text/csv;charset=utf-8,\uFEFF";
  csvContent += "CYBERGAME STADIUM - BÁO CÁO DOANH THU & DỊCH VỤ\n";
  csvContent += `Mốc thời gian: ${period.toUpperCase()}\n`;
  csvContent += "Mã đơn,Khách hàng,Số điện thoại,Tiền Booking,Tiền FnB,Tiền Thuê Gear,Tổng tiền,Phương thức,Trạng thái\n";

  if (STORE_DATA.transactions.length === 0) {
    csvContent += "(Chưa có dữ liệu giao dịch phát sinh)\n";
  } else {
    STORE_DATA.transactions.forEach(t => {
      csvContent += `"${t.code}","${t.customer}","${t.phone}",${t.bookingFee},${t.fnbFee},${t.gearFee},${t.total},"${t.method}","${t.status}"\n`;
    });
  }

  const encodedUri = encodeURI(csvContent);
  const link = document.createElement("a");
  link.setAttribute("href", encodedUri);
  link.setAttribute("download", `CyberGame_BaoCao_${period}_${Date.now()}.csv`);
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

// ==========================================================================
// 6. INITIALIZATION
// ==========================================================================
document.addEventListener('DOMContentLoaded', () => {
  initCharts();
  renderKPIs();
  renderTransactionsTable();
  renderGearTable();
  renderMaintenanceTable();
  renderTopFoodTable();
  setupEventListeners();

  // Fetch live database analytics & charts from SQL Server
  fetchLiveDashboardStats();

  updateChatNavBadge();
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
