// Karavul – Main JS
document.addEventListener('DOMContentLoaded', () => {
    // Theme Management
    const themeToggle = document.getElementById('themeToggle');
    const root = document.documentElement;
    
    // Auto-detect or load from localStorage
    let currentTheme = localStorage.getItem('theme');
    if (!currentTheme) {
        currentTheme = 'light';
    }
    root.setAttribute('data-theme', currentTheme);

    if (themeToggle) {
        themeToggle.addEventListener('click', () => {
            const newTheme = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
            root.setAttribute('data-theme', newTheme);
            localStorage.setItem('theme', newTheme);
            
            if (window.realtimeChartInstance) {
                initChart(window.currentChartPeriod || 'day'); // redraw with new colors
            }
        });
    }

    // Chart.js initialization
    const chartCanvas = document.getElementById('realtimeChart');
    if (chartCanvas) {
        window.currentChartPeriod = 'day';
        initChart(window.currentChartPeriod);
    }

    const liveChartCanvas = document.getElementById('liveChart');
    if (liveChartCanvas) {
        initLiveChart();
    }

    // Chart period buttons
    document.querySelectorAll('.chart-period-buttons .btn').forEach(btn => {
        btn.addEventListener('click', e => {
            document.querySelectorAll('.chart-period-buttons .btn').forEach(b => b.classList.remove('active'));
            const target = e.target;
            target.classList.add('active');
            
            const period = target.getAttribute('data-period');
            if (window.currentChartPeriod !== period) {
                window.currentChartPeriod = period;
                initChart(period);
            }
        });
    });

    // Auto-dismiss alerts
    document.querySelectorAll('.alert').forEach(el => {
        setTimeout(() => {
            el.style.transition = 'opacity 0.5s';
            el.style.opacity = '0';
            setTimeout(() => el.remove(), 500);
        }, 4000);
    });

    // Confirm delete dialogs
    document.querySelectorAll('[data-confirm]').forEach(el => {
        el.addEventListener('click', e => {
            const msg = el.getAttribute('data-confirm');
            if (!confirm(msg)) e.preventDefault();
        });
    });

    // Response time coloring
    document.querySelectorAll('.response-time-cell').forEach(el => {
        const ms = parseInt(el.textContent);
        if (!isNaN(ms)) {
            const span = el.querySelector('.response-time') || el;
            if (ms < 500) span.classList.add('fast');
            else if (ms < 2000) span.classList.add('medium');
            else span.classList.add('slow');
        }
    });

    // AJAX refresh dashboard every 30 seconds
    if (window.location.pathname === '/') {
        setInterval(() => {
            refreshDashboardStats();
        }, 30000);
    }

    // Dynamic email/phone input management
    setupDynamicInputs();
});

function setupDynamicInputs() {
    // Email add button
    const addEmailBtn = document.getElementById('addEmailBtn');
    const emailContainer = document.getElementById('emailContainer');
    if (addEmailBtn && emailContainer) {
        addEmailBtn.addEventListener('click', () => {
            const idx = emailContainer.querySelectorAll('.dynamic-input-row').length;
            const row = createDynamicRow('emails', idx, 'E-posta adresi');
            emailContainer.appendChild(row);
        });
    }

    // Phone add button
    const addPhoneBtn = document.getElementById('addPhoneBtn');
    const phoneContainer = document.getElementById('phoneContainer');
    if (addPhoneBtn && phoneContainer) {
        addPhoneBtn.addEventListener('click', () => {
            const idx = phoneContainer.querySelectorAll('.dynamic-input-row').length;
            const row = createDynamicRow('phones', idx, 'Telefon numarası');
            phoneContainer.appendChild(row);
        });
    }

    // Remove row buttons (delegated)
    document.addEventListener('click', e => {
        if (e.target.closest('.remove-input-row')) {
            const row = e.target.closest('.dynamic-input-row');
            if (row) row.remove();
        }
    });
}

