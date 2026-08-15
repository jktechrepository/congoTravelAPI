#!/bin/bash

# Script de test simplifié pour vérifier que les usages sont retournés dans la réponse d'authentification
# Usage: ./test_usages_auth_simple.sh [email] [password]

BASE_URL="https://localhost:7110"

# Couleurs
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Récupérer les arguments ou demander
EMAIL=${1:-""}
PASSWORD=${2:-""}

if [ -z "$EMAIL" ]; then
    echo -e "${BLUE}📝 Entrez les identifiants d'un utilisateur CLIENT:${NC}"
    read -p "Email ou Téléphone: " EMAIL
    read -sp "Mot de passe: " PASSWORD
    echo ""
fi

echo -e "${BLUE}🔐 Authentification...${NC}"

RESPONSE=$(curl -k -s -X POST "${BASE_URL}/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d "{\"emailOuTelephone\":\"${EMAIL}\",\"motDePasse\":\"${PASSWORD}\"}")

# Vérifier le succès
SUCCESS=$(echo "$RESPONSE" | jq -r '.success' 2>/dev/null)

if [ "$SUCCESS" != "true" ]; then
    echo -e "${RED}❌ Authentification échouée${NC}"
    echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"
    exit 1
fi

echo -e "${GREEN}✅ Authentification réussie${NC}"
echo ""

# Vérifier client.usages
if echo "$RESPONSE" | jq -e '.client.usages' > /dev/null 2>&1; then
    COUNT=$(echo "$RESPONSE" | jq '.client.usages | length' 2>/dev/null)
    echo -e "${GREEN}✅ client.usages existe (${COUNT} usage(s))${NC}"
    
    if [ "$COUNT" -gt 0 ]; then
        echo ""
        echo -e "${BLUE}📋 Usages:${NC}"
        echo "$RESPONSE" | jq '.client.usages[] | "  - \(.libelle) (Bâtiments: \(.nombreBatiment), Statut: \(.statut))"' 2>/dev/null
    fi
else
    echo -e "${RED}❌ client.usages n'existe pas${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}✅ Test réussi!${NC}"
