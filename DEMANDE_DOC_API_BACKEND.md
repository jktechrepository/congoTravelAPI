# Demande de documentation API — Congo Travel Web

**Objet :** Documentation API complète Congo Travel Web — alignement front sur endpoints existants + nouvelles fonctionnalités

**Date :** 7 juillet 2026  
**Émetteur :** Équipe Front — Congo Travel Web  
**Destinataire :** Équipe Backend Congo Travel

---

Bonjour,

Nous mettons à jour le **front Congo Travel Web** et avons besoin d'une **documentation API complète, à jour et exploitable** pour :

1. corriger les intégrations existantes (routes, payloads, droits, CORS),
2. intégrer les **nouvelles fonctionnalités**,
3. identifier clairement les **changements breaking** depuis la dernière version documentée.

---

## Contexte technique

| Élément | Valeur |
|--------|--------|
| **Front** | Vue 3 / Vite |
| **URL front (staging)** | `http://congotravel.kansaconsulting.com` |
| **URL front (prod)** | `https://congo-travel.com` (si applicable) |
| **API Dev** | `https://dev-congotravel.asdc-rdc.org` |
| **API Prod** | `https://prod-congotravel.asdc-rdc.org` |
| **Appels** | Directs navigateur (CORS requis) |
| **Auth** | `Authorization: Bearer {token}` |
| **Multi-tenant** | Header optionnel `X-Societe-Id` |
| **Temps réel** | Hub SignalR `/hubs/notifications` |
| **Mode routes** | Legacy (défaut) ou Guide/OpenAPI (`VITE_API_USE_GUIDE_ROUTES`) |

Merci de confirmer quelle variante de routes est **officielle** sur dev/prod, et la date de dépréciation de l'autre.

---

## Inventaire des endpoints actuellement consommés par le front

> Source : `src/services/Endpoint.service.js` et services associés du repo Congo Travel Web.

### 1. Auth & session

| Endpoint | Usage front |
|----------|-------------|
| `POST /api/Utilisateur/authentifier` | Login (legacy) |
| `POST /api/Auth/login` | Login (guide) |
| `POST /api/Utilisateur/deconnecter` | Logout (legacy) |
| `POST /api/Auth/logout` | Logout (guide) |
| `POST /api/Auth/refresh-token` | Refresh JWT |

### 2. Utilisateur & rôles

| Endpoint | Usage front |
|----------|-------------|
| `GET/POST /api/Utilisateur` | Liste / création |
| `POST /api/Utilisateur/create` | Création (guide) |
| `GET/PUT /api/Utilisateur/{id}` | Détail / mise à jour |
| `PUT /api/Utilisateur/toggle-statut/{id}` | Activation/désactivation |
| `GET /api/Utilisateur/role/{roleId}` | Par rôle |
| `GET /api/Utilisateur/role/nom/{nomRole}` | Par nom de rôle |
| `GET/POST/DELETE /api/Utilisateur/{id}/roles/{roleId}` | Gestion rôles |
| `PUT /api/Utilisateur/{id}/roles/{roleId}/primary` | Rôle principal |
| `POST /api/Utilisateur/change-password` | Changement mot de passe |
| `GET /api/Role` | Catalogue des rôles |

### 3. Agent

| Endpoint | Usage front |
|----------|-------------|
| `GET/POST /api/Agent` | Liste / création |
| `GET/PUT /api/Agent/{id}` | Détail / mise à jour |
| `PUT /api/Agent/toggle-statut/{id}` | Statut |
| `GET /api/Agent/statut/{statut}` | Filtre statut |
| `GET /api/Agent/exists/{id}` | Vérification existence |
| `POST /api/Agent/batch` | Création batch |
| `POST /api/Agent/{idAgent}/add-role` | Ajout rôle |
| `PUT /api/Agent/{idAgent}/replace-role` | Remplacement rôle |
| `PUT /api/Agent/{idAgent}/AffecterAgentSite` | Affectation site |
| `PUT /api/Agent/{idAgent}/site` | Site agent |
| `GET /api/Agent/serial-number/{serialNumber}` | Par numéro série |
| `PUT /api/Agent/{idAgent}/serial-number` | Mise à jour série |
| `PUT /api/Agent/matricule/{matricule}/serial-number` | Série par matricule |

