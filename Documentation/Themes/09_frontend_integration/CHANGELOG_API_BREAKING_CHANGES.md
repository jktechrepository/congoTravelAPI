# Changelog API & Breaking Changes — Congo Travel Web

**Date :** 7 juillet 2026  
**Émetteur :** Équipe Backend — Congo Travel API (CongoTravel)  
**Destinataire :** Équipe Front — Congo Travel Web  
**Référence :** réponse à `DEMANDE_DOC_API_BACKEND.md`

---

## Résumé exécutif

| Décision | Valeur |
|----------|--------|
| **Routes officielles** | **Legacy** (`/api/Utilisateur/*`, `GET/POST` racine des ressources) |
| **Routes guide** (`/api/Auth/*`, `*/get-all`, `*/create`) | **Non implémentées** — ne pas activer `VITE_API_USE_GUIDE_ROUTES=true` |
| **Swagger UI** | `{API_BASE}/swagger` |
| **OpenAPI JSON** | `{API_BASE}/swagger/v1/swagger.json` |
| **Hub SignalR** | `{API_BASE}/hubs/notifications` |
| **Multi-tenant** | Via **claim JWT `SocieteId`** — le header `X-Societe-Id` est autorisé en CORS mais **non consommé** par le backend |
| **Version API** | `v1` (endpoint Swagger) — domaine métier transport CongoTravel |

**Environnements :**

| Env | URL API |
|-----|---------|
| Dev | `https://dev-congotravel.asdc-rdc.org` |
| Prod | `https://prod-congotravel.asdc-rdc.org` |

**Action immédiate côté front :** mettre `VITE_API_USE_GUIDE_ROUTES=false` et adapter les dashboards/statistiques selon les mappings ci-dessous.

---

## 1. Breaking changes majeurs

### 1.1 Routes guide inexistantes (P0 — bloquant)

Si `VITE_API_USE_GUIDE_ROUTES=true`, les appels suivants retournent **404** :

| Ancien contrat (guide — front) | Contrat actuel (officiel — backend) | Action front |
|--------------------------------|-------------------------------------|--------------|
| `POST /api/Auth/login` | `POST /api/Utilisateur/authentifier` | Utiliser legacy |
| `POST /api/Auth/logout` | `POST /api/Utilisateur/deconnecter` | Utiliser legacy |
| `POST /api/Auth/refresh-token` | `POST /api/Utilisateur/refresh-token` | Utiliser legacy |
| `POST /api/Utilisateur/create` | `POST /api/Utilisateur` | Utiliser legacy |
| `POST /api/Client/create` | `POST /api/Client` | Utiliser legacy |
| `GET /api/Voyage/get-all` | `GET /api/Voyage/paged` (recommandé) ou `GET /api/Voyage` (legacy, non paginé) | Migrer vers `paged` |
| `POST /api/Voyage/create` | `POST /api/Voyage` | Utiliser legacy |
| `POST /api/Reservation/create` | `POST /api/Reservation` | Utiliser legacy |
| `POST /api/Paiement/create` | `POST /api/Paiement` | Utiliser legacy |
| `GET /api/Billet/get-all` | `GET /api/Billet` | Utiliser legacy |
| `POST /api/Billet/create` | `POST /api/Billet` | Utiliser legacy |
| `GET /api/Vehicule/get-all` | `GET /api/Vehicule` | Utiliser legacy |
| `POST /api/Vehicule/create` | `POST /api/Vehicule` | Utiliser legacy |
| `GET /api/Destination/get-all` | `GET /api/Destination` | Utiliser legacy |
| `POST /api/Destination/create` | `POST /api/Destination` | Utiliser legacy |

**Dépréciation des routes legacy :** aucune date de suppression planifiée pour les routes legacy actuellement implémentées. Les routes guide ne seront ajoutées que si un besoin de compatibilité est confirmé.

---

### 1.2 Modules supprimés (P0 — 404)

Ces endpoints étaient présents dans l'ancienne API Kenergie et **n'existent plus** :

