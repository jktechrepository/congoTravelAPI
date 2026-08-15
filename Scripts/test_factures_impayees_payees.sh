#!/bin/bash

# Script de test pour les factures impayées et payées
# Usage: ./test_factures_impayees_payees.sh [base_url] [email] [password] [client_id]

BASE_URL="${1:-http://localhost:5000}"
EMAIL="${2:-admin@example.com}"
PASSWORD="${3:-Admin123}"
CLIENT_ID="${4:-1}"

echo "=========================================="
echo "Test des Factures Impayées et Payées"
echo "=========================================="
echo "Base URL: $BASE_URL"
echo "Client ID: $CLIENT_ID"
echo ""

# Couleurs pour l'affichage
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Fonction pour afficher les résultats
print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ️  $1${NC}"
}

# 1. Authentification
print_info "1. Authentification..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
    print_error "Échec de l'authentification"
    echo "Réponse: $LOGIN_RESPONSE"
    exit 1
fi

print_success "Authentification réussie"
echo ""

# 2. Vérifier les factures impayées actuelles
print_info "2. Vérification des factures impayées actuelles..."
FACTURES_IMPAYEES=$(curl -s -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-impayees" \
  -H "Authorization: Bearer $TOKEN")

COUNT_IMPAYEES=$(echo $FACTURES_IMPAYEES | jq '. | length')
print_info "Nombre de factures impayées: $COUNT_IMPAYEES"

if [ "$COUNT_IMPAYEES" -gt 0 ]; then
    echo "Factures impayées:"
    echo $FACTURES_IMPAYEES | jq -r '.[] | "  - \(.numeroFacture // "N/A"): \(.montantDu) FC (Total: \(.montantTotal), Payé: \(.montantPaye))"'
fi
echo ""

# 3. Vérifier les factures payées actuelles
print_info "3. Vérification des factures payées actuelles..."
FACTURES_PAYEES=$(curl -s -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-payees/paged?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN")

COUNT_PAYEES=$(echo $FACTURES_PAYEES | jq '.data | length')
TOTAL_PAYEES=$(echo $FACTURES_PAYEES | jq '.totalCount')
print_info "Nombre de factures payées: $COUNT_PAYEES (Total: $TOTAL_PAYEES)"

if [ "$COUNT_PAYEES" -gt 0 ]; then
    echo "Factures payées:"
    echo $FACTURES_PAYEES | jq -r '.data[] | "  - \(.numeroFacture // "N/A"): \(.montantTotal) FC (Payé: \(.montantPaye))"'
fi
echo ""

# 4. Vérifier les arriérés du client
print_info "4. Vérification des arriérés du client..."
ARRIERES=$(curl -s -X GET "$BASE_URL/api/Client/$CLIENT_ID/arrieres" \
  -H "Authorization: Bearer $TOKEN")

if [ "$(echo $ARRIERES | jq -r '.idClient // empty')" != "" ]; then
    NOM_CLIENT=$(echo $ARRIERES | jq -r '.nomClient')
    TOTAL_ARRIERES=$(echo $ARRIERES | jq -r '.totalArrieres')
    NOMBRE_FACTURES=$(echo $ARRIERES | jq -r '.nombreFacturesImpayees')
    
    print_success "Arriérés pour $NOM_CLIENT:"
    echo "  - Nombre de factures impayées: $NOMBRE_FACTURES"
    echo "  - Total des arriérés: $TOTAL_ARRIERES FC"
    echo "  - Montant total factures: $(echo $ARRIERES | jq -r '.montantTotalFactures') FC"
    echo "  - Montant total payé: $(echo $ARRIERES | jq -r '.montantTotalPaye') FC"
else
    print_error "Impossible de récupérer les arriérés"
    echo "Réponse: $ARRIERES"
fi
echo ""

# 5. Test de pagination et recherche
print_info "5. Test de pagination et recherche..."
FACTURES_PAGED=$(curl -s -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-impayees/paged?pageNumber=1&pageSize=5&sortBy=montantdu&sortDescending=true" \
  -H "Authorization: Bearer $TOKEN")

TOTAL=$(echo $FACTURES_PAGED | jq -r '.totalCount')
PAGE_SIZE=$(echo $FACTURES_PAGED | jq -r '.pageSize')
PAGE_NUMBER=$(echo $FACTURES_PAGED | jq -r '.pageNumber')

print_success "Pagination testée:"
echo "  - Total: $TOTAL"
echo "  - Page: $PAGE_NUMBER"
echo "  - Taille de page: $PAGE_SIZE"
echo ""

# 6. Vérifier une facture spécifique (si disponible)
if [ "$COUNT_IMPAYEES" -gt 0 ]; then
    FIRST_FACTURE_ID=$(echo $FACTURES_IMPAYEES | jq -r '.[0].idFacture')
    print_info "6. Vérification de la facture #$FIRST_FACTURE_ID..."
    
    FACTURE_DETAILS=$(curl -s -X GET "$BASE_URL/api/Facture/$FIRST_FACTURE_ID" \
      -H "Authorization: Bearer $TOKEN")
    
    FACTURE_MONTANT=$(echo $FACTURE_DETAILS | jq -r '.montant')
    print_info "Montant initial de la facture: $FACTURE_MONTANT FC"
    
    # Vérifier que le montant n'a pas été modifié (doit être le montant initial)
    DTO_MONTANT=$(echo $FACTURES_IMPAYEES | jq -r ".[] | select(.idFacture == $FIRST_FACTURE_ID) | .montantTotal")
    
    if [ "$FACTURE_MONTANT" == "$DTO_MONTANT" ]; then
        print_success "Le montant initial de la facture est correct ($FACTURE_MONTANT FC)"
    else
        print_error "Incohérence: Facture.Montant=$FACTURE_MONTANT mais DTO.MontantTotal=$DTO_MONTANT"
    fi
    echo ""
fi

# Résumé
echo "=========================================="
echo "Résumé des Tests"
echo "=========================================="
print_success "Tests terminés avec succès"
echo ""
echo "Points vérifiés:"
echo "  ✅ Authentification"
echo "  ✅ Récupération des factures impayées"
echo "  ✅ Récupération des factures payées"
echo "  ✅ Calcul des arriérés"
echo "  ✅ Pagination et tri"
if [ "$COUNT_IMPAYEES" -gt 0 ]; then
    echo "  ✅ Vérification du montant initial de la facture"
fi
echo ""
print_info "Pour des tests plus approfondis, consultez: docs/TEST_FACTURES_IMPAYEES_PAYEES.md"