### 4. Société, site, config multi-tenant

| Endpoint | Usage front |
|----------|-------------|
| `GET/POST /api/Societe` | Liste / création (`{ societe, site }`) |
| `GET/PUT /api/Societe/{id}` | Détail / mise à jour |
| `GET/PUT /api/Societe/{id}/config` | Configuration métier société |
| `PUT /api/Societe/set-statut/{id}?Statut=` | Activation/désactivation |
| `GET/POST /api/Site` | Sites |
| `GET/PUT /api/Site/{id}` | Détail site |
| `GET /api/Site/societe/{idSociete}` | Sites par société |
| `PUT /api/Site/toggle-statut/{id}` | Statut site |
| `GET/POST /api/InfoPaiementSociete` | Infos paiement société |
| `GET/PUT/DELETE /api/InfoPaiementSociete/{id}` | CRUD |
| `GET /api/InfoPaiementSociete/site/{idSite}` | Par site |

### 5. Client

| Endpoint | Usage front |
|----------|-------------|
| `GET/POST /api/Client` | Liste / création |
| `POST /api/Client/create` | Création (guide) |
| `POST /api/Client/register` | Inscription publique |
| `GET/PUT /api/Client/{id}` | Détail / mise à jour |
| `GET /api/Client/paged` | Pagination globale |
| `GET /api/Client/societe/{idSociete}` | Par société |
| `GET /api/Client/societe/{idSociete}/paged` | Pagination société |
| `GET /api/Client/societe/{idSociete}/recherche` | Recherche société |
| `PUT /api/Client/toggle-statut/{id}` | Toggle statut |
| `PUT /api/Client/set-statut/{id}?Statut=` | Statut explicite |

### 6. Voyage & planification

| Endpoint | Usage front |
|----------|-------------|
| `GET/POST /api/Voyage` | Liste / création |
| `GET /api/Voyage/get-all` | Liste (guide) |
| `POST /api/Voyage/create` | Création (guide) |
| `GET/PUT /api/Voyage/{id}` | Détail / mise à jour |
| `GET /api/Voyage/paged` | Pagination globale (home/public) |
| `GET /api/Voyage/societe/{idSociete}` | Par société |
| `GET /api/Voyage/societe/{idSociete}/paged` | Pagination société |
| `GET /api/Voyage/{id}/sieges-disponibles` | Sièges disponibles |
| `POST /api/Voyage/{id}/reporter` | Report voyage |
| `GET/POST /api/PlanificationVoyage` | Planifications |
| `GET/PUT /api/PlanificationVoyage/{id}` | Détail / mise à jour |
| `PUT /api/PlanificationVoyage/{id}/toggle-statut` | Statut |
| `POST /api/PlanificationVoyage/{id}/generer` | Génération voyages |

**Query params utilisés côté front (Voyage paginé) :**  
`PageNumber`, `PageSize`, `SearchTerm`, `SortBy`, `SortDescending`, `date`, `periode`

### 7. Réservation & paiement

| Endpoint | Usage front |
|----------|-------------|
| `GET/POST /api/Reservation` | Liste / création |
| `POST /api/Reservation/create` | Création (guide) |
| `GET/PUT /api/Reservation/{id}` | Détail / mise à jour |
| `GET /api/Reservation/client/{idClient}` | Par client |
| `GET /api/Reservation/utilisateur/{idUtilisateur}` | Par utilisateur (caissier) |
| `GET /api/Reservation/utilisateur/{idUtilisateur}/client/{idClient}` | Croisement user/client |
| `POST /api/Reservation/reservation_with_paiement` | Réservation + paiement guichet |
| `POST /api/Reservation/reservation_with_paiement_electronique` | Réservation + paiement électronique |
| `POST /api/Reservation/with-passengers-and-paiement` | Réservation passagers + paiement |
| `GET/POST /api/Paiement` | Liste / création |
| `POST /api/Paiement/create` | Création (guide) |
| `GET /api/Paiement/{id}` | Détail |
| `GET /api/Paiement/reservation/{idReservation}` | Par réservation |
| `GET /api/Paiement/client/{idClient}` | Par client |
| `GET /api/Paiement/societe/{idSociete}` | Par société |
| `GET /api/Paiement/societe/{idSociete}/paged` | Pagination société |
| `GET /api/FlexPay/verifier/{orderNumber}` | Vérification paiement FlexPay |

