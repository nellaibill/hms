const LOG_CHANNELS = ['fullsetup', 'git', 'db', 'backend', 'frontend', 'tenant'];

async function api(method, url, body) {
  const res = await fetch(url, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok || data.ok === false) {
    throw new Error(data.error || `Request to ${url} failed`);
  }
  return data;
}

function setDeep(obj, dottedKey, value) {
  const keys = dottedKey.split('.');
  let target = obj;
  for (let i = 0; i < keys.length - 1; i++) {
    target = target[keys[i]] = target[keys[i]] || {};
  }
  target[keys[keys.length - 1]] = value;
}

function getDeep(obj, dottedKey) {
  return dottedKey.split('.').reduce((o, k) => (o == null ? undefined : o[k]), obj);
}

// ---- Logs ----

function renderLine(entry) {
  const cls = entry.level === 'error' ? 'line-error' : entry.level === 'ok' ? 'line-ok' : entry.level === 'cmd' ? 'line-cmd' : '';
  const span = document.createElement('div');
  if (cls) span.className = cls;
  span.textContent = entry.line;
  return span;
}

function loadLogBuffer(channel) {
  const el = document.getElementById(`log-${channel}`);
  if (!el) return;
  fetch(`/api/logs/${channel}`)
    .then((r) => r.json())
    .then((entries) => {
      el.innerHTML = '';
      entries.forEach((entry) => el.appendChild(renderLine(entry)));
      el.scrollTop = el.scrollHeight;
    });
}

// A single shared EventSource for every log panel — see lib/logs.js for why this must not be
// one-per-channel (browsers cap concurrent connections per origin, and a permanently-open
// stream per channel would exhaust that cap and starve every other fetch on the page).
function initLogStream() {
  const source = new EventSource('/api/logs/stream');
  source.onmessage = (event) => {
    const entry = JSON.parse(event.data);
    const el = document.getElementById(`log-${entry.channel}`);
    if (!el) return;
    el.appendChild(renderLine(entry));
    while (el.childNodes.length > 1000) el.removeChild(el.firstChild);
    el.scrollTop = el.scrollHeight;
  };
}

// ---- Status cards ----

function dot(cls) {
  return `<span class="dot ${cls}"></span>`;
}

async function refreshGitStatus() {
  const el = document.querySelector('#status-git .status-body');
  try {
    const { status } = await api('GET', '/api/git/status');
    if (!status.ok) {
      el.innerHTML = `${dot('dot-err')}${escapeHtml(status.error)}`;
      return;
    }
    const behindAhead = status.hasUpstream ? `${status.behind} behind / ${status.ahead} ahead` : 'no upstream';
    const dirty = status.dirtyCount > 0 ? `${status.dirtyCount} uncommitted` : 'clean';
    el.innerHTML = `${dot(status.dirtyCount > 0 ? 'dot-warn' : 'dot-ok')}${escapeHtml(status.branch)}<br>${behindAhead}, ${dirty}`;
  } catch (err) {
    el.innerHTML = `${dot('dot-err')}${escapeHtml(err.message)}`;
  }
}

async function refreshDbStatus() {
  const el = document.querySelector('#status-db .status-body');
  try {
    const { status } = await api('GET', '/api/db/status');
    if (!status.serverReachable) {
      el.innerHTML = `${dot('dot-err')}Postgres unreachable${status.error ? `<br>${escapeHtml(status.error)}` : ''}`;
      return;
    }
    const platform = status.platformDbExists ? 'exists' : 'missing';
    const tenant = status.defaultDbExists ? 'exists' : 'missing';
    const allGood = status.platformDbExists && status.defaultDbExists;
    el.innerHTML = `${dot(allGood ? 'dot-ok' : 'dot-warn')}Platform DB: ${platform}<br>Tenant DB: ${tenant}`;
  } catch (err) {
    el.innerHTML = `${dot('dot-err')}${escapeHtml(err.message)}`;
  }
}

async function refreshBackendStatus() {
  const el = document.querySelector('#status-backend .status-body');
  try {
    const { status } = await api('GET', '/api/backend/status');
    const running = status.status === 'running';
    el.innerHTML = `${dot(running && status.health === 'up' ? 'dot-ok' : running ? 'dot-warn' : 'dot-unknown')}Process: ${status.status}${status.pid ? ` (pid ${status.pid})` : ''}<br>Health: ${status.health}`;
  } catch (err) {
    el.innerHTML = `${dot('dot-err')}${escapeHtml(err.message)}`;
  }
}

