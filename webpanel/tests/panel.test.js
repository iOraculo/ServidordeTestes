const test = require('node:test');
const assert = require('node:assert/strict');
const http = require('node:http');
const {
  createApp,
  getJobName,
  normalizeHexAddress,
  validateRegistrationPayload,
  buildClientInfoXml
} = require('../server');

function startTestServer(app) {
  return new Promise((resolve) => {
    const server = http.createServer(app);
    server.listen(0, '127.0.0.1', () => {
      const { port } = server.address();
      resolve({ server, baseUrl: `http://127.0.0.1:${port}` });
    });
  });
}

test('validateRegistrationPayload accepts valid accounts and rejects invalid inputs', () => {
  const ok = validateRegistrationPayload({
    userid: 'oraculo_01',
    password: 'secret',
    sex: 'm',
    email: 'test@aureumro.local',
    accountType: 'tester'
  });
  assert.equal(ok.error, undefined);
  assert.equal(ok.data.groupId, 1);
  assert.equal(ok.data.sex, 'M');

  const player = validateRegistrationPayload({
    userid: 'player01',
    password: 'secret',
    sex: 'F',
    email: 'p@aureumro.local',
    accountType: 'player'
  });
  assert.equal(player.data.groupId, 0);

  const shortUser = validateRegistrationPayload({ userid: 'ab', password: 'secret' });
  assert.ok(shortUser.error);

  const badChars = validateRegistrationPayload({ userid: 'bad user!', password: 'secret' });
  assert.ok(badChars.error);
});

test('normalizeHexAddress normalizes 4Tools memory addresses', () => {
  assert.equal(normalizeHexAddress('15ff908'), '0x015FF908');
  assert.equal(normalizeHexAddress('0x01602568'), '0x01602568');
  assert.equal(normalizeHexAddress('invalid'), null);
});

test('getJobName maps Pre-Renewal jobs to PT-BR names', () => {
  assert.equal(getJobName(4008), 'Lorde');
  assert.equal(getJobName(4019), 'Criador');
  assert.equal(getJobName(9999), 'Classe #9999');
});

test('buildClientInfoXml escapes XML and embeds host and port', () => {
  const xml = buildClientInfoXml({ serverName: 'AureumRO & Testes', publicIp: '192.168.0.10', loginPort: 6900 });
  assert.match(xml, /<display>AureumRO &amp; Testes<\/display>/);
  assert.match(xml, /<address>192\.168\.0\.10<\/address>/);
  assert.match(xml, /<port>6900<\/port>/);
});

test('HTTP endpoints: /api/health, /api/status, /api/accounts, /api/ranking, /api/supported-servers.json', async () => {
  const accounts = [{ account_id: 2000000, userid: 'admin' }];
  const fakePool = {
    async query(sql, params) {
      if (sql.includes("FROM `login` WHERE `sex` != 'S'")) {
        return [[{ total: accounts.length }]];
      }
      if (sql.includes('FROM `char`') && sql.includes('SUM(`zeny`)')) {
        return [[{ total: 1, online: 1, totalZeny: 1234567 }]];
      }
      if (sql.includes('SELECT `char_id`')) {
        return [[{ char_id: 150000, name: 'Oraculo', class: 4008, base_level: 99, job_level: 70, zeny: 1234567, last_map: 'pvp_n_1-5', online: 1 }]];
      }
      if (sql.includes('LOWER(`userid`) = LOWER(?)')) {
        const found = accounts.filter((a) => a.userid.toLowerCase() === params[0].toLowerCase());
        return [found];
      }
      if (sql.startsWith('INSERT INTO `login`')) {
        const id = 2000000 + accounts.length;
        accounts.push({ account_id: id, userid: params[0] });
        return [{ insertId: id }];
      }
      throw new Error(`Unexpected SQL in mock: ${sql}`);
    }
  };

  const app = createApp({
    pool: fakePool,
    config: {
      serverName: 'AureumRO Testes',
      publicIp: '127.0.0.1',
      defaultPreset: 'sandbox',
      packetver: '20220406',
      rathenaHost: '127.0.0.1',
      loginPort: 6900,
      charPort: 6121,
      mapPort: 5121,
      allowRegistration: true,
      presetStateFile: '/nonexistent/state'
    },
    checkPort: async (_host, port) => port !== 5121
  });

  const { server, baseUrl } = await startTestServer(app);
  try {
    const health = await fetch(`${baseUrl}/api/health`).then((r) => r.json());
    assert.equal(health.status, 'ok');

    const status = await fetch(`${baseUrl}/api/status`).then((r) => r.json());
    assert.equal(status.services.login, true);
    assert.equal(status.services.map, false);
    assert.equal(status.preset.id, 'sandbox');
    assert.equal(status.stats.online, 1);

    const createRes = await fetch(`${baseUrl}/api/accounts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userid: 'novo_tester', password: 'password123', sex: 'M', email: 'novo@aureumro.local', accountType: 'tester' })
    });
    assert.equal(createRes.status, 201);
    const created = await createRes.json();
    assert.equal(created.userid, 'novo_tester');

    const dupRes = await fetch(`${baseUrl}/api/accounts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userid: 'novo_tester', password: 'password123', sex: 'M', email: 'novo@aureumro.local' })
    });
    assert.equal(dupRes.status, 409);

    const ranking = await fetch(`${baseUrl}/api/ranking`).then((r) => r.json());
    assert.equal(ranking.characters[0].className, 'Lorde');

    const serversJson = await fetch(`${baseUrl}/api/supported-servers.json?name=RagexeTest&hpAddress=0x015FF908&nameAddress=0x01602568&mapAddress=0x01600000`).then((r) => r.json());
    assert.equal(serversJson.length, 2);
    assert.equal(serversJson[1].name, 'RagexeTest');
    assert.equal(serversJson[1].mapAddress, '0x01600000');
  } finally {
    server.close();
  }
});

