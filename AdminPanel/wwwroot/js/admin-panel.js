'use strict';

// ── Tab switching ─────────────────────────────────────────

function switchTab(tabName, pushState = true) {
    document.querySelectorAll('.nav-item').forEach(item => {
        item.classList.toggle('active', item.dataset.tab === tabName);
    });

    document.querySelectorAll('.panel').forEach(panel => {
        panel.classList.toggle('active', panel.id === `panel-${tabName}`);
    });

    if (pushState) {
        const url = new URL(window.location.href);
        url.searchParams.set('tab', tabName);
        window.history.pushState({tab: tabName}, '', url);
    }

    if (tabName === 'health') loadHealthData();
    if (tabName === 'currencies') initCurrencyChartsOnce();
    if (tabName === 'games') initGameStatsOnce();
}

// ── Context menus ─────────────────────────────────────────

let _activeCtxMenu = null;
let _ctxRow = null;

const CTX_ITEMS = {
    currency: [
        {label: 'Edit', fn: () => openCurrencyEdit()},
        {label: 'Delete', fn: () => confirmDeleteCurrency(), danger: true}
    ],
    game: [
        {label: 'Edit', fn: () => openGameEdit()},
        {label: 'Delete', fn: () => confirmDeleteGame(), danger: true}
    ],
    user: [
        {label: 'Assign role', fn: () => openSetRole()}
    ]
};

function toggleCtxMenu(btn, type, evt) {
    if (evt) evt.stopPropagation();

    const menu = document.getElementById('ctx-menu');
    const row = btn.closest('.table-data-row');

    if (_activeCtxMenu && menu.classList.contains('open') && _ctxRow === row) {
        menu.classList.remove('open');
        _activeCtxMenu = null;
        return;
    }

    _ctxRow = row;

    menu.innerHTML = (CTX_ITEMS[type] || []).map(item =>
        `<button class="ctx-item${item.danger ? ' ctx-item-danger' : ''}" type="button"
                 onclick="(CTX_ITEMS['${type}'][${(CTX_ITEMS[type] || []).indexOf(item)}].fn)()"
        >${escHtml(item.label)}</button>`
    ).join('');

    const rect = btn.getBoundingClientRect();
    menu.style.top = (rect.bottom + 4) + 'px';
    menu.style.right = (window.innerWidth - rect.right) + 'px';
    menu.classList.add('open');
    _activeCtxMenu = menu;
}

function closeCtxMenus() {
    document.getElementById('ctx-menu')?.classList.remove('open');
    _activeCtxMenu = null;
}

// ── Modal ─────────────────────────────────────────────────

function openModal(title, bodyHtml) {
    closeCtxMenus();
    document.getElementById('modal-header-title').textContent = title;
    document.getElementById('modal-body').innerHTML = bodyHtml;
    document.getElementById('modal-overlay').classList.add('open');
}

function closeModal() {
    document.getElementById('modal-overlay').classList.remove('open');
}

// ── AJAX helpers ──────────────────────────────────────────

async function fetchPost(url, fields) {
    const res = await fetch(url, {
        method: 'POST',
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},
        body: new URLSearchParams(fields)
    });
    return res.json();
}

async function submitAjax(url, fields, successMsg, onSuccess) {
    try {
        const data = await fetchPost(url, fields);
        if (data.ok) {
            closeModal();
            showToast(successMsg, 'success');
            if (onSuccess) await onSuccess();
        } else {
            showToast(data.error || 'Error', 'error');
        }
    } catch (err) {
        showToast(err.message || 'Network error', 'error');
    }
}

// ── Table rendering helpers ───────────────────────────────

function fmtDate(str) {
    if (!str) return '—';
    const d = new Date(str);
    if (isNaN(d)) return str;
    return `${String(d.getDate()).padStart(2, '0')}.${String(d.getMonth() + 1).padStart(2, '0')}.${d.getFullYear()}`;
}

