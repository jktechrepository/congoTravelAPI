# Guide de Test Rapide - Factures Impayées/Payées

## Test Rapide en 5 Minutes

### Prérequis
- API en cours d'exécution
- Un compte utilisateur avec rôle Admin
- Au moins une catégorie de clients et un client dans cette catégorie

### Méthode 1 : Via Swagger UI (Recommandé)

1. **Ouvrir Swagger** : `http://localhost:5000/swagger`

2. **S'authentifier** :
   - Endpoint : `POST /api/Auth/login`
   - Body :
     ```json
     {
       "email": "votre_email@example.com",
       "password": "votre_mot_de_passe"
     }
     ```
   - Copier le `token` de la réponse

3. **Autoriser** :
   - Cliquer sur le bouton **"Authorize"** (en haut à droite)
   - Entrer : `Bearer {votre_token}`
   - Cliquer sur **"Authorize"** puis **"Close"**

4. **Créer une facture de test** :
   - Endpoint : `POST /api/Facture`
   - Body :
     ```json
     {
       "numeroFacture": "FACT-TEST-001",
       "montant": 100000.00,
       "dateEmission": "2025-01-15",
       "moisEmission": 1,
       "anneesEmission": 2025,
       "idCategorie": 1,
       "statut": true
     }
     ```
   - **Important** : Notez l'`idFacture` retourné
   - **Important** : Le `montant` est le montant initial (ce qui doit être payé)

5. **Vérifier que la facture est impayée** :
   - Endpoint : `GET /api/Client/{idClient}/factures-impayees`
   - Remplacez `{idClient}` par l'ID d'un client de la catégorie
   - **Résultat attendu** : La facture doit apparaître avec `montantDu = 100000`

6. **Créer un paiement partiel** :
   - Endpoint : `POST /api/Paiement`
   - Body :
     ```json
     {
       "idFacture": 1,
       "idClient": 1,
       "montantPaye": 30000.00,
       "datePaiement": "2025-01-20",
       "methodePaiement": "Mobile Money",
       "referenceTransaction": "REF-001",
       "statut": "Validé"
     }
     ```
   - Remplacez `idFacture` et `idClient` par les valeurs réelles

7. **Vérifier que la facture est toujours impayée** :
   - Endpoint : `GET /api/Client/{idClient}/factures-impayees`
   - **Résultat attendu** : 
     - `montantTotal = 100000`
     - `montantPaye = 30000`
     - `montantDu = 70000`

8. **Vérifier que Facture.Montant n'a pas changé** :
   - Endpoint : `GET /api/Facture/{idFacture}`
   - **Résultat attendu** : `montant = 100000` (inchangé)

9. **Créer un paiement complémentaire** :
   - Endpoint : `POST /api/Paiement`
   - Body :
     ```json
     {
       "idFacture": 1,
       "idClient": 1,
       "montantPaye": 70000.00,
       "datePaiement": "2025-01-25",
       "methodePaiement": "Espèces",
       "referenceTransaction": "REF-002",
       "statut": "Validé"
     }
     ```

10. **Vérifier que la facture est maintenant payée** :
    - Endpoint : `GET /api/Client/{idClient}/factures-payees/paged?pageNumber=1&pageSize=10`
    - **Résultat attendu** : La facture doit apparaître avec `montantPaye >= montantTotal`

11. **Vérifier que la facture n'est plus dans les impayées** :
    - Endpoint : `GET /api/Client/{idClient}/factures-impayees`
    - **Résultat attendu** : La facture ne doit **plus** apparaître

### Méthode 2 : Via cURL (Ligne de commande)