| Endpoint supprimé | Statut | Remplacement |
|-------------------|--------|--------------|
| `GET/POST /api/Facture` | **Supprimé** | Flux transport : `Reservation` + `Paiement` + `Billet` |
| `GET /api/CategorieClient` | **Supprimé** | Aucun équivalent — retirer du front |
| `GET /api/AdminDashboard/societe/{idSociete}` | **Supprimé** | `GET /api/Dashboard/{idSociete}` |
| `GET /api/Statistiques/generales/{id}` | **Supprimé** | `GET /api/Statistiques/{idSociete}` → propriété `.generales` |
| `GET /api/Statistiques/financieres/{id}` | **Supprimé** | `GET /api/Statistiques/{idSociete}` → propriété `.financieres` |
| `GET /api/Statistiques/operationnelles/{id}` | **Supprimé** | `GET /api/Statistiques/{idSociete}` → propriété `.operationnelles` |
| `GET /api/Statistiques/performance/{id}` | **Supprimé** | `GET /api/Statistiques/{idSociete}` → propriété `.performance` |
| `GET /api/Statistiques/consolidees/{id}` | **Supprimé** | `GET /api/Statistiques/{idSociete}` (réponse consolidée unique) |

---

### 1.3 Dashboards consolidés (P1 — cluster de 404)

Les dashboards ont été **refactorés** : un seul `GET` racine retourne un DTO unifié. Les sous-routes granulaires n'existent plus.

#### Gérant

| Ancienne route (front) | Nouvelle route | Champ DTO |
|------------------------|----------------|-----------|
| `GET /api/GerantDashboard` | `GET /api/GerantDashboard` | Racine — inchangé |
| `GET /api/GerantDashboard/societe/{id}` | — | Scope via JWT (`SocieteId`, `SiteId`) — pas de paramètre URL |
| `GET /api/GerantDashboard/statistiques` | `GET /api/GerantDashboard` | `.societeStatistiques` |
| `GET /api/GerantDashboard/alertes` | `GET /api/GerantDashboard` | `.alertesSociete` |
| `GET /api/GerantDashboard/societe-stats` | `GET /api/GerantDashboard` | `.societeStatistiques` |
| `GET /api/GerantDashboard/clients-stats` | `GET /api/GerantDashboard` | `.clientsStatistiques` |
| `GET /api/GerantDashboard/top-ca` | `GET /api/GerantDashboard` | `.top5ClientsCA` |
| `GET /api/GerantDashboard/top-arrieres` | `GET /api/GerantDashboard` | `.top5ClientsNonPayes` |
| `GET /api/GerantDashboard/alertes-societe` | `GET /api/GerantDashboard` | `.alertesSociete` |
| `GET /api/GerantDashboard/tendances` | `GET /api/GerantDashboard` | `.tendances` |
| `GET /api/GerantDashboard/paiements-stats` | `GET /api/GerantDashboard` | `.paiementsStatistiques` |
| `GET /api/GerantDashboard/statistiques-legacy` | — | **Supprimé** |
| `GET /api/GerantDashboard/alertes-legacy` | — | **Supprimé** |

**Rôles :** Gérant, Super-Admin. Le scope est déterminé par le JWT (société + site).

#### Caissier

| Ancienne route (front) | Nouvelle route | Champ DTO |
|------------------------|----------------|-----------|
| `GET /api/CaissierDashboard` | `GET /api/CaissierDashboard` | Racine — inchangé |
| `GET /api/CaissierDashboard/statistiques-journalieres` | `GET /api/CaissierDashboard` | `.statistiquesJournalieres` |
| `GET /api/CaissierDashboard/paiements-en-cours` | `GET /api/CaissierDashboard` | `.paiementsEnCours` |
| `GET /api/CaissierDashboard/paiements-recents` | `GET /api/CaissierDashboard` | `.paiementsRecents` |
| `GET /api/CaissierDashboard/recettes-journalieres` | `GET /api/CaissierDashboard` | `.recettesJournalieres` |
| `GET /api/CaissierDashboard/alertes-caissier` | `GET /api/CaissierDashboard` | `.alertesCaissier` |
| `GET /api/CaissierDashboard/resume-caisse` | `GET /api/CaissierDashboard` | `.resumeCaisse` |
| `GET /api/CaissierDashboard/rapport-caisse` | `GET /api/CaissierDashboard/rapport-caisse` | **Conservé** — route dédiée |

**Rôles :** Caissier, Super-Admin.

#### Financier