function createDynamicRow(type, idx, placeholder) {
    const div = document.createElement('div');
    div.className = 'dynamic-input-row';
    div.style.display = 'flex';
    div.style.gap = '8px';
    div.style.marginBottom = '8px';

    const input = document.createElement('input');
    input.type = type === 'phones' ? 'tel' : 'email';
    input.name = type === 'emails' ? `Emails[${idx}]` : `Phones[${idx}]`;
    input.placeholder = placeholder;
    input.className = 'form-control';

    const removeBtn = document.createElement('button');
    removeBtn.type = 'button';
    removeBtn.className = 'btn btn-danger btn-icon btn-sm remove-input-row';
    removeBtn.innerHTML = '&times;';
    removeBtn.title = 'Kaldır';

    div.appendChild(input);
    div.appendChild(removeBtn);
    return div;
}

async function initChart(period = 'day') {
    const canvas = document.getElementById('realtimeChart');
    if (!canvas) return;
    
    try {
        const response = await fetch(`/?handler=ChartData&period=${period}`);
        if (!response.ok) return;
        const result = await response.json();
        
        const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
        const textColor = isDark ? '#cccccc' : '#495057';
        const gridColor = isDark ? '#333333' : '#e5e5e5';
        
        const successColor = isDark ? '#4caf50' : '#388e3c'; // Green
        const successAlpha = isDark ? 'rgba(76,175,80,0.3)' : 'rgba(56,142,60,0.2)';
        
        const failColor = isDark ? '#f44336' : '#d32f2f'; // Red
        const failAlpha = isDark ? 'rgba(244,67,54,0.3)' : 'rgba(211,47,47,0.2)';

        if (window.realtimeChartInstance) {
            window.realtimeChartInstance.destroy();
        }

        const totalFail = result.failData ? result.failData.reduce((a, b) => a + b, 0) : 0;
        const totalSuccess = result.successData ? result.successData.reduce((a, b) => a + b, 0) : 0;

        window.realtimeChartInstance = new Chart(canvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: result.labels,
                datasets: [
                    {
                        label: `Başarısız (${totalFail})`,
                        data: result.failData,
                        borderColor: failColor,
                        backgroundColor: failAlpha,
                        borderWidth: 2,
                        fill: true,
                        tension: 0,
                        pointRadius: 0,
                        pointHoverRadius: 5
                    },
                    {
                        label: `Başarılı (${totalSuccess})`,
                        data: result.successData,
                        borderColor: successColor,
                        backgroundColor: successAlpha,
                        borderWidth: 2,
                        fill: true,
                        tension: 0,
                        pointRadius: 0,
                        pointHoverRadius: 5
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: { duration: 0 },
                plugins: { 
                    legend: { display: true, labels: { color: textColor } },
                    tooltip: { mode: 'index', intersect: false }
                },
                interaction: {
                    mode: 'nearest',
                    axis: 'x',
                    intersect: false
                },
                scales: {
                    x: {
                        grid: { color: gridColor },
                        ticks: { color: textColor, maxTicksLimit: 24 }
                    },
                    y: {
                        stacked: true,
                        grid: { color: gridColor },
                        ticks: { color: textColor },
                        beginAtZero: true
                    }
                }
            }
        });
    } catch (err) {
        console.error("Failed to load chart data", err);
    }
}

document.addEventListener('DOMContentLoaded', () => {
    // User Dropdown Menu
    const userMenuBtn = document.getElementById('userMenuBtn');
    const userDropdownMenu = document.getElementById('userDropdownMenu');
    
    if (userMenuBtn && userDropdownMenu) {
        userMenuBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            userDropdownMenu.classList.toggle('show');
        });

        document.addEventListener('click', (e) => {
            if (!userMenuBtn.contains(e.target) && !userDropdownMenu.contains(e.target)) {
                userDropdownMenu.classList.remove('show');
            }
        });
    }

    // Info Tooltips
    document.addEventListener('click', (e) => {
        const btn = e.target.closest('.info-tooltip-btn');
        
        // Hide all tooltips first
        document.querySelectorAll('.info-tooltip-content').forEach(tt => {
            if (btn && tt.previousElementSibling === btn) return;
            tt.classList.remove('show');
            if (tt.previousElementSibling) {
                tt.previousElementSibling.classList.remove('active');
            }
        });

        if (btn) {
            e.stopPropagation();
            const content = btn.nextElementSibling;
            if (content && content.classList.contains('info-tooltip-content')) {
                content.classList.toggle('show');
                btn.classList.toggle('active');
            }
        }
    });
});

