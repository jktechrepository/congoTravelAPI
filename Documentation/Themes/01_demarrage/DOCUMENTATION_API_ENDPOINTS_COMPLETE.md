# Catalogue complet des endpoints API

Ce document recense les endpoints exposes par les controllers backend.

## Regle de lecture

- Quand une route contient `api/[controller]`, remplace `[controller]` par le nom du controller sans suffixe `Controller`.
  - Exemple: `AgentController` -> `api/Agent`
  - Exemple: `PaiementController` -> `api/Paiement`
- Les routes `api/Devise` et `api/sync` sont deja explicites dans le code.

---

### AgentController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `POST /api/[controller]/{idAgent}/add-role`
- `PUT /api/[controller]/{idAgent}/replace-role`
- `GET /api/[controller]/{id}`
- `GET /api/[controller]/societe/{idSociete}`
- `GET /api/[controller]/societe/{idSociete}/paged`
- `GET /api/[controller]/statut/{statut}`
- `GET /api/[controller]/exists/{id}`
- `POST /api/[controller]`
- `POST /api/[controller]/batch`
- `PUT /api/[controller]/{id}`
- `PUT /api/[controller]/{idAgent}/AffecterAgentSite`
- `PUT /api/[controller]/{idAgent}/site`
- `DELETE /api/[controller]/{id}`
- `PUT /api/[controller]/toggle-statut/{id}`
- `GET /api/[controller]/serial-number/{serialNumber}`
- `PUT /api/[controller]/{idAgent}/serial-number`
- `PUT /api/[controller]/matricule/{matricule}/serial-number`

### AuditController
- Base route: `api/[controller]`
- `GET /api/[controller]/history/{tableName}/{recordId}`
- `GET /api/[controller]/user/{userId}`
- `GET /api/[controller]/recent`
- `GET /api/[controller]/school/{idSociete}`
- `GET /api/[controller]/search`
- `GET /api/[controller]/statistics`
- `GET /api/[controller]/suspicious`
- `GET /api/[controller]/me`

### AuthTestController
- Base route: `api/[controller]`
- `GET /api/[controller]/public`
- `GET /api/[controller]/protected`
- `GET /api/[controller]/permissions`

### BilletController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `POST /api/[controller]/societe/{idSociete:int}/passager/{idReservationPassenger:int}/billet/{idBillet:int}/embarquer`
- `GET /api/[controller]/{QrCode}/check` (QrCode = valeur exacte du code QR, pas l’id numérique du billet; query optionnelle `idVoyageCible` pour valider sur un voyage compatible)
- `POST /api/[controller]/societe/{idSociete:int}/billet/{idBillet:int}/reaffecter`
- `GET /api/[controller]/{id}`
- `POST /api/[controller]/paged`
- `GET /api/[controller]/reservation/{idReservation}`
- `GET /api/[controller]/qrcode/{qrCode}`
- `GET /api/[controller]/date/{date}`
- `GET /api/[controller]/daterange`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `DELETE /api/[controller]/{id}`
- `POST /api/[controller]/reservation/{idReservation}/paged`
- `POST /api/[controller]/date/{date}/paged`
- `GET /api/[controller]/count`
- `GET /api/[controller]/reservation/{idReservation}/count`
- `GET /api/[controller]/date/{date}/count`
- `GET /api/[controller]/daterange/count`

Notes (comportement):
- `POST /api/Billet/societe/{idSociete}/billet/{idBillet}/reaffecter` permet la réaffectation vers un autre voyage de même `IdDestination` si le billet est encore valide.
- Le corps accepte `idVoyageCible`, `confirmerPaiementDifferentiel`, `methodePaiement`, `referenceTransaction`, `commentaire`; si le différentiel tarifaire est positif et non confirmé, l’API retourne un conflit avec le montant à régulariser.
- La réaffectation est refusée si la demande arrive après la deadline calculée depuis le voyage source : `deadlineReaffectation = (DateDepart + HeureDepart) - HeuresLimiteReaffectation`.
- La réponse expose `differentielTarifaire`, `penalite`, `penaliteAppliquee`, `montantTotalRegularisation`, `heuresLimiteReaffectation`, `departVoyageSource`, `deadlineReaffectation`.
- Si le total de régularisation (différentiel + pénalité éventuelle) est positif et confirmé, une ligne de paiement est créée sur la réservation.

