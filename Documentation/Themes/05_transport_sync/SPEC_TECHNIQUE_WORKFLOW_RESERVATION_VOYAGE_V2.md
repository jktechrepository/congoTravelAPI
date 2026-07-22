# Spécification technique complète — Workflow réservation, sièges, voyage multi-destinations (V2)

**Document de référence** pour la refonte avant implémentation. Il consolide tous les éléments discutés : réservation pour soi ou pour tiers, plusieurs passagers par réservation, billet lié au passager et au siège, référentiel sièges par bus (`AliasBus/{1..N}`), voyage avec liste ordonnée de destinations.

**État du code actuel (référence)** : `Voyage` possède une seule `IdDestination` ; `Reservation` a `IdClient` + `NombreDePlace` ; `Billet` est lié à une réservation (optionnelle) et optionnellement à un client ; `BilletEmissionService` impose au plus un billet par réservation ; `Bus` expose `AliasBus` et `NombreSiege`.

---

## 1. Objectifs métier

1. Le client (acheteur) peut réserver pour **lui-même** ou pour **d’autres personnes**.
2. Une réservation peut concerner **plusieurs passagers**.
3. Chaque **billet** est associé à **un passager** (pas seulement à la réservation).
4. À la réservation, le système attribue un **siège** parmi les sièges du **bus du voyage**, avec un **code siège** stable et lisible.
5. Le format du code siège dans le référentiel est : **`{AliasBus}/{Numero}`** avec `Numero` entier de **1** à **`NombreSiege`** du bus (au moment de la génération du référentiel).
6. Un **voyage** peut être défini comme une **liste ordonnée de destinations** (ex. départ — étape 1 — étape 2 — …), sans se limiter à une seule paire ville départ/arrivée pour tout le trajet long.

---

## 2. Principes d’architecture

- **`Reservation`** : commande globale (voyage, acheteur, société, statut).
- **`ReservationPassenger`** : une ligne par personne transportée + rattache siège pour ce voyage.
- **`Siege`** : siège physique dans un **bus** ; identifié par `CodeSiege` = `{AliasBus}/{n}`.
- **`VoyageDestination`** : ordre des étapes du voyage ; réutilise le référentiel `Destination` existant comme « segment » métier (voir §5).
- **`VoyageSeatAllocation`** (recommandé) : occupation d’un siège **pour un voyage donné**, liée à un passager ; évite les doubles attributions sous charge concurrente.
- **`Billet`** : émis **par passager** ; porte les snapshots nécessaires (dont `CodeSiege`) pour l’historique.

Transactions : création réservation + passagers + allocations sièges + paiement + billets doit être **atomique** là où le métier l’exige (voir §10).

---

## 3. Modèle de données — entités et relations

### 3.1 `Bus` (existant — rappel)

- `IdBus`, `AliasBus` (string), `NombreSiege`, `IdSociete`, …
- Relation : **1 Bus — N Siege**.

### 3.2 `Siege` (nouveau)

Représente un siège du référentiel du bus.

| Champ | Type | Contraintes |
|-------|------|-------------|
| `IdSiege` | int PK identity | |
| `IdBus` | int FK → Bus | required |
| `NumeroOrdre` | int | required, unique avec `IdBus`, valeur 1..NombreSiege |
| `CodeSiege` | string(120) | required, unique `(IdBus, CodeSiege)` ; format **`{AliasBus}/{NumeroOrdre}`** au moment de la création |
| `Statut` | bool ou tinyint | siège actif / désactivé (panne, retrait) |
| `IdSociete` | int | required |
| `DateCreation`, `DateModification` | datetime | |

**Règles de génération**

- Lors de la création du référentiel sièges pour un bus (ou après création bus si procédé synchrone) : créer exactement **`NombreSiege`** lignes avec `NumeroOrdre = 1..NombreSiege` et `CodeSiege = $"{AliasBus.Trim()}/{NumeroOrdre}"`.
- Si `AliasBus` change après émission de billets : **ne pas** modifier rétroactivement les `CodeSiege` déjà utilisés sur des billets ; les sièges déjà créés peuvent garder l’ancien préfixe ou une stratégie explicite « régénération réservée aux bus sans réservation » (à trancher en prod).

