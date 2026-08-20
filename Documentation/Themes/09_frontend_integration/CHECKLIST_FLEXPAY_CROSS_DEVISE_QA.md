# CHECKLIST QA — FlexPay cross-devise

> **Intégration frontend (Vue.js + Flutter)** : contrat API, exemples JSON, snippets et checklist dev — voir [INTEGRATION_PAIEMENT_ELECTRONIQUE_CROSS_DEVISE_VUE_FLUTTER.md](INTEGRATION_PAIEMENT_ELECTRONIQUE_CROSS_DEVISE_VUE_FLUTTER.md).

## Pré-requis
- Société avec configuration FlexPay active (`InfoPaiementSociete.Statut=true`).
- Canal requis actif selon cas (`ActifMobileMoney` / `ActifCarteBancaire`).
- Taux actif présent dans `TauxChanges` pour les cas cross-devise.
- Données publiées et réservables pour chaque domaine (Transport, Evenement, Restaurant, SiteTouristique).

## 1) Transport — `POST /api/Reservation/reservation_with_paiement_electronique`

### Cas succès — même devise
- **Given** tarif voyage en `CDF`, `codeDevisePaiement=CDF`, `montantAPaye` exact.
- **Expect API**: `statut=EnAttente`, `orderNumberFlexPay` non vide, `codeDevisePaiement=CDF`.
- **Expect DB**: `CommandeReservationEnAttente` créée, `Paiement.StatutPaiementMetier=EnAttente`, holds actifs.

### Cas succès — cross-devise
- **Given** tarif en `CDF`, paiement demandé `USD`, taux actif `CDF->USD`.
- **Expect API**: `montantVoyage` en `CDF`, `montantFlexPay` converti en `USD`, `codeDevisePaiement=USD`.
- **Expect DB**: `CommandeReservationEnAttente.TauxVersDevisePaiement` renseigné.

### Cas échec — devise non autorisée
- **Given** `codeDevisePaiement=EUR`.
- **Expect API**: `400` avec message devise non supportée (`CDF`/`USD` uniquement).

### Cas échec — taux absent
- **Given** paiement `USD` sans taux actif.
- **Expect API**: `400` avec message métier sur absence de taux actif.

### Cas échec — devise canal interdite
- **Given** config canal Mobile Money autorise seulement `CDF`, mais demande `USD`.
- **Expect API**: `400` avec message devise non autorisée pour le canal.

## 2) Transport Callback/Verify — `POST /api/FlexPay/callback`, `GET /api/FlexPay/verifier/{orderNumber}`

### Callback succès
- **Given** `code=0`, montant cohérent, devise callback identique à la devise attendue.
- **Expect API**: `success=true`.
- **Expect DB**: réservation confirmée, paiement `Reussi`, commande supprimée, holds libérés/confirmés.

### Callback échec — devise incohérente
- **Given** `code=0`, devise callback différente de la devise attendue.
- **Expect API**: `success=false`, message de mismatch devise.
- **Expect DB**: aucune confirmation de réservation.

### Verify pending
- **Given** provider retourne statut pending.
- **Expect API**: `paymentPending=true`.
- **Expect DB**: commande et hold toujours présents.

## 3) Evenement — `POST /api/events/reservations/{id}/flexpay/initiate` + callback/verify

### Initiation cross-devise
- **Expect API**: `codeDeviseTarif`, `codeDevisePaiement`, `tauxApplique`, `montantFlexPay` cohérents.
- **Expect DB**: `EvenementPayment` en `PENDING` avec devise/taux persistés.

### Callback devise incohérente
- **Given** devise callback différente de `EvenementPayment.CodeDevise`.
- **Expect API**: `success=false`.
- **Expect DB**: réservation non confirmée, pas de tickets émis.

## 4) Restaurant — `POST /api/restaurants/reservations/{id}/flexpay/initiate` + callback/verify

### Initiation cross-devise
- **Expect API**: conversion correcte et `orderNumber` non vide.
- **Expect DB**: `RestaurantPayment` en `PENDING`, devise/taux persistés.

### Callback devise incohérente
- **Expect API**: `success=false`, message mismatch devise.
- **Expect DB**: réservation reste non confirmée.

## 5) SiteTouristique — `POST /api/sites-touristiques/reservations/{id}/flexpay/initiate` + callback/verify

### Initiation cross-devise
- **Expect API**: `montantTarif`, `montantFlexPay`, `codeDeviseTarif`, `codeDevisePaiement`, `tauxApplique`.
- **Expect DB**: `SiteTouristiquePayment` en `PENDING`, devise/taux persistés.

### Callback devise incohérente
- **Expect API**: `success=false`.
- **Expect DB**: réservation non confirmée.

## 6) Reversement auto + FraisPlateforme (PayOut, pas d'impact UX client)

Le `FraisPlateforme` n'est **pas** affiché au client : il réduit uniquement le montant PayOut vers le site.

- **Given** `AutoReversementPaiementElectronique=true`, `FraisPlateforme` + `CodeDeviseFraisPlateforme` (null = devise du paiement).
- **Expect** après callback succès (Transport / Événement / Restaurant / Site touristique) : une ligne `ReversementsSite` avec `ModulePaiement` du module, montant = `% × MontantPaye − frais converti`.
- **Expect** si taux de conversion du frais absent : pas de reversement auto, réservation **confirmée**.
- **Expect** `Evenement/5` et `Transport/5` ne se bloquent pas (idempotence composite).

## Régression minimale avant livraison
- Lancer: `dotnet test Tests/CongoTravel.Tests.csproj --filter "FullyQualifiedName~FlexPayRegressionTests|FullyQualifiedName~EvenementFlexPayCallbackServiceTests|FullyQualifiedName~RestaurantPhase3FlexPayTests|FullyQualifiedName~EvenementFlexPayInitiationServiceTests|FullyQualifiedName~ReversementSiteTests|FullyQualifiedName~SatelliteReversementTests" /p:UseAppHost=false`
- Critère: 0 échec sur ce périmètre.