| Ancienne route (front) | Nouvelle route | Champ DTO |
|------------------------|----------------|-----------|
| `GET /api/FinancierDashboard` | `GET /api/FinancierDashboard` | Racine |
| `GET /api/FinancierDashboard/statistiques-globales` | `GET /api/FinancierDashboard` | `.globalStatistiques` |
| `GET /api/FinancierDashboard/societes-financieres` | `GET /api/FinancierDashboard` | `.societesFinancieres` |
| `GET /api/FinancierDashboard/transactions-recentes` | `GET /api/FinancierDashboard` | `.transactionsRecentes` |
| `GET /api/FinancierDashboard/alertes-financieres` | `GET /api/FinancierDashboard` | `.alertesFinancieres` |
| `GET /api/FinancierDashboard/tendances-financieres` | `GET /api/FinancierDashboard` | `.tendances` |

**Rôles :** Financier, Super-Admin.

#### Client

| Ancienne route (front) | Nouvelle route | Champ DTO |
|------------------------|----------------|-----------|
| `GET /api/ClientDashboard` | `GET /api/ClientDashboard` | Racine |
| `GET /api/ClientDashboard/statistiques` | `GET /api/ClientDashboard` | `.statistiques` |
| `GET /api/ClientDashboard/reservations-recentes` | `GET /api/ClientDashboard` | `.reservationsRecentes` |
| `GET /api/ClientDashboard/paiements-recents` | `GET /api/ClientDashboard` | `.paiementsRecents` |
| `GET /api/ClientDashboard/voyages-client` | `GET /api/ClientDashboard` | `.voyagesClient` |
| `GET /api/ClientDashboard/alertes-client` | `GET /api/ClientDashboard` | `.alertesClient` |
| `GET /api/ClientDashboard/resume-client` | `GET /api/ClientDashboard` | `.resumeClient` |

**Rôles :** Client.

#### Admin société

| Ancienne route (front) | Nouvelle route |
|------------------------|----------------|
| `GET /api/AdminDashboard/societe/{idSociete}` | `GET /api/Dashboard/{idSociete}` |

**Permission :** `Dashboard.ReadAll`. La société demandée doit correspondre au JWT (sauf Super-Admin).

---

### 1.4 Écarts méthode / chemin (P1 — 404 ou 405)

| Endpoint front | Backend actuel | Breaking ? | Correction |
|----------------|----------------|------------|------------|
| `POST /api/Utilisateur/change-password` | `POST /api/Utilisateur/changer_mot_de_passe` | Oui | Changer le chemin |
| `GET /api/Utilisateur/role/nom/{nomRole}` | `GET /api/Role/nomRole/{nomRole}` | Oui | Utiliser `RoleController` |
| `PUT /api/Billet/societe/{id}/billet/{id}/reaffecter` | `POST /api/Billet/societe/{id}/billet/{id}/reaffecter` | Oui | Méthode **POST** |
| `POST /api/Devise/preview-conversion` | `GET /api/Devise/preview-conversion` | Oui | Méthode **GET** + query params |
| `PUT /api/CategorieSiege/toggle-statut/{id}` | `PUT /api/CategorieSiege/{id}/toggle-statut` | Oui | Inverser segments |
| `GET /api/InfoPaiementSociete` (liste) | — | Oui | Pas de GET racine ; utiliser `GET /api/InfoPaiementSociete/site/{idSite}` |
| `GET /api/CategorieSiege` (liste globale) | — | Oui | Utiliser `GET /api/CategorieSiege/societe/{idSociete}` |

---

## 2. Changelog par statut

### Nouveau (absent de l'ancienne doc front)

| Module | Endpoint | Notes |
|--------|----------|-------|
| Dashboard | `GET /api/Dashboard/{idSociete}` | Remplace AdminDashboard |
| Voyage | `GET /api/Voyage/search` | Recherche par ville départ/arrivée |
| Voyage | `GET /api/Voyage/paged` | Route paginée officielle (publique) |
| Billet | `GET /api/Billet/{qrCode}/check` | Vérification embarquement |
| Billet | `POST /api/Billet/societe/{id}/passager/{id}/billet/{id}/embarquer` | Embarquement |
| Auth | `POST /api/Utilisateur/revoke-token` | Révocation refresh token |
| Auth | `POST /api/Utilisateur/revoke-all-tokens` | Révocation globale |
| Événements | `api/events/*` | Billetterie événement (hors scope front actuel) |
| Finance | `GET /api/FinanceReporting/paiements/summary` | Synthèse paiements |
| Finance | `GET /api/FinanceReporting/rapport-caisse` | Rapport caisse multi-utilisateur |

