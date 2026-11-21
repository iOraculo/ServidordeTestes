const fs = require('fs');
const net = require('net');
const path = require('path');
const express = require('express');
const mysql = require('mysql2/promise');

const PRESET_METADATA = {
  sandbox: {
    id: 'sandbox',
    label: 'Sandbox de Testes',
    rates: '100x / 100x / 50x',
    description: 'Modo padrão para subir personagens rapidamente e testar o AureumRO-4Tools.'
  },
  aureumro: {
    id: 'aureumro',
    label: 'AureumRO Oficial',
    rates: '5x / 3x / 1x',
    description: 'Paridade de rates com o servidor oficial Pré-Renewal (Ep. 13.3 El Dicastes).'
  }
};

const PRE_RE_JOB_NAMES = {
  0: 'Aprendiz',
  1: 'Espadachim',
  2: 'Mago',
  3: 'Arqueiro',
  4: 'Noviço',
  5: 'Mercador',
  6: 'Gatuno',
  7: 'Cavaleiro',
  8: 'Sacerdote',
  9: 'Bruxo',
  10: 'Ferreiro',
  11: 'Caçador',
  12: 'Mercenário',
  14: 'Templário',
  15: 'Monge',
  16: 'Sábio',
  17: 'Arruaceiro',
  18: 'Alquimista',
  19: 'Bardo',
  20: 'Odalisca',
  23: 'Super Aprendiz',
  24: 'Justiceiro',
  25: 'Ninja',
  4001: 'Aprendiz T.',
  4002: 'Espadachim T.',
  4003: 'Mago T.',
  4004: 'Arqueiro T.',
  4005: 'Noviço T.',
  4006: 'Mercador T.',
  4007: 'Gatuno T.',
  4008: 'Lorde',
  4009: 'Sumo Sacerdote',
  4010: 'Arquimago',
  4011: 'Mestre-Ferreiro',
  4012: 'Atirador de Elite',
  4013: 'Algoz',
  4015: 'Paladino',
  4016: 'Mestre',
  4017: 'Professor',
  4018: 'Desordeiro',
  4019: 'Criador',
  4020: 'Menestrel',
  4021: 'Cigana',
  4046: 'Taekwon',
  4047: 'Mestre Taekwon',
  4049: 'Espiritualista'
};

function getJobName(classId) {
  return PRE_RE_JOB_NAMES[Number(classId)] || `Classe #${classId}`;
}

