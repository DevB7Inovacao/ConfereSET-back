#!/usr/bin/env bash
set -euo pipefail

EC2_HOST="52.6.22.249"
EC2_USER="ubuntu"
SSH_KEY="/c/keys/kpr_b7.pem"

REMOTE_TMP="/tmp/confere-back-new"
REMOTE_APP="/var/www/confere-back"
SERVICE_NAME="confere-back"

PROJECT_PATH="./ControlApi/ControlApi.csproj"
PUBLISH_DIR="./ControlApi/bin/Release/net8.0/publish"

HEALTH_LOCAL="http://127.0.0.1:5000/api/Health"
HEALTH_NGINX="http://localhost/api/Health"

die() { echo "ERRO: $*" >&2; exit 1; }
need_cmd() { command -v "$1" >/dev/null 2>&1 || die "Comando necessário não encontrado: $1"; }

need_cmd dotnet
need_cmd ssh
need_cmd scp

[ -f "$SSH_KEY" ] || die "Chave não encontrada em: $SSH_KEY"
[ -f "$PROJECT_PATH" ] || die "Projeto não encontrado em: $PROJECT_PATH"

echo "== 1) Publish =="
dotnet publish "$PROJECT_PATH" -c Release

[ -d "$PUBLISH_DIR" ] || die "Publish não encontrado em: $PUBLISH_DIR"

echo "== 2) Upload para /tmp na EC2 =="
ssh -i "$SSH_KEY" "${EC2_USER}@${EC2_HOST}" "mkdir -p '$REMOTE_TMP' && rm -rf '$REMOTE_TMP'/*"
scp -i "$SSH_KEY" -r "${PUBLISH_DIR}/"* "${EC2_USER}@${EC2_HOST}:${REMOTE_TMP}/"

echo "== 3) Trocar arquivos em /var/www/confere-back (sem backup) =="
ssh -i "$SSH_KEY" "${EC2_USER}@${EC2_HOST}" "set -euo pipefail
  sudo systemctl stop '$SERVICE_NAME' || true

  sudo mkdir -p '$REMOTE_APP'
  sudo rm -rf '$REMOTE_APP'/*

  sudo mv '$REMOTE_TMP'/* '$REMOTE_APP'/
  sudo rm -rf '$REMOTE_TMP'

  sudo systemctl start '$SERVICE_NAME'
  sudo systemctl status '$SERVICE_NAME' --no-pager || true

  echo '== Health (Kestrel) =='
  curl -fsS '$HEALTH_LOCAL' && echo || true

  echo '== Health (Nginx) =='
  curl -fsS '$HEALTH_NGINX' && echo || true
"

echo "== OK =="
echo "API: http://${EC2_HOST}/api/Health"