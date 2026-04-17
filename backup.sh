#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# Nadena Daily Backup Script
# Keeps last 30 days of SQLite database backups
#
# Install crontab entry:
#   sudo crontab -e
#   Add: 0 2 * * * /var/nadena/backup.sh >> /var/log/nadena-backup.log 2>&1
# =============================================================================

DB_PATH="/var/nadena/nadena.db"
BACKUP_DIR="/var/backups/nadena"
KEEP_DAYS=30
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/nadena-${DATE}.db"

echo "============================================"
echo "  Nadena Backup - $(date '+%Y-%m-%d %H:%M:%S')"
echo "============================================"

# Ensure backup directory exists
mkdir -p "${BACKUP_DIR}"

# Check if database exists
if [ ! -f "${DB_PATH}" ]; then
    echo "ERROR: Database not found at ${DB_PATH}"
    exit 1
fi

# Create backup using SQLite .backup for consistency
# (safer than cp while database is in use)
if command -v sqlite3 &> /dev/null; then
    sqlite3 "${DB_PATH}" ".backup '${BACKUP_FILE}'"
    echo "  Backup created using sqlite3 .backup: ${BACKUP_FILE}"
else
    # Fallback: copy the file (less safe but works)
    cp "${DB_PATH}" "${BACKUP_FILE}"
    echo "  Backup created using cp (sqlite3 not found): ${BACKUP_FILE}"
fi

# Verify backup was created
if [ ! -f "${BACKUP_FILE}" ]; then
    echo "ERROR: Backup file was not created!"
    exit 1
fi

BACKUP_SIZE=$(du -h "${BACKUP_FILE}" | cut -f1)
echo "  Backup size: ${BACKUP_SIZE}"

# Clean up backups older than KEEP_DAYS
DELETED_COUNT=0
find "${BACKUP_DIR}" -name "nadena-*.db" -type f -mtime +${KEEP_DAYS} | while read -r old_backup; do
    rm -f "${old_backup}"
    echo "  Deleted old backup: $(basename "${old_backup}")"
    DELETED_COUNT=$((DELETED_COUNT + 1))
done

echo "  Retention: last ${KEEP_DAYS} days"
echo "  Backup complete."
