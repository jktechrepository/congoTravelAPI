#!/bin/bash

# =====================================================
# Script de Test : Facturation via API
# =====================================================
# Ce script teste les endpoints de facturation après la refactorisation
# =====================================================

# Configuration
API_BASE_URL="https://localhost:7110/api"
TOKEN=""  # À remplir avec votre token JWT

# Couleurs pour l'affichage
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=========================================="
echo "Test de Facturation avec le modèle Usage"
echo "=========================================="
echo ""

# Fonction pour afficher les résultats
print_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✅ $2${NC}"
    else
        echo -e "${RED}❌ $2${NC}"
    fi
}

# Vérifier que le token est fourni
if [ -z "$TOKEN" ]; then
    echo -e "${YELLOW}⚠️  Veuillez définir votre token JWT dans la variable TOKEN${NC}"
    echo "   Exemple: export TOKEN='votre_token_jwt'"
    exit 1
fi

# Headers pour les requêtes
HEADERS=(
    -H "Authorization: Bearer $TOKEN"
    -H "Content-Type: application/json"
    -k  # Ignorer les certificats SSL pour localhost
)

echo "1. Test : Récupérer les factures existantes"
echo "--------------------------------------------"
RESPONSE=$(curl -s "${HEADERS[@]}" "$API_BASE_URL/Facture")
if echo "$RESPONSE" | grep -q "IdFacture"; then
    print_result 0 "Factures récupérées avec succès"
    echo "$RESPONSE" | jq '.[0:2]' 2>/dev/null || echo "$RESPONSE"
else
    print_result 1 "Erreur lors de la récupération des factures"
    echo "$RESPONSE"
fi
echo ""

echo "2. Test : Récupérer les usages d'un client"
echo "------------------------------------------"
RESPONSE=$(curl -s "${HEADERS[@]}" "$API_BASE_URL/Client/1/usages")
if echo "$RESPONSE" | grep -q "IdUsage\|idUsage"; then
    print_result 0 "Usages du client récupérés avec succès"
    echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"
else
    print_result 1 "Erreur ou aucun usage trouvé pour le client"
    echo "$RESPONSE"
fi
echo ""

echo "3. Test : Créer une nouvelle facture"
echo "-------------------------------------"
FACTURE_DATA='{
    "montant": 75000.00,
    "dateEmission": "2024-12-22",
    "moisEmission": 12,
    "anneesEmission": 2024,
    "idUsage": 1,
    "statut": true
}'

RESPONSE=$(curl -s -X POST "${HEADERS[@]}" -d "$FACTURE_DATA" "$API_BASE_URL/Facture")
if echo "$RESPONSE" | grep -q "IdFacture\|idFacture"; then
    print_result 0 "Facture créée avec succès"
    FACTURE_ID=$(echo "$RESPONSE" | jq -r '.IdFacture // .idFacture' 2>/dev/null)
    echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"
    
    if [ ! -z "$FACTURE_ID" ] && [ "$FACTURE_ID" != "null" ]; then
        echo ""
        echo "4. Test : Récupérer la facture créée (ID: $FACTURE_ID)"
        echo "------------------------------------------------------"
        RESPONSE2=$(curl -s "${HEADERS[@]}" "$API_BASE_URL/Facture/$FACTURE_ID")
        if echo "$RESPONSE2" | grep -q "IdFacture\|idFacture"; then
            print_result 0 "Facture récupérée avec succès"
            echo "$RESPONSE2" | jq '.' 2>/dev/null || echo "$RESPONSE2"
        else
            print_result 1 "Erreur lors de la récupération de la facture"
            echo "$RESPONSE2"
        fi
    fi
else
    print_result 1 "Erreur lors de la création de la facture"
    echo "$RESPONSE"
fi
echo ""

echo "5. Test : Calculer les arriérés d'un client"
echo "--------------------------------------------"
RESPONSE=$(curl -s "${HEADERS[@]}" "$API_BASE_URL/Arrieres/client/1")
if echo "$RESPONSE" | grep -q "totalArrieres\|TotalArrieres"; then
    print_result 0 "Arriérés calculés avec succès"
    echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"
    
    # Vérifier que nombreBatiment est pris en compte
    if echo "$RESPONSE" | grep -q "facturesImpayees"; then
        echo ""
        echo -e "${YELLOW}⚠️  Vérifiez manuellement que les montants sont multipliés par nombreBatiment${NC}"
    fi
else
    print_result 1 "Erreur lors du calcul des arriérés"
    echo "$RESPONSE"
fi
echo ""

echo "6. Test : Récupérer les factures par catégorie (via usage)"
echo "-----------------------------------------------------------"
RESPONSE=$(curl -s "${HEADERS[@]}" "$API_BASE_URL/Facture/categorie/1")
if echo "$RESPONSE" | grep -q "IdFacture\|idFacture\|\[\]"; then
    print_result 0 "Factures par catégorie récupérées"
    echo "$RESPONSE" | jq 'length' 2>/dev/null || echo "Réponse reçue"
    echo "$RESPONSE" | jq '.[0:2]' 2>/dev/null || echo "$RESPONSE"
else
    print_result 1 "Erreur lors de la récupération"
    echo "$RESPONSE"
fi
echo ""

echo "=========================================="
echo "Tests terminés"
echo "=========================================="
echo ""
echo "Pour tester manuellement :"
echo "1. Accédez à Swagger UI : https://localhost:7110/swagger"
echo "2. Authentifiez-vous avec votre token"
echo "3. Testez les endpoints de facturation"
echo ""