function currencyRowHtml(c) {
    const price = c.price != null
        ? Number(c.price).toLocaleString('en-US', {minimumFractionDigits: 2, maximumFractionDigits: 2})
        : '—';
    return `<div class="table-data-row cols-currency"
         data-id="${ea(c.id)}"
         data-mark="${ea(c.mark ?? '')}"
         data-title="${ea(c.title ?? '')}"
         data-culture="${ea(c.cultureInfo ?? '')}">
        <span class="cell">${escHtml(String(c.id ?? ''))}</span>
        <span class="cell">${escHtml(c.title ?? '')}</span>
        <span class="cell">${escHtml(String(c.steamCurrencyId ?? ''))}</span>
        <span class="cell">${escHtml(c.mark ?? '')}</span>
        <span class="cell">${escHtml(c.cultureInfo ?? '')}</span>
        <span class="cell">${escHtml(price)}</span>
        <span class="cell">${escHtml(fmtDate(c.dateUpdate))}</span>
        <div class="ctx-menu-wrap">
            <button class="btn-row-menu" type="button"
                    onclick="toggleCtxMenu(this,'currency',event)" aria-label="Actions">⋮</button>
        </div>
    </div>`;
}

function gameRowHtml(g) {
    return `<div class="table-data-row cols-game"
         data-id="${ea(g.id)}"
         data-title="${ea(g.title ?? '')}"
         data-icon-url="${ea(g.gameIconUrl ?? '')}">
        <span class="cell"><img src="${escHtml(g.gameIconUrl ?? '')}" alt=""/></span>
        <span class="cell">${escHtml(String(g.id ?? ''))}</span>
        <span class="cell">${escHtml(g.title ?? '')}</span>
        <span class="cell">${escHtml(String(g.steamGameId ?? ''))}</span>
        <a class="cell-link" href="${escHtml(g.gameIconUrl ?? '')}" target="_blank">Icon</a>
        <div class="ctx-menu-wrap">
            <button class="btn-row-menu" type="button"
                    onclick="toggleCtxMenu(this,'game',event)" aria-label="Actions">⋮</button>
        </div>
    </div>`;
}

function userRowHtml(u) {
    return `<div class="table-data-row cols-user"
         data-user-id="${ea(u.userId)}"
         data-nickname="${ea(u.nickname ?? '')}"
         data-steam-id="${ea(u.steamId ?? '')}">
        <span class="cell"><img src="${escHtml(u.imageUrlFull ?? '')}" alt=""/></span>
        <span class="cell">${escHtml(String(u.userId ?? ''))}</span>
        <span class="cell mono">${escHtml(u.steamId ?? '')}</span>
        <span class="cell">${escHtml(u.nickname ?? '')}</span>
        <span class="cell">${escHtml(u.role ?? '')}</span>
        <span class="cell">${escHtml(fmtDate(u.dateRegistration))}</span>
        <a class="cell-link" href="${escHtml(u.profileUrl ?? '')}" target="_blank">Steam</a>
        <div class="ctx-menu-wrap">
            <button class="btn-row-menu" type="button"
                    onclick="toggleCtxMenu(this,'user',event)" aria-label="Actions">⋮</button>
        </div>
    </div>`;
}

// ── Data refresh ──────────────────────────────────────────

async function refreshCurrencies() {
    try {
        const data = await fetch('/admin/AdminPanel/CurrenciesProxy').then(r => r.json());
        renderCurrenciesTable(data.currencies || []);
    } catch { /* ignore */ }
}

async function refreshGames() {
    try {
        const data = await fetch('/admin/AdminPanel/GamesProxy').then(r => r.json());
        renderGamesTable(data.games || []);
    } catch { /* ignore */ }
}

async function loadUsers(page, userId, nickname, steamId) {
    const params = new URLSearchParams({page: page || 1});
    if (userId) params.set('userId', userId);
    if (nickname) params.set('nickname', nickname);
    if (steamId) params.set('steamId', steamId);
    try {
        const data = await fetch('/admin/AdminPanel/UsersProxy?' + params).then(r => r.json());
        renderUsersTable(data, page || 1);
    } catch { /* ignore */ }
}

// ── Table render ──────────────────────────────────────────

