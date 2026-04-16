#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${ROOT_DIR}"

if [ -f ".env.production" ]; then
  echo "Using .env.production file"
  docker compose -f docker-compose.production.yml --env-file .env.production up -d --build
else
  echo ".env.production not found; using defaults from docker-compose.production.yml"
  docker compose -f docker-compose.production.yml up -d --build
fi
docker image prune -f

echo "Deployment completed. Service status:"
docker compose -f docker-compose.production.yml ps