### CaissierDashboardController
- Base route: `api/[controller]`
- `GET /api/[controller]` — dashboard complet (statistiques, paiements, recettes, alertes, résumé caisse)
- `GET /api/[controller]/rapport-caisse` — rapport caisse caissier (espèces vs électronique) ; scope JWT ; params `datePrecise?`, `dateDebut?`, `dateFin?`

### CategorieSiegeController
- Base route: `api/[controller]`
- `GET /api/[controller]/societe/{idSociete:int}`
- `GET /api/[controller]/{idCategorieSiege:int}`
- `POST /api/[controller]`
- `PUT /api/[controller]/{idCategorieSiege:int}`
- `PUT /api/[controller]/{idCategorieSiege:int}/toggle-statut`
- `DELETE /api/[controller]/{idCategorieSiege:int}`

### ClientController
- Base route: `api/[controller]`
- `POST /api/[controller]/register`
- `POST /api/[controller]/check-email`
- `GET /api/[controller]`
- `GET /api/[controller]/paged`
- `GET /api/[controller]/societe/{idSociete}` — clients ayant réservé dans la société ; `Client.ReadAll` ; scope JWT
- `GET /api/[controller]/societe/{idSociete}/paged` — idem + pagination/recherche/tri
- `GET /api/[controller]/societe/{idSociete}/recherche` — idem + `searchTerm` multi-champs
- `GET /api/[controller]/{id}`
- `GET /api/[controller]/nom/{nom}`
- `POST /api/[controller]`
- `POST /api/[controller]/simple`
- `PUT /api/[controller]/{id}`
- `DELETE /api/[controller]/{id}`
- `PUT /api/[controller]/toggle-statut/{id}`
- `PUT /api/[controller]/set-statut/{id}`

### ClientDashboardController
- Base route: `api/[controller]`
- `GET /api/[controller]` — dashboard complet (statistiques, réservations, paiements, voyages, alertes, résumé)

### CommunicationCampaignController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/paged`
- `GET /api/[controller]/{id}`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `DELETE /api/[controller]/{id}`
- `POST /api/[controller]/{id}/execute`
- `GET /api/[controller]/{id}/preview`

### DashboardController
- Base route: `api/[controller]`
- `GET /api/[controller]/{idSociete}`

### DestinationController
- Base route: `api/[controller]`
- `GET /api/[controller]` — liste **uniquement** les destinations avec `statut = true`
- `GET /api/[controller]/paged` — query optionnel `?idSociete={id}` pour filtrer par société ; **uniquement** `statut = true`
- `GET /api/[controller]/{id}` — scope JWT ; `403` hors société (hors Super-Admin) ; retourne la destination par ID **y compris si `statut = false`**
- `GET /api/[controller]/societe/{idSociete}` — destinations de la société (scope JWT ; Super-Admin : toutes sociétés) ; **uniquement** `statut = true`
- `GET /api/[controller]/societe/{idSociete}/paged` — scope JWT ; **uniquement** `statut = true`
- `GET /api/[controller]/search` — query `villeDepart`, `villeArrivee`, `idSociete` (obligatoire) ; scope JWT ; **uniquement** `statut = true`
- `POST /api/[controller]` — unicité `(IdSociete, VilleDepart, VilleArrivee)` ; `409` si doublon dans la même société ; `403` si `IdSociete` ≠ JWT (hors Super-Admin)
- `PUT /api/[controller]/{id}` — `409` doublon intra-société ; `403` hors scope
- `DELETE /api/[controller]/{id}` — `403` hors scope
- `PUT /api/[controller]/{id}/toggle-statut` — `403` hors scope