function renderCurrenciesTable(currencies) {
    const wrap = document.querySelector('#panel-currencies .table-rows-wrap');
    if (wrap) wrap.innerHTML = currencies.map(c => currencyRowHtml(c)).join('');

    const header = document.querySelector('#panel-currencies .table-header-row span');
    if (header) header.textContent = `Currencies (${currencies.length})`;

    const sel = document.getElementById('currency-chart-select');
    if (sel) {
        sel.innerHTML = currencies.map(c =>
            `<option value="${ea(c.id)}" data-title="${ea(c.title ?? '')}">${escHtml(c.title ?? '')} (${escHtml(c.mark ?? '')})</option>`
        ).join('');
    }

    const jsonEl = document.getElementById('currencies-json');
    if (jsonEl) jsonEl.textContent = JSON.stringify(currencies.map(c => ({id: c.id, title: c.title, mark: c.mark})));

    if (_chartsInited && currencies.length > 0) {
        loadCurrencyDynamics(currencies[0].id, currencies[0].title);
        loadUsersCountByCurrency();
    }
}

function renderGamesTable(games) {
    const wrap = document.querySelector('#panel-games .table-rows-wrap');
    if (wrap) wrap.innerHTML = games.map(g => gameRowHtml(g)).join('');

    const header = document.querySelector('#panel-games .table-header-row span');
    if (header) header.textContent = `Games (${games.length})`;

    const sel = document.getElementById('game-stats-select');
    if (sel) {
        sel.innerHTML = games.map(g =>
            `<option value="${ea(g.id)}">${escHtml(g.title ?? '')}</option>`
        ).join('');
        if (games.length > 0) loadGameStats(sel.value);
    }

    _gameStatsInited = !!games.length;
}

function renderUsersTable(data, currentPage) {
    const users = data.users || [];
    const pagesCount = data.pagesCount || 1;

    const wrap = document.querySelector('#panel-users .table-rows-wrap');
    if (wrap) wrap.innerHTML = users.map(u => userRowHtml(u)).join('');

    const header = document.querySelector('#panel-users .table-header-row span');
    if (header) header.textContent = `Users (${users.length} / page)`;

    const input = document.querySelector('.pagination .page-input');
    if (input) {
        input.max = String(pagesCount);
        input.value = String(currentPage || 1);
    }

    const pageCount = document.querySelector('.page-count');
    if (pageCount) pageCount.textContent = `/ ${pagesCount}`;
}

// ── Currency actions ──────────────────────────────────────

function openAddCurrencyModal() {
    openModal('Add currency', `
        <form id="modal-form">
            <div class="form-grid g-2">
                <label class="form-label">SteamCurrencyId</label>
                <input name="steamCurrencyId" class="form-input" type="text" pattern="\\d*" autofocus/>
                <label class="form-label">Mark</label>
                <input name="mark" class="form-input" type="text"/>
                <label class="form-label">Title</label>
                <input name="title" class="form-input" type="text"/>
                <label class="form-label">CultureInfo</label>
                <input name="cultureInfo" class="form-input" type="text"/>
            </div>
            <input type="submit" class="btn-submit" value="Add"/>
        </form>
    `);
    document.getElementById('modal-form').addEventListener('submit', async e => {
        e.preventDefault();
        const btn = e.target.querySelector('[type=submit]');
        if (btn) btn.disabled = true;
        await submitAjax('/admin/Currencies/AddCurrency', Object.fromEntries(new FormData(e.target)), 'Currency added', refreshCurrencies);
        if (btn) btn.disabled = false;
    });
}

function openCurrencyEdit() {
    const row = _ctxRow;
    closeCtxMenus();
    if (!row) return;
    const {id, mark, title, culture} = row.dataset;
    openModal('Edit currency', `
        <form id="modal-form">
            <input type="hidden" name="currencyId" value="${ea(id)}"/>
            <div class="form-grid g-2">
                <label class="form-label">Mark</label>
                <input name="mark" class="form-input" type="text" value="${ea(mark)}" autofocus/>
                <label class="form-label">Title</label>
                <input name="title" class="form-input" type="text" value="${ea(title)}"/>
                <label class="form-label">CultureInfo</label>
                <input name="cultureInfo" class="form-input" type="text" value="${ea(culture)}"/>
            </div>
            <input type="submit" class="btn-submit" value="Save"/>
        </form>
    `);
    document.getElementById('modal-form').addEventListener('submit', async e => {
        e.preventDefault();
        const btn = e.target.querySelector('[type=submit]');
        if (btn) btn.disabled = true;
        await submitAjax('/admin/Currencies/PutCurrency', Object.fromEntries(new FormData(e.target)), 'Currency updated', refreshCurrencies);
        if (btn) btn.disabled = false;
    });
}

