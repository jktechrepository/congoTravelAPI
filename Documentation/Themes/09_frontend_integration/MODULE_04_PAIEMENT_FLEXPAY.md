# MODULE 04 — Paiement FlexPay et multi-devise

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)

---

## Principe : un seul DTO réponse

Cash et électronique retournent **`ReservationWithPaiementResponseDto`** :

| Champ | Cash immédiat | FlexPay initiation | FlexPay après succès |
|-------|---------------|--------------------|----------------------|
| `statut` | `Succes` | `EnAttente` | `Succes` |
| `billets` | rempli | **`[]`** (normal) | rempli |
| `orderNumberFlexPay` | null | renseigné | optionnel |
| `holdExpireAt` | null | renseigné | null |

---

## Paiement cash (guichet)

```
POST /api/Reservation/with-passengers-and-paiement
POST /api/Reservation/reservation_with_paiement
```

Body inclut passagers, sièges, et bloc paiement (`montantPaye`, `methodePaiement: "Especes"`, etc.).

---

## Paiement électronique FlexPay (transport)

```
POST /api/Reservation/reservation_with_paiement_electronique
```

### Contrat devise (obligatoire)

- `montantAPaye` doit toujours être envoyé dans la devise tarif du voyage (`codeDeviseVoyage`).
- `codeDevisePaiement` indique la devise débitée chez FlexPay (`CDF` ou `USD` uniquement).
- Si `codeDevisePaiement` diffère de la devise tarif, l’API applique le taux actif société.
- Le callback/verify rejette une confirmation dont la devise provider diffère de la devise attendue.

### Flux mobile recommandé

```
1. POST reservation_with_paiement_electronique
   → statut EnAttente, billets=[], orderNumberFlexPay

2. Afficher « Validez sur votre téléphone »

3a. SignalR FlexPayPaymentConfirmed  →  afficher billets
3b. OU polling GET /api/FlexPay/verifier/{orderNumber}
   → statut Succes + billets remplis
```

### Flutter (extrait)

```dart
final init = await api.post('/Reservation/reservation_with_paiement_electronique', data: payload);
if (init.data['statut'] == 'EnAttente') {
  final order = init.data['orderNumberFlexPay'];
  // Attendre SignalR ou :
  final verified = await api.get('/FlexPay/verifier/$order');
  if (verified.data['statut'] == 'Succes') {
    final billets = verified.data['billets'] as List;
    // Afficher QR codes
  }
}
```

### Vue.js caisse

```js
const { data } = await api.post('/Reservation/reservation_with_paiement_electronique', payload);
if (data.statut === 'EnAttente') {
  await pollFlexPay(data.orderNumberFlexPay);
}
```

---

## Module Devise

| Endpoint | Usage |
|----------|-------|
| `GET /api/Devise/devises` | Liste devises actives |
| `GET /api/Devise/taux-change?idSociete=1&source=USD&cible=CDF` | Taux courant |
| `GET /api/Devise/preview-conversion?...` | Calcul avant paiement |
| `PUT /api/Devise/societe/{id}/devise-principale/{code}` | Admin |

### Exemple preview

```
GET /api/Devise/preview-conversion?idSociete=1&codeDeviseSource=USD&montant=25
```

```json
{
  "montantSource": 25,
  "montantConverti": 71262.50,
  "taux": 2850.50,
  "codeDevisePrincipale": "CDF"
}
```

---

## Paiement standalone

```
POST /api/Paiement
GET  /api/Paiement/{id}
POST /api/Paiement/paged
```

Champs multi-devise : `codeDevisePaiement`, `montantPayeDevisePrincipale`, `tauxVersDevisePrincipale`.

---

## Remboursement

```
POST /api/Remboursement
```

```json
{
  "idPaiement": 7001,
  "idSociete": 1,
  "montantRembourse": 25,
  "codeDeviseRemboursement": "USD",
  "motif": "Annulation"
}
```

---

## Reporting

```
GET /api/FinanceReporting/paiements/summary?idSociete=1&dateDebut=...&dateFin=...
```

---

## Références backend

- [`INTEGRATION_FLUTTER_FLEXPAY.md`](INTEGRATION_FLUTTER_FLEXPAY.md) — guide détaillé FlexPay transport
- [`FLEXPAY_STATUT_PAIEMENT_RULES.md`](../06_facturation_paiement/FLEXPAY_STATUT_PAIEMENT_RULES.md)
- [`DOCUMENTATION_MODULE_MULTIDEVISE_CONGOTRAVEL_API.md`](../06_facturation_paiement/DOCUMENTATION_MODULE_MULTIDEVISE_CONGOTRAVEL_API.md)
