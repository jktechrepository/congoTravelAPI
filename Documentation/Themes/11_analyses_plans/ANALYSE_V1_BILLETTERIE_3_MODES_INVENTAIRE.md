# ANALYSE V1 - Billetterie evenementielle a 3 modes d'inventaire

## Contexte et objectif

Objectif: etendre le perimetre produit pour couvrir la reservation de places pour des evenements
(concert, soiree, ceremonie) sans impacter les endpoints transport existants.

Strategie retenue: **2 modules separes + noyau partage**.

- Module `Transport` (deja stable): inchange en V1.
- Module `Evenementiel`: nouveau domaine pour les ceremonies/evenements.
- Noyau `TicketingCore` partage: **pattern metier** (etats, idempotence, cycle hold/confirm/ticket) — **pas** de tables communes avec le transport.

**Parcours utilisateur V1 documente** : creer session (Draft) → publier → hold → payer (CASH) → emission tickets → check entree → use ticket.

---

## 1) InventoryMode et invariants metier (A/B/C)

### Enum fonctionnelle

- `SeatNumbered` (Cas A): evenement avec sieges numerotes/categorie/section.
- `ClassQuota` (Cas B): classe sans siege attribue.
- `GlobalQuota` (Cas C): billet libre (quota global de session).

### Invariants transverses (obligatoires)

1. Pas de survente (sellable >= 0 en permanence).
2. Un hold reserve temporairement la capacite avant paiement.
3. Un hold expire automatiquement si non paye.
4. Une confirmation de paiement est idempotente.
5. Une annulation/liberation restitue la capacite.

### Invariants specifiques par mode

- `SeatNumbered`
  - Un siege ne peut avoir qu'un seul hold/achat actif pour une session.
  - Le siege doit appartenir a la section/categorie de la session.
- `ClassQuota`
  - `sold + held <= classCapacity` pour chaque classe.
  - Une ligne de reservation porte `classId + quantity`.
- `GlobalQuota`
  - `sold + held <= sessionCapacity`.
  - Une ligne de reservation porte `quantity` sans classe ni siege.

---

## 2) Noyau partage Reservation/Ticket/Payment

### 2.1 Entites conceptuelles (TicketingCore)

Noms **fonctionnels** du noyau (documentation / API) :

| Concept | Champs cles |
|---------|-------------|
| `EventSession` | `idSession`, `inventoryMode`, `startAtUtc`, `status` |
| `Reservation` | `idReservation`, `idSession`, `status`, `expiresAtUtc`, `customerRef`, `idempotencyKey` |
| `ReservationLine` | `idReservationLine`, `idReservation`, `quantity`, `unitPrice`, `seatId?`, `classId?` |
| `Ticket` | `idTicket`, `idReservationLine`, `ticketCode`, `status` |
| `Payment` | `idPayment`, `idReservation`, `provider`, `providerRef`, `status`, `amount` |

**Persistance V1** : tables SQL prefixees `Evenement*` (isolees du transport). Le TicketingCore ne partage **aucune** entite EF avec `Reservations`, `Billets`, `Paiements` transport.

### 2.2 Regle de compatibilite ReservationLine selon mode

- `SeatNumbered`: `seatId` obligatoire, `quantity = 1`, `classId` derive du siege.
- `ClassQuota`: `classId` obligatoire, `quantity > 0`, `seatId = null`.
- `GlobalQuota`: `quantity > 0`, `seatId = null`, `classId = null`.

### 2.3 Etats metier minimaux

- Reservation: `HOLD -> CONFIRMED` ou `HOLD -> EXPIRED` ou `HOLD -> CANCELLED`.
- Ticket: `ISSUED`, `USED`, `VOID`.
- Payment: `PENDING`, `SUCCEEDED`, `FAILED`, `REFUNDED`.

### 2.4 Mapping terminologie analyse ↔ tables SQL

Script de reference : `Scripts/production_evenement_ticketing_v1.sql`.