async function confirmDeleteCurrency() {
    const row = _ctxRow;
    closeCtxMenus();
    if (!row) return;
    const {id, title} = row.dataset;
    if (!confirm(`Delete currency "${title}" (#${id})?`)) return;
    await submitAjax('/admin/Currencies/DeleteCurrency', {currencyId: id}, `Currency "${title}" deleted`, refreshCurrencies);
}

// ── Game actions ──────────────────────────────────────────

function openAddGameModal() {
    openModal('Add game', `
        <form id="modal-form">
            <div class="form-grid g-2">
                <label class="form-label">SteamGameId</label>
                <input name="steamGameId" class="form-input" type="text" pattern="\\d*" autofocus/>
                <label class="form-label">IconUrlHash</label>
                <input name="iconUrlHash" class="form-input" type="text"/>
            </div>
            <input type="submit" class="btn-submit" value="Add"/>
        </form>
    `);
    document.getElementById('modal-form').addEventListener('submit', async e => {
        e.preventDefault();
        const btn = e.target.querySelector('[type=submit]');
        if (btn) btn.disabled = true;
        await submitAjax('/admin/Games/AddGame', Object.fromEntries(new FormData(e.target)), 'Game added', refreshGames);
        if (btn) btn.disabled = false;
    });
}

function openGameEdit() {
    const row = _ctxRow;
    closeCtxMenus();
    if (!row) return;
    const {id, title, iconUrl} = row.dataset;
    const hash = iconUrl ? iconUrl.split('/').pop().replace('.jpg', '') : '';
    openModal('Edit game', `
        <form id="modal-form">
            <input type="hidden" name="gameId" value="${ea(id)}"/>
            <div class="form-grid g-2">
                <label class="form-label">Title</label>
                <input name="title" class="form-input" type="text" value="${ea(title)}" autofocus/>
                <label class="form-label">IconUrlHash</label>
                <input name="iconUrlHash" class="form-input" type="text" value="${ea(hash)}"/>
            </div>
            <input type="submit" class="btn-submit" value="Save"/>
        </form>
    `);
    document.getElementById('modal-form').addEventListener('submit', async e => {
        e.preventDefault();
        const btn = e.target.querySelector('[type=submit]');
        if (btn) btn.disabled = true;
        await submitAjax('/admin/Games/PutGame', Object.fromEntries(new FormData(e.target)), 'Game updated', refreshGames);
        if (btn) btn.disabled = false;
    });
}

async function confirmDeleteGame() {
    const row = _ctxRow;
    closeCtxMenus();
    if (!row) return;
    const {id, title} = row.dataset;
    if (!confirm(`Delete game "${title}" (#${id})?`)) return;
    await submitAjax('/admin/Games/DeleteGame', {gameId: id}, `Game "${title}" deleted`, refreshGames);
}

// ── User actions ──────────────────────────────────────────

function openSetRole() {
    const row = _ctxRow;
    closeCtxMenus();
    if (!row) return;
    const {userId, nickname} = row.dataset;
    const rolesData = JSON.parse(document.getElementById('roles-json')?.textContent || '[]');
    const options = rolesData
        .map(r => `<option value="${ea(String(r.id))}">${escHtml(r.title)}</option>`)
        .join('');
    openModal(`Assign role — ${escHtml(nickname || '')} (#${userId})`, `
        <form id="modal-form">
            <input type="hidden" name="userId" value="${ea(userId)}"/>
            <div class="form-grid g-2">
                <label class="form-label">Role</label>
                <select name="roleId" class="form-input form-select">${options}</select>
            </div>
            <input type="submit" class="btn-submit" value="Apply"/>
        </form>
    `);
    document.getElementById('modal-form').addEventListener('submit', async e => {
        e.preventDefault();
        const btn = e.target.querySelector('[type=submit]');
        if (btn) btn.disabled = true;
        await submitAjax('/admin/Users/SetRole', Object.fromEntries(new FormData(e.target)), `Role assigned to ${nickname || userId}`, null);
        if (btn) btn.disabled = false;
    });
}

