#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# Nadena Production Deployment Script
# Run from the project root directory: ./deploy.sh
# =============================================================================

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
DEPLOY_DIR="/var/nadena"
WEB_API="${PROJECT_ROOT}/WebApi"
CLIENT_APP="${WEB_API}/ClientApp"
SERVICE_FILE="${PROJECT_ROOT}/nadena.service"
NGINX_CONF="${PROJECT_ROOT}/nadena.conf"
ENV_FILE="${PROJECT_ROOT}/nadena-environment"

echo "============================================"
echo "  Nadena Production Deployment"
echo "  $(date '+%Y-%m-%d %H:%M:%S')"
echo "============================================"

# --- Pre-flight checks ---
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: dotnet SDK not found. Install .NET 10 SDK first."
    exit 1
fi

if ! command -v npm &> /dev/null; then
    echo "ERROR: npm not found. Install Node.js first."
    exit 1
fi

if ! command -v nginx &> /dev/null; then
    echo "ERROR: nginx not found. Install nginx first."
    exit 1
fi

# --- Step 1: Build React frontend ---
echo ""
echo "[1/6] Building React frontend (production)..."
cd "${CLIENT_APP}"
npm ci --production=false
npm run build:prod
echo "  React build complete."

# --- Step 2: Publish .NET backend ---
echo ""
echo "[2/6] Publishing .NET backend..."
cd "${PROJECT_ROOT}"
dotnet publish WebApi -c Release -o "${DEPLOY_DIR}" --nologo
echo "  .NET publish complete."

# --- Step 3: Copy React build to wwwroot ---
echo ""
echo "[3/6] Copying React build to ${DEPLOY_DIR}/wwwroot/..."
rm -rf "${DEPLOY_DIR}/wwwroot"
cp -r "${CLIENT_APP}/build" "${DEPLOY_DIR}/wwwroot"
echo "  Static files copied."

# --- Step 4: Create dedicated service user (if needed) ---
echo ""
echo "[4/6] Setting up service user..."
if ! id -u nadena &>/dev/null; then
    sudo useradd --system --no-create-home --shell /usr/sbin/nologin nadena
    echo "  Created user 'nadena'."
else
    echo "  User 'nadena' already exists."
fi

# Ensure deploy directory ownership
sudo chown -R nadena:nadena "${DEPLOY_DIR}"
sudo mkdir -p /var/backups/nadena
sudo chown -R nadena:nadena /var/backups/nadena

# --- Step 5: Install systemd service ---
echo ""
echo "[5/6] Installing systemd service..."
sudo cp "${SERVICE_FILE}" /etc/systemd/system/nadena.service

# Install environment file (secrets)
if [ ! -f /etc/nadena/environment ]; then
    sudo mkdir -p /etc/nadena
    sudo cp "${ENV_FILE}" /etc/nadena/environment
    sudo chmod 600 /etc/nadena/environment
    sudo chown nadena:nadena /etc/nadena/environment
    echo "  WARNING: Edit /etc/nadena/environment with real secrets!"
else
    echo "  /etc/nadena/environment already exists (not overwriting)."
fi

sudo systemctl daemon-reload
sudo systemctl enable nadena
sudo systemctl restart nadena
echo "  Service restarted."

# --- Step 6: Install nginx config ---
echo ""
echo "[6/6] Installing nginx configuration..."
sudo cp "${NGINX_CONF}" /etc/nginx/sites-available/nadena.conf
sudo ln -sf /etc/nginx/sites-available/nadena.conf /etc/nginx/sites-enabled/nadena.conf

# Remove default site if it exists
if [ -f /etc/nginx/sites-enabled/default ]; then
    sudo rm /etc/nginx/sites-enabled/default
    echo "  Removed default nginx site."
fi

sudo nginx -t
sudo systemctl reload nginx
echo "  Nginx reloaded."

# --- Verify ---
echo ""
echo "============================================"
echo "  Deployment Complete!"
echo "============================================"
echo ""
echo "  Service status:  sudo systemctl status nadena"
echo "  Service logs:    sudo journalctl -u nadena -f"
echo "  Health check:    curl http://localhost:5034/health"
echo ""
echo "  Next steps:"
echo "  1. Edit /etc/nadena/environment with real secrets"
echo "  2. Run: certbot --nginx -d nadena.com -d www.nadena.com"
echo "  3. Uncomment HTTPS block in /etc/nginx/sites-available/nadena.conf"
echo "  4. Run: sudo systemctl reload nginx"
echo ""
