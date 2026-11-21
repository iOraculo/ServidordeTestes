#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
BACKUP_DIR="${ROOT_DIR}/backups"
mkdir -p "${BACKUP_DIR}"

STAMP="$(date '+%Y%m%d_%H%M%S')"
OUT_FILE="${BACKUP_DIR}/aureumro_${STAMP}.sql"

DB_USER="${DB_USER:-ragnarok}"
DB_PASSWORD="${DB_PASSWORD:-aureumro_db_pass}"
DB_NAME="${DB_NAME:-ragnarok}"

echo "[Backup] Exportando banco '${DB_NAME}' para '${OUT_FILE}'..."
docker exec aureumro-db mariadb-dump -u"${DB_USER}" "-p${DB_PASSWORD}" --single-transaction --routines --triggers "${DB_NAME}" > "${OUT_FILE}"
echo "[OK] Backup concluído: ${OUT_FILE}"