// ── Utilities ─────────────────────────────────────────────

function ea(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

function escHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

// ── Game stats ────────────────────────────────────────────

let _gameStatsInited = false;

function initGameStatsOnce() {
    if (_gameStatsInited) return;
    _gameStatsInited = true;
    const sel = document.getElementById('game-stats-select');
    if (sel?.value) loadGameStats(sel.value);
}

async function loadGameStats(gameId) {
    if (!gameId) return;
    const skinsEl = document.getElementById('game-skins-count');
    const itemsEl = document.getElementById('game-items-count');
    if (skinsEl) skinsEl.textContent = '...';
    if (itemsEl) itemsEl.textContent = '...';
    try {
        const res = await fetch(`/admin/AdminPanel/GameStatsProxy?gameId=${gameId}`);
        const data = await res.json();
        if (skinsEl) skinsEl.textContent = data.skinsCount != null ? data.skinsCount.toLocaleString('en-US') : '—';
        if (itemsEl) itemsEl.textContent = data.itemsCount != null ? data.itemsCount.toLocaleString('en-US') : '—';
    } catch {
        if (skinsEl) skinsEl.textContent = '—';
        if (itemsEl) itemsEl.textContent = '—';
    }
}

// ── Currency charts ───────────────────────────────────────

let _dynamicsChart = null;
let _usersChart = null;
let _chartsInited = false;

function initCurrencyChartsOnce() {
    if (_chartsInited || typeof Chart === 'undefined') return;
    _chartsInited = true;

    const currData = JSON.parse(document.getElementById('currencies-json')?.textContent || '[]');
    const palette = ['#8b5cf6', '#22d3ee', '#f472b6', '#34d399', '#fbbf24', '#60a5fa', '#a78bfa', '#fb923c'];
    const colors = currData.map((_, i) => palette[i % palette.length]);
    const gridColor = 'rgba(139, 92, 246, 0.08)';
    const tickColor = '#6b6e94';
    const tickFont = {family: "'JetBrains Mono', monospace", size: 10};

    const dynCtx = document.getElementById('currency-dynamics-chart');
    const usersCtx = document.getElementById('currency-users-chart');

    if (dynCtx) {
        _dynamicsChart = new Chart(dynCtx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: '',
                    data: [],
                    borderColor: '#8b5cf6',
                    backgroundColor: 'rgba(139, 92, 246, 0.08)',
                    tension: 0.4,
                    fill: true,
                    borderWidth: 2,
                    pointRadius: 3,
                    pointBackgroundColor: '#8b5cf6'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {display: false},
                    tooltip: {mode: 'index', intersect: false}
                },
                scales: {
                    x: {grid: {color: gridColor}, ticks: {color: tickColor, font: tickFont}},
                    y: {grid: {color: gridColor}, ticks: {color: tickColor, font: tickFont}}
                }
            }
        });
    }

    if (usersCtx) {
        _usersChart = new Chart(usersCtx, {
            type: 'doughnut',
            data: {
                labels: currData.map(c => c.title),
                datasets: [{
                    data: currData.map(() => 0),
                    backgroundColor: colors,
                    borderColor: 'rgba(10, 10, 20, 0.3)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            color: '#a8a8c0',
                            font: {family: "'JetBrains Mono', monospace", size: 11},
                            boxWidth: 12,
                            padding: 14
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: ctx => `${ctx.label}: ${ctx.parsed}`
                        }
                    }
                }
            }
        });
    }

    const sel = document.getElementById('currency-chart-select');
    if (sel) {
        sel.addEventListener('change', () => {
            const opt = sel.selectedOptions[0];
            loadCurrencyDynamics(sel.value, opt?.dataset.title || '');
        });
    }

    if (currData.length > 0) loadCurrencyDynamics(currData[0].id, currData[0].title);

    loadUsersCountByCurrency();
}

