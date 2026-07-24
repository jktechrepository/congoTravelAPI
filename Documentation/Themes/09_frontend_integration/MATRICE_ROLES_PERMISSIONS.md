# Matrice Rôles & Permissions — Congo Travel API

**Date :** 7 juillet 2026  
**Référence :** livrable #4 de `DEMANDE_DOC_API_BACKEND.md`  
**Source :** `Data/PermissionSeeder.cs`, `Attributes/PermissionAttribute.cs`, contrôleurs

---

## 1. Modèle d'autorisation

### Principes

| Mécanisme | Description |
|-----------|-------------|
| **JWT Bearer** | Tous les endpoints protégés exigent `Authorization: Bearer {token}` |
| **RBAC** | Permissions nommées `{Module}.{Action}` assignées aux rôles en base |
| **Multi-rôles** | Un utilisateur peut avoir plusieurs rôles ; permissions = union des rôles |
| **Overrides utilisateur** | Permissions personnalisées par utilisateur : **DENIED** > **GRANTED** > rôle |
| **Tenant** | Scope société via claim JWT `SocieteId` (pas `X-Societe-Id`) |
| **Super-Admin** | Reçoit **toutes** les permissions au seed ; bypass implicite RBAC |

### Claims JWT utiles côté front

| Claim | Usage |
|-------|-------|
| `NameIdentifier` / `UserId` | ID utilisateur |
| `Role` | Rôle principal (string) |
| `SocieteId` | ID société courante |
| `SiteId` | ID site (optionnel, gérant/caissier) |
| `IsSuperAdmin` | Présent si Super-Admin |

### Vérifier les permissions du user connecté

```http
GET /api/Permission/my-permissions
Authorization: Bearer {token}
```

Réponse : tableau de strings `["Client.Read", "Reservation.Create", ...]`.

---

## 2. Rôles système

### Rôles seedés (actifs — transport CongoTravel)

| Rôle | Code JWT | Niveau | Description |
|------|----------|--------|-------------|
| **Super-Admin** | `Super-Admin` | 1 | Accès global multi-sociétés, toutes permissions |
| **Admin** | `Admin` | 3 | Administration complète d'une société |
| **Gérant** | `Gerant` | 2 | Direction opérationnelle société (+ site JWT) |
| **Financier** | `Financier` | 3 | Reporting, paiements, remboursements |
| **Caissier** | `Caissier` | 4 | Guichet, encaissements, réservations terrain |
| **Client** | `Client` | 5 | Voyageur — réservations et profil personnels |

> **Note « Agent » :** dans l'API, `Agent` désigne une **entité métier** (`/api/Agent`), pas un rôle JWT seedé. Les agents terrain sont des utilisateurs liés (souvent rôle **Caissier**) avec un numéro de série appareil.

### Visibilité lecture agents (`GET /api/Agent*`)

Filtrage serveur (`RoleVisibilityHelper`, même matrice que les rôles assignables via `RoleService`) :

| Appelant (JWT `primaryRole`) | Agents visibles (rôle) | Société |
|------------------------------|------------------------|---------|
| Super-Admin | Tous | Toutes |
| Admin | Tous sauf `Super-Admin` | JWT uniquement |
| Gerant | Tous sauf `Super-Admin`, `Admin` | JWT uniquement |
| Autres (Caissier, Financier, …) | Tous sauf `Super-Admin`, `Admin`, `Gerant` | JWT uniquement |

`GET /api/Agent/{id}` (et équivalents détail) renvoie **404** si l'agent est hors périmètre (rôle caché ou autre société) — pas de confirmation d'existence.

### Rôles legacy (enum `UserRoles`, non seedés par défaut)

`Sous-Directeur`, `Secrétaire`, `Préfet`, `Technicien`, `Bailleur`, `Agent Support`, `Autre Personnel` — présents dans l'enum mais **sans assignation permissions** au seed. À configurer manuellement si utilisés.

---

## 3. Matrice synthétique — Module × Rôle

