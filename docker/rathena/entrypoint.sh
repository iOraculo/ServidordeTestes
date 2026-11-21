#!/usr/bin/env bash
set -euo pipefail

log() {
  printf '[AureumRO-rAthena] %s %s\n' "$(date '+%H:%M:%S')" "$*"
}

DB_HOST="${DB_HOST:-db}"
DB_PORT="${DB_PORT:-3306}"
DB_NAME="${DB_NAME:-ragnarok}"
DB_USER="${DB_USER:-ragnarok}"
DB_PASSWORD="${DB_PASSWORD:-aureumro_db_pass}"

SERVER_NAME="${SERVER_NAME:-AureumRO Testes}"
SERVER_PUBLIC_IP="${SERVER_PUBLIC_IP:-127.0.0.1}"
SERVER_PRESET="${SERVER_PRESET:-sandbox}"

INTER_SERVER_USER="${INTER_SERVER_USER:-s1}"
INTER_SERVER_PASS="${INTER_SERVER_PASS:-p1}"

GM_ADMIN_USER="${GM_ADMIN_USER:-admin}"
GM_ADMIN_PASS="${GM_ADMIN_PASS:-admin123}"
TESTER_USER="${TESTER_USER:-tester}"
TESTER_PASS="${TESTER_PASS:-tester123}"

LOGIN_PORT="${LOGIN_PORT:-6900}"
CHAR_PORT="${CHAR_PORT:-6121}"
MAP_PORT="${MAP_PORT:-5121}"

export DB_HOST DB_PORT DB_NAME DB_USER DB_PASSWORD
export SERVER_NAME SERVER_PUBLIC_IP SERVER_PRESET
export INTER_SERVER_USER INTER_SERVER_PASS
export GM_ADMIN_USER GM_ADMIN_PASS TESTER_USER TESTER_PASS
export LOGIN_PORT CHAR_PORT MAP_PORT

MYSQL_CMD=(mysql -h "${DB_HOST}" -P "${DB_PORT}" -u "${DB_USER}" "-p${DB_PASSWORD}" "${DB_NAME}")

log "Aguardando MariaDB em ${DB_HOST}:${DB_PORT}..."
for i in $(seq 1 60); do
  if "${MYSQL_CMD[@]}" -e "SELECT 1;" >/dev/null 2>&1; then
    log "MariaDB pronto!"
    break
  fi
  if [ "$i" -eq 60 ]; then
    log "ERRO: MariaDB não respondeu após 60 tentativas."
    exit 1
  fi
  sleep 2
done

# Inicializa schema oficial do rAthena caso seja o primeiro boot
TABLE_CHECK="$("${MYSQL_CMD[@]}" -NBe "SHOW TABLES LIKE 'login';" || true)"
if [ -z "${TABLE_CHECK}" ]; then
  log "Banco vazio detectado. Importando schema oficial do rAthena (main.sql, logs.sql, web.sql)..."
  "${MYSQL_CMD[@]}" < /rathena/sql-files/main.sql
  "${MYSQL_CMD[@]}" < /rathena/sql-files/logs.sql
  if [ -f /rathena/sql-files/web.sql ]; then
    "${MYSQL_CMD[@]}" < /rathena/sql-files/web.sql
  fi
  if [ -f /rathena/sql-files/roulette_default_data.sql ]; then
    "${MYSQL_CMD[@]}" < /rathena/sql-files/roulette_default_data.sql
  fi
  log "Schema importado com sucesso."
else
  log "Schema rAthena já presente no banco '${DB_NAME}'."
fi

# Aplica seed idempotente de conta inter-server (s1/p1), admin GM e tester
if [ -f /aureumro/db-seed/seed-accounts.sql ]; then
  log "Aplicando contas iniciais (inter-server, GM '${GM_ADMIN_USER}', tester '${TESTER_USER}')..."
  envsubst '${INTER_SERVER_USER} ${INTER_SERVER_PASS} ${GM_ADMIN_USER} ${GM_ADMIN_PASS} ${TESTER_USER} ${TESTER_PASS}' \
    < /aureumro/db-seed/seed-accounts.sql | "${MYSQL_CMD[@]}"
fi

# Determina preset ativo (se houver override salvo em /aureumro/state/active_preset, respeita-o)
mkdir -p /aureumro/state /rathena/conf/import /rathena/db/import /rathena/log
ACTIVE_PRESET="${SERVER_PRESET}"
if [ -f /aureumro/state/active_preset ]; then
  SAVED_PRESET="$(tr -d '[:space:]' < /aureumro/state/active_preset)"
  if [ -n "${SAVED_PRESET}" ] && [ -f "/aureumro/config/presets/${SAVED_PRESET}.conf" ]; then
    ACTIVE_PRESET="${SAVED_PRESET}"
  fi
fi

PRESET_FILE="/aureumro/config/presets/${ACTIVE_PRESET}.conf"
if [ ! -f "${PRESET_FILE}" ]; then
  log "AVISO: Preset '${ACTIVE_PRESET}' não encontrado, usando 'sandbox'."
  ACTIVE_PRESET="sandbox"
  PRESET_FILE="/aureumro/config/presets/sandbox.conf"
fi
printf '%s\n' "${ACTIVE_PRESET}" > /aureumro/state/active_preset

# Define zeny inicial por preset
if [ "${ACTIVE_PRESET}" = "aureumro" ]; then
  START_ZENY="0"
else
  START_ZENY="1000000"
fi
export START_ZENY

