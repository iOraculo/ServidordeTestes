#!/usr/bin/env bash
set -euo pipefail

LOGIN_PORT="${LOGIN_PORT:-6900}"
CHAR_PORT="${CHAR_PORT:-6121}"
MAP_PORT="${MAP_PORT:-5121}"

nc -z 127.0.0.1 "${LOGIN_PORT}" \
  && nc -z 127.0.0.1 "${CHAR_PORT}" \
  && nc -z 127.0.0.1 "${MAP_PORT}"
