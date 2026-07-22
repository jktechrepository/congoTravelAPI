#!/bin/bash

# Script de test pour le Dashboard
# Usage: ./scripts/test_dashboard.sh [idSociete]

BASE_URL="https://localhost:7110"
EMAIL="admin@kenergie.cd"
PASSWORD="Admin"

# Couleurs pour l'affichage
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}=== Test du Dashboard API ===${NC}\n"

# Étape 1: Authentification
echo -e "${YELLOW}1. Authentification...${NC}"
AUTH_RESPONSE=$(curl -s -k -X POST "${BASE_URL}/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d "{
    \"emailOuTelephone\": \"${EMAIL}\",
    \"motDePasse\": \"${PASSWORD}\"
  }")

# Extraire le token
TOKEN=$(echo $AUTH_RESPONSE | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo -e "${RED}❌ Erreur d'authentification${NC}"
  echo "Réponse: $AUTH_RESPONSE"
  exit 1
fi

echo -e "${GREEN}✅ Authentification réussie${NC}"
echo "Token: ${TOKEN:0:50}..."

# Extraire l'ID de la société depuis la réponse d'authentification
ID_SOCIETE=$(echo $AUTH_RESPONSE | grep -o '"idSociete":[0-9]*' | cut -d':' -f2)

if [ -z "$ID_SOCIETE" ]; then
  # Si pas dans la réponse, utiliser l'argument ou demander
  if [ -z "$1" ]; then
    echo -e "${YELLOW}ID de société non trouvé dans la réponse. Veuillez le fournir:${NC}"
    read -p "ID Société: " ID_SOCIETE
  else
    ID_SOCIETE=$1
  fi
fi

echo -e "\n${YELLOW}2. Récupération des statistiques du Dashboard pour la société ID: ${ID_SOCIETE}...${NC}"

# Étape 2: Appel au Dashboard
DASHBOARD_RESPONSE=$(curl -s -k -X GET "${BASE_URL}/api/Dashboard/${ID_SOCIETE}" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json")

# Vérifier si la requête a réussi
HTTP_CODE=$(curl -s -k -o /dev/null -w "%{http_code}" -X GET "${BASE_URL}/api/Dashboard/${ID_SOCIETE}" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json")

if [ "$HTTP_CODE" = "200" ]; then
  echo -e "${GREEN}✅ Requête réussie (HTTP ${HTTP_CODE})${NC}\n"
  echo "Réponse complète:"
  echo "$DASHBOARD_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$DASHBOARD_RESPONSE"
else
  echo -e "${RED}❌ Erreur HTTP ${HTTP_CODE}${NC}"
  echo "Réponse: $DASHBOARD_RESPONSE"
  exit 1
fi

echo -e "\n${GREEN}=== Test terminé ===${NC}"