async function loadUsersCountByCurrency() {
    if (!_usersChart) return;
    try {
        const res = await fetch('/admin/AdminPanel/UsersCountByCurrencyProxy');
        const data = await res.json();
        const items = data.items || [];
        const currData = JSON.parse(document.getElementById('currencies-json')?.textContent || '[]');
        _usersChart.data.datasets[0].data = currData.map(c => {
            const found = items.find(i => i.currencyId === c.id);
            return found ? found.usersCount : 0;
        });
        _usersChart.update();
    } catch { /* ignore */
    }
}

async function loadCurrencyDynamics(currencyId, title) {
    if (!_dynamicsChart) return;
    try {
        const res = await fetch(`/admin/AdminPanel/CurrencyDynamicsProxy?currencyId=${currencyId}`);
        const data = await res.json();
        const points = data.dynamic || [];
        _dynamicsChart.data.labels = points.map(p => new Date(p.dateUpdate).toLocaleDateString('en-US'));
        _dynamicsChart.data.datasets[0].data = points.map(p => p.exchangeRate);
        _dynamicsChart.data.datasets[0].label = title || '';
        _dynamicsChart.update();
    } catch { /* ignore */
    }
}

// ── Health checks ─────────────────────────────────────────

async function loadHealthData() {
    const content = document.getElementById('health-content');
    const btn = document.getElementById('health-refresh');
    const updated = document.getElementById('health-last-updated');

    if (!content) return;

    setHealthLoading(content, btn);

    try {
        const res = await fetch('/admin/AdminPanel/HealthProxy');
        const data = await res.json();

        renderHealthData(content, data);

        if (updated)
            updated.textContent = `Updated: ${new Date().toLocaleTimeString('en-US')}`;
    } catch (err) {
        content.innerHTML = errorCard(err.message);
    } finally {
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = '<span class="btn-refresh-icon">⟳</span> Refresh';
        }
    }
}

function setHealthLoading(content, btn) {
    const names = ['api', 'database', 'steam-market', 'steam-profile'];
    content.innerHTML = `<div class="health-grid">${names.map(n => loadingCard(n)).join('')}</div>`;
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<span class="btn-refresh-icon spin">⟳</span> Loading...';
    }
}

function loadingCard(name) {
    return `<div class="health-card loading">
        <div class="health-card-title"><div class="health-indicator"></div><span class="health-name">${name}</span></div>
        <span class="health-status-text">Loading...</span>
    </div>`;
}

function errorCard(msg) {
    return `<div class="health-card unhealthy" style="grid-column:1/-1">
        <div class="health-card-title"><div class="health-indicator"></div><span class="health-name">Connection error</span></div>
        <span class="health-status-text">Unavailable</span>
        <span class="health-duration">${escHtml(msg)}</span>
    </div>`;
}

function renderHealthData(content, data) {
    const entries = data.entries || {};
    const overall = (data.status || 'unknown').toLowerCase();

    const overallCard = `
        <div class="health-card ${overall} health-card-overall">
            <div class="health-card-title">
                <div class="health-indicator"></div>
                <span class="health-name">Overall</span>
            </div>
            <span class="health-status-text">${data.status || '—'}</span>
            <span class="health-duration">⏱ ${fmtDuration(data.totalDuration)}</span>
        </div>`;

    const cards = Object.entries(entries).map(([name, info]) => {
        const st = (info.status || 'unknown').toLowerCase();
        const tags = (info.tags || []).map(t => `<span class="health-tag">${escHtml(t)}</span>`).join('');
        return `
            <div class="health-card ${st}">
                <div class="health-card-title">
                    <div class="health-indicator"></div>
                    <span class="health-name">${escHtml(name)}</span>
                </div>
                <span class="health-status-text">${escHtml(info.status || '—')}</span>
                <span class="health-duration">⏱ ${fmtDuration(info.duration)}</span>
                ${tags ? `<div class="health-tags">${tags}</div>` : ''}
            </div>`;
    }).join('');

    const rows = Object.entries(entries).map(([name, info]) => {
        const st = (info.status || '').toLowerCase();
        const desc = info.description || info.exception || fmtData(info.data);
        return `
            <div class="health-detail-row">
                <span class="health-detail-cell name">${escHtml(name)}</span>
                <span class="health-status-text ${st}">${escHtml(info.status || '—')}</span>
                <span class="health-detail-cell mono">${fmtDuration(info.duration)}</span>
                <span class="health-detail-cell muted">${escHtml(desc)}</span>
            </div>`;
    }).join('');

    content.innerHTML = `
        <div class="health-grid">${overallCard}${cards}</div>
        <div class="health-detail-card">
            <div class="health-detail-header">Details</div>
            <div class="health-detail-list">
                ${rows || '<div class="health-empty">No data</div>'}
            </div>
        </div>`;
}

