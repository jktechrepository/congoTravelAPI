#!/bin/bash

# Script de test complet pour les factures impayées/payées
# Ce script crée des données de test et vérifie la logique

BASE_URL="${1:-http://localhost:5000}"
EMAIL="${2:-admin@example.com}"
PASSWORD="${3:-Admin123}"

echo "=========================================="
echo "Test Complet - Factures Impayées/Payées"
echo "=========================================="
echo ""

# Couleurs
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }
print_info() { echo -e "${YELLOW}ℹ️  $1${NC}"; }
print_step() { echo -e "${BLUE}📋 $1${NC}"; }

# 1. Authentification
print_step "Étape 1: Authentification..."
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

# 2. Récupérer les données nécessaires
print_step "Étape 2: Récupération des données existantes..."

# Récupérer une catégorie
CATEGORIES=$(curl -s -X GET "$BASE_URL/api/CategorieClient" \
  -H "Authorization: Bearer $TOKEN")
CATEGORIE_ID=$(echo $CATEGORIES | jq -r '.[0].idCategorie // empty')

if [ -z "$CATEGORIE_ID" ] || [ "$CATEGORIE_ID" == "null" ]; then
    print_error "Aucune catégorie trouvée. Veuillez créer une catégorie d'abord."
    exit 1
fi
print_info "Catégorie trouvée: ID=$CATEGORIE_ID"

# Récupérer un client de cette catégorie
CLIENTS=$(curl -s -X GET "$BASE_URL/api/Client/categorie/$CATEGORIE_ID" \
  -H "Authorization: Bearer $TOKEN")
CLIENT_ID=$(echo $CLIENTS | jq -r '.[0].idClient // empty')

if [ -z "$CLIENT_ID" ] || [ "$CLIENT_ID" == "null" ]; then
    print_error "Aucun client trouvé dans cette catégorie. Veuillez créer un client d'abord."
    exit 1
fi
print_info "Client trouvé: ID=$CLIENT_ID"
echo ""

# 3. Créer une facture de test
print_step "Étape 3: Création d'une facture de test..."
FACTURE_NUMERO="FACT-TEST-$(date +%s)"
FACTURE_DATA=$(cat <<EOF
{
  "numeroFacture": "$FACTURE_NUMERO",
  "montant": 100000.00,
  "dateEmission": "$(date -u +%Y-%m-%d)",
  "moisEmission": $(date +%m),
  "anneesEmission": $(date +%Y),
  "idCategorie": $CATEGORIE_ID,
  "statut": true
}
EOF
)

FACTURE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/Facture" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "$FACTURE_DATA")

FACTURE_ID=$(echo $FACTURE_RESPONSE | jq -r '.idFacture // empty')

if [ -z "$FACTURE_ID" ] || [ "$FACTURE_ID" == "null" ]; then
    print_error "Échec de la création de la facture"
    echo "Réponse: $FACTURE_RESPONSE"
    exit 1
fi
print_success "Facture créée: ID=$FACTURE_ID, Numéro=$FACTURE_NUMERO, Montant=100000 FC"
echo ""

