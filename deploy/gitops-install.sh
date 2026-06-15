#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/scheduler}"
REPO_URL="${REPO_URL:-https://github.com/savvakek228/Scheduler.git}"
BRANCH="${GITOPS_BRANCH:-master}"
WEBHOOK_PORT="${GITOPS_WEBHOOK_PORT:-9000}"
CONFIG_DIR="/etc/scheduler"
HOOKS_FILE="${CONFIG_DIR}/gitops-webhook.hooks.json"
SECRET_FILE="${CONFIG_DIR}/gitops-webhook.secret"
SERVICE_FILE="/etc/systemd/system/scheduler-gitops-webhook.service"
NGINX_SITE="/etc/nginx/sites-available/savvakam2.chickenkiller.com"

echo "Installing GitOps for Scheduler..."

apt-get update -qq
apt-get install -y -qq git webhook

mkdir -p "$CONFIG_DIR" /var/log
chmod 700 "$CONFIG_DIR"

if [ ! -f "$SECRET_FILE" ]; then
  openssl rand -hex 32 >"$SECRET_FILE"
  chmod 600 "$SECRET_FILE"
fi

WEBHOOK_SECRET="$(cat "$SECRET_FILE")"

if [ -d "$APP_DIR/.git" ]; then
  echo "Git repository already exists in ${APP_DIR}, updating remote"
  git -C "$APP_DIR" remote set-url origin "$REPO_URL"
else
  ENV_BACKUP=""
  if [ -f "$APP_DIR/.env.production" ]; then
    ENV_BACKUP="$(mktemp)"
    cp "$APP_DIR/.env.production" "$ENV_BACKUP"
  fi

  rm -rf "$APP_DIR"
  git clone --branch "$BRANCH" "$REPO_URL" "$APP_DIR"

  if [ -n "$ENV_BACKUP" ] && [ -f "$ENV_BACKUP" ]; then
    cp "$ENV_BACKUP" "$APP_DIR/.env.production"
    rm -f "$ENV_BACKUP"
  fi
fi

chmod +x "$APP_DIR/deploy/gitops-deploy.sh" "$APP_DIR/deploy.production.sh"

cat >"$HOOKS_FILE" <<EOF
[
  {
    "id": "scheduler-gitops",
    "execute-command": "${APP_DIR}/deploy/gitops-deploy.sh",
    "command-working-directory": "${APP_DIR}",
    "response-message": "Scheduler GitOps deploy started",
    "trigger-rule": {
      "and": [
        {
          "match": {
            "type": "payload-hmac-sha256",
            "secret": "${WEBHOOK_SECRET}",
            "parameter": {
              "source": "header",
              "name": "X-Hub-Signature-256"
            }
          }
        },
        {
          "match": {
            "type": "value",
            "value": "refs/heads/${BRANCH}",
            "parameter": {
              "source": "payload",
              "name": "ref"
            }
          }
        }
      ]
    }
  }
]
EOF
chmod 600 "$HOOKS_FILE"

cat >"$SERVICE_FILE" <<EOF
[Unit]
Description=Scheduler GitOps webhook
After=network.target

[Service]
Type=simple
ExecStart=/usr/bin/webhook -hooks ${HOOKS_FILE} -port ${WEBHOOK_PORT} -ip 127.0.0.1 -verbose
Restart=always
RestartSec=3

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now scheduler-gitops-webhook.service

if [ -f "$NGINX_SITE" ] && ! grep -q 'location /hooks/gitops' "$NGINX_SITE"; then
  python3 - <<'PY'
from pathlib import Path

site = Path("/etc/nginx/sites-available/savvakam2.chickenkiller.com")
text = site.read_text()
snippet = """
    location /hooks/gitops {
        proxy_pass http://127.0.0.1:9000/hooks/scheduler-gitops;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Hub-Signature-256 $http_x_hub_signature_256;
        proxy_set_header X-GitHub-Event $http_x_github_event;
        proxy_set_header X-GitHub-Delivery $http_x_github_delivery;
        proxy_set_header Content-Type $content_type;
    }
"""
marker = "location ^~ /.well-known/acme-challenge/"
if marker in text:
    text = text.replace(marker, snippet + "\n    " + marker, 1)
else:
    text = text.replace("location / {", snippet + "\n    location / {", 1)
site.write_text(text)
PY
  nginx -t
  systemctl reload nginx
fi

bash "$APP_DIR/deploy/gitops-deploy.sh"

echo ""
echo "GitOps installed."
echo "GitHub webhook URL: https://savvakam2.chickenkiller.com/hooks/gitops"
echo "Webhook secret: ${WEBHOOK_SECRET}"
echo "Deploy branch: ${BRANCH}"
