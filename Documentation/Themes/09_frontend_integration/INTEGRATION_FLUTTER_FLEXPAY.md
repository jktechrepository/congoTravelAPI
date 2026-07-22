# Intégration Flutter — Paiement électronique FlexPay

> Guide mobile pour `MOBILE_MONEY` / `CARTE_BANCAIRE`.  
> Règles backend détaillées : [`../06_facturation_paiement/FLEXPAY_STATUT_PAIEMENT_RULES.md`](../06_facturation_paiement/FLEXPAY_STATUT_PAIEMENT_RULES.md).

## Ce qui a changé (résumé)

| Avant | Maintenant |
|-------|------------|
| POST électronique → DTO dédié `InitiateFlexPayReservationResponseDto` | POST électronique → **`ReservationWithPaiementResponseDto`** (même modèle que le guichet cash) |
| `GET verifier` succès → `{ success, idReservation, idPaiement }` | `GET verifier` succès → **`ReservationWithPaiementResponseDto` complet avec `billets`** |
| Billets attendus dès le POST | **`billets: []` normal au POST** (`statut: EnAttente`) ; billets après validation Mobile Money |
| `verifier` immédiat traitait parfois un refus prématuré | `paymentPending: true` tant que l'utilisateur n'a pas validé sur le téléphone |

**Request body inchangé** — seul le **parsing des réponses** évolue côté Flutter.

---

## Un seul modèle Dart recommandé

Réutilisez le même parser que pour `POST /api/Reservation/with-passengers-and-paiement` (guichet cash).

Champs clés :

| Champ JSON | Cash (immédiat) | FlexPay initiation | FlexPay après succès (`verifier`) |
|------------|-----------------|--------------------|-----------------------------------|
| `statut` | `"Succes"` | `"EnAttente"` | `"Succes"` |
| `reservation.idReservation` | `> 0` | `0` | `> 0` |
| `reservation.statutReservation` | `"CONFIRMEE"` | `"EN_ATTENTE_PAIEMENT"` | `"CONFIRMEE"` |
| `paiement.statut` | `true` | `false` | `true` |
| `billets` | rempli | **`[]`** | rempli |
| `orderNumberFlexPay` | `null` | renseigné | optionnel |
| `holdExpireAt` | `null` | renseigné | optionnel |
| `transactionId` | UUID / ref | = `orderNumberFlexPay` | = order FlexPay |

Enum `statut` (string JSON) : `Succes`, `EnAttente`, `SuccesPaiementPartiel`, `Echec`, `Annule`.

---

## Flux mobile (Mobile Money)

```
1. POST /api/Reservation/reservation_with_paiement_electronique
   → statut EnAttente, billets vides, orderNumberFlexPay + holdExpireAt
   → Afficher écran « Validez sur votre téléphone »

2. (Optionnel) SignalR /hubs/notifications?access_token={jwt}
   → Écouter FlexPayPaymentConfirmed / FlexPayPaymentFailed
   → NE PAS appeler verifier dans onDisconnected

3. Polling GET /api/FlexPay/verifier/{orderNumberFlexPay} toutes les 3–5 s
   → paymentPending: true  → continuer
   → corps avec reservation + billets → succès, afficher billets
   → message « refusé » → échec

4. (Secours) GET /api/Reservation/{idReservation} si besoin
```

---

## 1. Initiation — POST électronique

**Endpoint** : `POST /api/Reservation/reservation_with_paiement_electronique`  
**Auth** : Bearer JWT  
**Body** : inchangé (`reservation` + `paiement` avec `phone` pour Mobile Money).

**Montant à payer** : lire le supplément depuis **`VoyageResponseDto`** (GET `/api/Voyage`, listes paginées) ou `GET /api/Societe/{idSociete}/config` :

```
montantAPaye = totalTarifsSieges + (montAddPaieElectronique × nombreDePlace)
```

- `montAddPaieElectronique` et `codeDeviseMontAddPaieElectronique` sont renvoyés sur chaque voyage (snapshot `ConfigSociete`).
- Si `montAddPaieElectronique = 0`, comportement inchangé (tarifs seuls).
- Le serveur rejette un `montantAPaye` sans supplément si la config société en impose un.

Exemple extrait `GET /api/Voyage/{id}` :

```json
{
  "id": 41,
  "prix": 50000,
  "codeDevisePrix": "CDF",
  "montAddPaieElectronique": 500,
  "codeDeviseMontAddPaieElectronique": "CDF"
}
```

**Réponse 200** — `ReservationWithPaiementResponseDto` :

```json
{
  "reservation": {
    "idReservation": 0,
    "idVoyage": 41,
    "idClient": 3,
    "statutReservation": "EN_ATTENTE_PAIEMENT",
    "statut": false,
    "passagers": [{ "nomComplet": "Jean Dupont", "telephone": "+243..." }]
  },
  "paiement": {
    "idPaiement": 122,
    "montantAPaye": 500,
    "montantPaye": 0,
    "statut": false,
    "idReservation": null
  },
  "billets": [],
  "billet": null,
  "transactionId": "GKnxlYOZ5RG9243896558249",
  "statut": "EnAttente",
  "message": "Validez le paiement sur votre téléphone Mobile Money...",
  "orderNumberFlexPay": "GKnxlYOZ5RG9243896558249",
  "holdExpireAt": "2026-05-29T11:00:00Z",
  "flexPayAccepted": true
}
```

**Actions Flutter** :

