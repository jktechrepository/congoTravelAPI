# MODULE 12 — Transport Aller-Retour (V1)

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)  
> Guide pratique Vue + Flutter : [`INTEGRATION_RESERVATION_ALLER_RETOUR_VUE_FLUTTER.md`](INTEGRATION_RESERVATION_ALLER_RETOUR_VUE_FLUTTER.md)  
> Spec backend : [`SPEC_ALLER_RETOUR_TRANSPORT_V1.md`](../05_transport_sync/SPEC_ALLER_RETOUR_TRANSPORT_V1.md)  
> Single-leg (inchangé) : [MODULE_03](MODULE_03_RESERVATION_BILLET.md) · [MODULE_04](MODULE_04_PAIEMENT_FLEXPAY.md)  
> Multi-devise FlexPay : [`INTEGRATION_PAIEMENT_ELECTRONIQUE_CROSS_DEVISE_VUE_FLUTTER.md`](INTEGRATION_PAIEMENT_ELECTRONIQUE_CROSS_DEVISE_VUE_FLUTTER.md)

---

## Objectif

Réserver un **aller-retour** sur **2 voyages distincts** (A→B puis B→A), avec :

- **1 liste de passagers** (strictement identique sur les 2 legs)
- **1 paiement unique** (cash ou FlexPay)
- **Sièges indépendants** par voyage (pas de miroir obligatoire)
- Endpoints **additifs** — les routes single-leg restent valides

---

## Règles métier (à appliquer côté UI)

| Règle | Comportement front |
|-------|--------------------|
| Voyages | 2 IDs distincts, même société |
| Géographie | `villeArrivee` aller = `villeDepart` retour et inverse (insensible à la casse) |
| Dates | Départ retour ≥ départ aller (même jour OK si heure retour ≥ heure aller) |
| Passagers | Même `nombreDePlace`, mêmes noms + `idCategorieSiege` sur les 2 legs |
| Tarif cash | `montantAPaye` = tarif_aller + tarif_retour (tolérance API 0,05) |
| Tarif FlexPay | Même base + **supplément électronique × places × 2** ; `montantAPaye` dans la **devise tarif** des voyages |
| Devises | Les 2 voyages doivent avoir la **même** `codeDevisePrix` |
| Annulation | Atomique des **2** legs (`POST .../cancel`) — pas d’annulation retour seul en V1 |

### Multi-devise (FlexPay AR)

- Les 2 voyages partagent la même devise tarif **D_t** (`codeDevisePrix`).
- `paiement.montantAPaye` = **toujours en D_t** (somme des 2 tarifs + supplément × places × 2).
- `paiement.codeDevisePaiement` = `CDF` ou `USD` (devise débitée **D_p**) ; conversion serveur → champs réponse `montantVoyage`, `montantFlexPay`, `tauxApplique`, `codeDevisePaiement`.
- Détail UX : [guide cross-devise](INTEGRATION_PAIEMENT_ELECTRONIQUE_CROSS_DEVISE_VUE_FLUTTER.md) + [MODULE_04](MODULE_04_PAIEMENT_FLEXPAY.md).

Hors scope V1 : sync offline AR, passagers différents, tarif promo AR, retour ouvert.

---

## Endpoints

| Méthode | Route | Usage | Rôles UX typiques |
|---------|-------|-------|-------------------|
| POST | `/api/Reservation/reservation_aller_retour_with_paiement` | Guichet cash | Caissier |
| POST | `/api/Reservation/reservation_aller_retour_with_paiement_electronique` | FlexPay (holds 2 voyages) | Caissier, Client |
| GET | `/api/Reservation/aller-retour/{id}` | Détail dossier (2 résas + billets) | Caissier, Client |
| POST | `/api/Reservation/aller-retour/{id}/cancel` | Annulation atomique | Caissier, Admin |

Auth : `Authorization: Bearer {token}` (comme les autres routes Reservation).  
Tenant : GET / cancel vérifient la société JWT vs dossier (SuperAdmin bypass).

### Codes HTTP