function fmtDuration(d) {
    if (!d) return '—';
    const parts = d.split(':');
    if (parts.length < 3) return d;
    const sec = parseFloat(parts[2]);
    return sec < 1 ? `${(sec * 1000).toFixed(1)} ms` : `${sec.toFixed(2)} s`;
}

function fmtData(data) {
    if (!data || !Object.keys(data).length) return '';
    return Object.entries(data).map(([k, v]) => `${k}: ${v}`).join(', ');
}

// ── Toast notifications ───────────────────────────────────

function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    document.body.appendChild(toast);

    requestAnimationFrame(() => requestAnimationFrame(() => toast.classList.add('show')));

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 400);
    }, 4000);
}

// ── Pagination ────────────────────────────────────────────

function initPagination() {
    const input = document.querySelector('.pagination .page-input');
    const plus = document.querySelector('.pagination .btn-page.plus');
    const minus = document.querySelector('.pagination .btn-page.minus');

    if (!input) return;

    const getFilters = () => ({
        userId: document.querySelector('.user-search-form [name=userId]')?.value || '',
        nickname: document.querySelector('.user-search-form [name=nickname]')?.value || '',
        steamId: document.querySelector('.user-search-form [name=steamId]')?.value || ''
    });

    const go = () => {
        const {userId, nickname, steamId} = getFilters();
        loadUsers(input.valueAsNumber || 1, userId, nickname, steamId);
    };

    input.addEventListener('change', go);

    plus?.addEventListener('click', () => {
        const max = parseInt(input.max) || Infinity;
        if (input.valueAsNumber < max) {
            input.stepUp();
            go();
        }
    });

    minus?.addEventListener('click', () => {
        if (input.valueAsNumber > 1) {
            input.stepDown();
            go();
        }
    });
}

// ── Init ──────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    const shell = document.querySelector('.admin-shell');
    const initTab = shell?.dataset.activeTab || 'currencies';

    switchTab(initTab, false);

    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', () => switchTab(item.dataset.tab));
    });

    window.addEventListener('popstate', e => {
        switchTab(e.state?.tab || 'currencies', false);
    });

    document.addEventListener('click', () => closeCtxMenus());

    document.getElementById('modal-overlay')?.addEventListener('click', e => {
        if (e.target.id === 'modal-overlay') closeModal();
    });

    document.getElementById('health-refresh')?.addEventListener('click', loadHealthData);

    document.getElementById('game-stats-select')?.addEventListener('change', e => {
        loadGameStats(e.target.value);
    });

    // Job forms — intercept to avoid page reload
    document.querySelectorAll('#panel-jobs form').forEach(form => {
        form.addEventListener('submit', async e => {
            e.preventDefault();
            const btn = form.querySelector('button[type=submit]');
            if (btn) { btn.disabled = true; btn.textContent = '...'; }
            try {
                const res = await fetch(form.action, {method: 'POST'});
                const data = await res.json();
                if (data.ok) showToast(data.message || 'Job triggered', 'success');
                else showToast(data.error || 'Error', 'error');
            } catch {
                showToast('Network error', 'error');
            } finally {
                if (btn) { btn.disabled = false; btn.textContent = '▶ Run'; }
            }
        });
    });

    // Server-side toast (after hard navigation, e.g. auth redirect)
    const toastEl = document.getElementById('server-toast');
    if (toastEl) showToast(toastEl.dataset.message, toastEl.dataset.type || 'info');

    initPagination();
});