async function refreshFrontendStatus() {
  const el = document.querySelector('#status-frontend .status-body');
  try {
    const { status } = await api('GET', '/api/frontend/status');
    const running = status.status === 'running';
    el.innerHTML = `${dot(running && status.reachable === 'up' ? 'dot-ok' : running ? 'dot-warn' : 'dot-unknown')}Process: ${status.status}${status.pid ? ` (pid ${status.pid})` : ''}<br>Reachable: ${status.reachable}`;
  } catch (err) {
    el.innerHTML = `${dot('dot-err')}${escapeHtml(err.message)}`;
  }
}

function refreshAllStatus() {
  refreshGitStatus();
  refreshDbStatus();
  refreshBackendStatus();
  refreshFrontendStatus();
}

function escapeHtml(str) {
  const div = document.createElement('div');
  div.textContent = str;
  return div.innerHTML;
}

// ---- Actions ----

function wireActionButtons() {
  document.querySelectorAll('button[data-action]').forEach((btn) => {
    btn.addEventListener('click', async () => {
      btn.disabled = true;
      const original = btn.textContent;
      btn.textContent = 'Working…';
      try {
        await api('POST', `/api/${btn.dataset.action}`);
      } catch (err) {
        alert(err.message);
      } finally {
        btn.disabled = false;
        btn.textContent = original;
        refreshAllStatus();
      }
    });
  });

  document.getElementById('fullSetupBtn').addEventListener('click', async (e) => {
    e.target.disabled = true;
    try {
      await api('POST', '/api/full-setup');
    } catch (err) {
      alert(err.message);
    } finally {
      setTimeout(() => { e.target.disabled = false; }, 2000);
    }
  });

  document.getElementById('tenantForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const formData = new FormData(e.target);
    const fields = Object.fromEntries(formData.entries());
    const submitBtn = e.target.querySelector('button[type=submit]');
    submitBtn.disabled = true;
    try {
      await api('POST', '/api/tenant/create', fields);
      alert('Hospital created.');
      e.target.reset();
    } catch (err) {
      alert(err.message);
    } finally {
      submitBtn.disabled = false;
    }
  });
}

// ---- Settings ----

async function openSettings() {
  const config = await api('GET', '/api/config');
  const form = document.getElementById('settingsForm');
  form.querySelectorAll('input').forEach((input) => {
    const value = getDeep(config, input.name);
    if (input.type === 'checkbox') {
      input.checked = !!value;
    } else {
      input.value = value ?? '';
    }
  });
  document.getElementById('importEnvResult').textContent = '';
  document.getElementById('settingsOverlay').classList.remove('hidden');
}

function closeSettings() {
  document.getElementById('settingsOverlay').classList.add('hidden');
}

function wireSettings() {
  document.getElementById('settingsBtn').addEventListener('click', openSettings);
  document.getElementById('closeSettingsBtn').addEventListener('click', closeSettings);
  document.getElementById('settingsOverlay').addEventListener('click', (e) => {
    if (e.target.id === 'settingsOverlay') closeSettings();
  });

  document.getElementById('settingsForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const form = e.target;
    const partial = {};
    form.querySelectorAll('input').forEach((input) => {
      const value = input.type === 'checkbox' ? input.checked : input.type === 'number' ? Number(input.value) : input.value;
      setDeep(partial, input.name, value);
    });
    await api('POST', '/api/config', partial);
    closeSettings();
    await loadBasePathLabel();
    refreshAllStatus();
  });

  document.getElementById('importEnvBtn').addEventListener('click', async () => {
    const resultEl = document.getElementById('importEnvResult');
    try {
      const result = await api('POST', '/api/config/import-env');
      resultEl.textContent = result.imported ? `Imported. ${result.note || ''}` : result.reason;
      if (result.imported) await openSettings();
    } catch (err) {
      resultEl.textContent = err.message;
    }
  });
}

async function loadBasePathLabel() {
  const config = await api('GET', '/api/config');
  document.getElementById('basePathLabel').textContent = config.basePath;
}

// ---- Init ----

LOG_CHANNELS.forEach(loadLogBuffer);
initLogStream();
wireActionButtons();
wireSettings();
loadBasePathLabel();
refreshAllStatus();
setInterval(refreshAllStatus, 5000);
