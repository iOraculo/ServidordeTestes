#!/usr/bin/env bash
set -euo pipefail

LOGIN_PORT="${LOGIN_PORT:-6900}"
CHAR_PORT="${CHAR_PORT:-6121}"
MAP_PORT="${MAP_PORT:-5121}"
WEBPANEL_PORT="${WEBPANEL_PORT:-8080}"

check_tcp() {
  local name="$1"
  local port="$2"
  if nc -z 127.0.0.1 "${port}" 2>/dev/null; then
    echo "  [OK] ${name} respondendo na porta ${port}"
  else
    echo "  [FALHA] ${name} não respondeu na porta ${port}"
    return 1
  fi
}

echo "Executando smoke-test do AureumRO Servidor de Testes..."
check_tcp "login-server" "${LOGIN_PORT}"
check_tcp "char-server" "${CHAR_PORT}"
check_tcp "map-server" "${MAP_PORT}"
check_tcp "webpanel" "${WEBPANEL_PORT}"

echo "Verificando API do Painel Web:"
curl -fsS "http://127.0.0.1:${WEBPANEL_PORT}/api/health" && echo ""
curl -fsS "http://127.0.0.1:${WEBPANEL_PORT}/api/status" && echo ""
echo "Smoke-test concluído com sucesso!"