**Remarque URL / QR** : le caractère `/` peut nécessiter un encodage correct dans les URLs ; en JSON/API ce n’est pas un problème.

### 3.3 `Voyage` (évolution)

**Actuellement** : `IdDestination` unique.

**Cible V2** :

- Conserver temporairement `IdDestination` pour **compatibilité** : pointer vers la destination « primaire » ou la **première étape** après migration (documentée).
- Ajouter navigation **`ICollection<VoyageDestination> Etapes`**.
- À terme : déprécier voire supprimer `IdDestination` une fois front et scripts migrés.

Champs voyage inchangés sauf évolutions métier ultérieures : `DateDepart`, `HeureDepart`, `Prix`, `IdBus`, `IdSociete`, etc.

### 3.4 `VoyageDestination` (nouveau)

Liste **ordonnée** des destinations constituant le trajet commercial du voyage.

| Champ | Type | Contraintes |
|-------|------|-------------|
| `IdVoyageDestination` | int PK | |
| `IdVoyage` | int FK | required |
| `IdDestination` | int FK → Destination | required |
| `Ordre` | int | ≥ 1 ; unique `(IdVoyage, Ordre)` |
| `IdSociete` | int | required |
| Optionnel | horaires segment | hors périmètre V1 si non requis |

**Sémantique** : `Destination` reste une entité avec `VilleDepart` / `VilleArrivee` (référentiel actuel). Une **étape** du voyage est une ligne de `VoyageDestination` ; l’enchaînement `Ordre` décrit la séquence vue par l’utilisateur (V1 → V2 → V3 selon les enregistrements choisis).

**Règle métier à valider** : une même `IdDestination` peut-elle apparaître deux fois sur le même voyage (aller-retour logique) ? Si non : unique `(IdVoyage, IdDestination)`.

### 3.5 `Reservation` (évolution)

| Élément | Décision |
|---------|----------|
| `IdClient` | Conserver comme **client acheteur** (payeur / compte principal) pendant la transition. |
| `IdUtilisateur`, `IdVoyage`, `IdSociete` | Inchangés. |
| `NombreDePlace` | **Déprécier comme source de vérité** ; devient dérivé = `nombre de ReservationPassenger` ou maintenu par synchronisation contrôlée. |
| Navigation | `ICollection<ReservationPassenger> Passagers` |

### 3.6 `ReservationPassenger` (nouveau)

| Champ | Type | Contraintes |
|-------|------|-------------|
| `IdReservationPassenger` | int PK | |
| `IdReservation` | int FK | required |
| `IdClient` | int? FK → Client | si passager = client connu |
| `NomComplet` | string(200) | required |
| `Telephone` | string(20)? | |
| `Email` | string(256)? | |
| `DocumentType`, `DocumentNumero` | string? | selon besoins contrôle |
| `DateNaissance`, `Genre` | optionnel | |
| `IdSociete` | int | required |
| `Statut` | bool | |
| `DateCreation`, `DateModification` | datetime | |

**Note** : l’attribution du siège peut être portée ici (`IdSiege`) et/ou uniquement via `VoyageSeatAllocation` (voir ci-dessous). Recommandation : **FK `IdSiege` sur le passager** + ligne d’allocation pour unicité par voyage.

### 3.7 `VoyageSeatAllocation` (nouveau — fortement recommandé)

Garantit qu’un **siège donné du bus** n’est attribué **qu’une fois par voyage** pour les réservations actives.

| Champ | Type | Contraintes |
|-------|------|-------------|
| `IdVoyageSeatAllocation` | int PK | |
| `IdVoyage` | int FK | required |
| `IdSiege` | int FK | required |
| `IdReservationPassenger` | int FK | required |
| `Statut` | string(20) | ex. `RESERVE`, `CONFIRME`, `ANNULE`, `LIBERE` |
| `DateCreation`, `DateModification` | datetime | |
| Optionnel | `DateExpiration` | pour paniers temporaires |