| Concept analyse | Table / colonne SQL | Notes |
|-----------------|---------------------|-------|
| `EventSession` | `EvenementSessions` (`IdEvenementSession`, `InventoryMode`, `StartAtUtc`, `Status`) | `Status`: Draft, Published, Closed, Cancelled |
| `Reservation` | `EvenementReservations` | `Status`: HOLD, CONFIRMED, CANCELLED, EXPIRED |
| `customerRef` | `CustomerRef` varchar(100) nullable | Pas de FK `Clients` obligatoire en V1 |
| `idempotencyKey` (reservation) | `IdempotencyKey` | Unique par `(IdSociete, IdempotencyKey)` |
| `ReservationLine` | `EvenementReservationLines` | `LineType` enum SQL |
| Mode A — payload `seatId` | `LineType = 'Seat'`, `IdEvenementSessionSeat` | `Quantite` doit etre 1 |
| Mode B — payload `classId` | `LineType = 'ClassQuota'`, `IdEvenementSessionClassQuota` | FK vers quota session+classe |
| Mode C — payload `quantity` | `LineType = 'GlobalQuota'` | `seat` et `classQuota` null |
| Inventaire global (C) | `EvenementSessionGlobalQuotas` | `CapaciteTotale`, `QuantiteHold`, `QuantiteVendue` |
| Inventaire par classe (B) | `EvenementSessionClassQuotas` | Par `(IdEvenementSession, IdEvenementClasse)` |
| Sieges (A) | `EvenementSessionSeats` | `SeatStatus`: Available, Held, Sold, Blocked |
| `Ticket` / `ticketCode` | `EvenementTickets.TicketCode` | Unique ; API utilise `ticketCode` (pas `barcode`) |
| `Payment` | `EvenementPayments` | `Provider`, `ProviderTxRef`, idempotence propre |

**Triggers** : `TRG_EvenementReservationLines_BI/BU` renforcent la coherence `LineType` ↔ colonnes (cf. `Scripts/verify_evenement_api_db_contract.sql`).

**Client V1** : `customerRef` est une reference opaque (telephone, code interne, etc.). Lien optionnel vers la table `Clients` reporte a une phase ulterieure.

---

## 3) Contrat API V1

Base route : `api/events` (controller dedie, namespace `Evenement`).

### 3.1 Creer un hold

- `POST /api/events/sessions/{idSession}/holds`

Payload commun:

```json
{
  "customerRef": "CUST-123",
  "idempotencyKey": "2f381ef0-5d88-4a88-a92d-d8f63d95ec3f",
  "items": []
}
```

Items selon mode:

- `SeatNumbered`
```json
{ "seatId": 1042, "quantity": 1 }
```
- `ClassQuota`
```json
{ "classId": 2, "quantity": 3 }
```
- `GlobalQuota`
```json
{ "quantity": 2 }
```

Reponse:
- `201 Created` avec `reservationId`, `expiresAtUtc`, `amountPreview`.
- `409 Conflict` si capacite indisponible.

### 3.2 Confirmer paiement

- `POST /api/events/reservations/{idReservation}/confirm-payment`

Corps (CASH V1) :

```json
{
  "methodePaiement": "CASH",
  "idempotencyKey": "2f381ef0-5d88-4a88-a92d-d8f63d95ec3f",
  "referenceTransaction": "CAISSE-001"
}
```

- Requete idempotente (meme `idempotencyKey` => meme resultat logique).
- Retour `200` si deja confirme.
- Ecrit dans `EvenementPayments`, passe la reservation a `CONFIRMED`, emet les tickets.

### 3.3 Annuler reservation

- `POST /api/events/reservations/{idReservation}/cancel`
- Libere la capacite non consommee et invalide les tickets non utilises (`VOID`).

### 3.4 Disponibilite

- `GET /api/events/sessions/{idSession}/availability`
- Reponse adaptee au mode:
  - A: liste sieges libres/occupes.
  - B: quotas restants par classe.
  - C: quota global restant.

### 3.5 API complementaires V1

| Route | Methode | Role |
|-------|---------|------|
| `/api/events/sessions/{id}` | GET | Detail session (mode, statut, horaires, societe) |
| `/api/events/sessions` | POST | Creation session back-office (`Draft`) |
| `/api/events/sessions/{id}/publish` | PUT | Publication + validation inventaire coherent avec `InventoryMode` |
| `/api/events/tickets/{ticketCode}/check` | GET | Controle entree (equivalent scan billet transport) |
| `/api/events/tickets/{ticketCode}/use` | POST | Marquer `USED` (idempotent si deja utilise) |

Reponses check ticket (inspirees du transport) :

