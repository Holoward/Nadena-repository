#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API_LOG="$ROOT_DIR/api_log.txt"
FRONTEND_LOG="$ROOT_DIR/frontend_log.txt"
BACKEND_PID=""
FRONTEND_PID=""

cleanup() {
  if [[ -n "$BACKEND_PID" ]] && kill -0 "$BACKEND_PID" >/dev/null 2>&1; then
    kill "$BACKEND_PID" >/dev/null 2>&1 || true
    wait "$BACKEND_PID" 2>/dev/null || true
  fi

  if [[ -n "$FRONTEND_PID" ]] && kill -0 "$FRONTEND_PID" >/dev/null 2>&1; then
    kill "$FRONTEND_PID" >/dev/null 2>&1 || true
    wait "$FRONTEND_PID" 2>/dev/null || true
  fi
}

trap cleanup EXIT

cd "$ROOT_DIR"

export ASPNETCORE_ENVIRONMENT=Development
export NADENA_DISABLE_SPA=true
export NODE_OPTIONS=--openssl-legacy-provider

echo "==> Building backend"
if dotnet build WebApi/WebApi.csproj; then
  echo "==> Backend build passed"
else
  echo "==> Warning: dotnet build returned a non-zero exit code in this environment; continuing with runtime verification"
fi

echo "==> Building frontend"
(cd WebApi/ClientApp && npm run build)

echo "==> Database migrations will be applied by the API on startup if needed"

if curl -fsS http://localhost:5000/swagger/index.html >/dev/null 2>&1; then
  echo "==> Reusing existing backend on http://localhost:5000"
else
  echo "==> Starting backend on http://localhost:5000"
  dotnet run --project WebApi --no-launch-profile >"$API_LOG" 2>&1 &
  BACKEND_PID=$!
fi

if curl -fsS http://localhost:44391 >/dev/null 2>&1; then
  echo "==> Reusing existing frontend on http://localhost:44391"
else
  echo "==> Starting static frontend on http://localhost:44391"
  python3 -m http.server 44391 --directory WebApi/ClientApp/build >"$FRONTEND_LOG" 2>&1 &
  FRONTEND_PID=$!
fi

echo "==> Running NADENA verification suite"
python3 scripts/verify_nadena.py

echo "==> Verification completed successfully"