**Contrainte d’unicité métier** : unique filtrée ou logique applicative pour `(IdVoyage, IdSiege)` lorsque `Statut` ∈ {actifs}. En MySQL, implémentation courante : table unique + statuts ou colonne `IsActive` + unique `(IdVoyage, IdSiege)` sur lignes actives (selon version / partial index disponible).

### 3.8 `Billet` (évolution)

| Champ | Décision |
|-------|----------|
| `IdReservation` | Conserver pour requêtes agrégées. |
| `IdClient` | Optionnel legacy ; ne pas se baser dessus pour le passager réel en V2. |
| **`IdReservationPassenger`** | **FK required** (après migration données). |
| **`IdSiege`** | **FK required** (cohérence référentiel). |
| **`CodeSiege`** | **string snapshot** (copie au moment émission ; = `Siege.CodeSiege` ou copie explicite). |
| `QrCode` | Doit encoder au minimum : identifiant billet / voyage / passager / code siège (détail implémentation QR existant). |

**Règle** : **1 billet actif max par `ReservationPassenger`** pour un voyage donné.

### 3.9 `Paiement` (évolution)

- Conserver `IdReservation`, montants, `IdBilletEmis` si déjà utilisé : en V2, **`IdBilletEmis` devient legacy** (premier billet seulement) ou **nullable et déprécié** au profit d’une relation **1 paiement → N billets** ou d’un simple dénombrement :
  - soit table lien `PaiementBillet` (`IdPaiement`, `IdBillet`),
  - soit champ `NombreBilletsEmis` + liste via jointure `Billet.IdReservation`.

Recommandation minimale : **lier les billets à la réservation** ; retrouver le paiement via `IdReservation` ; déprécier progressivement `IdBilletEmis`.

---

## 4. Règles métier détaillées

### 4.1 Capacité et sièges

- Le nombre maximal de passagers pour un voyage **ne peut pas** dépasser le nombre de sièges **actifs** du bus affecté au voyage (`COUNT(Siege WHERE IdBus = Voyage.IdBus AND Statut actif)`).
- À la réservation : pour chaque passager, sélectionner un **`IdSiege` libre** pour ce **`IdVoyage`** (pas encore allocation active).

### 4.2 Attribution automatique du siège

- Stratégie V1 : premier siège libre par ordre `NumeroOrdre` croissant (ou file d’attente métier).
- Option V2 : sièges préférés dans la requête client (liste de `CodeSiege` souhaités) avec repli automatique.

### 4.3 Paiement et émission billet

- **Paiement complet** (`EstComplet`) : émettre **un billet par passager**, chacun avec son `CodeSiege`.
- **Paiement partiel** : pas d’émission billet (ou politique métier à définir : billet « conditionnel » — hors scope recommandé en V1).
- Supprimer la règle « un seul billet par réservation » du service d’émission actuel ; la remplacer par « **pas de doublon billet pour un même passager** ».

### 4.4 Annulation / remboursement

- Annulation réservation : libérer allocations sièges (`Statut = ANNULE` / suppression logique) et invalider billets selon règles légales internes.

### 4.5 Voyage multi-destinations et billet

**V1** : le billet couvre **le voyage entier** (toutes les étapes) ; le siège est valide pour tout le trajet sur ce bus.

**V2 évolutive** (hors périmètre initial si non demandé) : billet segmenté (prix / montée-descente par étape).

---

## 5. Usage de `Destination` avec `VoyageDestination`

Le modèle actuel `Destination` décrit une paire `(VilleDepart, VilleArrivee)`. Pour représenter « V1 → V2 → V3 » :

- soit chaque étape est une entrée **référentiel** `Destination` avec départ/arrivée cohérents avec la chaîne ;
- soit à terme introduction d’une entité **`Arret`** ou **`Lieu`** et `Destination` devient une vue agrégée — **non requis** pour la V2 décrite si le référentiel segmentaire suffit.

La spec **ne impose pas** de changer la structure interne de `Destination` tant que les étapes sont correctement chaînées via `Ordre`.