- Conserver `orderNumberFlexPay` **ou** `transactionId` (même valeur).
- Démarrer un timer jusqu'à `holdExpireAt`.
- **Ne pas** naviguer vers l'écran billet tant que `statut != Succes` ou `billets.isEmpty`.
- Afficher `reservation.passagers` en preview si besoin (pas de QR à ce stade).

---

## 2. Vérification — GET verifier

**Endpoint** : `GET /api/FlexPay/verifier/{orderNumber}`  
**Auth** : Bearer JWT  
**`orderNumber`** : valeur de `orderNumberFlexPay` / `transactionId` du POST.

### Deux formes de réponse 200

Détection : **présence de la clé `reservation`** = DTO unifié ; **présence de `success`** = statut court.

#### A. Encore en attente

```json
{
  "success": true,
  "alreadyProcessed": false,
  "paymentPending": true,
  "message": "Paiement en attente de validation Mobile Money.",
  "idReservation": null,
  "idPaiement": 122
}
```

→ Continuer le polling (intervalle 3–5 s). **Ne pas** afficher d'échec.

#### B. Succès (paiement validé)

```json
{
  "reservation": {
    "idReservation": 154,
    "statutReservation": "CONFIRMEE",
    "passagers": [...]
  },
  "paiement": {
    "idPaiement": 122,
    "statut": true,
    "idReservation": 154
  },
  "billets": [
    { "idBillet": 1, "qrCode": "...", "codeBillet": "..." }
  ],
  "billet": { "idBillet": 1, "qrCode": "..." },
  "transactionId": "GKnxlYOZ5RG9243896558249",
  "statut": "Succes",
  "message": "Réservation créée après confirmation FlexPay."
}
```

→ Parser comme un achat cash réussi ; afficher QR / billets.

#### C. Échec définitif

```json
{
  "success": true,
  "paymentPending": false,
  "message": "Paiement refusé par FlexPay.",
  "idReservation": null,
  "idPaiement": null
}
```

→ Afficher échec, proposer nouvelle réservation.

---

## 3. SignalR (optionnel, recommandé)

**Hub** : `{baseUrl}/hubs/notifications?access_token={jwt}`

| Événement | Action Flutter |
|-----------|----------------|
| `FlexPayPaymentConfirmed` | `{ orderNumber, idReservation, idPaiement }` → appeler **une fois** `verifier` ou `GET /api/Reservation/{id}` pour récupérer les billets |
| `FlexPayPaymentFailed` | Afficher message d'échec |

**Erreurs fréquentes** :

- Ne pas appeler `verifier` dans `onclose` / `onDisconnected` du hub.
- Ne pas interpréter une déconnexion SignalR comme un échec de paiement.
- Garder le polling `verifier` comme secours si SignalR est indisponible.

---

## 4. Exemple de parsing Dart

```dart
/// Retourne null = continuer à attendre (pending).
/// Lance une exception ou retourne un Result pour échec.
Future<ReservationWithPaiementResponse?> pollFlexPayVerifier(
  String orderNumber,
) async {
  final res = await api.get('/api/FlexPay/verifier/$orderNumber');

  if (res.data is Map && res.data.containsKey('reservation')) {
    return ReservationWithPaiementResponse.fromJson(res.data);
  }

  final pending = res.data['paymentPending'] == true;
  if (pending) return null;

  final msg = (res.data['message'] as String?) ?? '';
  if (msg.toLowerCase().contains('refusé')) {
    throw FlexPayPaymentFailedException(msg);
  }

  return null;
}
```

Boucle recommandée :

```dart
Future<ReservationWithPaiementResponse> waitForFlexPayConfirmation({
  required String orderNumber,
  required DateTime holdExpireAt,
}) async {
  while (DateTime.now().toUtc().isBefore(holdExpireAt)) {
    final result = await pollFlexPayVerifier(orderNumber);
    if (result != null && result.statut == TransactionStatut.succes) {
      if (result.billets.isEmpty) {
        // Secours : GET /api/Reservation/{id}
        return await api.getReservationWithPaiement(result.reservation.idReservation);
      }
      return result;
    }
    await Future.delayed(const Duration(seconds: 4));
  }
  throw FlexPayTimeoutException();
}
```

---

## 5. Checklist migration depuis l'ancien code

- [ ] Supprimer le modèle `InitiateFlexPayReservationResponseDto` (ou le mapper vers le modèle unifié).
- [ ] POST électronique : parser `ReservationWithPaiementResponseDto`.
- [ ] Ne plus tester `idPaiementEnAttente` à la racine → utiliser `paiement.idPaiement`.
- [ ] `verifier` succès : parser `ReservationWithPaiementResponseDto`, pas `{ success, idReservation }`.
- [ ] Gérer `paymentPending: true` sans afficher d'échec.
- [ ] Écran billet : données depuis **verifier** ou `GET /api/Reservation/{id}`, pas depuis le POST initial.
- [ ] Carte bancaire : si `paymentUrl != null`, ouvrir WebView puis poller `verifier` comme Mobile Money.

---

## 6. Endpoints de référence

| Action | Méthode | Route |
|--------|---------|-------|
| Initier paiement | POST | `/api/Reservation/reservation_with_paiement_electronique` |
| Vérifier / finaliser | GET | `/api/FlexPay/verifier/{orderNumber}` |
| Détail + billets (secours) | GET | `/api/Reservation/{idReservation}` |
| SignalR | WS | `/hubs/notifications?access_token={jwt}` |

Guichet cash (modèle identique) : `POST /api/Reservation/with-passengers-and-paiement`.