### 8. Billet, véhicule, destination, référentiels

| Endpoint | Usage front |
|----------|-------------|
| `GET/POST /api/Billet` | Liste / création |
| `GET /api/Billet/get-all` | Liste (guide) |
| `POST /api/Billet/create` | Création (guide) |
| `GET /api/Billet/reservation/{idReservation}` | Par réservation |
| `GET /api/Billet/qrcode/{qrCode}` | Par QR code |
| `PUT /api/Billet/societe/{idSociete}/billet/{idBillet}/reaffecter` | Réaffectation billet |
| `GET/POST /api/Vehicule` | Véhicules |
| `GET /api/Vehicule/get-all` | Liste (guide) |
| `POST /api/Vehicule/create` | Création (guide) |
| `GET/PUT /api/Vehicule/{id}` | Détail / mise à jour |
| `GET /api/Vehicule/societe/{idSociete}` | Par société |
| `GET/POST /api/Vehicule/{id}/photos` | Photos véhicule |
| `GET/DELETE /api/Vehicule/{id}/photos/{idPhoto}` | Photo par id |
| `GET/POST /api/Destination` | Destinations |
| `GET /api/Destination/get-all` | Liste (guide) |
| `POST /api/Destination/create` | Création (guide) |
| `GET/PUT /api/Destination/{id}` | Détail / mise à jour |
| `GET /api/Destination/societe/{idSociete}` | Par société |
| `PUT /api/Destination/toggle-statut/{id}` | Statut |
| `GET/POST /api/CategorieSiege` | Catégories siège |
| `GET /api/CategorieSiege/societe/{idSociete}` | Par société |
| `PUT /api/CategorieSiege/toggle-statut/{id}` | Statut |
| `GET /api/CategorieClient` | Catégories client |
| `GET/POST /api/TypeVehicule` | Types véhicule |
| `GET/PUT /api/TypeVehicule/{id}` | Détail / mise à jour |

### 9. Devises, facture, plaintes, audit, communication

| Endpoint | Usage front |
|----------|-------------|
| `GET/POST /api/Devise/devises` | Devises |
| `GET /api/Devise/devises/societe/{idSociete}` | Devises société |
| `GET/PUT /api/Devise/devises/{id}` | Détail devise |
| `PUT /api/Devise/societe/{idSociete}/devise-principale/{code}` | Devise principale |
| `GET/POST /api/Devise/taux-change` | Taux de change |
| `POST /api/Devise/preview-conversion` | Prévisualisation conversion |
| `GET/POST /api/Facture` | Factures |
| `GET/POST /api/PlainteClient` | Plaintes client |
| `GET/PUT /api/PlainteClient/{id}` | Détail plainte |
| `GET /api/Audit/suspicious` | Activités suspectes |
| `GET /api/Audit/history/{tableName}/{recordId}` | Historique audit |
| `GET/POST /api/CommunicationCampaign` | Campagnes communication |
| `GET/PUT /api/CommunicationCampaign/{id}` | Détail campagne |

### 10. Statistiques & dashboards