### Modifié (existe mais contrat changé)

| Module | Endpoint | Changement |
|--------|----------|------------|
| Statistiques | `GET /api/Statistiques/{idSociete}` | Réponse consolidée `StatistiquesTransportDto` (5 sections en un objet) |
| Dashboards | `GET /api/{Role}Dashboard` | DTO unifié — sous-routes supprimées |
| Voyage | `GET /api/Voyage` | Marqué `[Obsolete]` — préférer `/paged` |
| Voyage | `POST /api/Voyage/paged` | Marqué `[Obsolete]` — préférer `GET /api/Voyage/paged` |
| SignalR | Hub URL | `/hubs/notifications` (plus `/hubs/dashboard`) |
| Paiement SignalR | `NewPaiement` | Payload transport (plus de `idFacture` / arriérés Kenergie) |

### Déprécié (encore fonctionnel)

| Endpoint | Remplacement recommandé |
|----------|-------------------------|
| `GET /api/Voyage` (liste complète) | `GET /api/Voyage/paged` |
| `GET /api/Voyage/societe/{id}` | `GET /api/Voyage/societe/{id}/paged` |
| `POST /api/Voyage/paged` | `GET /api/Voyage/paged` |

### Supprimé

Voir section 1.2 et sous-routes dashboards section 1.3.

---

## 3. Tableau de réponse (endpoints critiques)

| Module | Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Headers requis | Notes |
|--------|----------|---------|--------|------------|---------|----------------|-------|
| Auth | `/api/Utilisateur/authentifier` | POST | **Actif (officiel)** | Non | Public | — | Body : `emailOuTelephone`, `motDePasse` |
| Auth | `/api/Auth/login` | POST | **Absent** | Oui | — | — | Route guide non implémentée |
| Auth | `/api/Utilisateur/refresh-token` | POST | **Actif (officiel)** | Non | Authentifié | `Authorization: Bearer` | Body : `refreshToken` |
| Auth | `/api/Utilisateur/deconnecter` | POST | **Actif** | Non | Authentifié | `Authorization: Bearer` | |
| Voyage | `/api/Voyage/paged` | GET | **Actif (recommandé)** | Non | Public (`[AllowAnonymous]`) | — | Query : `pageNumber`, `pageSize`, `searchTerm`, `sortBy`, `sortDescending`, `date`, `periode` |
| Réservation | `/api/Reservation/reservation_with_paiement_electronique` | POST | **Actif** | Non | Caissier, Client | `Authorization: Bearer` | Déclenche FlexPay + SignalR |
| Paiement | `/api/FlexPay/verifier/{orderNumber}` | GET | **Actif** | Non | Authentifié | `Authorization: Bearer` | Polling statut paiement |
| Dashboard | `/api/CaissierDashboard/rapport-caisse` | GET | **Actif** | Non | Caissier, Super-Admin | `Authorization: Bearer` | Query : `datePrecise` ou `dateDebut`+`dateFin` ; scope JWT |
| Finance | `/api/FinanceReporting/rapport-caisse` | GET | **Actif** | Non | Financier, Admin | `Authorization: Bearer` | Query : `idSociete`, `idUtilisateur?`, dates ; permission `FinanceReporting.ReadAll` |
| Société | `/api/Societe/{id}/config` | GET/PUT | **Actif** | Non | Admin, Gérant | `Authorization: Bearer` | Tenant via JWT |
| Temps réel | `/hubs/notifications` | WS | **Actif** | Oui (URL) | Authentifié | `Authorization: Bearer` ou `?access_token=` | Hub officiel |

---

## 4. Auth — contrat actuel

### Login

```http
POST /api/Utilisateur/authentifier
Content-Type: application/json
```

```json
{
  "emailOuTelephone": "user@example.com",
  "motDePasse": "secret"
}
```

**Réponse 200 :**

```json
{
  "success": true,
  "accessToken": "eyJ...",
  "refreshToken": "...",
  "tokenType": "Bearer",
  "expiresIn": 7200,
  "utilisateur": {
    "idUtilisateur": 1,
    "nomComplet": "...",
    "email": "...",
    "idSociete": 1,
    "roles": ["Caissier"]
  }
}
```

