#!/bin/bash

# Script de test pour les endpoints de factures impayées
# Usage: ./scripts/test_factures_impayees.sh [TOKEN] [CLIENT_ID]

BASE_URL="https://localhost:7110"
TOKEN="${1:-}"
CLIENT_ID="${2:-1}"

# Couleurs pour l'affichage
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}=== Test des Endpoints Factures Impayées ===${NC}\n"

# Vérifier si un token est fourni
if [ -z "$TOKEN" ]; then
    echo -e "${RED}❌ Token manquant${NC}"
    echo "Usage: $0 [TOKEN] [CLIENT_ID]"
    echo "Exemple: $0 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...' 1"
    exit 1
fi

echo -e "${GREEN}✅ Token fourni${NC}"
echo -e "${GREEN}✅ Client ID: $CLIENT_ID${NC}\n"

# Test 1: Liste complète des factures impayées
echo -e "${YELLOW}Test 1: Liste complète des factures impayées${NC}"
RESPONSE=$(curl -k -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-impayees" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" -eq 200 ]; then
    echo -e "${GREEN}✅ Succès (HTTP $HTTP_CODE)${NC}"
    echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"
else
    echo -e "${RED}❌ Échec (HTTP $HTTP_CODE)${NC}"
    echo "$BODY"
fi
echo ""

# Test 2: Liste paginée (page 1)
echo -e "${YELLOW}Test 2: Liste paginée (page 1, 10 éléments)${NC}"
RESPONSE=$(curl -k -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-impayees/paged?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" -eq 200 ]; then
    echo -e "${GREEN}✅ Succès (HTTP $HTTP_CODE)${NC}"
    echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"
else
    echo -e "${RED}❌ Échec (HTTP $HTTP_CODE)${NC}"
    echo "$BODY"
fi
echo ""

# Test 3: Liste paginée avec tri
echo -e "${YELLOW}Test 3: Liste paginée avec tri par montant dû (décroissant)${NC}"
RESPONSE=$(curl -k -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-impayees/paged?pageNumber=1&pageSize=10&sortBy=montantDu&sortDescending=true" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" -eq 200 ]; then
    echo -e "${GREEN}✅ Succès (HTTP $HTTP_CODE)${NC}"
    echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"
else
    echo -e "${RED}❌ Échec (HTTP $HTTP_CODE)${NC}"
    echo "$BODY"
fi
echo ""

# Test 4: Arriérés complets
echo -e "${YELLOW}Test 4: Arriérés complets du client${NC}"
RESPONSE=$(curl -k -s -w "\n%{http_code}" -X GET "$BASE_URL/api/Client/$CLIENT_ID/arrieres" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" -eq 200 ]; then
    echo -e "${GREEN}✅ Succès (HTTP $HTTP_CODE)${NC}"
    echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"
else
    echo -e "${RED}❌ Échec (HTTP $HTTP_CODE)${NC}"
    echo "$BODY"
fi
echo ""

echo -e "${GREEN}=== Tests terminés ===${NC}"