- `200` : ticket valide, `embarquementAutorise` / `entreeAutorisee`, infos passager/session.
- `409` : deja utilise ou invalide.

---

## 4) Strategie anti-survente, paiement et expiration hold

### 4.1 Routines atomiques

- Creation hold:
  1. verifier disponibilite
  2. ecrire hold + decrement logique capacite
  3. commit unique transaction

- Confirmation paiement:
  1. verifier `Reservation.status == HOLD` et non expiree
  2. passer a `CONFIRMED`
  3. transferer `QuantiteHold` → `QuantiteVendue` (modes B/C) ou `Held` → `Sold` (mode A)
  4. emettre tickets (`ISSUED`)
  5. commit unique transaction

### 4.2 Verrous et contraintes

- **Mode A (`SeatNumbered`)** :
  - Pas d'index unique cross-reservation sur le siege.
  - Verrou **optimiste** : `UPDATE EvenementSessionSeats SET SeatStatus='Held', ... WHERE IdEvenementSessionSeat=@id AND SeatStatus='Available'` ; `ROW_COUNT() = 0` => `409`.
  - Cf. `Scripts/test_concurrency_evenement_ticketing_v1.sql` (test 3).
- **Mode B (`ClassQuota`)** :
  - `UPDATE ... SET QuantiteHold = QuantiteHold + @qty WHERE (QuantiteHold + QuantiteVendue + @qty) <= CapaciteTotale`.
- **Mode C (`GlobalQuota`)** :
  - Meme pattern sur `EvenementSessionGlobalQuotas`.

Checks SQL : `QuantiteHold + QuantiteVendue <= CapaciteTotale` sur tables quota.

### 4.3 Expiration HOLD (job)

Procedure SQL : `sp_ExpireEvenementHolds` (`Scripts/production_evenement_hold_expiration_job.sql`).

Pour chaque reservation `HOLD` expiree :
1. restitue `GlobalQuota` / `ClassQuota` / sieges `Held`
2. passe la reservation a `EXPIRED`

**Decision V1 — execution** :

- **Retenu** : `IHostedService` .NET appelant `sp_ExpireEvenementHolds` toutes les **1 minute** (logs applicatifs, observabilite).
- Alternative ops : event scheduler MariaDB seul (moins de visibilite cote API).

**Duree hold** :

- **Retenu V1** : nouveau champ `ConfigSociete.DureeHoldEvenementMinutes` (fallback 15 min si absent).
- Ne pas reutiliser `DureeHoldFlexPayMinutes` (couplage transport indesirable).

### 4.4 Strategie paiement autonome (decision validee)

**Choix** : flux paiement **autonome** via `EvenementPayments`, **sans** reutiliser `CommandeReservationEnAttente` ni le callback FlexPay transport.

| Aspect | Transport (inchange) | Evenementiel V1 |
|--------|----------------------|-----------------|
| Tables | `Paiements`, `CommandeReservationEnAttente`, `TransactionsFlexPay` | `EvenementPayments` uniquement |
| CASH | Confirmation directe reservation | `confirm-payment` synchrone → `SUCCEEDED` |
| Electronique | `FlexPayReservationService` + callback | **Phase 5** — service dedie `EvenementFlexPay*` |
| Callback URL | `/api/FlexPay/callback` transport | Callback separe evenement (a definir phase 5) |

Implications :

- `POST .../confirm-payment` ecrit dans `EvenementPayments` (`Provider`, `ProviderTxRef`, `IdempotencyKey`).
- Le pipeline transport (`FlexPayReservationService`, `FlexPayCallbackService`) reste **intact**.
- Paiement electronique evenement : namespace et tables separes ; branchement sur `EvenementPayments.Provider` / `ProviderTxRef`.

### 4.5 Multi-devise V1

- **Retenu** : une devise par ligne/quota (`CodeDevise` sur inventaire et lignes) ; **pas** de conversion via `IDeviseMontantConverter` en V1.
- Reutilisation du module multi-devise transport reportee (phase ulterieure).

---

## 5) Architecture C# proposee (Phase 1+)

Pas d'abstraction EF partagee avec `Reservation` transport en V1 — **préfixe `Evenement` obligatoire** sur toutes les entités, services, DTOs et enums (voir section 12).

