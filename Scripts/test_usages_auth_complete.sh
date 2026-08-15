#!/bin/bash

# Script de test complet pour vérifier que les usages sont retournés dans la réponse d'authentification
# Ce script vérifie d'abord si l'application est en cours d'exécution

BASE_URL="https://localhost:7110"
COLOR_GREEN='\033[0;32m'
COLOR_RED='\033[0;31m'
COLOR_YELLOW='\033[1;33m'
COLOR_BLUE='\033[0;34m'
COLOR_NC='\033[0m'

echo -e "${COLOR_BLUE}═══════════════════════════════════════════════════════════${COLOR_NC}"
echo -e "${COLOR_BLUE}🧪 Test : Usages dans la Réponse d'Authentification${COLOR_NC}"
echo -e "${COLOR_BLUE}═══════════════════════════════════════════════════════════${COLOR_NC}"
echo ""

# Vérifier si jq est installé
if ! command -v jq &> /dev/null; then
    echo -e "${COLOR_RED}❌ jq n'est pas installé.${COLOR_NC}"
    echo "   Installer avec: brew install jq (macOS)"
    exit 1
fi

# Vérifier si l'application est en cours d'exécution
echo -e "${COLOR_BLUE}🔍 Vérification de l'état de l'application...${COLOR_NC}"
HTTP_CODE=$(curl -k -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/Utilisateur/authentifier" \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone":"test","motDePasse":"test"}' 2>/dev/null)

if [ "$HTTP_CODE" == "000" ] || [ -z "$HTTP_CODE" ]; then
    echo -e "${COLOR_RED}❌ L'application n'est pas accessible sur ${BASE_URL}${COLOR_NC}"
    echo ""
    echo -e "${COLOR_YELLOW}💡 Pour démarrer l'application:${COLOR_NC}"
    echo "   1. cd /Users/mac/Documents/CongoTravelApi"
    echo "   2. dotnet run"
    echo "   3. Attendre que l'application démarre (généralement sur le port 7110)"
    echo "   4. Relancer ce script"
    echo ""
    exit 1
fi

echo -e "${COLOR_GREEN}✅ Application accessible${COLOR_NC}"
echo ""

# Demander les identifiants
echo -e "${COLOR_YELLOW}📝 Entrez les identifiants d'un utilisateur CLIENT avec des usages:${COLOR_NC}"
read -p "Email ou Téléphone: " EMAIL
read -sp "Mot de passe: " PASSWORD
echo ""

if [ -z "$EMAIL" ] || [ -z "$PASSWORD" ]; then
    echo -e "${COLOR_RED}❌ Email et mot de passe requis${COLOR_NC}"
    exit 1
fi

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
    MESSAGE=$(echo "$RESPONSE" | jq -r '.message' 2>/dev/null)
    if [ -n "$MESSAGE" ] && [ "$MESSAGE" != "null" ]; then
        echo "Message: $MESSAGE"
    fi
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
            echo "$RESPONSE" | jq -r '.client.usages[] | "  ✅ \(.libelle) - Bâtiments: \(.nombreBatiment) - Statut: \(.statut)"' 2>/dev/null
            
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
                echo -e "${COLOR_GREEN}✅ Tous les usages ont les propriétés requises${COLOR_NC}"
            else
                echo -e "${COLOR_RED}❌ $MISSING_PROPS usage(s) avec des propriétés manquantes${COLOR_NC}"
            fi
        else
            echo -e "${COLOR_YELLOW}⚠️  Le client n'a pas d'usages (liste vide)${COLOR_NC}"
            echo "   Cela peut être normal si le client n'a pas encore d'usages assignés"
        fi
    else
        echo -e "${COLOR_RED}❌ client.usages n'existe pas dans la réponse${COLOR_NC}"
        echo ""
        echo "Réponse client:"
        echo "$RESPONSE" | jq '.client' 2>/dev/null
        exit 1
    fi
else
    echo -e "${COLOR_YELLOW}⚠️  L'utilisateur n'est pas un client (client: null)${COLOR_NC}"
    echo "   Ce test nécessite un utilisateur avec IdClient associé"
    echo ""
    echo "Pour tester avec un client:"
    echo "  1. Créer un client via l'API"
    echo "  2. Assigner des usages au client"
    echo "  3. Créer un utilisateur associé à ce client"
    exit 0
fi

echo ""
echo -e "${COLOR_BLUE}═══════════════════════════════════════════════════════════${COLOR_NC}"
echo -e "${COLOR_GREEN}✅ Test terminé avec succès!${COLOR_NC}"
echo -e "${COLOR_BLUE}═══════════════════════════════════════════════════════════${COLOR_NC}"