---

## 6. Contrats API (vue cible)

### 6.1 Création réservation avec passagers, sièges et paiement

**Implémenté** (phases C–D) :

**POST** (équivalents, même handler et même corps) :

- `/api/Reservation/reservation_with_paiement` — route historique ;
- `/api/Reservation/with-passengers-and-paiement` — **alias Phase D** (nom explicite côté clients).

Contrôleur : `ReservationController.CreateReservationWithPaiement` — **`[Authorize]`** : joindre un JWT Bearer valide.

Corps JSON (**camelCase**, schéma `CreateReservationWithPaiementDto`) :

| Zone | Champs | Règles |
|------|--------|--------|
| `reservation` | `idVoyage`, `idClient` (acheteur), `nombreDePlace`, `idUtilisateur`, `idSociete` | Client et voyage existants ; capacité voyage vérifiée côté serveur. |
| `reservation.passagers` | liste optionnelle d’objets (`idClient?`, `nomComplet`, `telephone?`, `email?`, `documentType?`, `documentNumero?`, `genre?`) | Si **`nombreDePlace === 1`** et liste absente ou vide → **un passager synthétique** est créé à partir du client acheteur. Si **`nombreDePlace > 1`** → liste **obligatoire**, **`passagers.length === nombreDePlace`**. |
| `paiement` | `montantAPaye`, `montantPaye`, `methodePaiement`, `referenceTransaction?`, `idUtilisateur`, `idSociete` | Si `montantPaye` couvre la totalité → émission billet(s). |

Réponse **`ReservationWithPaiementResponseDto`** (extraits utiles V2) :

- `reservation`, `paiement`
- **`billets`** : tableau des billets émis (un par passager si paiement complet et flux avec passagers + allocations).
- **`billet`** : alias rétrocompat = **premier** élément de `billets` (référence toujours alignée avec `paiement.idBilletEmis` / premier billet).
- Chaque billet expose notamment `qrCode`, `codeSiege`, `idSiege`, `idReservationPassenger`.

**Non implémenté encore** : choix libre du siège côté client (`codeSiegeSouhaite`) ; attribution **automatique** par le service d’allocation.

**Bootstrap sync** : `SyncBootstrapDto.reservationWorkflowV2` expose les chemins ci-dessus + lectures §6.2 pour orienter les apps (pas un delta réservations ; voir doc `SyncController`).

Exemples prêts à l’emploi : **`CongoTravelApi.http`** (création + lectures) ; Postman **`CongoTravel_Workflow_V2.postman_collection.json`**.

### 6.2 Lectures utiles

Implémentées (**JWT** requis, même contrôleurs que le reste de l’API) :

| Méthode | Route | Réponse (schéma) |
|---------|--------|------------------|
| GET | `/api/Voyage/{id}/destinations` | Liste `VoyageEtapeReadDto` : `ordre`, `idDestination`, `villeDepart`, `villeArrivee`, … |
| GET | `/api/Voyage/{id}/sieges-disponibles` | Liste `SiegeLibreReadDto` : `idSiege`, `numeroOrdre`, `codeSiege` |
| GET | `/api/Reservation/{id}/passagers` | Liste `ReservationPassengerReadDto` |
| GET | `/api/Reservation/{id}/billets` | Liste `BilletResponseDto` (profil AutoMapper existant : voyage, client, `nomPassager`, etc.) |

Code : `VoyageController`, `ReservationController` ; données via `IVoyageRepository` / `IReservationRepository` / `IBilletRepository`.  
Mappings lecture : **`WorkflowReservationMappingProfile`** (`VoyageEtapeReadDto`, `SiegeLibreReadDto`, `ReservationPassengerReadDto`).

Collections Postman : **`CongoTravel_Workflow_V2.postman_collection.json`** (workflow V2 minimal, même variables `baseUrl` / `accessToken` que la collection principale si besoin).

### 6.3 Compatibilité