### Refresh

```http
POST /api/Utilisateur/refresh-token
Authorization: Bearer {accessToken}
Content-Type: application/json
```

```json
{ "refreshToken": "..." }
```

### Logout

```http
POST /api/Utilisateur/deconnecter
Authorization: Bearer {accessToken}
```

### Changement mot de passe

```http
POST /api/Utilisateur/changer_mot_de_passe
Authorization: Bearer {accessToken}
```

> ⚠️ Le chemin `change-password` n'existe pas.

---

## 5. Voyage paginé — contrat query

```http
GET /api/Voyage/paged?pageNumber=1&pageSize=20&searchTerm=kin&sortBy=date&sortDescending=true&date=2026-07-07&periode=Jour
```

| Paramètre | Type | Défaut | Description |
|-----------|------|--------|-------------|
| `pageNumber` | int | 1 | Numéro de page |
| `pageSize` | int | 20 | Taille de page |
| `searchTerm` | string | — | Recherche textuelle |
| `sortBy` | string | — | Champ de tri |
| `sortDescending` | bool | false | Tri décroissant |
| `date` | DateTime? | — | Filtre date |
| `periode` | enum | `Jour` | `Jour` \| `Hebdomadaire` \| `Mensuel` \| `Tout` |

**Réponse :** `PagedResult<VoyageResponseDto>` avec `data`, `totalCount`, `pageNumber`, `pageSize`.

> Les noms de query params sont en **camelCase** côté ASP.NET (`pageNumber`, pas `PageNumber`). Les deux sont généralement acceptés selon la config JSON, mais **camelCase est recommandé**.

---

## 6. Réservation + paiement électronique (FlexPay)

```http
POST /api/Reservation/reservation_with_paiement_electronique
Authorization: Bearer {token}
Content-Type: application/json
```

Flux :
1. Création réservation + initiation paiement FlexPay
2. Retour `orderNumber` pour suivi
3. Polling : `GET /api/FlexPay/verifier/{orderNumber}`
4. Notification temps réel : événements SignalR `FlexPayPaymentConfirmed` / `FlexPayPaymentFailed`

---

## 7. SignalR — contrat actuel

| Élément | Valeur |
|---------|--------|
| **URL** | `{API_BASE}/hubs/notifications` |
| **Auth** | Header `Authorization: Bearer {token}` ou query `?access_token={token}` |
| **Groupes** | `user_{userId}`, `all_users` (auto à la connexion) |

### Événements serveur → client

| Événement | Déclencheur | Payload (extrait) |
|-----------|-------------|-------------------|
| `FlexPayPaymentConfirmed` | Callback FlexPay OK | `{ orderNumber, idReservation, idPaiement, status: "confirmed", timestampUtc }` |
| `FlexPayPaymentFailed` | Callback FlexPay échec | `{ orderNumber, status: "failed", timestampUtc }` |
| `ReceiveNotification` | Notification générale | Objet notification mappé |
| `NewPaiement` | Nouveau paiement | Données paiement transport |
| `SuperAdminDashboardUpdated` | Màj dashboard SA | Données dashboard |
| `NotificationMarkedAsRead` | Accusé lecture | `notificationId` |
| `ConnectionStatus` | Statut connexion | Objet statut |

> L'ancienne doc `SignalR-Integration.md` référence `/hubs/dashboard` et des payloads Kenergie (factures, arriérés) — **obsolète**.

---

## 8. CORS

### Configuration backend

- **Dev :** toutes origines autorisées
- **Prod :** liste `Cors:AllowedOrigins` dans la configuration serveur

### Origines à configurer côté déploiement

```
http://congotravel.kansaconsulting.com
https://congo-travel.com
```

Headers autorisés en prod : `Content-Type`, `Authorization`, `Accept`, `Origin`, `X-Requested-With`, `X-Societe-Id`, `Cache-Control`, `Pragma`, `Expires`.

### `X-Societe-Id`

| Situation | Comportement |
|-----------|--------------|
| Header envoyé | Autorisé par CORS, **ignoré** par le backend |
| Scope tenant | Déterminé par le **claim JWT `SocieteId`** |
| Super-Admin | Peut passer `idSociete` en query sur certains endpoints liste |

