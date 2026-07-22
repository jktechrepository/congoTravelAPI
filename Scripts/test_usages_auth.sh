#!/bin/bash

# Script de test pour vérifier que les usages sont retournés dans la réponse d'authentification

BASE_URL="https://localhost:7110"
COLOR_GREEN='\033[0;32m'
COLOR_RED='\033[0;31m'
COLOR_YELLOW='\033[1;33m'
COLOR_BLUE='\033[0;34m'
COLOR_NC='\033[0m' # No Color

echo -e "${COLOR_BLUE}═══════════════════════════════════════════════════════════${COLOR_NC}"
echo -e "${COLOR_BLUE}🧪 Test : Usages dans la Réponse d'Authentification${COLOR_NC}"
echo -e "${COLOR_BLUE}═══════════════════════════════════════════════════════════${COLOR_NC}"
echo ""

# Vérifier si jq est installé
if ! command -v jq &> /dev/null; then
    echo -e "${COLOR_RED}❌ jq n'est pas installé. Installation requise pour ce script.${COLOR_NC}"
    echo "   Installer avec: brew install jq (macOS) ou apt-get install jq (Linux)"
    exit 1
fi

# Demander les identifiants
echo -e "${COLOR_YELLOW}📝 Entrez les identifiants d'un utilisateur CLIENT avec des usages:${COLOR_NC}"
read -p "Email ou Téléphone: " EMAIL
read -sp "Mot de passe: " PASSWORD
echo ""

echo ""
echo -e "${COLOR_BLUE}🔐 Authentification en cours...${COLOR_NC}"

# Authentification
RESPONSE=$(curl -k -s -X POST "${BASE_URL}/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d "{\"emailOuTelephone\":\"${EMAIL}\",\"motDePasse\":\"${PASSWORD}\"}")

# Vérifier si la requête a réussi
HTTP_CODE=$(curl -k -s -o /dev/null -w "%{http_code}" -X POST "${BASE_URL}/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d "{\"emailOuTelephone\":\"${EMAIL}\",\"motDePasse\":\"${PASSWORD}\"}")

if [ "$HTTP_CODE" != "200" ]; then
    echo -e "${COLOR_RED}❌ Erreur HTTP: $HTTP_CODE${COLOR_NC}"
    echo "Réponse:"
    echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"
    exit 1
fi

# Vérifier si la réponse contient success: true
SUCCESS=$(echo "$RESPONSE" | jq -r '.success' 2>/dev/null)

if [ "$SUCCESS" != "true" ]; then
    echo -e "${COLOR_RED}❌ Authentification échouée${COLOR_NC}"
    echo "Réponse:"
    echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"
    exit 1
fi

echo -e "${COLOR_GREEN}✅ Authentification réussie${COLOR_NC}"
echo ""

# Vérifier si client existe
if echo "$RESPONSE" | jq -e '.client' > /dev/null 2>&1; then
    echo -e "${COLOR_GREEN}✅ client existe dans la réponse${COLOR_NC}"
    
    # Vérifier si usages existe
    if echo "$RESPONSE" | jq -e '.client.usages' > /dev/null 2>&1; then
        echo -e "${COLOR_GREEN}✅ client.usages existe${COLOR_NC}"
        
        # Compter les usages
        COUNT=$(echo "$RESPONSE" | jq '.client.usages | length' 2>/dev/null)
        echo -e "${COLOR_BLUE}📊 Nombre d'usages: ${COLOR_NC}${COUNT}"
        
        if [ "$COUNT" -gt 0 ]; then
            echo ""
            echo -e "${COLOR_BLUE}📋 Liste des usages:${COLOR_NC}"
            echo "$RESPONSE" | jq '.client.usages[] | {
                idUsage,
                libelle,
                nombreBatiment,
                dateAttribution,
                statut
            }' 2>/dev/null
            
            # Vérifier que tous les usages sont actifs
            INACTIFS=$(echo "$RESPONSE" | jq '[.client.usages[] | select(.statut == false)] | length' 2>/dev/null)
            if [ "$INACTIFS" -eq 0 ]; then
                echo ""
                echo -e "${COLOR_GREEN}✅ Tous les usages sont actifs (statut: true)${COLOR_NC}"
            else
                echo ""
                echo -e "${COLOR_YELLOW}⚠️  Attention: $INACTIFS usage(s) inactif(s) trouvé(s)${COLOR_NC}"
            fi
            
            # Vérifier les propriétés requises
            echo ""
            echo -e "${COLOR_BLUE}🔍 Vérification des propriétés:${COLOR_NC}"
            MISSING_PROPS=$(echo "$RESPONSE" | jq '[.client.usages[] | select(.idUsage == null or .libelle == null or .libelle == "")] | length' 2>/dev/null)
            if [ "$MISSING_PROPS" -eq 0 ]; then
                echo -e "${COLOR_GREEN}✅ Tous les usages ont les propriétés requises (idUsage, libelle)${COLOR_NC}"
            else
                echo -e "${COLOR_RED}❌ $MISSING_PROPS usage(s) avec des propriétés manquantes${COLOR_NC}"
            fi
        else
            echo -e "${COLOR_YELLOW}⚠️  Le client n'a pas d'usages (liste vide)${COLOR_NC}"
        fi
    else
        echo -e "${COLOR_RED}❌ client.usages n'existe pas dans la réponse${COLOR_NC}"
        echo "Réponse client:"
        echo "$RESPONSE" | jq '.client' 2>/dev/null
        exit 1
    fi
else
    echo -e "${COLOR_YELLOW}⚠️  L'utilisateur n'est pas un client (client: null)${COLOR_NC}"
    echo "   Ce test nécessite un utilisateur avec IdClient associé"
    exit 0
fi

echo ""
echo -e "${COLOR_BLUE}═══════════════════════════════════════════════════════════${COLOR_NC}"
echo -e "${COLOR_GREEN}✅ Test terminé avec succès!${COLOR_NC}"
echo -e "${COLOR_BLUE}═══════════════════════════════════════════════════════════${COLOR_NC}"