```bash
# 1. Authentification
TOKEN=$(curl -X POST "http://localhost:5000/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"votre_email@example.com","password":"votre_mot_de_passe"}' \
  | jq -r '.token')

echo "Token: $TOKEN"

# 2. Créer une facture
FACTURE_ID=$(curl -X POST "http://localhost:5000/api/Facture" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "numeroFacture": "FACT-TEST-001",
    "montant": 100000.00,
    "dateEmission": "2025-01-15",
    "moisEmission": 1,
    "anneesEmission": 2025,
    "idCategorie": 1,
    "statut": true
  }' | jq -r '.idFacture')

echo "Facture créée: ID=$FACTURE_ID"

# 3. Vérifier les factures impayées (avant paiement)
echo "Factures impayées (avant paiement):"
curl -X GET "http://localhost:5000/api/Client/1/factures-impayees" \
  -H "Authorization: Bearer $TOKEN" | jq

# 4. Créer un paiement partiel
curl -X POST "http://localhost:5000/api/Paiement" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"idFacture\": $FACTURE_ID,
    \"idClient\": 1,
    \"montantPaye\": 30000.00,
    \"datePaiement\": \"2025-01-20\",
    \"methodePaiement\": \"Mobile Money\",
    \"referenceTransaction\": \"REF-001\",
    \"statut\": \"Validé\"
  }" | jq

# 5. Vérifier les factures impayées (après paiement partiel)
echo "Factures impayées (après paiement partiel):"
curl -X GET "http://localhost:5000/api/Client/1/factures-impayees" \
  -H "Authorization: Bearer $TOKEN" | jq

# 6. Vérifier que Facture.Montant n'a pas changé
echo "Détails de la facture:"
curl -X GET "http://localhost:5000/api/Facture/$FACTURE_ID" \
  -H "Authorization: Bearer $TOKEN" | jq '.montant'

# 7. Créer un paiement complémentaire
curl -X POST "http://localhost:5000/api/Paiement" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"idFacture\": $FACTURE_ID,
    \"idClient\": 1,
    \"montantPaye\": 70000.00,
    \"datePaiement\": \"2025-01-25\",
    \"methodePaiement\": \"Espèces\",
    \"referenceTransaction\": \"REF-002\",
    \"statut\": \"Validé\"
  }" | jq

# 8. Vérifier les factures payées
echo "Factures payées:"
curl -X GET "http://localhost:5000/api/Client/1/factures-payees/paged?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN" | jq

# 9. Vérifier que la facture n'est plus dans les impayées
echo "Factures impayées (après paiement complet):"
curl -X GET "http://localhost:5000/api/Client/1/factures-impayees" \
  -H "Authorization: Bearer $TOKEN" | jq
```

## Checklist de Vérification

### ✅ Points à vérifier

- [ ] **Facture sans paiement** → Apparaît dans les impayées avec `montantDu = montantTotal`
- [ ] **Facture partiellement payée** → Apparaît dans les impayées avec `montantDu = montantTotal - montantPaye`
- [ ] **Facture entièrement payée** → Apparaît dans les payées, n'apparaît **pas** dans les impayées
- [ ] **Facture.Montant reste inchangé** → Toujours égal au montant initial, même après paiements
- [ ] **Paiements avec statut "En attente"** → Ne sont pas comptabilisés (seuls les "Validé" comptent)
- [ ] **Calcul par client** → Chaque client a ses propres paiements pour une facture

## Résultats Attendus

### Facture Impayée
```json
{
  "idFacture": 1,
  "numeroFacture": "FACT-TEST-001",
  "montantTotal": 100000.00,
  "montantPaye": 30000.00,
  "montantDu": 70000.00,
  "joursRetard": 10
}
```

### Facture Payée
```json
{
  "idFacture": 1,
  "numeroFacture": "FACT-TEST-001",
  "montantTotal": 100000.00,
  "montantPaye": 100000.00,
  "datePaiementComplet": "2025-01-25T10:30:00"
}
```

## Dépannage

### Problème : "Aucune facture trouvée"
- Vérifiez que le client a `idCategorieClient` renseigné
- Vérifiez que la facture a `idCategorie` correspondant à la catégorie du client
- Vérifiez que la facture a `statut = true`

### Problème : "Facture.Montant a changé"
- C'est un bug ! Le montant initial ne doit jamais être modifié
- Vérifiez que `PaiementService` et `FactureService` ne modifient plus `Facture.Montant`

### Problème : "Calcul incorrect"
- Vérifiez que les paiements ont `statut = "Validé"`
- Vérifiez que les paiements ont le bon `idClient` et `idFacture`
- Vérifiez que `Facture.Montant` représente bien le montant initial

## Support

Pour plus de détails, consultez : `docs/TEST_FACTURES_IMPAYEES_PAYEES.md`