log "Aplicando configurações AureumRO (preset='${ACTIVE_PRESET}', IP='${SERVER_PUBLIC_IP}')..."

# Renderiza templates conf/import
envsubst '${DB_HOST} ${DB_PORT} ${DB_NAME} ${DB_USER} ${DB_PASSWORD}' \
  < /aureumro/config/import/inter_conf.txt.tmpl > /rathena/conf/import/inter_conf.txt

envsubst '${INTER_SERVER_USER} ${INTER_SERVER_PASS} ${SERVER_NAME} ${SERVER_PUBLIC_IP} ${LOGIN_PORT} ${CHAR_PORT} ${START_ZENY}' \
  < /aureumro/config/import/char_conf.txt.tmpl > /rathena/conf/import/char_conf.txt

envsubst '${INTER_SERVER_USER} ${INTER_SERVER_PASS} ${SERVER_PUBLIC_IP} ${CHAR_PORT} ${MAP_PORT}' \
  < /aureumro/config/import/map_conf.txt.tmpl > /rathena/conf/import/map_conf.txt

cp /aureumro/config/import/login_conf.txt /rathena/conf/import/login_conf.txt
cp /aureumro/config/import/packet_conf.txt /rathena/conf/import/packet_conf.txt
cp /aureumro/config/import/groups.yml /rathena/conf/import/groups.yml
cp /aureumro/config/import/motd.txt /rathena/conf/motd.txt

# Combina battle_conf base + preset de rates ativo
{
  cat /aureumro/config/import/battle_conf.txt
  printf '\n// === Preset Ativo: %s ===\n' "${ACTIVE_PRESET}"
  cat "${PRESET_FILE}"
} > /rathena/conf/import/battle_conf.txt

# Normaliza possíveis quebras de linha CRLF (Windows) nas configs
sed -i 's/\r$//' /rathena/conf/import/* /rathena/conf/motd.txt

# Converte MOTD de UTF-8 para Windows-1252 (encoding esperado pelo cliente Ragnarok)
if iconv -f UTF-8 -t WINDOWS-1252//TRANSLIT /rathena/conf/motd.txt > /rathena/conf/motd.txt.cp1252 2>/dev/null; then
  mv /rathena/conf/motd.txt.cp1252 /rathena/conf/motd.txt
else
  rm -f /rathena/conf/motd.txt.cp1252
fi

# Copia DB customizada (Doce de Elvira etc.) para /rathena/db/import
if [ -d /aureumro/db/import ]; then
  cp -r /aureumro/db/import/* /rathena/db/import/
  sed -i 's/\r$//' /rathena/db/import/*
fi

# Instala NPCs customizados AureumRO + Laboratório 4Tools
rm -rf /rathena/npc/aureumro
cp -r /aureumro/npc/aureumro /rathena/npc/aureumro
cp /aureumro/npc/scripts_aureumro.conf /rathena/npc/scripts_aureumro.conf
sed -i 's/\r$//' /rathena/npc/aureumro/*.txt /rathena/npc/scripts_aureumro.conf

# Converte scripts NPC de UTF-8 para Windows-1252 para exibir acentos PT-BR corretamente no cliente
for npc_file in /rathena/npc/aureumro/*.txt; do
  if iconv -f UTF-8 -t WINDOWS-1252//TRANSLIT "${npc_file}" > "${npc_file}.cp1252" 2>/dev/null; then
    mv "${npc_file}.cp1252" "${npc_file}"
  else
    rm -f "${npc_file}.cp1252"
  fi
done

if ! grep -q "npc/scripts_aureumro.conf" /rathena/npc/scripts_custom.conf; then
  printf '\n// AureumRO Test Server & Laboratório 4Tools\nimport: npc/scripts_aureumro.conf\n' >> /rathena/npc/scripts_custom.conf
fi

# Gerenciamento limpo dos processos login / char / map
PIDS=()
cleanup() {
  log "Encerrando servidores rAthena com segurança..."
  for pid in "${PIDS[@]}"; do
    if kill -0 "$pid" 2>/dev/null; then
      kill -TERM "$pid" 2>/dev/null || true
    fi
  done
  wait || true
  log "Servidores encerrados."
  exit 0
}
trap cleanup SIGTERM SIGINT

cd /rathena

log "Iniciando login-server (porta ${LOGIN_PORT})..."
./login-server &
LOGIN_PID=$!
PIDS+=("${LOGIN_PID}")

for i in $(seq 1 30); do
  if nc -z 127.0.0.1 "${LOGIN_PORT}" 2>/dev/null; then
    break
  fi
  sleep 1
done

log "Iniciando char-server (porta ${CHAR_PORT})..."
./char-server &
CHAR_PID=$!
PIDS+=("${CHAR_PID}")

for i in $(seq 1 30); do
  if nc -z 127.0.0.1 "${CHAR_PORT}" 2>/dev/null; then
    break
  fi
  sleep 1
done

log "Iniciando map-server (porta ${MAP_PORT})..."
./map-server &
MAP_PID=$!
PIDS+=("${MAP_PID}")

log "AureumRO Servidor de Testes online! Preset='${ACTIVE_PRESET}' | IP='${SERVER_PUBLIC_IP}'"

# Aguarda caso algum dos 3 serviços encerre inesperadamente
wait -n "${LOGIN_PID}" "${CHAR_PID}" "${MAP_PID}"
EXIT_CODE=$?
log "ERRO: Um dos processos do rAthena encerrou (código ${EXIT_CODE}). Encerrando demais serviços..."
cleanup