- **`/api/Reservation/reservation_with_paiement`** et **`/api/Reservation/with-passengers-and-paiement`** portent le **pipeline V2** : passagers persistés, `VoyageSeatAllocation`, **N billets** si paiement complet.
- `passagers` est **obligatoire** (un passager par place) et chaque passager doit fournir `idCategorieSiege`.
- Le backend conserve un contrôle strict: `paiement.montantAPaye` doit correspondre au total calculé à partir des catégories de sièges réellement attribuées.

---

## 7. Services et orchestration

### 7.1 Nouveaux services suggérés

- **`ISiegeService`** : génération référentiel sièges pour un bus ; resynchronisation si `NombreSiege` augmente (création des lignes manquantes uniquement avec précaution).
- **`IVoyageSeatAllocationService`** : liste sièges disponibles pour un voyage ; réserve atomiquement N sièges pour N passagers.
- **`IReservationFlowService`** (ou extension de `ReservationWithPaiementService`) : pipeline validation capacité → création réservation → passagers → allocations → paiement → émission N billets.

### 7.2 Concurrence

- Utiliser **transaction** avec stratégie de retry déjà présente (`CreateExecutionStrategy`).
- Appuyer l’unicité `(IdVoyage, IdSiege)` sur allocations **actives** pour éviter course critique entre deux ventes.

---

## 8. Migration depuis l’existant

Ordre recommandé :

1. Créer tables `Siege`, `VoyageDestination`, `ReservationPassenger`, `VoyageSeatAllocation`.
2. Backfill **Siege** pour tous les bus existants selon `AliasBus` + `NombreSiege`.
3. Pour chaque **Voyage** existant : créer une ligne `VoyageDestination` avec `Ordre = 1` et `IdDestination = Voyage.IdDestination`.
4. Pour **Reservation** existantes : créer **un** `ReservationPassenger` depuis `IdClient` ; attribuer **un** siège libre du bus du voyage ; créer allocation ; rattacher **Billet** existant au passager + siège + snapshot `CodeSiege`.
5. Cas **`NombreDePlace` > 1** sans détail passagers : traitement **manuel ou script assisté** (liste de contacts ou duplication contrôlée) — à documenter dans Runbook migration.

---

## 9. Tests et critères d’acceptation

- Réservation 1 passager (soi) : siège attribué, billet avec bon `CodeSiege`.
- Réservation N passagers : N sièges distincts, N billets.
- Impossible de dépasser capacité bus pour un même voyage.
- Deux réservations concurrentes sur le dernier siège : une seule réussit (contrainte + comportement retry attendu).
- Voyage avec plusieurs `VoyageDestination` : ordre correct exposé API ; aucune régression sur liste voyages existante si `IdDestination` maintenu en miroir.

---

## 10. Points ouverts (à trancher avec le métier)

1. Réutilisation de la même **destination** deux fois sur un même voyage (aller-retour).
2. Évolution tarifaire **par segment** vs prix global sur `Voyage`.
3. Politique si **réduction du `NombreSiege`** après création de sièges (interdit ? désactivation sièges uniquement ?).
4. Politique si **`AliasBus`** change après ventes.

---

## 11. Références fichiers code actuels impactés (liste indicative)

- `Models/Voyage.cs`, `Models/Reservation.cs`, `Models/Billet.cs`, `Models/Bus.cs`
- `Data/CongoTravelDbContext.cs`
- `Services/ReservationWithPaiementService.cs`, `Services/BilletEmissionService.cs`
- `Controllers/ReservationController.cs`, `Controllers/VoyageController.cs`
- DTOs sous `Models/DTOs/` et profils AutoMapper associés
- Nouvelles migrations EF Core sous `Migrations/`

---

## 12. Checklist d’implémentation ordonnée

Les phases sont séquentielles ; à l’intérieur d’une phase, certains sous-points peuvent être parallélisés si les dépendances le permettent.

### 12.0 État résumé & tests manuels (post Phase C)

