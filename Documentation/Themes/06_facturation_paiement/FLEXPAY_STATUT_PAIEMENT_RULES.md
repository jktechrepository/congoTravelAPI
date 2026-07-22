# Règles Statut / StatutPaiement — FlexPay et non-régression CASH

> **Guide portable (autre projet)** : [`Integration-FlexPay-From-CongoTravelAPI.md`](../../../Integration-FlexPay-From-CongoTravelAPI.md)  
> (architecture, flux, endpoints, SQL, checklist de portage).  
> Référence prestataire générique : [`Integration-FlexPay-From-LexMusicaAPI.md`](../../../Integration-FlexPay-From-LexMusicaAPI.md).

## Chemins de paiement

| Méthode | Endpoint | `Paiement.Statut` à la création | Réservation |
|---------|----------|----------------------------------|-------------|
| `CASH` (et legacy espèces) | `POST /api/Reservation/reservation_with_paiement` | `true` | Immédiate |
| `MOBILE_MONEY`, `CARTE_BANCAIRE` | `POST /api/Reservation/reservation_with_paiement_electronique` | `false` | Après callback FlexPay uniquement |

## Réponse initiation (alignée guichet)

`POST reservation_with_paiement_electronique` renvoie **`ReservationWithPaiementResponseDto`** (identique à `with-passengers-and-paiement`), avec :

- `statut` = `EnAttente`
- `reservation.idReservation` = `0`, `statutReservation` = `EN_ATTENTE_PAIEMENT`
- `billets` = `[]`
- champs FlexPay optionnels renseignés : `orderNumberFlexPay`, `holdExpireAt`, `idCommandeReservationEnAttente`, etc.

Le guichet cash laisse ces champs FlexPay à `null`.
| Sync offline batch | `POST /api/sync/payments/batch` | `true` | Non (CASH / espèces seulement) |

## `Paiement.Statut` (bool)

- `true` : paiement validé / comptabilisé (guichet, callback FlexPay réussi).
- `false` : en attente (initiation FlexPay).

Les dashboards caissier / client et `FinanceReporting` filtrent sur `Statut == true` pour les encaissements validés.

## `StatutPaiementMetier` (int?, enum)

| Valeur | Nom | Usage |
|--------|-----|--------|
| 0 | EnAttente | Initiation FlexPay |
| 1 | Reussi | CASH ou callback OK |
| 2 | Echec | Callback échec |
| 3–5 | Annule / Remboursement… | Extensions |

Null sur les lignes historiques : interpréter via `Statut` booléen.

## Reporting

- **FinanceReporting** `GET paiements/summary` : uniquement `Statut == true`.
- **Sync arriérés** : uniquement paiements validés (`Statut == true`).

## Sièges

- **CASH** : allocation `VoyageSeatAllocation` `CONFIRME` (inchangé).
- **FlexPay initiation** : `SiegeHoldEnAttente` avec TTL (`FlexPay:SeatHoldMinutes`, défaut 15).
- **Disponibilité** : `ISiegeDisponibiliteService` = allocations CONFIRME + holds non expirés.

## Callback FlexPay (phase 2)

| Route | Auth | Rôle |
|-------|------|------|
| `POST /api/FlexPay/callback` | Public | Confirmation → réservation + billets |
| `GET /api/FlexPay/verifier/{orderNumber}` | JWT | Secours API check FlexPay |
| `GET/POST/PUT/DELETE /api/InfoPaiementSociete` | Super-admin | 1 marchand / site |

### Config société (règles billet / réaffectation / horizon)

Voir [`DOCUMENTATION_API_CONFIGSOCIETE.md`](../04_clients_referentiels/DOCUMENTATION_API_CONFIGSOCIETE.md) — `GET/PUT /api/Societe/{id}/config`. Le hold FlexPay par société utilise `dureeHoldFlexPayMinutes` (fallback appsettings si absent).

### Repli site principal (satellites)

Lors de l’initiation FlexPay ou du callback / verifier, la résolution marchand suit :

1. `InfoPaiementSociete` **active** du `idSite` demandeur ;
2. sinon `InfoPaiementSociete` du **site principal actif** (`IsSitePrincipal = true`, `Statut = true`) de la même société ;
3. sinon erreur explicite (aucune config active).

Le **`idSite` opérationnel** (réservation, paiement, agent) reste celui du guichet demandeur ; seuls token / code marchand FlexPay peuvent provenir du site principal.

Migration colonne : `SiteIsSitePrincipal` (backfill : site avec InfoPaiement actif, sinon plus ancien site actif, sinon premier site).

**Vérification UAT / prod** :