| Endpoint | Usage front |
|----------|-------------|
| `GET /api/Statistiques/{idSociete}` | Agrégat société |
| `GET /api/Statistiques/generales/{idSociete}` | Stats générales |
| `GET /api/Statistiques/financieres/{idSociete}` | Stats financières |
| `GET /api/Statistiques/operationnelles/{idSociete}` | Stats opérationnelles |
| `GET /api/Statistiques/performance/{idSociete}` | Performance |
| `GET /api/Statistiques/consolidees/{idSociete}` | Consolidées |
| `GET /api/SuperAdminDashboard` | Dashboard super-admin |
| `GET /api/Dashboard/{idSociete}` | Dashboard admin société |
| `GET /api/AdminDashboard/societe/{idSociete}` | Fallback legacy admin |
| `GET /api/GerantDashboard` | Dashboard gérant (unifié) |
| `GET /api/GerantDashboard/societe/{idSociete}` | Gérant par société |
| `GET /api/GerantDashboard/statistiques` | Stats gérant |
| `GET /api/GerantDashboard/alertes` | Alertes gérant |
| `GET /api/GerantDashboard/societe-stats` | Stats société |
| `GET /api/GerantDashboard/clients-stats` | Stats clients |
| `GET /api/GerantDashboard/top-ca` | Top CA |
| `GET /api/GerantDashboard/top-arrieres` | Top arriérés |
| `GET /api/GerantDashboard/alertes-societe` | Alertes société |
| `GET /api/GerantDashboard/tendances` | Tendances |
| `GET /api/GerantDashboard/paiements-stats` | Stats paiements |
| `GET /api/GerantDashboard/statistiques-legacy` | Legacy stats |
| `GET /api/GerantDashboard/alertes-legacy` | Legacy alertes |
| `GET /api/CaissierDashboard` | Dashboard caissier |
| `GET /api/CaissierDashboard/statistiques-journalieres` | Stats journalières |
| `GET /api/CaissierDashboard/paiements-en-cours` | Paiements en cours |
| `GET /api/CaissierDashboard/paiements-recents` | Paiements récents |
| `GET /api/CaissierDashboard/recettes-journalieres` | Recettes journalières |
| `GET /api/CaissierDashboard/alertes-caissier` | Alertes caissier |
| `GET /api/CaissierDashboard/resume-caisse` | Résumé caisse |
| `GET /api/CaissierDashboard/rapport-caisse` | Rapport caisse |
| `GET /api/FinancierDashboard` | Dashboard financier |
| `GET /api/FinancierDashboard/statistiques-globales` | Stats globales |
| `GET /api/FinancierDashboard/societes-financieres` | Sociétés financières |
| `GET /api/FinancierDashboard/transactions-recentes` | Transactions récentes |
| `GET /api/FinancierDashboard/alertes-financieres` | Alertes financières |
| `GET /api/FinancierDashboard/tendances-financieres` | Tendances financières |
| `GET /api/ClientDashboard` | Dashboard client |
| `GET /api/ClientDashboard/statistiques` | Stats client |
| `GET /api/ClientDashboard/reservations-recentes` | Réservations récentes |
| `GET /api/ClientDashboard/paiements-recents` | Paiements récents |
| `GET /api/ClientDashboard/voyages-client` | Voyages client |
| `GET /api/ClientDashboard/alertes-client` | Alertes client |
| `GET /api/ClientDashboard/resume-client` | Résumé client |
| `GET /api/FinanceReporting/paiements/summary` | Synthèse paiements |
| `GET /api/FinanceReporting/rapport-caisse` | Rapport caisse finance |

### 11. Temps réel

| Endpoint | Usage front |
|----------|-------------|
| `WS /hubs/notifications` | Notifications SignalR (FlexPay, alertes) |

---

## Routes legacy vs guide (à clarifier)

Le front bascule selon `VITE_API_USE_GUIDE_ROUTES` :