function initLiveChart() {
    const canvas = document.getElementById('liveChart');
    if (!canvas) return;

    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    const textColor = isDark ? '#cccccc' : '#495057';
    const gridColor = isDark ? '#333333' : '#e5e5e5';
    
    const successColor = isDark ? '#4caf50' : '#388e3c'; // Green
    const failColor = isDark ? '#f44336' : '#d32f2f'; // Red
    
    const successFill = isDark ? 'rgba(76,175,80,0.3)' : 'rgba(56,142,60,0.2)';
    const failFill = isDark ? 'rgba(244,67,54,0.3)' : 'rgba(211,47,47,0.2)';

    const MAX_POINTS = 60;
    
    const labels = Array(MAX_POINTS).fill('');
    // Fill initial labels with past 60 seconds roughly
    for (let i = 0; i < MAX_POINTS; i++) {
        let d = new Date();
        d.setSeconds(d.getSeconds() - (MAX_POINTS - i));
        labels[i] = d.toLocaleTimeString([], { hour12: false });
    }
    const successData = Array(MAX_POINTS).fill(0);
    const failData = Array(MAX_POINTS).fill(0);
    
    let currentSuccess = 0;
    let currentFail = 0;

    const liveChartInstance = new Chart(canvas.getContext('2d'), {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Başarısız',
                    data: failData,
                    borderColor: failColor,
                    backgroundColor: failFill,
                    borderWidth: 1,
                    fill: true,
                    tension: 0,
                    pointRadius: 0,
                    pointHoverRadius: 4
                },
                {
                    label: 'Başarılı',
                    data: successData,
                    borderColor: successColor,
                    backgroundColor: successFill,
                    borderWidth: 1,
                    fill: true,
                    tension: 0,
                    pointRadius: 0,
                    pointHoverRadius: 4
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 0 },
            interaction: {
                mode: 'index',
                intersect: false,
            },
            plugins: { 
                legend: { display: false },
                tooltip: { enabled: true }
            },
            scales: {
                x: {
                    grid: { color: gridColor },
                    ticks: { color: textColor, maxTicksLimit: 10 }
                },
                y: {
                    stacked: true,
                    grid: { color: gridColor },
                    ticks: { color: textColor, precision: 0 },
                    beginAtZero: true,
                    suggestedMax: 5
                }
            }
        }
    });

    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const ws = new WebSocket(`${protocol}//${window.location.host}/ws/realtime`);
    
    ws.onmessage = (event) => {
        try {
            const data = JSON.parse(event.data);
            if (data.type === 'check_result') {
                if (data.isSuccess) {
                    currentSuccess++;
                } else {
                    currentFail++;
                }
            }
        } catch (e) { }
    };

    setInterval(() => {
        successData.shift();
        failData.shift();
        labels.shift();
        
        successData.push(currentSuccess);
        failData.push(currentFail);
        labels.push(new Date().toLocaleTimeString([], { hour12: false }));
        
        currentSuccess = 0;
        currentFail = 0;
        
        liveChartInstance.update('none');
    }, 1000);
}

