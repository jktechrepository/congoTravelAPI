#!/bin/bash

# Script de test pour vérifier que plusieurs devices peuvent coexister pour un utilisateur
# Ce test vérifie que la correction de la logique UserDevice fonctionne correctement

BASE_URL="https://localhost:7110"
EMAIL="admin@kenergie.cd"
PASSWORD="Admin"

# Couleurs pour l'affichage
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  Test: Multiple Devices par Utilisateur${NC}"
echo -e "${BLUE}========================================${NC}\n"

# Étape 1: Authentification initiale
echo -e "${YELLOW}1. Authentification initiale...${NC}"
AUTH_RESPONSE=$(curl -s -k -X POST "${BASE_URL}/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d "{
    \"emailOuTelephone\": \"${EMAIL}\",
    \"motDePasse\": \"${PASSWORD}\",
    \"fcmToken\": \"FCM_TOKEN_DEVICE_1_$(date +%s)\",
    \"deviceType\": \"Android\",
    \"deviceModel\": \"Samsung Galaxy S21\",
    \"osVersion\": \"Android 12\"
  }")

# Extraire le token et l'ID utilisateur
TOKEN=$(echo $AUTH_RESPONSE | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)
USER_ID=$(echo $AUTH_RESPONSE | grep -o '"idUtilisateur":[0-9]*' | cut -d':' -f2)

if [ -z "$TOKEN" ] || [ -z "$USER_ID" ]; then
  echo -e "${RED}❌ Erreur d'authentification${NC}"
  echo "Réponse: $AUTH_RESPONSE"
  exit 1
fi

echo -e "${GREEN}✅ Authentification réussie${NC}"
echo "   User ID: $USER_ID"
echo "   Token: ${TOKEN:0:50}..."

# Étape 2: Vérifier le premier device
echo -e "\n${YELLOW}2. Vérification du premier device (Android)...${NC}"
DEVICES_RESPONSE=$(curl -s -k -X GET "${BASE_URL}/api/UserDevice/utilisateur/${USER_ID}" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json")

DEVICE_COUNT=$(echo $DEVICES_RESPONSE | grep -o '"idUserDevice"' | wc -l | tr -d ' ')
echo "   Nombre de devices trouvés: $DEVICE_COUNT"

if [ "$DEVICE_COUNT" -ge "1" ]; then
  echo -e "${GREEN}✅ Premier device enregistré${NC}"
else
  echo -e "${RED}❌ Aucun device trouvé${NC}"
  exit 1
fi

# Étape 3: Se connecter avec un deuxième device Android (même type)
echo -e "\n${YELLOW}3. Connexion avec un deuxième device Android (même type)...${NC}"
AUTH_RESPONSE_2=$(curl -s -k -X POST "${BASE_URL}/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d "{
    \"emailOuTelephone\": \"${EMAIL}\",
    \"motDePasse\": \"${PASSWORD}\",
    \"fcmToken\": \"FCM_TOKEN_DEVICE_2_$(date +%s)\",
    \"deviceType\": \"Android\",
    \"deviceModel\": \"Xiaomi Redmi Note 10\",
    \"osVersion\": \"Android 11\"
  }")

TOKEN_2=$(echo $AUTH_RESPONSE_2 | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN_2" ]; then
  echo -e "${RED}❌ Erreur lors de la deuxième authentification${NC}"
  exit 1
fi

echo -e "${GREEN}✅ Deuxième authentification réussie${NC}"

# Étape 4: Vérifier que les deux devices existent
echo -e "\n${YELLOW}4. Vérification que les deux devices coexistent...${NC}"
DEVICES_RESPONSE_2=$(curl -s -k -X GET "${BASE_URL}/api/UserDevice/utilisateur/${USER_ID}" \
  -H "Authorization: Bearer ${TOKEN_2}" \
  -H "Content-Type: application/json")

DEVICE_COUNT_2=$(echo $DEVICES_RESPONSE_2 | grep -o '"idUserDevice"' | wc -l | tr -d ' ')
echo "   Nombre de devices trouvés: $DEVICE_COUNT_2"

if [ "$DEVICE_COUNT_2" -ge "2" ]; then
  echo -e "${GREEN}✅ Les deux devices coexistent (attendu: 2, trouvé: $DEVICE_COUNT_2)${NC}"
else
  echo -e "${RED}❌ Problème: Seulement $DEVICE_COUNT_2 device(s) trouvé(s) au lieu de 2${NC}"
  echo "   Réponse: $DEVICES_RESPONSE_2"
  exit 1
fi

# Étape 5: Se connecter avec un troisième device (iOS)
echo -e "\n${YELLOW}5. Connexion avec un troisième device (iOS)...${NC}"
AUTH_RESPONSE_3=$(curl -s -k -X POST "${BASE_URL}/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d "{
    \"emailOuTelephone\": \"${EMAIL}\",
    \"motDePasse\": \"${PASSWORD}\",
    \"fcmToken\": \"FCM_TOKEN_DEVICE_3_$(date +%s)\",
    \"deviceType\": \"iOS\",
    \"deviceModel\": \"iPhone 13 Pro\",
    \"osVersion\": \"iOS 15.4\"
  }")

TOKEN_3=$(echo $AUTH_RESPONSE_3 | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN_3" ]; then
  echo -e "${RED}❌ Erreur lors de la troisième authentification${NC}"
  exit 1
fi

echo -e "${GREEN}✅ Troisième authentification réussie${NC}"

# Étape 6: Vérifier que les trois devices existent
echo -e "\n${YELLOW}6. Vérification finale: les trois devices coexistent...${NC}"
DEVICES_RESPONSE_3=$(curl -s -k -X GET "${BASE_URL}/api/UserDevice/utilisateur/${USER_ID}" \
  -H "Authorization: Bearer ${TOKEN_3}" \
  -H "Content-Type: application/json")

DEVICE_COUNT_3=$(echo $DEVICES_RESPONSE_3 | grep -o '"idUserDevice"' | wc -l | tr -d ' ')
echo "   Nombre de devices trouvés: $DEVICE_COUNT_3"

# Afficher les détails des devices
echo -e "\n${BLUE}Détails des devices:${NC}"
echo "$DEVICES_RESPONSE_3" | grep -o '"deviceType":"[^"]*"' | sed 's/"deviceType":"/   - Type: /' | sed 's/"$//'
echo "$DEVICES_RESPONSE_3" | grep -o '"deviceModel":"[^"]*"' | sed 's/"deviceModel":"/   - Model: /' | sed 's/"$//'

if [ "$DEVICE_COUNT_3" -ge "3" ]; then
  echo -e "\n${GREEN}========================================${NC}"
  echo -e "${GREEN}✅ TEST RÉUSSI${NC}"
  echo -e "${GREEN}   Les $DEVICE_COUNT_3 devices coexistent correctement${NC}"
  echo -e "${GREEN}   La correction fonctionne !${NC}"
  echo -e "${GREEN}========================================${NC}"
  exit 0
else
  echo -e "\n${RED}========================================${NC}"
  echo -e "${RED}❌ TEST ÉCHOUÉ${NC}"
  echo -e "${RED}   Seulement $DEVICE_COUNT_3 device(s) trouvé(s) au lieu de 3${NC}"
  echo -e "${RED}   Les devices s'écrasent encore !${NC}"
  echo -e "${RED}========================================${NC}"
  exit 1
fi