| Module | Legacy | Guide (OpenAPI) |
|--------|--------|-----------------|
| Login | `POST /api/Utilisateur/authentifier` | `POST /api/Auth/login` |
| Logout | `POST /api/Utilisateur/deconnecter` | `POST /api/Auth/logout` |
| Utilisateur create | `POST /api/Utilisateur` | `POST /api/Utilisateur/create` |
| Client create | `POST /api/Client` | `POST /api/Client/create` |
| Voyage list | `GET /api/Voyage` | `GET /api/Voyage/get-all` |
| Voyage create | `POST /api/Voyage` | `POST /api/Voyage/create` |
| Réservation create | `POST /api/Reservation` | `POST /api/Reservation/create` |
| Paiement create | `POST /api/Paiement` | `POST /api/Paiement/create` |
| Billet list | `GET /api/Billet` | `GET /api/Billet/get-all` |
| Billet create | `POST /api/Billet` | `POST /api/Billet/create` |
| Véhicule list | `GET /api/Vehicule` | `GET /api/Vehicule/get-all` |
| Véhicule create | `POST /api/Vehicule` | `POST /api/Vehicule/create` |
| Destination list | `GET /api/Destination` | `GET /api/Destination/get-all` |
| Destination create | `POST /api/Destination` | `POST /api/Destination/create` |

**Question backend :** quelle variante est officielle sur dev et prod ? Quand les routes legacy seront-elles supprimées ?

---

## Livrables attendus

Merci de fournir :

1. **OpenAPI/Swagger à jour** (fichier JSON/YAML + URL Swagger UI)
2. **Changelog API** avec statut par endpoint : `nouveau` / `modifié` / `déprécié` / `supprimé`
3. **Tableau breaking changes** (ancien contrat → nouveau contrat)
4. **Matrice rôles/permissions** (SuperAdmin, Gérant, Caissier, Client, Agent…)
5. **Règles `X-Societe-Id`** (quand obligatoire, quand interdit)
6. **Exemples request/response** (200, 400, 401, 403, 404, 500) pour les endpoints critiques
7. **Configuration CORS** pour les origines front autorisées
8. **Documentation SignalR** : événements, authentification hub, URL exacte
9. **Collection Postman/Insomnia** exportable
10. **Version API** déployée sur dev et prod + date de release

---

## Priorité d'intégration

1. Auth + refresh + logout
2. Voyage / Réservation / Billet
3. Paiement guichet + électronique (FlexPay + SignalR)
4. Dashboards (Caissier, Gérant, Client, SuperAdmin)
5. FinanceReporting / rapport caisse
6. Société / Site / Config (`/api/Societe/{id}/config`)

---

## Critères d'acceptation côté front

La documentation sera considérée complète lorsque nous pourrons :

- mettre à jour le front **sans essais/erreurs** sur dev,
- distinguer clairement endpoints legacy vs guide,
- valider login, recherche voyage, réservation, paiement, dashboards et rapport caisse,
- déployer en prod sans erreurs CORS ni contrat JSON ambigu.

---

## Tableau à remplir par le backend

| Module | Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Headers requis | Notes |
|--------|----------|---------|--------|------------|---------|----------------|-------|
| Auth | `/api/Utilisateur/authentifier` | POST | | | | Authorization | |
| Auth | `/api/Auth/login` | POST | | | | | Variante guide |
| Voyage | `/api/Voyage/paged` | GET | | | | | PageNumber, PageSize, SortBy… |
| Réservation | `/api/Reservation/reservation_with_paiement_electronique` | POST | | | | | |
| Paiement | `/api/FlexPay/verifier/{orderNumber}` | GET | | | | | |
| Dashboard | `/api/CaissierDashboard/rapport-caisse` | GET | | | Caissier | Authorization, X-Societe-Id | |
| Finance | `/api/FinanceReporting/rapport-caisse` | GET | | | | | |
| Société | `/api/Societe/{id}/config` | GET/PUT | | | Admin | Authorization, X-Societe-Id | |
| Temps réel | `/hubs/notifications` | WS | | | | Bearer token | SignalR |

---

## Planning demandé

Merci d'indiquer :

- la **date de livraison** de la documentation,
- un **contact technique** pour questions d'intégration,
- une **fenêtre de support** (ex. point technique hebdomadaire pendant la migration).

---

Cordialement,  
**[Nom]**  
Équipe Front — Congo Travel Web