**Convention** : préfixe `Evenement` (pas de suffixe `*Evenement` ; pas de réutilisation de `Billet`, `Reservation`, `Paiement` transport).

### Arborescence

```
Models/Evenement/
  EvenementSession.cs
  EvenementClasse.cs
  EvenementSessionSection.cs
  EvenementSessionGlobalQuota.cs
  EvenementSessionClassQuota.cs
  EvenementSessionSeat.cs
  EvenementReservation.cs
  EvenementReservationLine.cs
  EvenementTicket.cs
  EvenementPayment.cs
  Enums/
    EvenementInventoryMode.cs
    EvenementReservationStatus.cs
    EvenementTicketStatus.cs
    EvenementPaymentStatus.cs

Models/DTOs/Evenement/
  EvenementHoldRequestDto.cs
  EvenementReservationResponseDto.cs
  EvenementTicketCheckResponseDto.cs
  EvenementTicketResponseDto.cs
  ...

Services/Evenement/
  IEvenementHoldService.cs / EvenementHoldService.cs
  IEvenementPaymentService.cs / EvenementPaymentService.cs
  IEvenementTicketService.cs / EvenementTicketService.cs
  IEvenementAvailabilityService.cs / EvenementAvailabilityService.cs
  IEvenementSessionService.cs / EvenementSessionService.cs
  EvenementHoldExpirationHostedService.cs
  Strategies/
    IEvenementInventoryHoldStrategy.cs
    EvenementGlobalQuotaHoldStrategy.cs
    EvenementClassQuotaHoldStrategy.cs
    EvenementSeatNumberedHoldStrategy.cs

Controllers/
  EvenementSessionController.cs       # [Route("api/events/sessions")]
  EvenementReservationController.cs   # holds, confirm-payment, cancel
  EvenementTicketController.cs        # check, use

Data/CongoTravelDbContext.cs
  ConfigureEvenementEntities()        # configurations EF isolees
```

Namespaces : `CongoTravel.Models.Evenement`, `CongoTravel.Models.DTOs.Evenement`, `CongoTravel.Services.Evenement`.

### DbSet (ajouts Phase 1 — noms distincts du transport)

```csharp
public DbSet<EvenementSession> EvenementSessions { get; set; }
public DbSet<EvenementClasse> EvenementClasses { get; set; }
public DbSet<EvenementSessionSection> EvenementSessionSections { get; set; }
public DbSet<EvenementSessionGlobalQuota> EvenementSessionGlobalQuotas { get; set; }
public DbSet<EvenementSessionClassQuota> EvenementSessionClassQuotas { get; set; }
public DbSet<EvenementSessionSeat> EvenementSessionSeats { get; set; }
public DbSet<EvenementReservation> EvenementReservations { get; set; }
public DbSet<EvenementReservationLine> EvenementReservationLines { get; set; }
public DbSet<EvenementTicket> EvenementTickets { get; set; }
public DbSet<EvenementPayment> EvenementPayments { get; set; }
```

**Inchangé** : `Reservations`, `Billets`, `Paiements`, `Sieges`, etc.

### Interfaces services (pas de `IReservationService` partagé)

| Transport (existant) | Evenementiel (nouveau) |
|---------------------|------------------------|
| `IReservationRepository` / `ReservationService` | `IEvenementReservationService` |
| `IBilletRepository` / `BilletService` | `IEvenementTicketService` |
| `IPaiementRepository` | `IEvenementPaymentService` |
| `ISiegeDisponibiliteService` | `IEvenementHoldService` + strategies |

Selection strategie : `EvenementInventoryMode` de la session → `IEvenementInventoryHoldStrategy` via factory ou switch injecte.

**Interdit V1** : heritage commun, `ReservationBase`, AutoMapper `Reservation` ↔ `EvenementReservation`, extension de `ReservationController` / `BilletController`.

---

## 6) Securite, permissions et tenancy

- Toutes les routes `[Authorize]` + resolution `IdSociete` JWT (meme pattern que transport / `ICurrentUserService`).
- Super-admin : acces multi-societe sur gestion sessions.

Permissions proposees (seeder dedie) :