| Code | Quand |
|------|--------|
| **200** | Succès (cash `Succes` / `SuccesPaiementPartiel`, FlexPay `EnAttente`, GET détail, cancel) |
| **400** | `InvalidOperationException` métier → `{ "message": "..." }` ; ModelState invalide |
| **403** | Société JWT ≠ dossier (GET / cancel) |
| **404** | Dossier AR introuvable (GET / cancel) |
| **500** | Cash avec `statut: Echec` renvoyé tel quel ; autres erreurs serveur `{ "message": "Erreur interne..." }` |

### Mapping `statut` transaction (réponse create / initiate)

| Valeur | Signification front |
|--------|---------------------|
| `Succes` | Cash OK, billets émis (ou FlexPay finalisé côté verifier) |
| `SuccesPaiementPartiel` | Cash partiel : dossier `EN_ATTENTE_PAIEMENT`, pas (tous) les billets |
| `EnAttente` | FlexPay initié — **pas encore** d’`idReservationAllerRetour` définitif |
| `Echec` | Échec métier / technique (souvent HTTP 500 en cash) |
| `Annule` | Transaction annulée |

Statuts **agrégat** (`allerRetour.statut`) : `EN_ATTENTE_PAIEMENT` \| `CONFIRMEE` \| `ANNULEE`  
Enum leg (`allerRetourLeg`) : `Aller = 1`, `Retour = 2`
---

## Contrats JSON

### Request cash — `CreateReservationAllerRetourWithPaiementDto`

```json
{
  "idVoyageAller": 101,
  "idVoyageRetour": 205,
  "idClient": 12,
  "nombreDePlace": 2,
  "idUtilisateur": 5,
  "idSociete": 1,
  "idSite": 3,
  "passagers": [
    {
      "idCategorieSiege": 1,
      "nomComplet": "Jean Dupont",
      "telephone": "+243800000001",
      "email": null,
      "documentType": "CNI",
      "documentNumero": "AB123",
      "genre": "M"
    },
    {
      "idCategorieSiege": 2,
      "nomComplet": "Marie Dupont"
    }
  ],
  "paiement": {
    "montantAPaye": 50000,
    "montantPaye": 50000,
    "methodePaiement": "Especes",
    "referenceTransaction": null,
    "idUtilisateur": 5,
    "idSociete": 1,
    "idSite": 3
  }
}
```

### Request FlexPay — `InitiateFlexPayReservationAllerRetourDto`

Même structure voyage / client / passagers ; bloc `paiement` :

```json
{
  "idVoyageAller": 101,
  "idVoyageRetour": 205,
  "idClient": 12,
  "nombreDePlace": 2,
  "idUtilisateur": 5,
  "idSociete": 1,
  "idSite": 3,
  "passagers": [ /* identiques */ ],
  "paiement": {
    "montantAPaye": 52000,
    "methodePaiement": "MOBILE_MONEY",
    "codeDevisePaiement": "CDF",
    "phone": "243800000001",
    "idUtilisateur": 5,
    "idSociete": 1,
    "idSite": 3
  }
}
```

Règles FlexPay (identiques MODULE_04) :

- `montantAPaye` = montant **attendu en devise tarif voyage** (billets + supplément × places × 2)
- `codeDevisePaiement` = `CDF` ou `USD` (devise débitée chez FlexPay)
- `idSite` **obligatoire** (marchand FlexPay)
- `phone` obligatoire si `MOBILE_MONEY`

### Response — `ReservationAllerRetourWithPaiementResponseDto`

```json
{
  "transactionId": "A1B2C3D4",
  "statut": "Succes",
  "message": "Réservation aller-retour créée avec succès",
  "dateCreation": "2026-08-26T10:00:00Z",
  "allerRetour": {
    "idReservationAllerRetour": 42,
    "idVoyageAller": 101,
    "idVoyageRetour": 205,
    "idReservationAller": 9001,
    "idReservationRetour": 9002,
    "idPaiement": 7001,
    "statut": "CONFIRMEE",
    "idSociete": 1,
    "idClient": 12,
    "idUtilisateur": 5,
    "idSite": 3,
    "origine": "CAISSIER",
    "reservationAller": { "idReservation": 9001, "idVoyage": 101, "statutReservation": "CONFIRMEE", "allerRetourLeg": 1 },
    "reservationRetour": { "idReservation": 9002, "idVoyage": 205, "statutReservation": "CONFIRMEE", "allerRetourLeg": 2 },
    "paiement": {
      "idPaiement": 7001,
      "idReservation": 9001,
      "idReservationAllerRetour": 42,
      "montantAPaye": 50000,
      "estComplet": true
    },
    "billetsAller": [ { "idBillet": 1, "idReservation": 9001, "qrCode": "..." } ],
    "billetsRetour": [ { "idBillet": 2, "idReservation": 9002, "qrCode": "..." } ]
  },
  "idCommandeReservationEnAttente": null,
  "orderNumberFlexPay": null,
  "holdExpireAt": null,
  "paymentUrl": null,
  "flexPayAccepted": null
}
```