### DeviseController
- Base route: `api/Devise`
- `GET /api/Devise/devises`
- `POST /api/Devise/devises`
- `GET /api/Devise/devises/{idDeviseMonetaire:int}`
- `PUT /api/Devise/devises/{idDeviseMonetaire:int}`
- `PUT /api/Devise/societe/{idSociete:int}/devise-principale/{codeDevise}`
- `POST /api/Devise/taux-change`
- `GET /api/Devise/taux-change`
- `GET /api/Devise/preview-conversion`

### FinanceReportingController
- Base route: `api/[controller]`
- `GET /api/[controller]/paiements/summary` — inclut `byOrigineGroupe` (totaux CLIENT / AGENT / INCONNU)
- `GET /api/[controller]/rapport-caisse` — rapport caisse (espèces vs électronique) avec filtres `idSociete`, `idUtilisateur?`, `datePrecise?`, `dateDebut?`, `dateFin?`

### FlexPayController
- Base route: `api/[controller]`
- `POST /api/[controller]/callback` (public — callback FlexPay)
- `GET /api/[controller]/verifier/{orderNumber}` (JWT — secours + polling front)
- `GET /api/[controller]/approve` (redirect carte, informatif)
- `GET /api/[controller]/cancel` (redirect carte, informatif)
- `GET /api/[controller]/decline` (redirect carte, informatif)

SignalR hub associé : `GET /hubs/notifications` — événements `FlexPayPaymentConfirmed` / `FlexPayPaymentFailed` (voir `FLEXPAY_STATUT_PAIEMENT_RULES.md`).

### FinancierDashboardController
- Base route: `api/[controller]`
- `GET /api/[controller]` — dashboard finance transport complet (scope société selon rôle JWT)
- Accès : `HasFinanceAccess` (Super-Admin, Gérant, Financier)
- **Supprimé v1 :** sous-routes `statistiques-globales`, `societes-financieres`, `transactions-recentes`, `alertes-financieres`, `tendances-financieres`

### GerantDashboardController
- Base route: `api/[controller]`
- `GET /api/[controller]` — dashboard Gérant transport complet (société du JWT)
- Accès : Gérant ou Super-Admin
- **Supprimé v1 :** `societe/{idSociete}`, `statistiques`, `alertes`

### InitController
- Base route: `api/[controller]`
- `POST /api/[controller]/initialize`
- `POST /api/[controller]/fix-permissions`
- `POST /api/[controller]/test-email`
- `GET /api/[controller]/diagnostic-permissions/{userId}`

### MetricsController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/system`
- `GET /api/[controller]/application`
- `GET /api/[controller]/database`
- `GET /api/[controller]/business`
- `GET /api/[controller]/health`
- `GET /api/[controller]/status`

### NotificationPreferenceController
- Base route: `api/[controller]`
- `GET /api/[controller]/mes-preferences`
- `PUT /api/[controller]/mes-preferences`
- `DELETE /api/[controller]/mes-preferences`

### PaiementController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/{id}`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `DELETE /api/[controller]/{id}`
- `GET /api/[controller]/reservation/{idReservation}`
- `GET /api/[controller]/client/{idClient}`
- `GET /api/[controller]/societe/{idSociete}`
- `GET /api/[controller]/societe/{idSociete}/paged` — query optionnelle `origineGroupe=CLIENT|AGENT|INCONNU`

Notes (comportement):
- `GET /api/Paiement/societe/{idSociete}/paged?origineGroupe=CLIENT` filtre les paiements auto-service (`Origine=CLIENT`). Réponse inclut `origine` (granulaire) et `origineGroupe` (CLIENT | AGENT | INCONNU).
- `GET /api/Paiement/reservation/{idReservation}` retourne les paiements de la réservation (filtre `Paiements.IdReservation` + `IsDeleted == false`).
- `GET /api/Paiement/client/{idClient}` retourne les paiements liés aux réservations du client (via `Reservation.IdClient`).

### PermissionController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/{id}`
- `GET /api/[controller]/by-category/{category}`
- `GET /api/[controller]/role/{roleId}`
- `GET /api/[controller]/my-permissions`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `DELETE /api/[controller]/{id}`
- `POST /api/[controller]/assign`
- `POST /api/[controller]/revoke`
- `POST /api/[controller]/assign-bulk`
- `GET /api/[controller]/check/{permissionName}`