Légende : **✅** CRUD complet · **📖** Lecture seule · **➕** Création + lecture · **🔐** JWT seul (pas de `[Permission]` sur le contrôleur) · **👑** Super-Admin uniquement · **❌** Non assigné au seed

| Module | Super-Admin | Admin | Gérant | Financier | Caissier | Client |
|--------|:-----------:|:-----:|:------:|:---------:|:--------:|:------:|
| Société | ✅ | 📖✏️ | 📖✏️ | ❌ | ❌ | ❌ |
| Config société | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Site | ✅ | ✅ | ✅ | 📖 | 📖 | ❌ |
| Utilisateur | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Agent (entité) | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Client | ✅ | ✅ | ✅ | 📖 | 📖 | 📖 |
| Voyage | ✅ | ✅ | ✅ | 📖 | 🔐 | 📖 |
| Planification voyage | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Réservation | ✅ | ✅ | ✅ | 📖 | 🔐 | ➕📖 |
| Paiement | ✅ | ✅ | ✅ | ✅ | 🔐 | 📖 |
| Billet | ✅ | ✅ | ✅ | 📖 | 🔐 | 📖 |
| Véhicule / Destination | ✅ | ✅ | ✅ | 📖 | ❌ | 📖 |
| Type véhicule / Cat. siège | ✅ | ✅ | ✅ | 📖 | ❌ | ❌ |
| Devise / Taux change | ✅ | ✅ | ✅ | 📖 | 📖 | ❌ |
| Remboursement | ✅ | ✅ | ✅ | ✅ | ➕📖 | ❌ |
| Reversement site | ✅ | ✅ | ✅ | ➕📖 | ❌ | ❌ |
| Plainte client | ✅ | ✅ | ✅ | ❌ | ❌ | ➕📖 |
| Communication | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Audit | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Statistiques | ✅ | ✅ | ✅ | 📖 | ❌ | ❌ |
| Dashboard admin | ✅ | ✅ | ✅ | 📖 | ❌ | ❌ |
| Dashboard gérant | 👑+Gérant | ❌ | 🔐 rôle | ❌ | ❌ | ❌ |
| Dashboard caissier | 👑+Caissier | ❌ | ❌ | ❌ | 🔐 rôle | ❌ |
| Dashboard financier | 👑+Fin. | ❌ | 🔐* | 🔐 rôle | ❌ | ❌ |
| Dashboard client | 👑+Client | ❌ | ❌ | ❌ | ❌ | 🔐 rôle |
| SuperAdmin dashboard | 👑 | ❌ | ❌ | ❌ | ❌ | ❌ |
| Finance reporting | ✅ | ✅ | ✅ | 📖 | ❌ | ❌ |
| FlexPay vérifier | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Sync offline | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Événement (billetterie) | ✅ | ✅ | ✅ | 📖➕ | ➕📖 | ➕📖 |
| Info paiement société | 👑 | ❌ | ❌ | ❌ | ❌ | ❌ |
| Voyage public (`/paged`) | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 |
| Auth login | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 | 🔓 |

\* Le dashboard financier accepte aussi le rôle **Gérant** via `HasFinanceAccess`.

---

## 4. Détail des permissions par rôle (seed)

### Super-Admin

**Toutes les permissions** du catalogue (~150+). Aucune restriction RBAC.

Contrôles additionnels hardcodés :
- `SuperAdminDashboard` → `IsSuperAdmin` obligatoire
- `InfoPaiementSociete` → `IsSuperAdmin` obligatoire

---

### Admin

**Société :** `Societe.Read`, `Societe.ReadAll`, `Societe.Update` (pas Create/Delete)

**CRUD complet** sur :
`Utilisateur`, `Agent`, `Client`, `PlainteClient`, `CommunicationCampaign`, `Voyage`, `TypeVehicule`, `Reservation`, `Paiement`, `Destination`, `Vehicule`, `Billet`, `Site`, `ConfigSociete`, `Devise`, `TauxChange`, `Remboursement`, `ReversementSite`, `CategorieSiege`, `NotificationPreference`