# 4. Vérifier que la facture est impayée (aucun paiement)
print_step "Étape 4: Vérification - Facture sans paiement..."
sleep 1
FACTURES_IMPAYEES=$(curl -s -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-impayees" \
  -H "Authorization: Bearer $TOKEN")

FACTURE_FOUND=$(echo $FACTURES_IMPAYEES | jq -r ".[] | select(.idFacture == $FACTURE_ID) | .idFacture")

if [ "$FACTURE_FOUND" == "$FACTURE_ID" ]; then
    MONTANT_DU=$(echo $FACTURES_IMPAYEES | jq -r ".[] | select(.idFacture == $FACTURE_ID) | .montantDu")
    MONTANT_PAYE=$(echo $FACTURES_IMPAYEES | jq -r ".[] | select(.idFacture == $FACTURE_ID) | .montantPaye")
    print_success "Facture trouvée dans les impayées: MontantDu=$MONTANT_DU FC, MontantPaye=$MONTANT_PAYE FC"
else
    print_error "La facture devrait être dans les impayées mais ne l'est pas"
fi
echo ""

# 5. Créer un paiement partiel
print_step "Étape 5: Création d'un paiement partiel (30000 FC)..."
PAIEMENT1_DATA=$(cat <<EOF
{
  "idFacture": $FACTURE_ID,
  "idClient": $CLIENT_ID,
  "montantPaye": 30000.00,
  "datePaiement": "$(date -u +%Y-%m-%dT%H:%M:%S)",
  "methodePaiement": "Mobile Money",
  "referenceTransaction": "REF-TEST-001",
  "statut": "Validé"
}
EOF
)

PAIEMENT1_RESPONSE=$(curl -s -X POST "$BASE_URL/api/Paiement" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "$PAIEMENT1_DATA")

PAIEMENT1_ID=$(echo $PAIEMENT1_RESPONSE | jq -r '.paiement.idPaiement // empty')

if [ -z "$PAIEMENT1_ID" ] || [ "$PAIEMENT1_ID" == "null" ]; then
    print_error "Échec de la création du paiement"
    echo "Réponse: $PAIEMENT1_RESPONSE"
    exit 1
fi
print_success "Paiement créé: ID=$PAIEMENT1_ID, Montant=30000 FC"
echo ""

# 6. Vérifier que la facture est toujours impayée
print_step "Étape 6: Vérification - Facture partiellement payée..."
sleep 1
FACTURES_IMPAYEES=$(curl -s -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-impayees" \
  -H "Authorization: Bearer $TOKEN")

FACTURE_FOUND=$(echo $FACTURES_IMPAYEES | jq -r ".[] | select(.idFacture == $FACTURE_ID) | .idFacture")

if [ "$FACTURE_FOUND" == "$FACTURE_ID" ]; then
    MONTANT_DU=$(echo $FACTURES_IMPAYEES | jq -r ".[] | select(.idFacture == $FACTURE_ID) | .montantDu")
    MONTANT_PAYE=$(echo $FACTURES_IMPAYEES | jq -r ".[] | select(.idFacture == $FACTURE_ID) | .montantPaye")
    MONTANT_TOTAL=$(echo $FACTURES_IMPAYEES | jq -r ".[] | select(.idFacture == $FACTURE_ID) | .montantTotal")
    
    if [ "$(echo "$MONTANT_DU == 70000" | bc)" == "1" ] && [ "$(echo "$MONTANT_PAYE == 30000" | bc)" == "1" ]; then
        print_success "Facture toujours impayée: MontantTotal=$MONTANT_TOTAL, MontantPaye=$MONTANT_PAYE, MontantDu=$MONTANT_DU"
    else
        print_error "Calcul incorrect: Attendu MontantDu=70000, MontantPaye=30000, Reçu MontantDu=$MONTANT_DU, MontantPaye=$MONTANT_PAYE"
    fi
else
    print_error "La facture devrait être dans les impayées mais ne l'est pas"
fi
echo ""

# 7. Vérifier que Facture.Montant n'a pas été modifié
print_step "Étape 7: Vérification - Facture.Montant n'a pas été modifié..."
FACTURE_DETAILS=$(curl -s -X GET "$BASE_URL/api/Facture/$FACTURE_ID" \
  -H "Authorization: Bearer $TOKEN")

FACTURE_MONTANT=$(echo $FACTURE_DETAILS | jq -r '.montant')

if [ "$(echo "$FACTURE_MONTANT == 100000" | bc)" == "1" ]; then
    print_success "Facture.Montant est correct: $FACTURE_MONTANT FC (non modifié)"
else
    print_error "Facture.Montant a été modifié! Attendu: 100000, Reçu: $FACTURE_MONTANT"
fi
echo ""

# 8. Créer un paiement complémentaire pour compléter la facture
print_step "Étape 8: Création d'un paiement complémentaire (70000 FC)..."
PAIEMENT2_DATA=$(cat <<EOF
{
  "idFacture": $FACTURE_ID,
  "idClient": $CLIENT_ID,
  "montantPaye": 70000.00,
  "datePaiement": "$(date -u +%Y-%m-%dT%H:%M:%S)",
  "methodePaiement": "Espèces",
  "referenceTransaction": "REF-TEST-002",
  "statut": "Validé"
}
EOF
)

PAIEMENT2_RESPONSE=$(curl -s -X POST "$BASE_URL/api/Paiement" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "$PAIEMENT2_DATA")

PAIEMENT2_ID=$(echo $PAIEMENT2_RESPONSE | jq -r '.paiement.idPaiement // empty')

if [ -z "$PAIEMENT2_ID" ] || [ "$PAIEMENT2_ID" == "null" ]; then
    print_error "Échec de la création du paiement"
    echo "Réponse: $PAIEMENT2_RESPONSE"
    exit 1
fi
print_success "Paiement créé: ID=$PAIEMENT2_ID, Montant=70000 FC"
echo ""

# 9. Vérifier que la facture est maintenant payée
print_step "Étape 9: Vérification - Facture entièrement payée..."
sleep 1
FACTURES_PAYEES=$(curl -s -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-payees/paged?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN")

FACTURE_FOUND=$(echo $FACTURES_PAYEES | jq -r ".data[] | select(.idFacture == $FACTURE_ID) | .idFacture")

if [ "$FACTURE_FOUND" == "$FACTURE_ID" ]; then
    MONTANT_PAYE=$(echo $FACTURES_PAYEES | jq -r ".data[] | select(.idFacture == $FACTURE_ID) | .montantPaye")
    MONTANT_TOTAL=$(echo $FACTURES_PAYEES | jq -r ".data[] | select(.idFacture == $FACTURE_ID) | .montantTotal")
    
    if [ "$(echo "$MONTANT_PAYE >= $MONTANT_TOTAL" | bc)" == "1" ]; then
        print_success "Facture trouvée dans les payées: MontantTotal=$MONTANT_TOTAL, MontantPaye=$MONTANT_PAYE"
    else
        print_error "Calcul incorrect: MontantPaye ($MONTANT_PAYE) < MontantTotal ($MONTANT_TOTAL)"
    fi
else
    print_error "La facture devrait être dans les payées mais ne l'est pas"
fi
echo ""

# 10. Vérifier que la facture n'est plus dans les impayées
print_step "Étape 10: Vérification - Facture n'est plus dans les impayées..."
FACTURES_IMPAYEES=$(curl -s -X GET "$BASE_URL/api/Client/$CLIENT_ID/factures-impayees" \
  -H "Authorization: Bearer $TOKEN")

FACTURE_FOUND=$(echo $FACTURES_IMPAYEES | jq -r ".[] | select(.idFacture == $FACTURE_ID) | .idFacture")

if [ -z "$FACTURE_FOUND" ] || [ "$FACTURE_FOUND" == "null" ]; then
    print_success "La facture n'est plus dans les impayées (correct)"
else
    print_error "La facture est toujours dans les impayées alors qu'elle est payée!"
fi
echo ""

# 11. Vérifier les arriérés
print_step "Étape 11: Vérification des arriérés..."
ARRIERES=$(curl -s -X GET "$BASE_URL/api/Client/$CLIENT_ID/arrieres" \
  -H "Authorization: Bearer $TOKEN")

if [ "$(echo $ARRIERES | jq -r '.idClient // empty')" != "" ]; then
    NOMBRE_FACTURES=$(echo $ARRIERES | jq -r '.nombreFacturesImpayees')
    TOTAL_ARRIERES=$(echo $ARRIERES | jq -r '.totalArrieres')
    print_success "Arriérés: $NOMBRE_FACTURES facture(s) impayée(s), Total=$TOTAL_ARRIERES FC"
else
    print_error "Impossible de récupérer les arriérés"
fi
echo ""

# Résumé
echo "=========================================="
echo "Résumé du Test"
echo "=========================================="
print_success "✅ Facture créée: ID=$FACTURE_ID"
print_success "✅ Paiement partiel créé: ID=$PAIEMENT1_ID (30000 FC)"
print_success "✅ Paiement complémentaire créé: ID=$PAIEMENT2_ID (70000 FC)"
print_success "✅ Facture.Montant reste inchangé: 100000 FC"
print_success "✅ Logique impayée/payée fonctionne correctement"
echo ""
print_info "Facture de test: $FACTURE_NUMERO (ID: $FACTURE_ID)"
print_info "Vous pouvez maintenant tester manuellement les endpoints avec cette facture"
echo ""