### PlainteClientController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/paged`
- `GET /api/[controller]/en-attente`
- `GET /api/[controller]/assignees/{idAgent}`
- `GET /api/[controller]/mes-plaintes`
- `GET /api/[controller]/{id}`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `PATCH /api/[controller]/{id}/assigner`
- `PATCH /api/[controller]/{id}/statut`
- `PATCH /api/[controller]/{id}/resoudre`
- `DELETE /api/[controller]/{id}`

### RemboursementController
- Base route: `api/[controller]`
- `POST /api/[controller]`

### ReservationController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/{id}`
- `GET /api/[controller]/{id:int}/passagers`
- `GET /api/[controller]/Societe/{idSociete:int}/voyage/{idVoyage:int}`
- `GET /api/[controller]/{id:int}/billets`
- `POST /api/[controller]/paged`
- `GET /api/[controller]/utilisateur/{idUtilisateur}`
- `GET /api/[controller]/client/{idClient}`
- `GET /api/[controller]/voyage/{idVoyage}`
- `GET /api/[controller]/statutreservation/{statutReservation}`
- `GET /api/[controller]/date/{date}`
- `GET /api/[controller]/daterange`
- `GET /api/[controller]/utilisateur/{idUtilisateur}/client/{idClient}`
- `GET /api/[controller]/voyage/{idVoyage}/statut/{statutReservation}`
- `GET /api/[controller]/statut/{statut}`
- `GET /api/[controller]/active`
- `GET /api/[controller]/inactive`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `DELETE /api/[controller]/{id}`
- `POST /api/[controller]/utilisateur/{idUtilisateur}/paged`
- `POST /api/[controller]/client/{idClient}/paged`
- `POST /api/[controller]/voyage/{idVoyage}/paged`
- `POST /api/[controller]/statutreservation/{statutReservation}/paged`
- `GET /api/[controller]/count`
- `GET /api/[controller]/utilisateur/{idUtilisateur}/count`
- `GET /api/[controller]/client/{idClient}/count`
- `GET /api/[controller]/voyage/{idVoyage}/count`
- `GET /api/[controller]/statutreservation/{statutReservation}/count`
- `GET /api/[controller]/date/{date}/count`
- `GET /api/[controller]/statut/{statut}/count`
- `GET /api/[controller]/active/count`
- `GET /api/[controller]/inactive/count`
- `POST /api/[controller]/reservation_with_paiement`
- `POST /api/[controller]/reservation_with_paiement_electronique` (FlexPay — initiation sans réservation)
- `POST /api/[controller]/with-passengers-and-paiement`

### RoleController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/nomRole/{nomRole}`
- `GET /api/[controller]/{id}`
- `GET /api/[controller]/nom/{nom}`
- `GET /api/[controller]/exists/{id}`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `DELETE /api/[controller]/{id}`
- `PUT /api/[controller]/toggle-statut/{id}`

### SiteController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/{id:int}`
- `GET /api/[controller]/societe/{idSociete:int}`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id:int}`
- `PUT /api/[controller]/toggle-statut/{id:int}`
- `DELETE /api/[controller]/{id:int}`

### SocieteController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/{id}`
- `GET /api/[controller]/nom/{nom}`
- `GET /api/[controller]/code/{code}`
- `GET /api/[controller]/statut/{statut}`
- `GET /api/[controller]/{id}/utilisateurs`
- `GET /api/[controller]/{id}/agents`
- `GET /api/[controller]/{id}/agents/caissiers`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `DELETE /api/[controller]/{id}`
- `PUT /api/[controller]/toggle-statut/{id}`
- `PUT /api/[controller]/set-statut/{id}`

### StatistiquesController
- Base route: `api/[controller]`
- `GET /api/[controller]/{idSociete:int}?debut=&fin=` — statistiques transport consolidées (générales, financières, opérationnelles, performance)

### SyncController
- Base route: `api/sync`
- `GET /api/sync/bootstrap`
- `GET /api/sync/clients`
- `GET /api/sync/arrears`
- `GET /api/sync/deletions`
- `POST /api/sync/payments/batch`

