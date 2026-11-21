const formatNumber = (n) => new Intl.NumberFormat('pt-BR').format(Number(n || 0));

function setServiceBadge(id, isOnline) {
  const el = document.getElementById(id);
  if (!el) return;
  el.textContent = isOnline ? 'Online' : 'Offline';
  el.className = `dot ${isOnline ? 'online' : 'offline'}`;
}

async function fetchStatus() {
  try {
    const res = await fetch('/api/status');
    const data = await res.json();

    document.getElementById('server-name').textContent = data.serverName;
    document.getElementById('packetver-label').textContent = data.packetver;
    document.getElementById('preset-badge').textContent = `${data.preset.label} · ${data.preset.rates}`;
    document.getElementById('port-login').textContent = data.ports.login;
    document.getElementById('port-char').textContent = data.ports.char;
    document.getElementById('port-map').textContent = data.ports.map;
    document.getElementById('endpoint-ip').textContent = `${data.publicIp}:${data.ports.login}`;

    setServiceBadge('svc-login', data.services.login);
    setServiceBadge('svc-char', data.services.char);
    setServiceBadge('svc-map', data.services.map);
    setServiceBadge('svc-db', data.services.database);

    document.getElementById('stat-online').textContent = formatNumber(data.stats.online);
    document.getElementById('stat-accounts').textContent = formatNumber(data.stats.accounts);
    document.getElementById('stat-characters').textContent = formatNumber(data.stats.characters);
    document.getElementById('stat-zeny').textContent = `${formatNumber(data.stats.zenyCirculation)} z`;
  } catch {
    setServiceBadge('svc-login', false);
    setServiceBadge('svc-char', false);
    setServiceBadge('svc-map', false);
    setServiceBadge('svc-db', false);
  }
}

async function fetchRanking() {
  const tbody = document.getElementById('ranking-body');
  try {
    const res = await fetch('/api/ranking?limit=30');
    const data = await res.json();
    if (!res.ok) {
      tbody.innerHTML = `<tr><td colspan="7" class="muted center">${data.error || 'Erro ao carregar ranking.'}</td></tr>`;
      return;
    }

    if (!data.characters || data.characters.length === 0) {
      tbody.innerHTML = '<tr><td colspan="7" class="muted center">Nenhum personagem criado ainda. Crie sua conta e entre no jogo!</td></tr>';
      return;
    }

    tbody.innerHTML = data.characters
      .map(
        (c, idx) => `
        <tr>
          <td>${idx + 1}</td>
          <td><strong>${escapeHtml(c.name)}</strong></td>
          <td>${escapeHtml(c.className)}</td>
          <td>${c.baseLevel} / ${c.jobLevel}</td>
          <td><code>${escapeHtml(c.lastMap || 'prontera')}</code></td>
          <td>${formatNumber(c.zeny)} z</td>
          <td><span class="dot ${c.online ? 'online' : 'offline'}">${c.online ? 'Online' : 'Offline'}</span></td>
        </tr>`
      )
      .join('');
  } catch {
    tbody.innerHTML = '<tr><td colspan="7" class="muted center">Não foi possível consultar o banco de dados.</td></tr>';
  }
}

function escapeHtml(text) {
  return String(text ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function setupTabs() {
  const tabs = document.querySelectorAll('.tab');
  const panels = document.querySelectorAll('.tab-panel');
  tabs.forEach((btn) => {
    btn.addEventListener('click', () => {
      tabs.forEach((b) => b.classList.remove('active'));
      panels.forEach((p) => p.classList.remove('active'));
      btn.classList.add('active');
      const panel = document.getElementById(`panel-${btn.dataset.tab}`);
      if (panel) panel.classList.add('active');
      if (btn.dataset.tab === 'ranking') fetchRanking();
    });
  });
}

function setupThemeToggle() {
  const btn = document.getElementById('theme-toggle');
  const root = document.documentElement;
  btn.addEventListener('click', () => {
    const next = root.dataset.theme === 'obsidian' ? 'parchment' : 'obsidian';
    root.dataset.theme = next;
    btn.textContent = next === 'obsidian' ? 'Tema: Obsidiana' : 'Tema: Pergaminho';
  });
}

function setupRegistrationForm() {
  const form = document.getElementById('register-form');
  const feedback = document.getElementById('register-feedback');

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    feedback.className = 'feedback';
    feedback.textContent = 'Criando conta...';

    const fd = new FormData(form);
    const payload = Object.fromEntries(fd.entries());

    try {
      const res = await fetch('/api/accounts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      const data = await res.json();
      if (!res.ok) {
        feedback.className = 'feedback err';
        feedback.textContent = data.error || 'Falha ao criar conta.';
        return;
      }

      feedback.className = 'feedback ok';
      feedback.textContent = `Conta '${data.userid}' criada com sucesso (${data.role}, ID #${data.accountId})!`;
      form.reset();
      fetchStatus();
    } catch {
      feedback.className = 'feedback err';
      feedback.textContent = 'Erro de comunicação com a API do painel.';
    }
  });
}

function buildToolsQuery() {
  const params = new URLSearchParams();
  params.set('name', document.getElementById('tools-name').value.trim() || 'Ragexe');
  params.set('hpAddress', document.getElementById('tools-hp').value.trim() || '0x015FF908');
  params.set('nameAddress', document.getElementById('tools-name-addr').value.trim() || '0x01602568');
  const mapAddr = document.getElementById('tools-map-addr').value.trim();
  if (mapAddr) params.set('mapAddress', mapAddr);
  return `/api/supported-servers.json?${params.toString()}`;
}

function setupToolsForm() {
  const form = document.getElementById('tools-form');
  const copyBtn = document.getElementById('copy-tools-json');

  form.addEventListener('submit', (e) => {
    e.preventDefault();
    window.location.href = buildToolsQuery();
  });

  copyBtn.addEventListener('click', async () => {
    const res = await fetch(buildToolsQuery());
    const text = await res.text();
    await navigator.clipboard.writeText(text);
    const original = copyBtn.textContent;
    copyBtn.textContent = 'JSON copiado!';
    setTimeout(() => {
      copyBtn.textContent = original;
    }, 1800);
  });
}

document.getElementById('refresh-ranking').addEventListener('click', fetchRanking);

setupTabs();
setupThemeToggle();
setupRegistrationForm();
setupToolsForm();
fetchStatus();
setInterval(fetchStatus, 15000);