| Sujet | Détail |
|--------|--------|
| Endpoint création | `POST /api/Reservation/reservation_with_paiement` ou alias `POST /api/Reservation/with-passengers-and-paiement` (JWT requis) |
| Contrat entrée / sortie | Voir **§6.1** ; DTOs `CreateReservationWithPaiementDto`, `ReservationWithPaiementResponseDto` |
| Exemples HTTP / Postman | `CongoTravelApi.http` ; `CongoTravel_Workflow_V2.postman_collection.json` |
| QR par passager | Le QR reste au format existant (`QrCodeService`) ; unicité portée par la base ; pas de surcharge dédiée « passager » dans la chaîne du QR pour l’instant. |

**Phase C (domain services)** : considérée **réalisée** dans le dépôt (sièges bus, allocation sérialisable, émission multi-billets, orchestration dans `ReservationWithPaiementService`).  
**Phase D** : réalisée (alias POST, mappings AutoMapper lecture, hints bootstrap sync `reservationWorkflowV2`). Évolution future : delta sync réservations/billets si besoin métier.

---

### Phase A — Socle données (sans changer le comportement API métier critique)

| Ordre | Tâche | Livrable |
|-------|--------|----------|
| A1 | Ajouter les modèles `Siege`, `VoyageDestination`, `ReservationPassenger`, `VoyageSeatAllocation` | Fichiers dans `Models/` |
| A2 | Configurer `CongoTravelDbContext` : `DbSet`, relations, index, contraintes uniques documentées dans la spec §3 | `Data/CongoTravelDbContext.cs` |
| A3 | Migration EF : création des tables + colonnes nullable sur entités existantes si nécessaire pour migration données | `Migrations/` |
| A4 | Script/service **backfill sièges** : pour chaque `Bus`, générer `NombreSiege` lignes `Siege` avec `CodeSiege = AliasBus/{1..N}` | Hosted service ponctuel, migration SQL idempotent, ou endpoint admin protégé |
| A5 | Backfill `VoyageDestination` : pour chaque `Voyage` existant, une ligne `(Ordre=1, IdDestination=Voyage.IdDestination)` | Données |
| A6 | (`dotnet ef database update` sur environnements dev/staging) | Base à jour |

**Critère de fin de phase A** : toutes les tables existent ; tous les bus ont le bon nombre de sièges ; tous les voyages ont au moins une étape.

---

### Phase B — Évolution modèles existants et données historiques

| Ordre | Tâche | Livrable |
|-------|--------|----------|
| B1 | Étendre `Billet` : `IdReservationPassenger`, `IdSiege`, `CodeSiege` (snapshots) — colonnes nullable au début si besoin | Modèle + migration |
| B2 | Migrer les réservations/billets historiques : 1 `ReservationPassenger` par réservation existante ; rattacher billet ; créer `VoyageSeatAllocation` | Script migration données + journal anomalies (`NombreDePlace > 1`) |
| B3 | Rendre obligatoires les FK nouvelles sur `Billet` après backfill complet | Migration finale contraintes |
| B4 | Décision et implémentation `Paiement` : déprécier `IdBilletEmis` ou table lien `PaiementBillet` | Selon §3.9 |

**Critère de fin de phase B** : aucun billet « orphelin » ; réconciliation impossible documentée dans un runbook.

---

### Phase C — Domain services ✅ (implémenté)

| Ordre | Tâche | Livrable |
|-------|--------|----------|
| C1 | `ISiegeService` / implémentation : génération sièges bus, désactivation siège | ✅ `Services/ISiegeService.cs`, `Services/SiegeService.cs`, DI `Program.cs` (`EstActif` ; pas d’endpoint dédié « désactivation ») |
| C2 | `IVoyageSeatAllocationService` : sièges disponibles par voyage ; réservation atomique de N sièges | ✅ `Services/VoyageSeatAllocationService.cs` (transaction `Serializable` + `EnsureSeatsForBusAsync` dans la même unité) |
| C3 | Adapter `BusService` ou pipeline création bus : appeler génération sièges après création / mise à jour `NombreSiege` | ✅ `Services/BusService.cs` après `SaveChanges` create/update |
| C4 | Refactor `BilletEmissionService` : N billets ; lever la contrainte « un billet par réservation » | ✅ `EmitBilletsPourPaiementAsync`, validation par passager ; `EmitreBilletAsync` → premier billet |
| C5 | Extension `ReservationWithPaiementService` : création passagers + allocations + paiement + émission | ✅ `Services/ReservationWithPaiementService.cs` + validations capacité |

