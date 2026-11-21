const formatNumber = (n) => new Intl.NumberFormat('pt-BR').format(Number(n || 0));

const HUB_NPCS = [
  { name: 'Guia AureumRO', x: 156, y: 195, wing: 'center', wingLabel: 'Centro', tag: '* AureumRO Hub *', desc: 'NPC central com mapa das alas, guia rápido do 4Tools, lista de comandos e teleporte direto para a Arena.' },
  { name: 'Curandeira AureumRO', x: 152, y: 193, wing: 'west', wingLabel: 'Ala Oeste', tag: '[ Curandeira ]', desc: 'Cura 100% HP/SP, buffs completos (Bênção, Agi, Assumptio) e modo sem buffs para testar Autobuff no 4Tools.' },
  { name: 'Mestra das Classes', x: 150, y: 191, wing: 'west', wingLabel: 'Ala Oeste', tag: '[ Classes 99/70 ]', desc: 'Troca direta para qualquer Transclasse ou Expandida 99/70 com @allskill maximizado, Reset e Aluguel.' },
  { name: 'Arsenal Divino (+10)', x: 148, y: 193, wing: 'west', wingLabel: 'Ala Oeste', tag: '[ Arsenal Divino +10 ]', desc: 'Kits completos por classe com Itens Divinos, equipamentos +10, cartas MVP encaixadas e kit ATK x DEF.' },
  { name: 'Caixa do Iniciante', x: 148, y: 189, wing: 'west', wingLabel: 'Ala Oeste', tag: '[ Kit Iniciante ]', desc: 'Entrega suprimentos iniciais, Poções, Manuais de Combate, Goma de Mascar e Zeny inicial.' },
  { name: 'Ferreiro & Cartas MVP', x: 146, y: 195, wing: 'west', wingLabel: 'Ala Oeste', tag: '[ Refino & MVP ]', desc: 'Refinador seguro até +10 sem quebra, Removedor de Cartas grátis e catálogo de Cartas MVP.' },
  { name: 'Mestre das Builds', x: 146, y: 191, wing: 'west', wingLabel: 'Ala Oeste', tag: '[ Builds & Stats ]', desc: 'Aplica presets de atributos em 1 clique (WoE/MVP/Crit/Cast) e salva/carrega até 3 builds por personagem.' },
  { name: 'Teleportadora AureumRO', x: 160, y: 193, wing: 'east', wingLabel: 'Ala Leste', tag: '[ Teleporte ]', desc: 'Teleporte para cidades, campos, dungeons Pré-Renewal (Bio3, Thor, Nameless) e El Dicastes (Ep. 13.3).' },
  { name: 'Estilista AureumRO', x: 162, y: 191, wing: 'east', wingLabel: 'Ala Leste', tag: '[ Estilista ]', desc: 'Personalização de cabelo e cores com Modo Seguro (0..3) para kRO e Modo Expandido (0..553).' },
  { name: 'Sistemas AureumRO', x: 164, y: 193, wing: 'east', wingLabel: 'Ala Leste', tag: '[ MVP / ET / WoE ]', desc: 'Sala MVP Privada (guild_vs3), reset de cooldown da Torre Sem Fim, WoE sob demanda e ranking de abates.' },
  { name: 'Kafra & Lixeira', x: 164, y: 189, wing: 'east', wingLabel: 'Ala Leste', tag: '[ Kafra & QoL ]', desc: 'Armazém universal, Ponto de Retorno, Lixeira instantânea e reabastecimento rápido (@restock).' },
  { name: 'Roleta & Visuais', x: 166, y: 195, wing: 'east', wingLabel: 'Ala Leste', tag: '[ Roleta & Visuais ]', desc: 'Costumes na aba Visual, Auras Contínuas persistentes no login, Roleta Poring e Homúnculos Express.' },
  { name: 'Instrutor de Debuffs', x: 152, y: 185, wing: 'south', wingLabel: 'Ala Sul · 4Tools', tag: '[ Debuffs 4Tools ]', desc: 'Aplica dano controlado, pulsos contínuos, debuffs clássicos e debuffs de Doce de Elvira para testar Autopot.' },
  { name: 'Suprimentos 4Tools', x: 156, y: 185, wing: 'south', wingLabel: 'Ala Sul · 4Tools', tag: '[ Suprimentos ]', desc: 'Entrega grátis de Poções, Yggdrasil, Panaceia, Água Benta, Doce de Elvira (23044), Gemas e 10M Zeny.' },
  { name: 'Arena de Treino 4Tools', x: 160, y: 185, wing: 'south', wingLabel: 'Ala Sul · 4Tools', tag: '[ Arena 4Tools ]', desc: 'Entrada para pvp_n_1-5 (fora de cidade) com Dummies 5M HP, Medidor de DPS em tempo real e Stress Test.' }
];