### TypeVehiculeController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/societe/{idSociete}` — types de la société (scope JWT ; Super-Admin : toutes sociétés)
- `GET /api/[controller]/{id}`
- `POST /api/[controller]/paged` — query optionnel `?idSociete={id}` pour filtrer par société
- `GET /api/[controller]/statut/{statut}`
- `POST /api/[controller]` — unicité `(IdSociete, Libelle)` ; `409` si doublon dans la même société ; `403` si `IdSociete` ≠ JWT (hors Super-Admin)
- `PUT /api/[controller]/{id}` — `409` doublon intra-société ; `403` hors scope
- `DELETE /api/[controller]/{id}` — `403` hors scope

### UtilisateurController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/{id}`
- `GET /api/[controller]/email`
- `GET /api/[controller]/role/{roleId}`
- `GET /api/[controller]/statut/{statut}`
- `GET /api/[controller]/exists/{id}`
- `GET /api/[controller]/exists/email/{email}`
- `GET /api/[controller]/societe/{idSociete}`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `PUT /api/[controller]/{id}/admin`
- `DELETE /api/[controller]/{id}`
- `POST /api/[controller]/authentifier`
- `POST /api/[controller]/deconnecter`
- `POST /api/[controller]/mot-de-passe-oublie`
- `POST /api/[controller]/mot-de-passe-oublie/confirmer`
- `POST /api/[controller]/changer_mot_de_passe`
- `PUT /api/[controller]/toggle-statut/{id}` — self : désactivation (`Utilisateur.DeactivateSelf`) ; admin : toggle autre user (`Utilisateur.Update`, scope société)
- `POST /api/[controller]/reinitialiser-masse`
- `POST /api/[controller]/reinitialiser-un`
- `GET /api/[controller]/{id}/roles`
- `POST /api/[controller]/{id}/roles/{roleId}`
- `DELETE /api/[controller]/{id}/roles/{roleId}`
- `PUT /api/[controller]/{id}/roles/{roleId}/primary`
- `POST /api/[controller]/refresh-token`
- `POST /api/[controller]/revoke-token`
- `POST /api/[controller]/revoke-all-tokens`

### VehiculeController
- Base route: `api/[controller]`
- `GET /api/[controller]`
- `GET /api/[controller]/paged`
- `GET /api/[controller]/{id}`
- `GET /api/[controller]/societe/{idSociete}`
- `GET /api/[controller]/societe/{idSociete}/paged`
- `GET /api/[controller]/type/{typeVehicule}`
- `GET /api/[controller]/societe/{idSociete}/type/{typeVehicule}`
- `GET /api/[controller]/alias/{aliasVehicule}/societe/{idSociete}`
- `GET /api/[controller]/numero/{aliasVehicule}/societe/{idSociete}`
- `GET /api/[controller]/statut/{statut}`
- `GET /api/[controller]/marque/{marque}`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `PUT /api/[controller]/{id}/toggle-statut`
- `DELETE /api/[controller]/{id}`
- `GET /api/[controller]/count`
- `GET /api/[controller]/societe/{idSociete}/count`
- `GET /api/[controller]/type/{typeVehicule}/count`
- `GET /api/[controller]/{id}/photos`
- `POST /api/[controller]/{id}/photos`
- `PUT /api/[controller]/{id}/photos/{photoId}/ordre`
- `DELETE /api/[controller]/{id}/photos/{photoId}`

