# Embarquement billet et passagers embarqués par voyage

Référence fonctionnelle et API pour le scan à l’embarquement et la consultation de la liste des passagers déjà embarqués.

**Code principal** : `Controllers/BilletController.cs`, `Services/BilletService.cs`, `Controllers/VoyageController.cs`, `Services/VoyageService.cs`.

> Guide de migration complet (reproduction dans un autre projet) : [`GUIDE_MIGRATION_IDENTITE_PASSAGER_CHECK_BILLET.md`](GUIDE_MIGRATION_IDENTITE_PASSAGER_CHECK_BILLET.md)

---

## 1. Enregistrer un embarquement (scan billet)

### Endpoint

`POST /api/Billet/societe/{idSociete}/passager/{idReservationPassenger}/billet/{idBillet}/embarquer`

- **Auth** : JWT (`[Authorize]` sur le contrôleur).
- **Corps** : aucun.
- **Query optionnelle** : `idVoyageCible` pour embarquer sur un voyage alternatif compatible (même `IdDestination`).
- **Utilisateur** : l’identifiant JWT (`sub` / `NameIdentifier`) est enregistré sur l’historique d’embarquement lorsqu’il est un entier valide.

### Réponses

| Code | Cas |
|------|-----|
| 200 | Embarquement enregistré ; corps : `EmbarquerBilletResponseDto` (`idEmbarquement`, `dateEmbarquementUtc`, `idUtilisateurEnregistrement`, `billet` mappé en `BilletResponseDto`). |
| 400 / 404 / 409 | Erreur métier ou ressource ; corps typique : `{ "message": "..." }`. |

### Règles de validation (ordre logique)

1. **Billet** : existe ; `billet.IdSociete == idSociete`.
2. **Éligibilité** (`EvaluerEligibiliteEmbarquementAsync`) : billet non utilisé (`IsUsed`), pas de ligne existante dans `BilletEmbarquements` pour ce billet ; si réservation liée : réservation active, non annulée, statut confirmé (`CONFIRMEE` / `CONFIRME`), voyage actif, billet dans sa période de validité ; **fenêtre d’embarquement** (voir § 1.1) ; cohérence réservation / voyage.
3. **Passager** : `billet.IdReservationPassenger` doit correspondre à `idReservationPassenger` ; le passager existe ; `passenger.IdSociete == idSociete` ; si le billet a une réservation, `billet.IdReservation == passenger.IdReservation`.
4. **Persistance** : `IsUsed = true`, insertion `BilletEmbarquement` ; en cas de course concurrente sur l’index unique billet → **409**.

### 1.1 Fenêtre d’embarquement (billet lié à une réservation avec voyage)

Les horaires sont basés sur **`DateTime.Now`** (fuseau du serveur / machine hébergeant l’API).

- **Référence jour** : `jourDepart = voyage.DateDepart.Date` (minuit du jour civil de départ du voyage).
- **Ouverture** : `jourDepart − 3 heures`.
- **Fermeture** : `jourDepart + 24 heures` (jusqu’au lendemain à minuit, même référence locale que ci-dessus).

En dehors de cette fenêtre : réponse **400**, code métier côté check billet `HorsFenetreEmbarquement` ; les **textes** des messages d’erreur pour ce code sont stables (compatibilité affichage front).

**Billet sans réservation** : la fenêtre voyage ne s’applique pas sur ce chemin court ; les contrôles société / passager restent obligatoires.

### Contrôle préalable (QR)

`GET /api/Billet/{QrCode}/check` — même règles d’éligibilité exposées dans `BilletCheckResponseDto` (`embarquementAutorise`, `statut`, `message`, etc.). Query optionnelle `idVoyageCible`.

Champs horaires voyage (quand le voyage de référence est connu) :

- `dateDepartVoyage` : jour civil de départ (`voyage.DateDepart.Date`)
- `heureDepartVoyage` : heure de départ du trajet (`voyage.HeureDepart`)

Ces champs sont renseignés pour les statuts `Valide`, `HorsFenetreEmbarquement`, `BilletExpire`, `BilletPasEncoreValide`, etc. — pas seulement lorsque l’embarquement est autorisé. Le message d’expiration (`BilletExpire`) porte sur la **fin de validité du billet** (`DateValiditeFin`), distincte de l’heure de départ affichée.

Identité affichée à l’embarquement (quand le billet est reconnu) :

- `nomClient` : `NomComplet` du passager lié au billet (`ReservationPassagers`), **pas** le nom du client payeur
- `telephoneClient` : `Telephone` du même passager, **pas** celui du client payeur

Ces noms de propriétés sont conservés pour compatibilité frontend ; seule la source des valeurs côté backend reflète le passager réel transporté.

### Consultation billet par QR code

`GET /api/Billet/qrcode/{qrCode}` — retourne un tableau de `BilletResponseDto` pour les billets dont le `QrCode` contient la valeur fournie.

Même sémantique d’identité que le check QR ci-dessus, **uniquement sur cette route** :

- `nomClient` : `NomComplet` du passager lié au billet (`ReservationPassagers`)
- `telephoneClient` : `Telephone` du même passager

Les autres routes billet (`GET {id}`, `reservation/{id}`, liste paginée, réponse d’embarquement, etc.) conservent le mapping AutoMapper standard (`nomClient` = client payeur, `nomPassager` = passager).

### Réaffectation explicite de billet

`POST /api/Billet/societe/{idSociete}/billet/{idBillet}/reaffecter`