Enum `statut` (transaction) : `Succes` | `SuccesPaiementPartiel` | `Echec` | `Annule` | `EnAttente`  
Enum leg : `Aller = 1`, `Retour = 2`  
Statuts agrégat : `EN_ATTENTE_PAIEMENT` | `CONFIRMEE` | `ANNULEE`

### Initiation FlexPay (réponse partielle)

```json
{
  "statut": "EnAttente",
  "message": "Validez le paiement sur votre téléphone Mobile Money...",
  "transactionId": "FP-ORDER-...",
  "allerRetour": {
    "idVoyageAller": 101,
    "idVoyageRetour": 205,
    "statut": "EN_ATTENTE_PAIEMENT",
    "paiement": { "idPaiement": 7001, "statut": false }
  },
  "idCommandeReservationEnAttente": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "orderNumberFlexPay": "ORD-...",
  "referenceFlexPay": "RT-...",
  "montantVoyage": 52000,
  "codeDeviseVoyage": "CDF",
  "montantFlexPay": 52000,
  "codeDevisePaiement": "CDF",
  "holdExpireAt": "2026-08-26T10:15:00Z",
  "paymentUrl": null,
  "flexPayAccepted": true
}
```

À l’initiation FlexPay :

- **`idReservationAllerRetour` est absent / non définitif** — l’agrégat est créé au **callback** FlexPay.
- Utiliser `orderNumberFlexPay`, `holdExpireAt`, `montantFlexPay` / `codeDevisePaiement` pour l’UI d’attente.

Après succès FlexPay :

1. Récupérer `idReservation` (**aller**) via SignalR / `GET /api/FlexPay/verifier/{orderNumber}`
2. `GET /api/Reservation/{idReservation}` → lire `idReservationAllerRetour` (désormais présent)
3. `GET /api/Reservation/aller-retour/{id}` → `billetsAller` + `billetsRetour`

### GET détail — `ReservationAllerRetourResponseDto`

`GET /api/Reservation/aller-retour/{id}` retourne le dossier complet :

| Champ | Type | Notes |
|-------|------|--------|
| `idReservationAllerRetour` | int | PK agrégat |
| `idVoyageAller` / `idVoyageRetour` | int | Voyages liés |
| `idReservationAller` / `idReservationRetour` | int? | Null tant que non matérialisés (ex. avant callback) |
| `idPaiement` | int? | Paiement unique |
| `statut` | string | `EN_ATTENTE_PAIEMENT` \| `CONFIRMEE` \| `ANNULEE` |
| `idSociete` / `idClient` / `idUtilisateur` / `idSite` | — | Tenant / acteurs |
| `origine` | string | Ex. `CAISSIER` |
| `dateCreation` / `dateModification` | ISO-8601 | |
| `reservationAller` / `reservationRetour` | `ReservationResponseDto?` | Inclut `allerRetourLeg` (1/2) |
| `paiement` | `PaiementResponseDto?` | `idReservationAllerRetour` renseigné |
| `billetsAller` / `billetsRetour` | `BilletResponseDto[]` | QR par leg ; vides si non confirmé |

---

## Flux Vue.js (guichet cash)

```
1. Sélection voyage aller + voyage retour (filtre UI : villes miroir + dates)
2. Saisie passagers (1 liste)
3. Calculer montant = tarifAller + tarifRetour (afficher détail)
4. POST /api/Reservation/reservation_aller_retour_with_paiement
5. Si statut === Succes → afficher QR billetsAller + billetsRetour
6. Stocker idReservationAllerRetour pour détail / annulation
```

### TypeScript (extrait)

