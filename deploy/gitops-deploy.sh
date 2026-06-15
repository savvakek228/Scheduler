#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/scheduler}"
BRANCH="${GITOPS_BRANCH:-master}"
LOG_FILE="${GITOPS_LOG:-/var/log/scheduler-gitops.log}"

log() {
  echo "[$(date -Iseconds)] $*" | tee -a "$LOG_FILE"
}

log "GitOps deploy started (branch=${BRANCH})"

cd "$APP_DIR"

if [ ! -d .git ]; then
  log "ERROR: ${APP_DIR} is not a git repository"
  exit 1
fi

git fetch origin "$BRANCH"
git checkout "$BRANCH"
git reset --hard "origin/$BRANCH"

sed -i 's/\r$//' deploy.production.sh deploy/gitops-deploy.sh 2>/dev/null || true
chmod +x deploy.production.sh deploy/gitops-deploy.sh

# VPS provider rejects custom Host headers on public app port.
sed -i 's|\${SCHEDULER_HTTP_PORT:-8080}:8080|127.0.0.1:5001:8080|' docker-compose.production.yml

if [ ! -f .env.production ] && [ -f .env.production.example ]; then
  cp .env.production.example .env.production
fi

bash deploy.production.sh >>"$LOG_FILE" 2>&1

log "GitOps deploy finished successfully"
