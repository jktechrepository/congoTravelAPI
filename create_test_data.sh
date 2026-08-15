#!/bin/bash

echo "🔧 Création des données de test pour reservation_with_paiement"
echo "================================================================"

BASE_URL="http://localhost:5000/api"

# 1. Créer un utilisateur
echo "1️⃣ Création d'un utilisateur de test..."
USER_RESPONSE=$(curl -s -X POST "$BASE_URL/utilisateur" \
  -H "Content-Type: application/json" \
  -d '{
    "nom": "Test",
    "prenom": "User",
    "email": "test.user@example.com",
    "telephone": "123456789",
    "motDePasse": "Test123456!",
    "idSociete": 1,
    "idRole": 2
  }')

USER_ID=$(echo $USER_RESPONSE | grep -o '"idUtilisateur":[0-9]*' | cut -d':' -f2)
echo "   ✅ Utilisateur créé avec ID: $USER_ID"

# 2. Créer un client
echo "2️⃣ Création d'un client de test..."
CLIENT_RESPONSE=$(curl -s -X POST "$BASE_URL/client" \
  -H "Content-Type: application/json" \
  -d '{
    "nom": "Test",
    "prenom": "Client",
    "email": "test.client@example.com",
    "telephone": "987654321",
    "idSociete": 1
  }')

CLIENT_ID=$(echo $CLIENT_RESPONSE | grep -o '"idClient":[0-9]*' | cut -d':' -f2)
echo "   ✅ Client créé avec ID: $CLIENT_ID"

# 3. Créer un voyage
echo "3️⃣ Création d'un voyage de test..."
VOYAGE_RESPONSE=$(curl -s -X POST "$BASE_URL/voyage" \
  -H "Content-Type: application/json" \
  -d '{
    "idBus": 1,
    "idDestination": 1,
    "dateVoyage": "'$(date -d "+1 day" -I)'",
    "heureVoyage": "08:00",
    "prixVoyage": 25000,
    "idSociete": 1
  }')

VOYAGE_ID=$(echo $VOYAGE_RESPONSE | grep -o '"idVoyage":[0-9]*' | cut -d':' -f2)
echo "   ✅ Voyage créé avec ID: $VOYAGE_ID"

echo ""
echo "📊 Données de test créées :"
echo "   - Utilisateur ID: $USER_ID"
echo "   - Client ID: $CLIENT_ID" 
echo "   - Voyage ID: $VOYAGE_ID"
echo ""

# 4. Tester notre endpoint avec les IDs créés
echo "4️⃣ Test de l'endpoint reservation_with_paiement..."
curl -X POST "$BASE_URL/Reservation/reservation_with_paiement" \
  -H "Content-Type: application/json" \
  -d '{
    "reservation": {
      "idVoyage": '$VOYAGE_ID',
      "idClient": '$CLIENT_ID',
      "idUtilisateur": '$USER_ID',
      "idSociete": 1,
      "nombreDePlace": 2
    },
    "paiement": {
      "montantAPaye": 50000,
      "montantPaye": 50000,
      "methodePaiement": "MOBILE_MONEY",
      "referenceTransaction": "TEST_'$(date +%s)'",
      "idUtilisateur": '$USER_ID',
      "idSociete": 1
    }
  }' \
  -w "\n📊 Statistiques HTTP:\n  - Code: %{http_code}\n  - Temps: %{time_total}s\n  - Taille: %{size_download} octets\n" \
  -s | jq .

echo ""
echo "================================================================"
echo "✅ Test terminé"