```ts
const { data } = await api.post(
  '/Reservation/reservation_aller_retour_with_paiement',
  payload
);

if (data.statut === 'Succes') {
  const ar = data.allerRetour;
  const billets = [
    ...(ar.billetsAller ?? []),
    ...(ar.billetsRetour ?? []),
  ];
  // Afficher QR
} else if (data.statut === 'Echec') {
  showError(data.message);
}
```

### Annulation (guichet)

```ts
await api.post(`/Reservation/aller-retour/${idReservationAllerRetour}/cancel`);
```

Libère les sièges des **2** legs ; agrégat → `ANNULEE`.

---

## Flux Flutter (voyageur FlexPay)

```
1. Choix aller + retour + passagers
2. POST reservation_aller_retour_with_paiement_electronique
   → statut EnAttente, orderNumberFlexPay, holdExpireAt
3. Afficher « Validez sur votre téléphone » (ou paymentUrl carte)
4a. SignalR FlexPayPaymentConfirmed (idReservation = aller)
4b. OU polling GET /api/FlexPay/verifier/{orderNumber}
5. GET /api/Reservation/{idReservation} → idReservationAllerRetour
6. GET /api/Reservation/aller-retour/{id} → billetsAller + billetsRetour
```

### Dart (extrait)

```dart
final init = await api.post(
  '/Reservation/reservation_aller_retour_with_paiement_electronique',
  data: payload,
);

if (init.data['statut'] == 'EnAttente') {
  final order = init.data['orderNumberFlexPay'] as String;
  // Attendre SignalR ou :
  final verified = await api.get('/FlexPay/verifier/$order');
  if (verified.data['statut'] == 'Succes') {
    final idReservationAller = verified.data['reservation']?['idReservation']
        ?? verified.data['idReservation'];
    final res = await api.get('/Reservation/$idReservationAller');
    final idAr = res.data['idReservationAllerRetour'] as int?;
    if (idAr != null) {
      final detail = await api.get('/Reservation/aller-retour/$idAr');
      final billetsAller = detail.data['billetsAller'] as List;
      final billetsRetour = detail.data['billetsRetour'] as List;
      // Afficher QR codes (2 N billets)
    }
  }
}
```

> Le verifier / SignalR reste orienté **réservation aller** (rétrocompat). Toujours recharger le dossier AR via `GET aller-retour/{id}` pour les billets retour.

Détails multi-devise / SignalR : [MODULE_04](MODULE_04_PAIEMENT_FLEXPAY.md), [`INTEGRATION_FLUTTER_FLEXPAY.md`](INTEGRATION_FLUTTER_FLEXPAY.md).

---

## Embarquement

Les billets AR sont des billets transport classiques (`BilletResponseDto`).

- Scan / check : `GET /api/Billet/{QrCode}/check?idVoyageCible=...` — [MODULE_03](MODULE_03_RESERVATION_BILLET.md)
- Un billet = un leg (aller **ou** retour) ; vérifier le voyage cible au gate

---

## Checklist QA front

- [ ] UI refuse 2 voyages même ID
- [ ] Filtre villes miroir + même société
- [ ] Date/heure retour ≥ aller
- [ ] Une seule liste passagers (N = nombreDePlace)
- [ ] Cash : montant = somme des 2 tarifs ; billets des 2 legs affichés
- [ ] FlexPay : montant inclut supplément × places × 2 ; statut EnAttente puis reload AR
- [ ] `GET aller-retour/{id}` affiche aller + retour + paiement
- [ ] Cancel annule les 2 legs (sièges libérés)
- [ ] Single-leg MODULE_03/04 non régressé

---

## Erreurs fréquentes (400)

| Message typique | Cause UI |
|-----------------|----------|
| Incompatibilité géographique | Villes non miroir |
| Départ retour … postérieur | Date/heure retour avant aller |
| Montant à payer incohérent | Mauvaise somme / oubli supplément FlexPay |
| IdSite requis | FlexPay sans site |
| Places insuffisantes | Capacité d’un des 2 voyages |
| Devises tarif … identiques | Voyages en devises différentes |

---

## Guide d’intégration pratique

Pas-à-pas Vue (cash) + Flutter (FlexPay), modèles TS/Dart, checklist QA étendue :

→ [`INTEGRATION_RESERVATION_ALLER_RETOUR_VUE_FLUTTER.md`](INTEGRATION_RESERVATION_ALLER_RETOUR_VUE_FLUTTER.md)