function escapeXml(value) {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

function defaultCheckPort(host, port, timeoutMs = 1500) {
  return new Promise((resolve) => {
    const socket = new net.Socket();
    let settled = false;

    const finish = (ok) => {
      if (!settled) {
        settled = true;
        socket.destroy();
        resolve(ok);
      }
    };

    socket.setTimeout(timeoutMs);
    socket.once('connect', () => finish(true));
    socket.once('timeout', () => finish(false));
    socket.once('error', () => finish(false));
    socket.connect(Number(port), host);
  });
}

function readActivePreset(stateFile, fallbackPreset) {
  try {
    if (stateFile && fs.existsSync(stateFile)) {
      const value = fs.readFileSync(stateFile, 'utf8').trim().toLowerCase();
      if (PRESET_METADATA[value]) {
        return PRESET_METADATA[value];
      }
    }
  } catch {
    // fallback silencioso
  }
  const normalized = String(fallbackPreset || 'sandbox').trim().toLowerCase();
  return PRESET_METADATA[normalized] || PRESET_METADATA.sandbox;
}

function normalizeHexAddress(raw) {
  if (!raw || typeof raw !== 'string') return null;
  const trimmed = raw.trim();
  const match = /^(?:0x)?([0-9a-fA-F]{6,8})$/.exec(trimmed);
  if (!match) return null;
  return '0x' + match[1].toUpperCase().padStart(8, '0');
}

function validateRegistrationPayload(body) {
  const userid = String(body?.userid || '').trim();
  const password = String(body?.password || '');
  const email = String(body?.email || 'tester@aureumro.local').trim();
  const sex = String(body?.sex || 'M').trim().toUpperCase();
  const accountType = String(body?.accountType || 'tester').trim().toLowerCase();

  if (!/^[A-Za-z0-9_]{4,23}$/.test(userid)) {
    return { error: 'Usuário deve ter entre 4 e 23 caracteres (apenas letras, números e _).' };
  }
  if (password.length < 4 || password.length > 32) {
    return { error: 'Senha deve ter entre 4 e 32 caracteres.' };
  }
  if (!['M', 'F'].includes(sex)) {
    return { error: 'Sexo inválido. Escolha M ou F.' };
  }
  if (email.length > 39 || !email.includes('@')) {
    return { error: 'Informe um e-mail válido (máximo 39 caracteres).' };
  }

  // player = group_id 0 | tester = group_id 1 (Super Player com comandos de teste)
  const groupId = accountType === 'player' ? 0 : 1;

  return {
    data: { userid, password, email, sex, groupId }
  };
}

function buildClientInfoXml({ serverName, publicIp, loginPort }) {
  const safeName = escapeXml(serverName);
  const safeIp = escapeXml(publicIp);
  const safePort = escapeXml(loginPort);

  return `<?xml version="1.0" encoding="euc-kr" ?>
<clientinfo>
  <desc>AureumRO Servidor de Testes (Pré-Renewal Ep. 13.3)</desc>
  <servicetype>korea</servicetype>
  <servertype>primary</servertype>
  <connection>
    <display>${safeName}</display>
    <desc>Servidor de Testes Pré-Renewal 99/70 — Compatível com AureumRO-4Tools</desc>
    <address>${safeIp}</address>
    <port>${safePort}</port>
    <version>55</version>
    <langtype>1</langtype>
    <registrationweb>http://${safeIp}:8080</registrationweb>
    <loading>
      <image>loading00.jpg</image>
    </loading>
  </connection>
</clientinfo>
`;
}

function createApp({ pool, config, checkPort = defaultCheckPort }) {
  const app = express();
  app.use(express.json({ limit: '64kb' }));
  app.use(express.static(path.join(__dirname, 'public')));

  app.get('/api/health', (_req, res) => {
    res.json({ status: 'ok', service: 'aureumro-test-webpanel' });
  });

  app.get('/api/status', async (_req, res) => {
    const [loginUp, charUp, mapUp] = await Promise.all([
      checkPort(config.rathenaHost, config.loginPort),
      checkPort(config.rathenaHost, config.charPort),
      checkPort(config.rathenaHost, config.mapPort)
    ]);

    let dbConnected = false;
    let stats = {
      accounts: 0,
      characters: 0,
      online: 0,
      zenyCirculation: 0
    };

    try {
      const [[accRow]] = await pool.query("SELECT COUNT(*) AS total FROM `login` WHERE `sex` != 'S'");
      const [[charRow]] = await pool.query(
        'SELECT COUNT(*) AS total, COALESCE(SUM(CASE WHEN `online` > 0 THEN 1 ELSE 0 END), 0) AS online, COALESCE(SUM(`zeny`), 0) AS totalZeny FROM `char`'
      );
      dbConnected = true;
      stats = {
        accounts: Number(accRow?.total || 0),
        characters: Number(charRow?.total || 0),
        online: Number(charRow?.online || 0),
        zenyCirculation: Number(charRow?.totalZeny || 0)
      };
    } catch {
      dbConnected = false;
    }

    const preset = readActivePreset(config.presetStateFile, config.defaultPreset);

    res.json({
      serverName: config.serverName,
      episode: '13.3 El Dicastes (Pré-Renewal)',
      levelCap: '99 / 70',
      packetver: config.packetver,
      publicIp: config.publicIp,
      ports: {
        login: config.loginPort,
        char: config.charPort,
        map: config.mapPort
      },
      services: {
        login: loginUp,
        char: charUp,
        map: mapUp,
        database: dbConnected
      },
      preset,
      stats,
      allowRegistration: config.allowRegistration,
      serverTime: new Date().toISOString()
    });
  });

  app.get('/api/ranking', async (req, res) => {
    const limit = Math.min(Math.max(Number(req.query.limit) || 25, 1), 100);
    try {
      const [rows] = await pool.query(
        'SELECT `char_id`, `name`, `class`, `base_level`, `job_level`, `zeny`, `last_map`, `online` FROM `char` ORDER BY `online` DESC, `base_level` DESC, `job_level` DESC, `char_id` ASC LIMIT ?',
        [limit]
      );
      const characters = rows.map((row) => ({
        charId: row.char_id,
        name: row.name,
        classId: row.class,
        className: getJobName(row.class),
        baseLevel: row.base_level,
        jobLevel: row.job_level,
        zeny: Number(row.zeny || 0),
        lastMap: row.last_map,
        online: Number(row.online) > 0
      }));
      res.json({ characters });
    } catch (err) {
      res.status(503).json({ error: 'Banco de dados indisponível ou ainda inicializando.', details: err.code || 'DB_ERROR' });
    }
  });

  app.post('/api/accounts', async (req, res) => {
    if (!config.allowRegistration) {
      return res.status(403).json({ error: 'Cadastro de contas desativado na configuração atual.' });
    }

    const validated = validateRegistrationPayload(req.body);
    if (validated.error) {
      return res.status(400).json({ error: validated.error });
    }

    const { userid, password, email, sex, groupId } = validated.data;

    try {
      const [existing] = await pool.query('SELECT `account_id` FROM `login` WHERE LOWER(`userid`) = LOWER(?) LIMIT 1', [userid]);
      if (existing.length > 0) {
        return res.status(409).json({ error: `O usuário '${userid}' já está cadastrado.` });
      }

      const [result] = await pool.query(
        'INSERT INTO `login` (`userid`, `user_pass`, `sex`, `email`, `group_id`) VALUES (?, ?, ?, ?, ?)',
        [userid, password, sex, email, groupId]
      );

      return res.status(201).json({
        accountId: result.insertId,
        userid,
        sex,
        email,
        groupId,
        role: groupId === 1 ? 'Tester (Super Player)' : 'Jogador'
      });
    } catch (err) {
      return res.status(500).json({ error: 'Falha ao criar conta no banco do rAthena.', details: err.code || 'DB_ERROR' });
    }
  });

  app.get('/api/clientinfo.xml', (_req, res) => {
    const xml = buildClientInfoXml({
      serverName: config.serverName,
      publicIp: config.publicIp,
      loginPort: config.loginPort
    });
    res.setHeader('Content-Type', 'application/xml; charset=utf-8');
    res.setHeader('Content-Disposition', 'attachment; filename="clientinfo.xml"');
    res.send(xml);
  });

  app.get('/api/supported-servers.json', (req, res) => {
    const processName = String(req.query.name || 'Ragexe').trim() || 'Ragexe';
    const hpAddress = normalizeHexAddress(req.query.hpAddress) || '0x015FF908';
    const nameAddress = normalizeHexAddress(req.query.nameAddress) || '0x01602568';
    const mapAddress = normalizeHexAddress(req.query.mapAddress);

    const entry = {
      name: processName,
      description: `${config.serverName} — Pré-Renewal Ep. 13.3 (PACKETVER ${config.packetver})`,
      hpAddress,
      nameAddress
    };
    if (mapAddress) {
      entry.mapAddress = mapAddress;
    }

    const payload = [
      {
        name: 'AureumRO',
        description: 'AureumRO — O goldtimes que você sempre sonhou',
        hpAddress: '0x015FF908',
        nameAddress: '0x01602568'
      },
      entry
    ];

    res.setHeader('Content-Type', 'application/json; charset=utf-8');
    res.setHeader('Content-Disposition', 'attachment; filename="supported_servers.json"');
    res.send(JSON.stringify(payload, null, 2));
  });

  return app;
}

function loadConfigFromEnv() {
  return {
    port: Number(process.env.PORT || 8080),
    serverName: process.env.SERVER_NAME || 'AureumRO Testes',
    publicIp: process.env.SERVER_PUBLIC_IP || '127.0.0.1',
    defaultPreset: process.env.SERVER_PRESET || 'sandbox',
    packetver: process.env.RATHENA_PACKETVER || '20220406',
    rathenaHost: process.env.RATHENA_HOST || '127.0.0.1',
    loginPort: Number(process.env.LOGIN_PORT || 6900),
    charPort: Number(process.env.CHAR_PORT || 6121),
    mapPort: Number(process.env.MAP_PORT || 5121),
    allowRegistration: String(process.env.WEBPANEL_ALLOW_REGISTRATION || 'true').toLowerCase() !== 'false',
    presetStateFile: process.env.PRESET_STATE_FILE || '/aureumro/state/active_preset',
    db: {
      host: process.env.DB_HOST || '127.0.0.1',
      port: Number(process.env.DB_PORT || 3306),
      user: process.env.DB_USER || 'ragnarok',
      password: process.env.DB_PASSWORD || 'aureumro_db_pass',
      database: process.env.DB_NAME || 'ragnarok'
    }
  };
}

if (require.main === module) {
  const config = loadConfigFromEnv();
  const pool = mysql.createPool({
    host: config.db.host,
    port: config.db.port,
    user: config.db.user,
    password: config.db.password,
    database: config.db.database,
    waitForConnections: true,
    connectionLimit: 10
  });

  const app = createApp({ pool, config });
  app.listen(config.port, '0.0.0.0', () => {
    console.log(`[AureumRO-WebPanel] Painel disponível na porta ${config.port} (${config.serverName})`);
  });
}

module.exports = {
  createApp,
  getJobName,
  normalizeHexAddress,
  validateRegistrationPayload,
  buildClientInfoXml,
  PRESET_METADATA
};