**Recommandation front :** ne pas compter sur `X-Societe-Id` pour le filtrage tenant.

---

## 9. Statistiques — migration

**Ancien modèle (5 appels) :**

```
GET /api/Statistiques/generales/{id}
GET /api/Statistiques/financieres/{id}
GET /api/Statistiques/operationnelles/{id}
GET /api/Statistiques/performance/{id}
GET /api/Statistiques/consolidees/{id}
```

**Nouveau modèle (1 appel) :**

```http
GET /api/Statistiques/{idSociete}?debut=2026-01-01&fin=2026-07-07
Authorization: Bearer {token}
```

**Réponse :**

```json
{
  "generales": { },
  "financieres": { },
  "operationnelles": { },
  "performance": { },
  "periode": { },
  "codeDevisePrincipale": "CDF",
  "dateGeneration": "2026-07-07T..."
}
```

---

## 10. Rapport caisse — deux endpoints distincts

| Endpoint | Scope | Rôle | Usage |
|----------|-------|------|-------|
| `GET /api/CaissierDashboard/rapport-caisse` | Caissier connecté (JWT) | Caissier | Vue personnelle caisse |
| `GET /api/FinanceReporting/rapport-caisse?idSociete={id}` | Société (+ utilisateur optionnel) | Financier, Admin | Reporting consolidé |

Query dates communes : `datePrecise` **ou** `dateDebut` + `dateFin`.

---

## 11. Checklist migration front

- [ ] `VITE_API_USE_GUIDE_ROUTES=false`
- [ ] Remplacer tous les appels `/api/Auth/*` par `/api/Utilisateur/*`
- [ ] Remplacer sous-routes dashboards par lecture des propriétés DTO unifié
- [ ] Remplacer `AdminDashboard` par `GET /api/Dashboard/{idSociete}`
- [ ] Supprimer intégrations `Facture` et `CategorieClient`
- [ ] Consolider appels `Statistiques/*` en un seul GET
- [ ] Corriger `change-password` → `changer_mot_de_passe`
- [ ] Corriger méthodes POST/GET (billet réaffectation, devise preview)
- [ ] Mettre à jour URL SignalR → `/hubs/notifications`
- [ ] Écouter `FlexPayPaymentConfirmed` / `FlexPayPaymentFailed`
- [ ] Vérifier CORS sur dev/prod avec les origines front

---

## 12. Ressources complémentaires

| Document | Chemin |
|----------|--------|
| **Matrice endpoints complète** | `Documentation/Themes/09_frontend_integration/MATRICE_ENDPOINTS_FRONT_COMPLETE.md` |
| **Matrice rôles & permissions** | `Documentation/Themes/09_frontend_integration/MATRICE_ROLES_PERMISSIONS.md` |
| **Collection Postman** | `postman/CongoTravel_API.postman_collection.json` |
| **Environnements Postman** | `postman/CongoTravel_API.postman_environment.json` (dev), `postman/CongoTravel_API_Prod.postman_environment.json` (prod) |
| Contrat front complet | `Documentation/Themes/09_frontend_integration/DOCUMENTATION_BACKEND_CONTRACT_FRONTENDS.md` |
| Inventaire endpoints | `Documentation/Themes/01_demarrage/DOCUMENTATION_API_ENDPOINTS_COMPLETE.md` |
| Dashboards | `Documentation/Themes/07_dashboards_reporting/DOCUMENTATION_DASHBOARDS.md` |
| Auth & JWT | `Documentation/Themes/02_securite_auth/DOCUMENTATION_AUTHENTIFICATION.md` |
| FlexPay | `Documentation/Themes/09_frontend_integration/MODULE_04_PAIEMENT_FLEXPAY.md` |
| Config société | `Documentation/Themes/04_clients_referentiels/DOCUMENTATION_API_CONFIGSOCIETE.md` |
| Swagger live | `{API_BASE}/swagger` |

---

## Contact & support

- **Swagger / OpenAPI :** disponible sur chaque environnement (`/swagger`, `/swagger/v1/swagger.json`)
- **Collection Postman :** à générer depuis Swagger (export en cours)
- **Point technique hebdomadaire :** à planifier avec l'équipe front

---

*Document généré à partir de l'analyse du code source CongoTravel API — juillet 2026.*