| Permission | Usage |
|------------|-------|
| `Evenement.Session.Read` | Liste / detail session, availability |
| `Evenement.Session.Write` | Creer, publier, fermer session |
| `Evenement.Hold.Create` | POST holds |
| `Evenement.Reservation.Confirm` | confirm-payment, cancel |
| `Evenement.Ticket.Check` | GET check ticket |
| `Evenement.Ticket.Use` | POST use ticket (controleur entree) |
| `Evenement.Dashboard.Read` | GET dashboard événement |

Isolation : toute requete filtre `IdSociete` ; reservation et session doivent appartenir a la meme societe.

---

## 7) Roadmap de delivery revisee

| Phase | Contenu | Prerequis |
|-------|---------|-----------|
| **0** | Cadrage invariants + contrats API | Fait |
| **0bis** | Affinage analyse (ce document) | Fait |
| **1** | Socle : modeles EF `Evenement*`, DbContext, script SQL ou migration EF, permissions seeder, hosted service expiration | Doc affinee |
| **2** | **Mode C** (`GlobalQuota`) : hold → confirm **CASH** → tickets → availability | Phase 1 + tests concurrence SQL |
| **3** | **Mode B** (`ClassQuota`) | Mode C stable |
| **4** | **Mode A** (`SeatNumbered`) + plan de salle | Mode B stable |
| **5** | Paiement electronique evenement (FlexPay autonome) | Mode C + `EvenementPayments` — **fait** (voir `DOCUMENTATION_FLEXPAY_EVENEMENT_V1.md`) |
| **6** | Convergence : reporting, dashboards transverses | Phases 2-5 — **fait** (voir `DOCUMENTATION_DASHBOARD_EVENEMENT_V1.md`) |

Ordre modes : **C → B → A** (complexite croissante). Paiement **CASH d'abord** sur Mode C pour valider le cycle complet sans FlexPay.

### Phase 1 — detail socle technique

- Introduire entites `Evenement*` dans `CongoTravelDbContext` (ou contexte separe si prefere plus tard).
- Instrumentation idempotence (logs structurels sur `idempotencyKey`).
- Aucune modification des controllers transport existants.

---

## 8) Criteres d'acceptation V1

1. Aucun endpoint transport existant ne change de comportement.
2. Les 3 modes passent les tests anti-survente (`Scripts/test_concurrency_evenement_ticketing_v1.sql`).
3. Hold expiration fonctionne et restitue la capacite (`sp_ExpireEvenementHolds` + hosted service).
4. Confirm paiement CASH idempotent.
5. Confirm paiement FlexPay idempotent (callback + verify).
6. Availability refletant l'etat reel de stock.
7. Parcours entree : check + use ticket operationnels.
8. Frontiere Transport / Evenementiel / Paiement documentee et respectee en code.

---

## 9) Decision architecture

Conserver les domaines `Transport` et `Evenementiel` separes. Le noyau `TicketingCore` est un **patron metier** (etats, idempotence, cycle hold/confirm/ticket), pas un module EF commun. Cette separation limite le risque de regression et permet un deploiement progressif mode par mode.

---

## 10) Decisions reportees (hors V1 stricte)

| Sujet | Decision reportee |
|-------|-------------------|
| Conversion multi-devise | Une devise par session en V1 ; `IDeviseMontantConverter` plus tard |
| API remboursement | Etat `REFUNDED` en base ; endpoint `refund` non expose V1 |
| Lien `customerRef` → `Clients` | Optionnel futur ; pas de FK V1 |
| Reporting unifie transport/evenement | Phase 6 — dashboard événement dédié ; fusion widgets transport reportée |
| FlexPay evenement | Phase 5 — **livre** (`DOCUMENTATION_FLEXPAY_EVENEMENT_V1.md`) |

---

## 11) Artefacts SQL associes (valides)

| Script | Role |
|--------|------|
| `Scripts/production_evenement_ticketing_v1.sql` | DDL tables `Evenement*`, triggers, index |
| `Scripts/rollback_evenement_ticketing_v1.sql` | Rollback complet |
| `Scripts/test_concurrency_evenement_ticketing_v1.sql` | Tests manuels anti-survente A/B/C |
| `Scripts/production_evenement_hold_expiration_job.sql` | Procedure `sp_ExpireEvenementHolds` + event scheduler optionnel |
| `Scripts/verify_evenement_api_db_contract.sql` | Verification alignement payload API ↔ colonnes DB |

---

## 12) Convention de nommage C# — Evenementiel vs Transport

