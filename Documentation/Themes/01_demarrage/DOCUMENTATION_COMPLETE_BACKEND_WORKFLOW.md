# Documentation complete du backend (état actuel)

## Objectif
Cette documentation présente une analyse approfondie de l'API ASP.NET Core actuelle pour préparer des changements majeurs de workflow, sans modifier le comportement existant.

## Portee de l'analyse
- Models (entites metier + DTOs)
- Interfaces
- Services
- Controllers
- Middleware, auth/permissions
- Data/EF Core
- Hub temps reel
- Fichiers `.md` existants

## Vue d'ensemble architecture
Le projet est construit comme une API monolithique modulaire ASP.NET Core, avec un schema classique:
- `Controllers` pour l'exposition HTTP
- `Services` pour la logique metier et l'orchestration
- `Data/CongoTravelDbContext.cs` pour la persistence EF Core
- Attributs et middlewares pour la securite (JWT/RBAC) et l'observabilite

Point d'entree principal:
- `Program.cs` configure DI, JWT Bearer, Swagger, CORS, rate limiting, Serilog et pipeline middleware.

## Couches et composants principaux

### Data / EF Core
Fichiers cle:
- `Data/CongoTravelDbContext.cs`
- `Data/PermissionSeeder.cs`
- `Migrations/CongoTravelDbContextModelSnapshot.cs`

Observations:
- Modele relationnel riche et indexation explicite.
- Presence de seeders role/permission utiles au bootstrap.
- Multi-tenant appuye sur `IdSociete`.

Note dev (migrations):
- En environnement **Development**, l'application tente d'appliquer les migrations EF au démarrage via `DevelopmentDatabaseMigrationHelper`.
- Si la base existe déjà mais que `__EFMigrationsHistory` n'est pas alignée, EF peut tenter de rejouer l'initiale et échouer avec `Table 'auditlogs' already exists`.
- Script de resynchronisation (si nécessaire) : `Scripts/sync-ef-migration-history-before-flexpay.sql`.

### Models (entites domaine)
Domaines principaux identifies:
- Organisation et IAM:
  - `Models/Societe.cs`
  - `Models/Utilisateur.cs`
  - `Models/Agent.cs`
  - `Models/Role.cs`
  - `Models/Permission.cs`
  - `Models/UserRole.cs`
  - `Models/RolePermission.cs`
  - `Models/UserPermission.cs`
- Transport et billetterie:
  - `Models/Destination.cs`
  - `Models/TypeBus.cs`
  - `Models/Bus.cs`
  - `Models/Voyage.cs`
  - `Models/Reservation.cs`
  - `Models/Billet.cs`
  - `Models/Paiement.cs`
  - `Models/PhotoVehicule.cs` (photos véhicule, liées à `Vehicule`)
- Client et relation usager:
  - `Models/Client.cs`
  - `Models/PlainteClient.cs`
  - `Models/CommunicationCampaign.cs`
- Notification, audit, securite:
  - `Models/Notification.cs`
  - `Models/NotificationPreference.cs`
  - `Models/AuditLog.cs`
  - `Models/UserDevice.cs`
  - `Models/SmsLog.cs`
  - `Models/RefreshToken.cs`
  - `Models/PasswordResetToken.cs`

Relations metier structurantes:
- `Societe` est un pivot multi-tenant.
- Flux coeur transport: `Voyage` -> `Reservation` -> `Paiement` (+ emission de `Billet`).
- RBAC N-N via `Role` <-> `Permission`.

Enrichissement des réponses:
- `VehiculeResponseDto` renvoie `photos[]` (table `PhotoVehicules`, binaire `PhotoData`, exposé en `photoBase64`).
- `VoyageResponseDto` renvoie `photosVehicules[]` correspondant aux photos actives du véhicule affecté au voyage.

### DTOs
Repertoire:
- `Models/DTOs/`