test('GM Admin endpoints: /api/admin/unstuck, /api/admin/give-zeny, /api/admin/preset, /api/admin/logs', async () => {
  const fs = require('node:fs');
  const os = require('node:os');
  const path = require('node:path');
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'aureumro-panel-test-'));
  const stateFile = path.join(tmpDir, 'active_preset');
  const logDir = path.join(tmpDir, 'logs');
  fs.mkdirSync(logDir, { recursive: true });
  fs.writeFileSync(path.join(logDir, 'map-msg_log.log'), '[Status]: Map Server Ready\n[Info]: 16 custom NPCs loaded\n', 'utf8');

  const fakePool = {
    async query(sql, params) {
      if (sql.includes('SET `clothes_color` = 0')) {
        return [{ affectedRows: params && params[0] === 'MissingChar' ? 0 : 1 }];
      }
      if (sql.includes('SET `zeny` = LEAST')) {
        return [{ affectedRows: params && params[1] === 'Oraculo' ? 1 : 0 }];
      }
      throw new Error(`Unexpected SQL: ${sql}`);
    }
  };

  const app = createApp({
    pool: fakePool,
    config: {
      serverName: 'AureumRO Testes',
      publicIp: '127.0.0.1',
      defaultPreset: 'sandbox',
      packetver: '20220406',
      rathenaHost: '127.0.0.1',
      loginPort: 6900,
      charPort: 6121,
      mapPort: 5121,
      allowRegistration: true,
      presetStateFile: stateFile,
      logDir
    },
    checkPort: async () => true
  });

  const { server, baseUrl } = await startTestServer(app);
  try {
    const unstuck = await fetch(`${baseUrl}/api/admin/unstuck`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ charName: 'Oraculo' })
    });
    assert.equal(unstuck.status, 200);

    const zenyOk = await fetch(`${baseUrl}/api/admin/give-zeny`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ charName: 'Oraculo', amount: 1000000 })
    });
    assert.equal(zenyOk.status, 200);

    const zenyBad = await fetch(`${baseUrl}/api/admin/give-zeny`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ charName: 'Oraculo', amount: -10 })
    });
    assert.equal(zenyBad.status, 400);

    const presetRes = await fetch(`${baseUrl}/api/admin/preset`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ preset: 'aureumro' })
    });
    assert.equal(presetRes.status, 200);
    assert.equal(fs.readFileSync(stateFile, 'utf8').trim(), 'aureumro');

    const logs = await fetch(`${baseUrl}/api/admin/logs?service=map&lines=20`).then((r) => r.json());
    assert.equal(logs.lines.length, 2);
    assert.match(logs.lines[0], /Map Server Ready/);
  } finally {
    server.close();
    fs.rmSync(tmpDir, { recursive: true, force: true });
  }
});
