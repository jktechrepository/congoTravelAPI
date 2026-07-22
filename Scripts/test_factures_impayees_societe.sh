#!/bin/bash

# Script de test pour les endpoints de factures impayées par société
# Usage: ./scripts/test_factures_impayees_societe.sh [idSociete]

BASE_URL="https://localhost:7110"
EMAIL="admin@kenergie.cd"
PASSWORD="Admin"

# Couleurs pour l'affichage
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${YELLOW}=== Test des Factures Impayées par Société ===${NC}\n"

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

echo -e "\n${YELLOW}2. Test de l'endpoint LISTE COMPLÈTE${NC}"
echo -e "${BLUE}GET /api/Paiement/societe/${ID_SOCIETE}/factureImpayee${NC}"

HTTP_CODE=$(curl -s -k -o /tmp/response_liste.json -w "%{http_code}" -X GET "${BASE_URL}/api/Paiement/societe/${ID_SOCIETE}/factureImpayee" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json")

if [ "$HTTP_CODE" = "200" ]; then
  echo -e "${GREEN}✅ Requête réussie (HTTP ${HTTP_CODE})${NC}"
  echo -e "\nRéponse (format JSON):"
  cat /tmp/response_liste.json | python3 -m json.tool 2>/dev/null || cat /tmp/response_liste.json
  
  # Compter le nombre de factures impayées
  COUNT=$(cat /tmp/response_liste.json | python3 -c "import sys, json; data = json.load(sys.stdin); print(len(data) if isinstance(data, list) else 0)" 2>/dev/null || echo "0")
  echo -e "\n${GREEN}Nombre de factures impayées: ${COUNT}${NC}"
else
  echo -e "${RED}❌ Erreur HTTP ${HTTP_CODE}${NC}"
  cat /tmp/response_liste.json
  exit 1
fi

echo -e "\n${YELLOW}3. Test de l'endpoint PAGINÉ (sans paramètres)${NC}"
echo -e "${BLUE}GET /api/Paiement/societe/${ID_SOCIETE}/paged/factureImpayee${NC}"

HTTP_CODE=$(curl -s -k -o /tmp/response_paged.json -w "%{http_code}" -X GET "${BASE_URL}/api/Paiement/societe/${ID_SOCIETE}/paged/factureImpayee" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json")

if [ "$HTTP_CODE" = "200" ]; then
  echo -e "${GREEN}✅ Requête réussie (HTTP ${HTTP_CODE})${NC}"
  echo -e "\nRéponse (format JSON):"
  cat /tmp/response_paged.json | python3 -m json.tool 2>/dev/null || cat /tmp/response_paged.json
  
  # Extraire les métadonnées de pagination
  TOTAL=$(cat /tmp/response_paged.json | python3 -c "import sys, json; data = json.load(sys.stdin); print(data.get('totalCount', 0))" 2>/dev/null || echo "0")
  PAGE=$(cat /tmp/response_paged.json | python3 -c "import sys, json; data = json.load(sys.stdin); print(data.get('pageNumber', 0))" 2>/dev/null || echo "0")
  SIZE=$(cat /tmp/response_paged.json | python3 -c "import sys, json; data = json.load(sys.stdin); print(data.get('pageSize', 0))" 2>/dev/null || echo "0")
  echo -e "\n${GREEN}Métadonnées: Total=${TOTAL}, Page=${PAGE}, Taille=${SIZE}${NC}"
else
  echo -e "${RED}❌ Erreur HTTP ${HTTP_CODE}${NC}"
  cat /tmp/response_paged.json
  exit 1
fi

echo -e "\n${YELLOW}4. Test de l'endpoint PAGINÉ avec recherche${NC}"
echo -e "${BLUE}GET /api/Paiement/societe/${ID_SOCIETE}/paged/factureImpayee?searchTerm=0228${NC}"

HTTP_CODE=$(curl -s -k -o /tmp/response_search.json -w "%{http_code}" -X GET "${BASE_URL}/api/Paiement/societe/${ID_SOCIETE}/paged/factureImpayee?searchTerm=0228" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json")

if [ "$HTTP_CODE" = "200" ]; then
  echo -e "${GREEN}✅ Requête réussie (HTTP ${HTTP_CODE})${NC}"
  echo -e "\nRésultats de recherche:"
  cat /tmp/response_search.json | python3 -m json.tool 2>/dev/null || cat /tmp/response_search.json
else
  echo -e "${RED}❌ Erreur HTTP ${HTTP_CODE}${NC}"
  cat /tmp/response_search.json
fi

echo -e "\n${YELLOW}5. Test de l'endpoint PAGINÉ avec tri${NC}"
echo -e "${BLUE}GET /api/Paiement/societe/${ID_SOCIETE}/paged/factureImpayee?sortBy=MontantDu&sortDescending=true${NC}"

HTTP_CODE=$(curl -s -k -o /tmp/response_sort.json -w "%{http_code}" -X GET "${BASE_URL}/api/Paiement/societe/${ID_SOCIETE}/paged/factureImpayee?sortBy=MontantDu&sortDescending=true" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json")

if [ "$HTTP_CODE" = "200" ]; then
  echo -e "${GREEN}✅ Requête réussie (HTTP ${HTTP_CODE})${NC}"
  echo -e "\nRésultats triés par MontantDu (décroissant):"
  cat /tmp/response_sort.json | python3 -m json.tool 2>/dev/null || cat /tmp/response_sort.json
else
  echo -e "${RED}❌ Erreur HTTP ${HTTP_CODE}${NC}"
  cat /tmp/response_sort.json
fi

# Nettoyer les fichiers temporaires
rm -f /tmp/response_*.json

echo -e "\n${GREEN}=== Tests terminés ===${NC}"

