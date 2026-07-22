#!/usr/bin/env bash
# Smoke tests post-déploiement — CongoTravelAPI
# Usage: BASE_URL=https://api.example.com ./Scripts/smoke_tests.sh
#        BASE_URL=http://localhost:5110 TOKEN=eyJ... ./Scripts/smoke_tests.sh

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5110}"
TOKEN="${TOKEN:-}"

failures=0

check() {
  local name="$1"
  local url="$2"
  local expected="${3:-200}"
  local extra_args=("${@:4}")

  local code
  if [[ -n "$TOKEN" ]]; then
    code=$(curl -s -o /dev/null -w "%{http_code}" "${extra_args[@]}" -H "Authorization: Bearer $TOKEN" "$url" || echo "000")
  else
    code=$(curl -s -o /dev/null -w "%{http_code}" "${extra_args[@]}" "$url" || echo "000")
  fi

  if [[ "$code" == "$expected" ]]; then
    echo "OK   $name ($code)"
  else
    echo "FAIL $name (attendu $expected, reçu $code) — $url"
    failures=$((failures + 1))
  fi
}

echo "Smoke tests CongoTravel — $BASE_URL"
echo "---"

check "health/ready" "$BASE_URL/health/ready" "200"
check "health/live" "$BASE_URL/health/live" "200"
check "swagger" "$BASE_URL/swagger/index.html" "200"

if [[ -n "$TOKEN" ]]; then
  check "voyages (auth)" "$BASE_URL/api/Voyage" "200"
else
  echo "SKIP voyages (TOKEN non fourni)"
fi

echo "---"
if [[ "$failures" -gt 0 ]]; then
  echo "$failures test(s) en échec"
  exit 1
fi

echo "Tous les smoke tests sont passés."