function showToast(ok, message) {
  const container = document.getElementById('toast-container');
  if (!container) return;
  const toast = document.createElement('div');
  toast.className = `toast ${ok ? 'toast-ok' : 'toast-err'}`;
  toast.innerHTML = `<span class="toast-icon">${ok ? '✦' : '✕'}</span><span>${escapeHtml(message)}</span>`;
  container.appendChild(toast);
  requestAnimationFrame(() => toast.classList.add('show'));
  setTimeout(() => {
    toast.classList.remove('show');
    setTimeout(() => toast.remove(), 300);
  }, 3600);
}

function startServerClock() {
  const el = document.getElementById('server-clock-time');
  if (!el) return;
  const tick = () => {
    el.textContent = new Date().toLocaleTimeString('pt-BR', { hour12: false });
  };
  tick();
  setInterval(tick, 1000);
}

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

function renderClassChart(characters) {
  const container = document.getElementById('class-chart');
  const totalEl = document.getElementById('class-chart-total');
  if (!container) return;

  const list = Array.isArray(characters) ? characters : [];
  if (totalEl) totalEl.textContent = `${list.length} ${list.length === 1 ? 'personagem' : 'personagens'}`;

  if (list.length === 0) {
    container.innerHTML = '<p class="muted">Nenhum personagem criado ainda para calcular a distribuição.</p>';
    return;
  }

  const counts = new Map();
  for (const c of list) {
    const cls = c.className || 'Desconhecida';
    counts.set(cls, (counts.get(cls) || 0) + 1);
  }

  const sorted = [...counts.entries()].sort((a, b) => b[1] - a[1]).slice(0, 10);
  const max = sorted[0][1] || 1;

  container.innerHTML = sorted
    .map(([cls, count]) => {
      const pct = Math.round((count / list.length) * 100);
      const width = Math.max(8, Math.round((count / max) * 100));
      return `
        <div class="chart-row">
          <span class="chart-label">${escapeHtml(cls)}</span>
          <div class="chart-track">
            <div class="chart-bar" style="width: ${width}%"></div>
          </div>
          <span class="chart-meta">${count} (${pct}%)</span>
        </div>`;
    })
    .join('');
}

async function fetchRanking() {
  const tbody = document.getElementById('ranking-body');
  try {
    const res = await fetch('/api/ranking?limit=30');
    const data = await res.json();
    if (!res.ok) {
      tbody.innerHTML = `<tr><td colspan="7" class="muted center">${data.error || 'Erro ao carregar ranking.'}</td></tr>`;
      renderClassChart([]);
      return;
    }

    renderClassChart(data.characters || []);

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
    renderClassChart([]);
  }
}

function setupHubMap() {
  const canvas = document.getElementById('prontera-hub-map');
  if (!canvas) return;

  const wingEl = document.getElementById('hub-npc-wing');
  const nameEl = document.getElementById('hub-npc-name');
  const coordsEl = document.getElementById('hub-npc-coords');
  const tagEl = document.getElementById('hub-npc-tag');
  const descEl = document.getElementById('hub-npc-desc');

  const minX = 144;
  const maxX = 168;
  const minY = 183;
  const maxY = 197;

  const selectNpc = (npc, btn) => {
    canvas.querySelectorAll('.hub-pin').forEach((p) => p.classList.remove('active'));
    if (btn) btn.classList.add('active');
    if (wingEl) wingEl.textContent = npc.wingLabel;
    if (nameEl) nameEl.textContent = npc.name;
    if (coordsEl) coordsEl.textContent = `prontera (${npc.x}, ${npc.y})`;
    if (tagEl) tagEl.textContent = npc.tag;
    if (descEl) descEl.textContent = npc.desc;
  };

  HUB_NPCS.forEach((npc, index) => {
    const leftPct = ((npc.x - minX) / (maxX - minX)) * 84 + 8;
    const topPct = ((maxY - npc.y) / (maxY - minY)) * 80 + 10;

    const pin = document.createElement('button');
    pin.type = 'button';
    pin.className = `hub-pin wing-${npc.wing}${index === 0 ? ' active' : ''}`;
    pin.style.left = `${leftPct.toFixed(1)}%`;
    pin.style.top = `${topPct.toFixed(1)}%`;
    pin.title = `${npc.name} — prontera (${npc.x}, ${npc.y})`;
    pin.innerHTML = `<span class="hub-pin-dot"></span><span class="hub-pin-label">${escapeHtml(npc.name.split(' ')[0])}</span>`;
    pin.addEventListener('mouseenter', () => selectNpc(npc, pin));
    pin.addEventListener('click', () => selectNpc(npc, pin));
    canvas.appendChild(pin);
  });
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
      if (btn.dataset.tab === 'admin') fetchLogs();
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
    showToast(true, `Tema alterado para ${next === 'obsidian' ? 'Obsidiana & Ouro' : 'Pergaminho Clássico'}.`);
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
        const msg = data.error || 'Falha ao criar conta.';
        feedback.className = 'feedback err';
        feedback.textContent = msg;
        showToast(false, msg);
        return;
      }

      const okMsg = `Conta '${data.userid}' criada com sucesso (${data.role}, ID #${data.accountId})!`;
      feedback.className = 'feedback ok';
      feedback.textContent = okMsg;
      showToast(true, okMsg);
      form.reset();
      fetchStatus();
    } catch {
      const msg = 'Erro de comunicação com a API do painel.';
      feedback.className = 'feedback err';
      feedback.textContent = msg;
      showToast(false, msg);
    }
  });
}