Sous-domaines identifies:
- `Authentification`, `Client`, `Bus`, `TypeBus`, `Destination`, `Voyage`, `Reservation`, `Billet`, `Paiement`, `PlainteClient`, `Communication`, `Sync`, `Dashboard`, `Statistiques`, `Metrics`, `Pagination`.

Exemples:
- `Models/DTOs/Sync/SyncRequestDto.cs`
- `Models/DTOs/Reservation/CreateReservationWithPaiementDto.cs`

### Interfaces
Dossiers principaux:
- `Services/Repositories/`
- `Services/` (interfaces transverses)

Familles:
- Repositories metier:
  - `IClientRepository`, `IAgentRepository`, `ISocieteRepository`, `IVoyageRepository`, `IReservationRepository`, `IBilletRepository`, `IPaiementRepository`, `IBusRepository`, `ITypeBusRepository`, `IDestinationRepository`, `IPlainteClientRepository`.
- Auth/securite:
  - `ICurrentUserService`, `IPermissionService`, `ISimpleJwtService`, `IRefreshTokenService`, `IUserAuthorizationService`.
- Notifications:
  - `INotificationService`, `INotificationRepository`, `INotificationDispatcher`, `INotificationSender`, `INotificationJobQueue`, `ISignalRNotificationService`, `ISmsNotificationService`, `IEmailService`.
- Infrastructure:
  - `IFileStorageService`, `IAntivirusService`, `ICacheService`, `IClientFilterService`, `IQrCodeService`, `ICursorService`, `IWatermarkService`, `ISyncService`, `IStatistiquesService`.

## Services
Services metier principaux (`Services/`):
- `ClientService`, `AgentService`, `SocieteService`, `UtilisateurService`, `RoleService`, `PermissionService`
- `DestinationService`, `BusService`, `TypeBusService`, `VoyageService`, `ReservationService`, `BilletService`, `PaiementService`
- `PlainteClientService`, `CommunicationCampaignService`, `NotificationPreferenceService`, `NotificationService`

Services d'orchestration:
- `ReservationWithPaiementService`
- `BilletEmissionService`
- `CommunicationDispatchService`
- `PaiementNotificationService`
- `VehiculePhotoService` (gestion `PhotoVehicules` : ajout/remplacement/ordre)

Services dashboards/metrics:
- `DashboardService`, `GerantDashboardService`, `FinancierDashboardService`, `CaissierDashboardService`, `ClientDashboardService`, `MetricsService`

Services techniques:
- `SimpleJwtService`, `RefreshTokenService`, `CurrentUserService`, `AuditService`, `CacheService`
- `FileStorageService`, `S3FileStorageService`, `AntivirusService`
- `QrCodeService`, `WatermarkService`, `CursorService`, `ClientFilterService`

Services asynchrones notifications:
- `Services/Notifications/NotificationJobWorker.cs`
- `Services/Notifications/NotificationJobQueue.cs`
- `Services/Notifications/NotificationDispatcher.cs`
- `Services/Notifications/NotificationSender.cs`

## Controllers
Controllers identifies (`Controllers/`):
- `AgentController`
- `AuditController`
- `AuthTestController`
- `BilletController`
- `BusController`
- `CaissierDashboardController`
- `ClientController`
- `ClientDashboardController`
- `CommunicationCampaignController`
- `DashboardController`
- `DestinationController`
- `FinancierDashboardController`
- `GerantDashboardController`
- `InitController`
- `MetricsController`
- `NotificationPreferenceController`
- `PaiementController`
- `PermissionController`
- `PlainteClientController`
- `ReservationController`
- `RoleController`
- `SocieteController`
- `StatistiquesController`
- `SyncController`
- `TypeBusController`
- `UtilisateurController`
- `VoyageController`