**Lecture reporting :** `Metrics.ReadAll`, `Audit.ReadAll`, `Audit.DetectSuspicious`, `Dashboard.ReadAll`, `FinanceReporting.ReadAll`, `Statistiques.ReadAll`

**Sync :** `Sync.Execute`

**Événement :** toutes permissions `Evenement.*`

---

### Gérant

Même périmètre qu'**Admin** (seed identique par catégorie).

Restrictions métier additionnelles (code) :
- Ne peut pas modifier un agent avec rôle `Admin`
- Ne peut pas attribuer le rôle `Super-Admin` ni `Admin` (sauf Super-Admin/Admin)
- **Liste / détail agents** : voir [Visibilité lecture agents](#visibilité-lecture-agents-get-apiagent)
- Dashboard gérant : scope **site JWT** (pas vue société globale)

---

### Financier

| Catégorie | Permissions |
|-----------|-------------|
| Client, Voyage, TypeVehicule, Reservation, Destination, Vehicule, Billet, Site, Devise, TauxChange | `Read`, `ReadAll` uniquement |
| Paiement | CRUD complet |
| Remboursement | CRUD complet |
| ReversementSite | `Create`, `Read` (pas `ReadAll`) |
| Reporting | `Dashboard.ReadAll`, `FinanceReporting.ReadAll`, `Statistiques.ReadAll` |
| Événement | `Session.Read`, `Reservation.Confirm`, `Ticket.Check`, `Dashboard.Read` |

Dashboard : `HasFinanceAccess` → Super-Admin, Gérant, Financier.

---

### Caissier

| Catégorie | Permissions seedées |
|-----------|---------------------|
| Client | `Read`, `ReadAll` |
| Site | `Read`, `ReadAll` |
| Devise, TauxChange | `Read`, `ReadAll` |
| Remboursement | `Create`, `Read`, `ReadAll` |
| Événement | `Session.Read`, `Hold.Create`, `Reservation.Confirm`, `Ticket.Check`, `Ticket.Use` |

> **⚠️ Écart important :** le seed **n'assigne pas** `Reservation.*`, `Paiement.*`, `Billet.*`, `Voyage.*` au Caissier, mais les contrôleurs `Reservation`, `Paiement`, `Billet` n'utilisent que `[Authorize]` + filtre tenant JWT. **L'accès effectif repose sur le JWT valide**, pas sur une permission RBAC explicite. Le front peut continuer à cibler le rôle Caissier ; une évolution backend pourrait ajouter des `[Permission]` sur ces endpoints.

Dashboard caissier : contrôle **rôle hardcodé** `Caissier` (+ Super-Admin).

---

### Client

| Catégorie | Permissions |
|-----------|-------------|
| Client | `Read`, `ReadAll` (son profil) |
| PlainteClient | `Create`, `Read`, `ReadAll` |
| ClientDashboard | `ReadAll` |
| Reservation | `Create`, `Read`, `ReadAll` |
| Paiement, Billet | `Read`, `ReadAll` |
| Voyage, Destination | `Read`, `ReadAll` |
| Événement | `Session.Read`, `Hold.Create` |
| Tous | `Utilisateur.DeactivateSelf` |

---

## 5. Catalogue des permissions (par catégorie)

### Transport — cœur métier

| Permission | Description |
|------------|-------------|
| `Voyage.Create` / `.Read` / `.ReadAll` / `.Update` / `.Delete` | Voyages |
| `Reservation.Create` / `.Read` / `.ReadAll` / `.Update` / `.Delete` | Réservations |
| `ReservationPassenger.*` | Passagers réservation |
| `Paiement.*` | Paiements |
| `Billet.*` | Billets |
| `BilletEmbarquement.*` | Embarquement |
| `Vehicule.*` | Véhicules |
| `Destination.*` | Destinations |
| `TypeVehicule.*` | Types véhicule |
| `CategorieSiege.*` | Catégories siège |
| `VoyageDestination.*` | Étapes voyage |
| `VoyageSeatAllocation.*` | Allocation sièges |
| `VoyageTarifCategorieSiege.*` | Tarifs par catégorie |
| `Siege.*` | Sièges |

### Organisation

| Permission | Description |
|------------|-------------|
| `Societe.*` | Sociétés |
| `ConfigSociete.Read` / `.Update` | Configuration métier |
| `Site.*` | Sites |
| `Agent.*` | Agents terrain |
| `Client.*` | Clients voyageurs |

### Finance

| Permission | Description |
|------------|-------------|
| `Devise.*` | Devises |
| `TauxChange.Create` / `.Read` / `.ReadAll` | Taux de change |
| `Remboursement.*` | Remboursements |
| `ReversementSite.Create` / `.Read` / `.ReadAll` | Reversements FlexPay PayOut |
| `FinanceReporting.ReadAll` | Rapports finance |
| `Dashboard.ReadAll` | Dashboard admin société |
| `Statistiques.ReadAll` | Statistiques consolidées |
| `Metrics.ReadAll` | Métriques système |

### Utilisateurs & sécurité

| Permission | Description |
|------------|-------------|
| `Utilisateur.*` | Utilisateurs |
| `Utilisateur.ChangePassword` | Réinitialisation mot de passe (admin) |
| `Utilisateur.DeactivateSelf` | Auto-désactivation (tous rôles) |
| `Role.*` | Rôles |
| `Permission.*` | Permissions RBAC |
| `Permission.Assign` / `.Revoke` | Attribution permissions |
| `Audit.ReadAll` | Journal audit |
| `Audit.DetectSuspicious` | Activités suspectes |

### Communication & plaintes

| Permission | Description |
|------------|-------------|
| `PlainteClient.*` | Plaintes |
| `CommunicationCampaign.*` | Campagnes |
| `NotificationPreference.*` | Préférences notifications |
| `Notification.*` | Notifications |

### Sync & événement

| Permission | Description |
|------------|-------------|
| `Sync.Execute` | Sync offline agent |
| `Evenement.Session.Read` / `.Write` | Sessions événement |
| `Evenement.Hold.Create` | Hold billetterie |
| `Evenement.Reservation.Confirm` | Confirmation / annulation |
| `Evenement.Ticket.Check` / `.Use` | Contrôle entrée |
| `Evenement.Dashboard.Read` | Dashboard événement |
| `ClientDashboard.ReadAll` | Dashboard client |

---

## 6. Matrice endpoints critiques front × accès

| Endpoint | Auth | Contrôle d'accès | Rôles typiques |
|----------|------|------------------|----------------|
| `POST /api/Utilisateur/authentifier` | Public | — | Tous |
| `POST /api/Utilisateur/refresh-token` | JWT | — | Tous authentifiés |
| `GET /api/Voyage/paged` | Public | `[AllowAnonymous]` | Tous |
| `POST /api/Voyage` | JWT | `Voyage.Create` (implicite via service) | Admin, Gérant |
| `POST /api/Reservation` | JWT | Tenant JWT | Caissier, Client, Admin |
| `POST /api/Reservation/reservation_with_paiement` | JWT | Tenant JWT | Caissier |
| `POST /api/Reservation/reservation_with_paiement_electronique` | JWT | Tenant JWT | Caissier, Client |
| `GET /api/FlexPay/verifier/{order}` | JWT | `[Authorize]` | Tous authentifiés |
| `GET /api/Paiement/societe/{id}/paged` | JWT | Tenant JWT | Admin, Financier |
| `GET /api/Societe/{id}/config` | JWT | `ConfigSociete.Read` + tenant | Admin, Gérant |
| `PUT /api/Societe/{id}/config` | JWT | `ConfigSociete.Update` + tenant | Admin, Gérant |
| `GET /api/Dashboard/{idSociete}` | JWT | `Dashboard.ReadAll` + tenant | Admin, Gérant, Financier |
| `GET /api/GerantDashboard` | JWT | Rôle `Gerant` hardcodé | Gérant |
| `GET /api/CaissierDashboard` | JWT | Rôle `Caissier` hardcodé | Caissier |
| `GET /api/CaissierDashboard/rapport-caisse` | JWT | Rôle `Caissier` hardcodé | Caissier |
| `GET /api/FinancierDashboard` | JWT | `HasFinanceAccess` | Financier, Gérant, Super-Admin |
| `GET /api/ClientDashboard` | JWT | Rôle `Client` hardcodé | Client |
| `GET /api/SuperAdminDashboard` | JWT | `IsSuperAdmin` hardcodé | Super-Admin |
| `GET /api/Statistiques/{id}` | JWT | `Statistiques.ReadAll` + tenant | Admin, Gérant, Financier |
| `GET /api/FinanceReporting/rapport-caisse` | JWT | `FinanceReporting.ReadAll` | Financier, Admin |
| `GET /api/Client/societe/{id}/paged` | JWT | `Client.ReadAll` + tenant | Admin, Caissier |
| `GET /api/Audit/suspicious` | JWT | `Audit.DetectSuspicious` | Super-Admin, Admin |
| `GET /api/InfoPaiementSociete/site/{id}` | JWT | `IsSuperAdmin` hardcodé | Super-Admin |
| `WS /hubs/notifications` | JWT | `[Authorize]` hub | Tous authentifiés |

---

## 7. Règles tenant (complément rôles)

| Situation | Règle |
|-----------|-------|
| Utilisateur standard | Ne voit que les données de `JWT.SocieteId` |
| Super-Admin | Peut passer `idSociete` en query sur certaines listes |
| Dashboard gérant | Filtré sur site JWT (repli société si site absent) |
| Dashboard caissier | Filtré sur `JWT.UserId` + société |
| `GET /api/Dashboard/{idSociete}` | `idSociete` route doit matcher JWT (sauf Super-Admin) |
| Header `X-Societe-Id` | **Ignoré** — ne pas utiliser pour l'autorisation |

---

## 8. Hiérarchie des rôles (création utilisateurs)

| Acteur | Peut créer / gérer |
|--------|-------------------|
| Super-Admin | Tous rôles, toutes sociétés |
| Admin | Utilisateurs société (sauf Super-Admin) ; agents |
| Gérant | Utilisateurs sauf Admin et Super-Admin ; agents sauf Admin |
| Caissier / Client | Aucune gestion utilisateurs |

---

## 9. Points d'attention pour le front

1. **Ne pas se baser uniquement sur le rôle JWT** pour les modules Reservation/Paiement/Billet — vérifier aussi `GET /api/Permission/my-permissions` si besoin granulaire.

2. **Dashboards** : accès par **rôle hardcodé**, pas par permission `*Dashboard.ReadAll` (sauf Admin `Dashboard.ReadAll` et Client `ClientDashboard.ReadAll`).

3. **Routes publiques** : `Voyage/paged`, `Voyage/search`, `Client/register`, `FlexPay/callback` — pas de token requis.

4. **Multi-rôles** : un utilisateur Admin + Financier cumule les permissions des deux rôles.

5. **Rôle « Agent »** : utiliser les endpoints `/api/Agent` avec un compte staff (Caissier/Admin), pas un rôle JWT dédié.

---

## 10. Fichiers associés

| Document | Chemin |
|----------|--------|
| Changelog & breaking changes | `CHANGELOG_API_BREAKING_CHANGES.md` |
| Matrice endpoints | `MATRICE_ENDPOINTS_FRONT_COMPLETE.md` |
| Collection Postman | `postman/CongoTravel_API.postman_collection.json` |
| Auth détaillée | `Documentation/Themes/02_securite_auth/DOCUMENTATION_AUTHENTIFICATION.md` |
| Seeder source | `Data/PermissionSeeder.cs` |

---

*Généré à partir du code source CongoTravel API — juillet 2026.*
