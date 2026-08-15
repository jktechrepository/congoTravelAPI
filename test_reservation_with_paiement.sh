#!/bin/bash

# Script de test pour l'endpoint reservation_with_paiement
# Test de la transaction unifiée réservation + paiement

echo "🧪 Test de l'endpoint POST /api/reservation/reservation_with_paiement"
echo "================================================================"

# URL de l'API (adapter selon votre environnement)
API_URL="http://localhost:7110/api/reservation/reservation_with_paiement"

# Token d'authentification (adapter selon votre setup)
# Pour le test, vous pouvez utiliser un token valide ou commenter la ligne d'en-tête
AUTH_TOKEN="Bearer VOTRE_TOKEN_ICI"

# Données de test pour la réservation avec paiement
curl -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "Authorization: $AUTH_TOKEN" \
  -d '{
    "reservation": {
      "idVoyage": 1,
      "idClient": 1,
      "idUtilisateur": 1,
      "idSociete": 1,
      "nombreDePlace": 2
    },
    "paiement": {
      "montantAPaye": 50000,
      "montantPaye": 50000,
      "methodePaiement": "MOBILE_MONEY",
      "referenceTransaction": "TEST_'$(date +%s)'",
      "idUtilisateur": 1,
      "idSociete": 1
    }
  }' \
  -w "\n📊 Statistiques HTTP:\n  - Code: %{http_code}\n  - Temps: %{time_total}s\n  - Taille: %{size_download} octets\n" \
  -s

echo ""
echo "================================================================"
echo "✅ Test terminé"
echo ""
echo "📝 Notes importantes:"
echo "1. Assurez-vous que l'API est démarrée sur le bon port"
echo "2. Vérifiez que les IDs (voyage, client, utilisateur, société) existent en base"
echo "3. Adaptez le token d'authentification selon votre configuration"
echo "4. Pour tester le paiement partiel, mettez montantPaye < montantAPaye"
echo "5. Pour tester l'échec, utilisez des IDs invalides"
echo ""
echo "🔧 Statuts de réservation valides:"
echo "   - EN_ATTENTE: Réservation créée, en attente de paiement"
echo "   - CONFIRMEE: Paiement complet, réservation confirmée"
echo "   - ANNULEE: Réservation annulée"
echo ""
echo "💡 Comportement attendu:"
echo "   - Paiement complet → Statut: CONFIRMEE + Billet généré"
echo "   - Paiement partiel → Statut: EN_ATTENTE (pas de billet)"
echo "   - Erreur → Transaction rollback complet"
echo ""
echo "🔧 Corrections appliquées (v3.0 - FINALE):"
echo "   ✅ Transaction strategy: CreateExecutionStrategy() SANS transaction manuelle"
echo "   ✅ Statuts valides: EN_ATTENTE, CONFIRMEE, ANNULEE"
echo "   ✅ Pas de BeginTransactionAsync() (géré automatiquement par MySQL)"
echo "   ✅ Mapping correct: IdUtilisateur, IdSociete"
echo "   ✅ Atomicité garantie: tout réussit ou tout est annulé"
echo ""
echo "💡 Solution technique:"
echo "   - MySQL Retry Strategy gère TOUT automatiquement"
echo "   - SaveChangesAsync() suffit pour la transaction"
echo "   - Pas de BeginTransactionAsync() ni Commit/Rollback manuels"
