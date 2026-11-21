#!/usr/bin/env bash
set -euo pipefail

PRESET="${1:-}"
if [ "${PRESET}" != "sandbox" ] && [ "${PRESET}" != "aureumro" ]; then
  echo "Uso: $0 <sandbox|aureumro>"
  echo "  sandbox  -> 100x Base / 100x Job / 50x Drop (padrão de testes)"
  echo "  aureumro ->   5x Base /   3x Job /  1x Drop (rates oficiais AureumRO)"
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

if [ -f "${ROOT_DIR}/.env" ]; then
  if grep -q '^SERVER_PRESET=' "${ROOT_DIR}/.env"; then
    sed -i.bak "s/^SERVER_PRESET=.*/SERVER_PRESET=${PRESET}/" "${ROOT_DIR}/.env"
    rm -f "${ROOT_DIR}/.env.bak"
  else
    printf '\nSERVER_PRESET=%s\n' "${PRESET}" >> "${ROOT_DIR}/.env"
  fi
fi

if docker ps --format '{{.Names}}' | grep -qx 'aureumro-rathena'; then
  echo "[Preset] Gravando preset '${PRESET}' no volume de estado e reiniciando rAthena..."
  docker exec aureumro-rathena sh -c "printf '%s\n' '${PRESET}' > /aureumro/state/active_preset"
  docker compose -f "${ROOT_DIR}/docker-compose.yml" restart rathena
  echo "[Preset] Servidor reiniciado com preset '${PRESET}'!"
else
  echo "[Preset] Arquivo .env atualizado para '${PRESET}'. Suba a stack com: make up"
fi