function buildToolsQuery() {
  const params = new URLSearchParams();
  params.set('name', document.getElementById('tools-name').value.trim() || 'FroggoClient');
  params.set('hpAddress', document.getElementById('tools-hp').value.trim() || '0x01138DDC');
  params.set('nameAddress', document.getElementById('tools-name-addr').value.trim() || '0x0113B7B0');
  const mapAddr = document.getElementById('tools-map-addr').value.trim();
  if (mapAddr) params.set('mapAddress', mapAddr);
  return `/api/supported-servers.json?${params.toString()}`;
}

function setupToolsForm() {
  const form = document.getElementById('tools-form');
  const copyBtn = document.getElementById('copy-tools-json');

  form.addEventListener('submit', (e) => {
    e.preventDefault();
    showToast(true, 'Baixando supported_servers.json...');
    window.location.href = buildToolsQuery();
  });

  copyBtn.addEventListener('click', async () => {
    const res = await fetch(buildToolsQuery());
    const text = await res.text();
    await navigator.clipboard.writeText(text);
    const original = copyBtn.textContent;
    copyBtn.textContent = 'JSON copiado!';
    showToast(true, 'JSON do supported_servers.json copiado para a área de transferência!');
    setTimeout(() => {
      copyBtn.textContent = original;
    }, 1800);
  });
}

async function fetchLogs() {
  const out = document.getElementById('logs-output');
  const svc = document.getElementById('log-service-select').value;
  out.textContent = `Carregando logs de ${svc}-server...`;
  try {
    const res = await fetch(`/api/admin/logs?service=${encodeURIComponent(svc)}&lines=100`);
    const data = await res.json();
    if (!res.ok) {
      out.textContent = data.error || 'Erro ao carregar logs.';
      return;
    }
    if (data.lines && data.lines.length > 0) {
      out.textContent = data.lines.join('\n');
      out.scrollTop = out.scrollHeight;
    } else {
      out.textContent = data.note || 'Nenhuma linha de log disponível ainda.';
    }
  } catch {
    out.textContent = 'Falha ao consultar API de logs.';
  }
}

function setupAdminTools() {
  const feedback = document.getElementById('admin-feedback');

  const setMsg = (ok, text) => {
    feedback.className = `feedback ${ok ? 'ok' : 'err'}`;
    feedback.textContent = text;
    showToast(ok, text);
  };

  document.getElementById('unstuck-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const charName = document.getElementById('admin-unstuck-name').value.trim();
    const res = await fetch('/api/admin/unstuck', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ charName })
    });
    const data = await res.json();
    setMsg(res.ok, data.message || data.error || 'Concluído.');
  });

  document.getElementById('zeny-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const charName = document.getElementById('admin-zeny-name').value.trim();
    const amount = Number(document.getElementById('admin-zeny-amount').value);
    const res = await fetch('/api/admin/give-zeny', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ charName, amount })
    });
    const data = await res.json();
    setMsg(res.ok, data.message || data.error || 'Concluído.');
    if (res.ok) fetchStatus();
  });

  const switchPreset = async (preset) => {
    const res = await fetch('/api/admin/preset', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ preset })
    });
    const data = await res.json();
    setMsg(res.ok, data.message || data.error || 'Preset atualizado.');
    if (res.ok) fetchStatus();
  };

  document.getElementById('btn-preset-sandbox').addEventListener('click', () => switchPreset('sandbox'));
  document.getElementById('btn-preset-aureumro').addEventListener('click', () => switchPreset('aureumro'));
  document.getElementById('refresh-logs').addEventListener('click', fetchLogs);
  document.getElementById('log-service-select').addEventListener('change', fetchLogs);
}

document.getElementById('refresh-ranking').addEventListener('click', fetchRanking);

setupTabs();
setupThemeToggle();
setupRegistrationForm();
setupToolsForm();
setupAdminTools();
setupHubMap();
startServerClock();
fetchStatus();
setInterval(fetchStatus, 15000);