- Audit SQL : [`Scripts/verify-infopaiement-site-fallback.sql`](../../../Scripts/verify-infopaiement-site-fallback.sql)
- Smoke HTTP satellite : [`SMOKE_INFOPAIEMENT_SITE_FALLBACK.http`](../../../SMOKE_INFOPAIEMENT_SITE_FALLBACK.http)
- Tests : `dotnet test Tests/CongoTravel.Tests.csproj --filter "FullyQualifiedName~FlexPay_initiate_satellite|FullyQualifiedName~InfoPaiementResolutionServiceTests"`
- Log attendu : `FlexPay InfoPaiement fallback — site demandeur … → site principal …`

Succès (`code == "0"`) : idempotence sur `Paiement` déjà validé, création réservation depuis holds, paiement intégral, suppression commande en attente.

Échec : libération holds, `StatutPaiementMetier = Echec`, suppression commande en attente.

Migration : `FlexPayCallbackAndInfoPaiement`.

## Vérification base après test

Script SQL : [`Scripts/verify-flexpay-callback-state.sql`](../../../Scripts/verify-flexpay-callback-state.sql)

État attendu après callback **succès** (`code = "0"`) :

| Table | Attendu |
|-------|---------|
| `CommandesReservationEnAttente` | 0 ligne pour l'`orderNumber` |
| `SiegeHoldsEnAttente` | 0 ligne |
| `Paiements` | `Statut=true`, `IdReservation` renseigné |
| `CallbacksFlexPay` | 1+ audit avec `TraiteAvecSucces=true` |
| `Reservations` | 1 ligne `CONFIRMEE` |

## Front : polling (fallback)

Tant que le callback FlexPay n'est pas reçu, le front peut interroger (secours) :

`GET /api/FlexPay/verifier/{orderNumber}` (JWT)

**Important** : ne pas appeler `verifier` immédiatement après l'initiation ni sur la fermeture SignalR (`onclose`) — attendre la validation Mobile Money sur le téléphone ou un intervalle de polling.

| Situation | Réponse JSON (extrait) | Action front |
|-----------|------------------------|--------------|
| Paiement encore en attente FlexPay (statut `"2"`) | `success: true`, `paymentPending: true`, `idReservation: null` | Continuer polling 3–5 s jusqu'à `holdExpireAt` |
| Succès finalisé | **`ReservationWithPaiementResponseDto` complet** (`statut: "Succes"`, `billets[]`, `reservation.idReservation > 0`) | Parser comme le guichet cash, afficher billets |
| Échec définitif FlexPay (statut `"1"`) | `success: true`, `paymentPending: false`, message « refusé » | Afficher échec, libérer l'UI |
| Déjà traité (idempotence) | `ReservationWithPaiementResponseDto` avec billets | Même parser que succès |

**Note** : au `POST reservation_with_paiement_electronique`, `billets: []` est normal (`statut: EnAttente`). Les billets arrivent dans la réponse **`GET verifier`** après validation Mobile Money.

Exemple succès verifier :

```json
{
  "reservation": { "idReservation": 154, "statutReservation": "CONFIRMEE", "passagers": [...] },
  "paiement": { "idPaiement": 122, "statut": true },
  "billets": [{ "idBillet": 1, "qrCode": "..." }],
  "billet": { "idBillet": 1 },
  "transactionId": "GKnxlYOZ5RG9243896558249",
  "statut": "Succes",
  "message": "Réservation créée après confirmation FlexPay."
}
```

Exemple en attente :

```json
{
  "success": true,
  "alreadyProcessed": false,
  "paymentPending": true,
  "message": "Paiement en attente de validation Mobile Money.",
  "idReservation": null,
  "idPaiement": 107
}
```

Recommandation : polling toutes les 3–5 s pendant `holdExpireAt`, puis arrêt.

Les pages `GET /api/FlexPay/approve|cancel|decline` sont informatives (redirect carte) — elles **ne finalisent pas** la réservation.

## SignalR (temps réel)

Hub : `GET /hubs/notifications` (WebSocket, JWT via query `access_token` ou header Bearer).

Événements émis après callback :

| Événement | Quand | Payload |
|-----------|-------|---------|
| `FlexPayPaymentConfirmed` | `code == "0"` | `{ orderNumber, idReservation, idPaiement, status: "confirmed", timestampUtc }` |
| `FlexPayPaymentFailed` | `code != "0"` | `{ orderNumber, message, status: "failed", timestampUtc }` |

Groupe cible : `user_{idUtilisateur}` (utilisateur de la commande en attente).

Exemple connexion JS :

```js
const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_BASE}/hubs/notifications`, { accessTokenFactory: () => jwt })
  .withAutomaticReconnect()
  .build();

connection.on("FlexPayPaymentConfirmed", (payload) => {
  // payload.idReservation disponible — rediriger vers confirmation
});

connection.on("FlexPayPaymentFailed", (payload) => {
  // afficher message d'échec
});

await connection.start();
```

Conserver le polling `verifier` comme secours si SignalR est déconnecté, avec intervalle (pas au `onclose` du hub).

Connexion hub : `wss://{host}/hubs/notifications?access_token={jwt}` (ou header Bearer).