**Critère de fin de phase C** : tests d’intégration automatisés encore ouverts (**Phase E**) ; tests manuels via **`CongoTravelApi.http`** + §6.1.

---

### Phase D — DTOs, mapping, API ✅

| Ordre | Tâche | Livrable |
|-------|--------|----------|
| D1 | DTOs request/response multi-passagers + sièges | ✅ `ReservationDataDto.Passagers`, `ReservationPassengerInputDto` / `ReservationPassengerReadDto`, `ReservationWithPaiementResponseDto.Billets`, `VoyageEtapeReadDto`, `SiegeLibreReadDto` |
| D2 | Profils AutoMapper | ✅ `WorkflowReservationMappingProfile` (+ profil billets existant pour `GET .../billets`) |
| D3 | `POST .../with-passengers-and-paiement` + endpoints lecture | ✅ Alias + `GET` §6.2 |
| D4 | Mapper endpoint legacy `reservation_with_paiement` vers pipeline V2 (1 passager) | ✅ Inchangé : `nombreDePlace: 1` sans `passagers` → passager synthétique |
| D5 | Mettre à jour `SyncController` / bootstrap sync | ✅ `SyncBootstrapDto.ReservationWorkflowV2` (`ReservationWorkflowV2ApiHintsDto`) + doc XML bootstrap |

**Critère de fin de phase D** : Swagger expose deux routes POST équivalentes ; Postman / `.http` à jour ; §6 aligné — **OK**. Phase suivante recommandée : **§E** (tests automatisés, charge, logs, prod SQL).

---

### Phase E — Qualité, perf, exploitation

| Ordre | Tâche | Livrable |
|-------|--------|----------|
| E1 | Tests unitaires : attribution sièges, capacité, unicité allocation | ✅ `Tests/WorkflowSeatAllocationTests.cs` |
| E2 | Test charge léger ou test concurrentiel sur dernier siège | ⚠️ test `Concurrent_allocations_OnSingleSeat` **Skip** sous InMemory (unicité non garantie) ; même fichier + comportement prod via transactions sérialisables MySQL |
| E3 | Logs structurés : `TransactionId`, `IdVoyage`, `IdReservation`, sièges attribués | ✅ `VoyageSeatAllocationService`, `ReservationWithPaiementService` (scopes + `IdSieges`) |
| E4 | Mise à jour `deployProduction.sql` ou procédure prod équivalente | ✅ Commentaire fin `deployProduction.sql` + `Scripts/deployProduction_workflow_v2_addon.sql` (EF `-i`) |

**Critère de fin de phase E** : tests sièges / capacité verts ; concurrence documentée (Skip InMemory + garantie prod SQL/transactions) ; logs enrichis ; procédure SQL prod documentée — **OK**.

---

### Phase F — Décommissionnement progressif

| Ordre | Tâche |
|-------|--------|
| F1 | Front/client migrés vers nouveau endpoint |
| F2 | Marquer `NombreDePlace` et `IdDestination` seule sur `Voyage` comme dépréciés dans la doc API |
| F3 | Supprimer ou durcir endpoints legacy après fenêtre de transition |

---

### Ordre des commits Git suggéré (granularité PR)

1. `feat(db): Siege + VoyageDestination tables and migrations`
2. `feat(db): ReservationPassenger + VoyageSeatAllocation`
3. `feat(data): backfill sièges et étapes voyage`
4. `feat(domain): SiegeService + allocation service`
5. `feat(billet): emission multi-passagers`
6. `feat(reservation): flow with-passengers-and-paiement`
7. `feat(api): controllers + DTOs`
8. `test: reservation seat concurrency`
9. `chore: deprecations + docs`

---

*Fin du document — version consolidée pour implémentation workflow V2 (spec + checklist).*
