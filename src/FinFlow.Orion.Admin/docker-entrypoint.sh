#!/bin/sh
# Substitutes the API base URL into the published wwwroot/appsettings.json at
# container start, so the same image can point at different Api deployments
# without rebuilding — set API_BASE_URL to override the default.
set -eu

APPSETTINGS=/usr/share/nginx/html/appsettings.json
API_BASE_URL="${API_BASE_URL:-http://localhost:8080}"

if [ -f "$APPSETTINGS" ]; then
  tmp=$(mktemp)
  sed "s#\"ApiBaseUrl\":[[:space:]]*\"[^\"]*\"#\"ApiBaseUrl\": \"$API_BASE_URL\"#" "$APPSETTINGS" > "$tmp"
  mv "$tmp" "$APPSETTINGS"
fi

exec nginx -g "daemon off;"