- Permet de réaffecter un billet vers un voyage de même `IdDestination` (strict).
- **Éligibilité dédiée** (distincte de l’embarquement / scan QR) : pas de fenêtre d’embarquement (J-3h / J+24h) ; la réaffectation vers un voyage futur reste possible même si l’embarquement n’est pas encore « ouvert ».
- Le billet doit être lié à une **réservation confirmée** et à un **passager** (`IdReservationPassenger`) pour permettre le déplacement du siège.
- Le **voyage cible** doit avoir un départ futur : si `DateDepart + HeureDepart <= now` → refus **409** (`Le voyage cible a déjà départé`).
- Réaffectation vers le **même voyage** que la réservation actuelle → refus **409**.
- Vérifie la disponibilité d’un siège dans la **même catégorie** sur le voyage cible (allocations `CONFIRME` + **holds FlexPay** actifs via `ISiegeDisponibiliteService`). Si aucun siège libre → **409**.
- Calcule le différentiel tarifaire (`prixVoyageCible - prixVoyageInitial`) :
  - si `delta <= 0` : réaffectation directe ;
  - si `delta > 0` : confirmation explicite requise (`confirmerPaiementDifferentiel=true`), sinon conflit métier.
- Calcule aussi une pénalité en pourcentage :
  - `penaliteAppliquee = billet.PenaliteOverride ?? (montantPayeBillet * configSociete.PenaliteReaffectationPourcentage / 100)`,
  - `montantPayeBillet` = part payée du billet (prorata multi-passagers),
  - appliquée uniquement si le départ du voyage **source** est déjà passé (`now > DateDepart + HeureDepart`).
- Contrôle une **fenêtre limite de réaffectation** avant départ **source** :
  - `departSource = voyageSource.DateDepart + voyageSource.HeureDepart`,
  - `deadlineReaffectation = departSource - configSociete.HeuresLimiteReaffectation`,
  - si `now > deadlineReaffectation` : réaffectation refusée (409).
- Montant total de régularisation = `max(delta, 0) + penaliteAppliquee`.
- Quand le total est positif et confirmé, un paiement de régularisation est enregistré dans `Paiements` (méthode/référence via `methodePaiement` / `referenceTransaction`).

Exemples (réponse):
- **Cas accepté** (dans la fenêtre, voyage cible futur, siège disponible) :
  - `message: "Billet réaffecté avec succès."`
  - `heuresLimiteReaffectation: 2`
  - `departVoyageSource`, `deadlineReaffectation`
  - `differentielTarifaire`, `penalite`, `montantTotalRegularisation`
- **Cas refusé** (hors fenêtre source) :
  - `message: "Réaffectation non autorisée: la fenêtre limite est dépassée."`
  - `heuresLimiteReaffectation`, `departVoyageSource`, `deadlineReaffectation`
- **Cas refusé** (voyage cible déjà parti) :
  - `message: "Le voyage cible a déjà départé. Réaffectation non autorisée."`
- **Cas refusé** (siège indisponible) :
  - `message: "Aucun siège disponible dans la catégorie du billet pour le voyage cible (réservation ou paiement en cours)."`

---

## 2. Liste des passagers embarqués pour un voyage (critères métier)

### Endpoint

`GET /api/Voyage/passagers-embarques`

### Paramètres query

| Paramètre | Obligatoire | Description |
|-----------|-------------|-------------|
| `idDestination` | Oui | `Voyage.IdDestination` (destination principale du voyage). |
| `idVehicule` | Oui | `Voyage.IdVehicule`. |
| `dateDepart` | Oui | Date de départ ; **seul le jour civil** est utilisé (`Date` normalisée). Ex. `2026-05-13`. |
| `heureDepart` | Non | Si fourni : filtre strict sur `Voyage.HeureDepart` (égalité). Ex. `08:30:00` ou `08:30`. |

Validation applicative : `idDestination` et `idVehicule` > 0 ; `dateDepart` non défaut à `default(DateTime)`.

### Résolution du voyage

- Les voyages candidats sont ceux dont `DateDepart.Date`, `IdDestination` et `IdVehicule` correspondent, et éventuellement `HeureDepart` si `heureDepart` est passé.
- **0** voyage → **404** (`{ "message": "..." }`).
- **Plus d’un** voyage encore candidat → **400** (ambiguïté : typiquement plusieurs départs le même jour sans `heureDepart`, ou doublons anormaux avec `heureDepart`).
- **Exactement 1** voyage → **200** avec un **tableau** (éventuellement vide si aucun embarquement enregistré).

### Corps de réponse 200

Tableau d’objets `PassagerEmbarqueVoyageItemDto` :

- `idEmbarquement`, `dateEmbarquementUtc`, `idBillet`, `idReservationPassenger`, `idReservation`, `idVoyage`, `nomComplet`, `telephone`, `idUtilisateurEnregistrement`.

Tri : par `dateEmbarquementUtc` croissant.

### Auth

JWT requis (`[Authorize]` sur `VoyageController`).

---

## 3. Intégration frontend (rappels)

- Pour désambigüiser un jour avec plusieurs rotations, envoyer **`heureDepart`** aligné sur la valeur stockée en base pour `Voyage.HeureDepart`.
- Les messages d’erreur **400 / 404** pour `passagers-embarques` sont des chaînes évolutives pour le détail ; le **contrat stable** pour le front reste surtout le **code HTTP** et la **forme** `{ "message": string }` pour les erreurs, et la **structure** du DTO pour le 200.