Objectif : **zéro collision** avec les modèles transport existants (`Reservation`, `Billet`, `Paiement`, `Siege`…) et alignement strict avec les tables SQL `Evenement*`.

### Principe retenu

- **Préfixe `Evenement`** sur toutes les classes, interfaces, enums, DTOs et permissions du module événementiel.
- **Pas de suffixe** `*Evenement` (ex. `ReservationEvenement` interdit).
- **Terme `Ticket`** côté événement — jamais `Billet` (réservé au transport bus).

### Entités EF : table SQL → classe C#

| Table SQL | Classe C# | Équivalent transport (ne pas réutiliser) |
|-----------|-----------|------------------------------------------|
| `EvenementSessions` | `EvenementSession` | — |
| `EvenementClasses` | `EvenementClasse` | `CategorieSiege` |
| `EvenementSessionSections` | `EvenementSessionSection` | — |
| `EvenementSessionGlobalQuotas` | `EvenementSessionGlobalQuota` | — |
| `EvenementSessionClassQuotas` | `EvenementSessionClassQuota` | — |
| `EvenementSessionSeats` | `EvenementSessionSeat` | `Siege` |
| `EvenementReservations` | `EvenementReservation` | `Reservation` |
| `EvenementReservationLines` | `EvenementReservationLine` | — |
| `EvenementTickets` | `EvenementTicket` | `Billet` |
| `EvenementPayments` | `EvenementPayment` | `Paiement` |

Clés primaires SQL déjà préfixées (`IdEvenementReservation`, `IdEvenementTicket`, …) — pas de collision avec `IdReservation`, `IdBillet`, `IdPaiement`.

### Termes interdits vs termes à utiliser

| Éviter | Utiliser à la place |
|--------|---------------------|
| `ReservationEvenement` | `EvenementReservation` |
| `BilletEvenement`, `EvenementBillet` | `EvenementTicket` |
| `PaiementEvenement` | `EvenementPayment` |
| `SiegeEvenement` | `EvenementSessionSeat` |
| `ClasseEvenement` (seul) | `EvenementClasse` |
| Réutiliser `BilletResponseDto` | `EvenementTicketResponseDto` |
| Route `api/Billet/events/...` | `api/events/tickets/...` |

### Routes API

| Transport (inchangé) | Evenementiel (nouveau) |
|---------------------|--------------------------|
| `api/Reservation` | `api/events/reservations` |
| `api/Billet` | `api/events/tickets` |
| `api/Paiement` | confirm-payment sous réservations événement |

### Enums (préfixe `Evenement`)

| Enum C# | Rôle | Transport (distinct) |
|---------|------|----------------------|
| `EvenementInventoryMode` | SeatNumbered, ClassQuota, GlobalQuota | — |
| `EvenementReservationStatus` | HOLD, CONFIRMED, CANCELLED, EXPIRED | `StatutReservation` (string) |
| `EvenementTicketStatus` | ISSUED, USED, VOID | `Billet.IsUsed` |
| `EvenementPaymentStatus` | PENDING, SUCCEEDED, FAILED, REFUNDED | `StatutPaiement` |

### Garde-fous anti-régression transport

1. Tables SQL `Evenement*` — aucune FK vers `Reservations` / `Billets`.
2. Classes C# distinctes — pas d'héritage ni partial partagé avec le transport.
3. `ConfigureEvenementEntities()` isolé dans `CongoTravelDbContext` — ne pas modifier les configs `Reservation` / `Billet`.
4. Controllers et routes séparés — pas d'extension de `ReservationController` ni `BilletController`.
5. Services dans `Services/Evenement/` — pas de modification de `BilletService`, `ReservationService`, `PaiementService`.
6. Tests dédiés `Evenement*Tests.cs` — ne pas altérer les tests transport sauf régression explicite.

### Critères de validation avant merge Phase 1

- Une seule classe `Reservation` dans `Models/` (transport).
- Une seule classe `Billet` dans `Models/` (transport).
- Toutes les nouvelles entités commencent par `Evenement`.
- Aucun `DbSet` événement ne réutilise un nom existant (`Reservations`, `Billets`, `Paiements`, …).

---

**Prochaine etape implementation** : déploiement prod (migrations EF + scripts SQL) ou enrichissements post-V1 (widgets transport dans GerantDashboard, tendances 12 mois).