Groupes fonctionnels:
- IAM/securite: `UtilisateurController`, `RoleController`, `PermissionController`, `AuthTestController`, `AuditController`
- Transport: `DestinationController`, `TypeBusController`, `BusController`, `VoyageController`, `ReservationController`, `BilletController`, `PaiementController`
- Relation client/communication: `ClientController`, `PlainteClientController`, `CommunicationCampaignController`, `NotificationPreferenceController`
- Pilotage/BI: `DashboardController`, `StatistiquesController`, `MetricsController`, dashboards role-specifiques
- Operations: `InitController`, `SyncController`

Points de contrat API (paiement):
- `GET /api/Paiement/reservation/{idReservation}` : paiements liés à `Paiement.IdReservation`.
- `GET /api/Paiement/client/{idClient}` : paiements liés aux réservations du client (`Reservation.IdClient`).

## Middleware et securite
Fichiers:
- `Middleware/AutoBearerMiddleware.cs`
- `Middleware/MetricsTrackingMiddleware.cs`
- `Middleware/JwtMiddleware.cs`
- `Middleware/SimpleAuthMiddleware.cs`
- `Attributes/JwtAuthorizeAttribute.cs`
- `Attributes/PermissionAttribute.cs`

Etat actuel:
- JWT natif ASP.NET Core actif via `UseAuthentication`/`UseAuthorization`.
- RBAC avec attribut custom `Permission`.
- Coexistence de plusieurs approches auth (middleware custom + pipeline natif), a clarifier avant refonte.

## Hub temps reel
Fichier:
- `Hubs/NotificationHub.cs`

Observation:
- La brique SignalR existe, mais le mapping hub n'est pas toujours active dans le pipeline selon la configuration courante.

## Analyse de la documentation `.md` existante
Constat global:
- Le projet contient beaucoup de documentation historique et d'analyse.
- Une partie de la documentation est robuste, mais plusieurs fichiers semblent obsoletes, dupliques, ou decalés par rapport au code actuel.

Themes bien couverts:
- Auth/JWT
- Sync offline
- Dashboards/statistiques
- Communication et plaintes
- Planification fonctionnelle et analyses de risques

Points de vigilance documentaires:
- `README.md` semble deconnecte du domaine actuel.
- `INDEX_DOCUMENTATION.md` et `INDEX_DOCUMENTATION_COMPLETE.md` referencent des blocs historiques non alignes avec l'etat runtime.
- Plusieurs docs sur facturation/arrears/sous-modules supprimes ou renames peuvent induire en erreur lors d'une refonte.

## Risques techniques avant changements majeurs
1. Injection de dependance potentiellement incomplete:
   - `ISyncService` et `IStatistiquesService` meritent verification stricte entre interfaces, implementations et enregistrement DI.
2. Ambiguite auth:
   - Duplication des mecanismes JWT (natif + custom middleware) pouvant complexifier le workflow securite.
3. Incoherence code/doc:
   - Endpoints et modules references dans des `.md` non toujours disponibles dans les controllers actifs.
4. Dette de nomenclature:
   - Traces de terminologie historique dans docs/commentaires, risque de confusion equipe.
5. Temps reel partiellement active:
   - Hub present mais integration runtime a confirmer selon environnement.

## Documentation cible recommandee pour la phase suivante
Pour preparer les changements de workflow en securite, je recommande de conserver ce fichier comme base puis d'ajouter:
- Une matrice **Controller -> Service -> Interface -> DTO -> Regles auth**
- Une cartographie **workflow actuels (as-is)** puis **workflow cibles (to-be)**
- Un registre de compatibilite: endpoints conserves/modifies/supprimes
- Un plan de migration par lot (ordre de refonte + impacts tests)

## Plan d'action propose (avant implementation des changements)
1. Valider les workflows metier cibles avec toi.
2. Transformer cette documentation en spec executable (checklist technique par module).
3. Appliquer les changements par increments (auth, sync, reservations/paiements, dashboards, docs).
4. Aligner la documentation finale avec le code reel a chaque increment.

---

Document genere pour servir de reference de refonte backend ASP.NET Core.