### PlanificationVoyageController
- Base route: `api/[controller]`
- `GET /api/[controller]?idSociete=` — liste des templates de planification
- `GET /api/[controller]/{id}` — détail template
- `POST /api/[controller]` — créer un template (jours semaine, étapes, tarifs optionnels)
- `PUT /api/[controller]/{id}` — modifier template (n'affecte pas les voyages déjà générés)
- `PUT /api/[controller]/{id}/toggle-statut` — activer/désactiver
- `DELETE /api/[controller]/{id}` — supprimer ou désactiver si voyages liés
- `POST /api/[controller]/{id}/generer` — génération batch (`mode`: SemaineCourante, MoisCourant, MoisProchain, PeriodePersonnalisee)

Permissions : `Voyage.Read`, `Voyage.Create`, `Voyage.Update`, `Voyage.Delete`. Scope société JWT (Super-Admin exempt).

### VoyageController
- Base route: `api/[controller]`
- `GET /api/[controller]/paged` — query `date?`, `periode?` (`Jour` défaut, `Hebdomadaire`, `Mensuel`, `Tout` = sans filtre DateDepart)
- `GET /api/[controller]/search` — query `villeDepart?`, `villeArrivee?`, `idSociete?`, `date?`, `periode?`, `searchTerm?`, pagination/tri (`pageNumber`, `pageSize`, `sortBy`, `sortDescending`)
- `GET /api/[controller]`
- `GET /api/[controller]/{id:int}/tarifs-categorie-siege` — permission `Voyage.Read`
- `PATCH /api/[controller]/{id:int}/tarifs-categorie-siege/{idCategorieSiege}` — permission `Voyage.Update` (voir `Documentation/Themes/05_transport_sync/DOCUMENTATION_TARIFICATION_VOYAGE.md`)
- `PUT /api/[controller]/{id:int}/tarifs-categorie-siege` — permission `Voyage.Update`
- `GET /api/[controller]/passagers-embarques` (query: `idDestination`, `idVehicule`, `dateDepart` obligatoires ; `heureDepart` optionnel — voir `Documentation/Themes/05_transport_sync/DOCUMENTATION_EMBARQUEMENT_BILLET_ET_PASSAGERS_VOYAGE.md`)
- `GET /api/[controller]/{id}`
- `GET /api/[controller]/{id:int}/destinations`
- `GET /api/[controller]/{id:int}/sieges-disponibles`
- `GET /api/[controller]/{id:int}/sieges-indisponibles`
- `POST /api/[controller]/paged`
- `GET /api/[controller]/societe/{idSociete}`
- `GET /api/[controller]/societe/{idSociete}/paged` — query `date?`, `periode?` (`Jour` défaut, `Hebdomadaire`, `Mensuel`, `Tout` = sans filtre DateDepart)
- `GET /api/[controller]/site/{idSite}`
- `GET /api/[controller]/site/{idSite}/paged` — query `date?`, `periode?` (`Jour` défaut, `Hebdomadaire`, `Mensuel`, `Tout` = sans filtre DateDepart)
- `GET /api/[controller]/vehicule/{idVehicule}`
- `GET /api/[controller]/vehicule/{idVehicule}/paged` — query `date?`, `periode?` (`Jour` défaut, `Hebdomadaire`, `Mensuel`, `Tout` = sans filtre DateDepart)
- `GET /api/[controller]/destination/{idDestination}`
- `GET /api/[controller]/destination/{idDestination}/paged` — query `date?`, `periode?` (`Jour` défaut, `Hebdomadaire`, `Mensuel`, `Tout` = sans filtre DateDepart)
- `GET /api/[controller]/date/{date}`
- `GET /api/[controller]/vehicule/{idVehicule}/destination/{idDestination}`
- `GET /api/[controller]/daterange`
- `GET /api/[controller]/statut/{statut}`
- `GET /api/[controller]/pricerange`
- `POST /api/[controller]`
- `PUT /api/[controller]/{id}`
- `DELETE /api/[controller]/{id}`
- `POST /api/[controller]/societe/{idSociete}/paged`
- `POST /api/[controller]/site/{idSite}/paged`
- `POST /api/[controller]/vehicule/{idVehicule}/paged`
- `POST /api/[controller]/destination/{idDestination}/paged`
- `GET /api/[controller]/count`
- `GET /api/[controller]/vehicule/{idVehicule}/count`
- `GET /api/[controller]/destination/{idDestination}/count`
- `GET /api/[controller]/date/{date}/count`
- `GET /api/[controller]/statut/{statut}/count`

Exemples `GET /api/Voyage/search` :
- `GET /api/Voyage/search?villeDepart=Kinshasa&pageNumber=1&pageSize=20`
- `GET /api/Voyage/search?villeDepart=Kinshasa&villeArrivee=Matadi&idSociete=1&pageNumber=1&pageSize=20&periode=Tout`
- `GET /api/Voyage/search?searchTerm=BUS-ALPHA&date=2026-07-01&periode=Hebdomadaire&pageNumber=1&pageSize=20`

---

### EvenementClasseController
- Base route: `api/events/classes`
- `GET /api/events/classes`
- `GET /api/events/classes/societe/{idSociete}`
- `GET /api/events/classes/by-libelle?libelle=&idSociete=`
- `GET /api/events/classes/{id}`
- `POST /api/events/classes`
- `PUT /api/events/classes/{id}`
- `PUT /api/events/classes/{id}/toggle-statut`

### EvenementSessionController
- Base route: `api/events/sessions`
- `GET /api/events/sessions`
- `GET /api/events/sessions/societe/{idSociete}`
- `GET /api/events/sessions/status/{status}`
- `GET /api/events/sessions/inventory-mode/{inventoryMode}`
- `GET /api/events/sessions/code/{codeSession}`
- `GET /api/events/sessions/date/{date}`
- `GET /api/events/sessions/daterange`
- `GET /api/events/sessions/{id}`
- `POST /api/events/sessions`
- `PUT /api/events/sessions/{id}/publish`
- `GET /api/events/sessions/{id}/availability`
- `POST /api/events/sessions`

### EvenementReservationController
- Base route: `api/events/reservations`
- `GET /api/events/reservations`
- `GET /api/events/reservations/societe/{idSociete}`
- `GET /api/events/reservations/societe/{idSociete}/session/{idEvenementSession}`
- `GET /api/events/reservations/session/{idEvenementSession}`
- `GET /api/events/reservations/status/{status}`
- `GET /api/events/reservations/reference/{reference}`
- `GET /api/events/reservations/date/{date}`
- `GET /api/events/reservations/daterange`
- `GET /api/events/reservations/{id}/tickets`
- `GET /api/events/reservations/{id}`
- `POST /api/events/reservations/with-paiement`
- `POST /api/events/reservations/with-paiement-electronique`
- `POST /api/events/reservations/{id}/cancel`

### EvenementTicketController
- Base route: `api/events/tickets`
- `GET /api/events/tickets`
- `GET /api/events/tickets/societe/{idSociete}`
- `GET /api/events/tickets/societe/{idSociete}/reservation/{idEvenementReservation}`
- `GET /api/events/tickets/reservation/{idEvenementReservation}`
- `GET /api/events/tickets/session/{idEvenementSession}`
- `GET /api/events/tickets/status/{status}`
- `GET /api/events/tickets/code/{ticketCode}`
- `GET /api/events/tickets/date/{date}`
- `GET /api/events/tickets/daterange`
- `GET /api/events/tickets/{id}`
- `GET /api/events/tickets/{ticketCode}/check`
- `POST /api/events/tickets/{ticketCode}/use`

### EvenementFlexPayController
- Base route: `api/events/flexpay`
- `POST /api/events/flexpay/callback`
- `GET /api/events/flexpay/verifier/{orderNumber}`
- `GET /api/events/flexpay/approve`
- `GET /api/events/flexpay/cancel`
- `GET /api/events/flexpay/decline`

### EvenementDashboardController
- Base route: `api/events/dashboard`
- `GET /api/events/dashboard`
- `GET /api/events/dashboard/super-admin`

### InfoPaiementSocieteController
- Base route: `api/InfoPaiementSociete`
- `GET /api/InfoPaiementSociete/site/{idSite}`
- `POST /api/InfoPaiementSociete`
- `PUT /api/InfoPaiementSociete/{id}`
- `DELETE /api/InfoPaiementSociete/{id}`

### ReversementSiteController
- Base route: `api/ReversementSite`
- `POST /api/ReversementSite`
- `GET /api/ReversementSite/{id}`
- `GET /api/ReversementSite/site/{idSite}`
- `GET /api/ReversementSite/verifier/{orderNumber}`

### SuperAdminDashboardController
- Base route: `api/SuperAdminDashboard`
- `GET /api/SuperAdminDashboard`