async function refreshDashboardStats() {
    try {
        const response = await fetch(`/?handler=Stats&period=${window.currentChartPeriod || 'day'}`);
        if (!response.ok) return;
        const result = await response.json();

        // Update Time
        const timeEl = document.getElementById('last-update-time');
        if (timeEl) timeEl.textContent = new Date().toLocaleTimeString([], { hour12: false });

        // Update Stats Grid
        if (document.getElementById('stat-total')) document.getElementById('stat-total').textContent = result.stats.totalMonitors;
        if (document.getElementById('stat-up')) document.getElementById('stat-up').textContent = result.stats.upMonitors;
        if (document.getElementById('stat-down')) document.getElementById('stat-down').textContent = result.stats.downMonitors;
        if (document.getElementById('stat-incidents')) document.getElementById('stat-incidents').textContent = result.stats.activeIncidents;
        if (document.getElementById('stat-uptime')) document.getElementById('stat-uptime').innerHTML = `${result.stats.last24hUptimePercent}<span style="font-size:1rem;">%</span>`;
        if (document.getElementById('stat-response')) document.getElementById('stat-response').innerHTML = `${result.stats.avgResponseTimeMs}<span style="font-size:0.9rem; color:var(--text-muted);">ms</span>`;

        // Update History Chart
        if (window.chartInstance && result.chartData) {
            window.chartInstance.data.labels = result.chartData.labels;
            window.chartInstance.data.datasets[0].data = result.chartData.failData;
            window.chartInstance.data.datasets[1].data = result.chartData.successData;
            window.chartInstance.update('none');
        }

        // Update Table
        const tbody = document.getElementById('monitor-table-body');
        if (tbody && result.stats.monitors) {
            tbody.innerHTML = '';
            result.stats.monitors.forEach(m => {
                const tr = document.createElement('tr');
                
                let badge = '';
                if (!m.isActive) badge = '<span class="badge badge-paused">⏸ PAUSED</span>';
                else if (m.currentStatus === 1) badge = '<span class="badge badge-up">✓ UP</span>';
                else if (m.currentStatus === 2) badge = '<span class="badge badge-down">✗ DOWN</span>';
                else if (m.currentStatus === 3) badge = '<span class="badge badge-warning">⚠ WARN</span>';
                else badge = '<span class="badge badge-unknown">? -</span>';

                let errorHtml = '';
                if (m.lastErrorMessage && m.currentStatus !== 1) {
                    errorHtml = `<div style="font-size:0.72rem; color:var(--red); margin-top:2px;">${escapeHtml(m.lastErrorMessage)}</div>`;
                }

                let timeStr = '-';
                if (m.lastCheckedAt) {
                    const d = new Date(m.lastCheckedAt);
                    timeStr = d.toLocaleTimeString([], { hour12: false });
                }

                let statusHtml = m.lastStatusCode 
                    ? `<span style="font-family:monospace; color:${m.lastStatusCode < 400 ? 'var(--green)' : 'var(--red)'};">${m.lastStatusCode}</span>` 
                    : '<span style="color:var(--text-muted);">-</span>';

                let respHtml = m.lastResponseTimeMs != null 
                    ? `<span class="response-time ${m.lastResponseTimeMs < 500 ? 'fast' : (m.lastResponseTimeMs < 2000 ? 'medium' : 'slow')}">${m.lastResponseTimeMs} ms</span>` 
                    : '<span style="color:var(--text-muted);">-</span>';

                let upColor = m.uptimePercent24h >= 99 ? 'var(--green)' : (m.uptimePercent24h >= 90 ? 'var(--yellow)' : 'var(--red)');

                let actionHtml = `
                    <div class="actions">
                        <a href="/Monitors/Detail?id=${m.id}" class="btn btn-secondary btn-sm btn-icon" title="Detay">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                        </a>
                        <a href="/Monitors/Edit?id=${m.id}" class="btn btn-secondary btn-sm btn-icon" title="Düzenle">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                        </a>
                    </div>
                `;

                tr.innerHTML = `
                    <td>${badge}</td>
                    <td>
                        <div class="monitor-name">${escapeHtml(m.name)}</div>
                        <div class="url-text">${escapeHtml(m.url)}</div>
                        ${errorHtml}
                    </td>
                    <td style="color:var(--text-muted); font-size:0.8rem;">${timeStr}</td>
                    <td>${statusHtml}</td>
                    <td class="response-time-cell">${respHtml}</td>
                    <td>
                        <span style="color:${upColor}; font-weight:600;">${m.uptimePercent24h}%</span>
                    </td>
                    <td style="color:var(--text-muted); font-size:0.8rem;">${m.contactGroupName || '-'}</td>
                    <td>${actionHtml}</td>
                `;
                tbody.appendChild(tr);
            });
        }
    } catch (e) {
        console.error("Stats refresh failed", e);
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.innerText = text;
    return div.innerHTML;
}
