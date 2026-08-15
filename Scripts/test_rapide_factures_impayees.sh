#!/bin/bash

# Script de test rapide pour les factures impayées
# Usage: ./scripts/test_rapide_factures_impayees.sh [idSociete]

BASE_URL="https://localhost:7110"
EMAIL="admin@kenergie.cd"
PASSWORD="Admin"
ID_SOCIETE=${1:-1}

echo "🔐 Authentification..."
TOKEN=$(curl -k -s -X POST "${BASE_URL}/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d "{\"emailOuTelephone\":\"${EMAIL}\",\"motDePasse\":\"${PASSWORD}\"}" \
  | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "❌ Erreur d'authentification"
  exit 1
fi

echo "✅ Authentifié"
echo ""

echo "📋 Test 1: Liste complète (Société ID=${ID_SOCIETE})"
HTTP_CODE=$(curl -k -s -o /tmp/factures_impayees.json -w "%{http_code}" \
  -X GET "${BASE_URL}/api/Paiement/societe/${ID_SOCIETE}/factureImpayee" \
  -H "Authorization: Bearer ${TOKEN}")

if [ "$HTTP_CODE" = "200" ]; then
  echo "✅ Code HTTP: $HTTP_CODE"
  python3 -m json.tool /tmp/factures_impayees.json 2>/dev/null || cat /tmp/factures_impayees.json
elif [ "$HTTP_CODE" = "404" ]; then
  echo "❌ Code HTTP: $HTTP_CODE - Route non trouvée"
  echo "⚠️  L'application doit être redémarrée pour que les nouveaux endpoints soient disponibles"
else
  echo "❌ Code HTTP: $HTTP_CODE"
  cat /tmp/factures_impayees.json
fi

echo ""
echo "📋 Test 2: Liste paginée"
HTTP_CODE=$(curl -k -s -o /tmp/factures_impayees_paged.json -w "%{http_code}" \
  -X GET "${BASE_URL}/api/Paiement/societe/${ID_SOCIETE}/paged/factureImpayee" \
  -H "Authorization: Bearer ${TOKEN}")

if [ "$HTTP_CODE" = "200" ]; then
  echo "✅ Code HTTP: $HTTP_CODE"
  python3 -m json.tool /tmp/factures_impayees_paged.json 2>/dev/null || cat /tmp/factures_impayees_paged.json
elif [ "$HTTP_CODE" = "404" ]; then
  echo "❌ Code HTTP: $HTTP_CODE - Route non trouvée"
  echo "⚠️  L'application doit être redémarrée pour que les nouveaux endpoints soient disponibles"
else
  echo "❌ Code HTTP: $HTTP_CODE"
  cat /tmp/factures_impayees_paged.json
fi

rm -f /tmp/factures_impayees*.json

