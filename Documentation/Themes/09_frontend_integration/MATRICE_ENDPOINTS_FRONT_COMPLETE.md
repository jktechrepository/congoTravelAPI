# Matrice complète des endpoints — Congo Travel Web × Backend

**Date :** 7 juillet 2026  
**Référence :** inventaire front `DEMANDE_DOC_API_BACKEND.md`  
**Complément :** `CHANGELOG_API_BREAKING_CHANGES.md`

**Légende statuts :**

| Statut | Signification |
|--------|---------------|
| **Actif** | Endpoint implémenté, utilisable tel quel |
| **Absent** | Non implémenté (404) |
| **Supprimé** | Retiré de l'API (ex-Kenergie) |
| **Consolidé** | Données disponibles via un endpoint parent (DTO unifié) |
| **Déprécié** | Encore fonctionnel, remplacement recommandé |
| **Breaking** | Existe sous un autre chemin ou une autre méthode |

**Headers :** sauf mention « Public », `Authorization: Bearer {token}` requis. `X-Societe-Id` autorisé en CORS mais **non utilisé** — tenant via JWT.

---

## 1. Auth & session

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes / remplacement |
|----------|---------|--------|------------|---------|----------------------|
| `/api/Utilisateur/authentifier` | POST | **Actif** | Non | Public | Body : `emailOuTelephone`, `motDePasse` |
| `/api/Utilisateur/auth/google` | POST | **Actif** | Non | Public | Body : `idToken` — **même response body** que `authentifier` |
| `/api/Utilisateur/auth/apple` | POST | **Actif** | Non | Public | Body : `idToken` (Apple) — **même response body** que `authentifier` |
| `/api/Auth/login` | POST | **Absent** | Oui | — | Utiliser `/api/Utilisateur/authentifier` |
| `/api/Utilisateur/deconnecter` | POST | **Actif** | Non | JWT | |
| `/api/Auth/logout` | POST | **Absent** | Oui | — | Utiliser `/api/Utilisateur/deconnecter` |
| `/api/Auth/refresh-token` | POST | **Absent** | Oui | — | Utiliser `/api/Utilisateur/refresh-token` |
| `/api/Utilisateur/refresh-token` | POST | **Actif** | Non | JWT | Body : `refreshToken` |

---

## 2. Utilisateur & rôles

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes / remplacement |
|----------|---------|--------|------------|---------|----------------------|
| `/api/Utilisateur` | GET | **Actif** | Non | Admin, Super-Admin | Liste utilisateurs |
| `/api/Utilisateur` | POST | **Actif** | Non | Admin | Création |
| `/api/Utilisateur/create` | POST | **Absent** | Oui | — | Utiliser `POST /api/Utilisateur` |
| `/api/Utilisateur/{id}` | GET | **Actif** | Non | Admin / soi-même | |
| `/api/Utilisateur/{id}` | PUT | **Actif** | Non | Admin / soi-même | |
| `/api/Utilisateur/toggle-statut/{id}` | PUT | **Actif** | Non | Admin | |
| `/api/Utilisateur/role/{roleId}` | GET | **Actif** | Non | Admin | Paginé |
| `/api/Utilisateur/role/nom/{nomRole}` | GET | **Absent** | Oui | — | Utiliser `GET /api/Role/nomRole/{nomRole}` |
| `/api/Utilisateur/{id}/roles/{roleId}` | GET | **Absent** | Non | — | Utiliser `GET /api/Utilisateur/{id}/roles` |
| `/api/Utilisateur/{id}/roles/{roleId}` | POST | **Actif** | Non | Admin | Ajout rôle |
| `/api/Utilisateur/{id}/roles/{roleId}` | DELETE | **Actif** | Non | Admin | Retrait rôle |
| `/api/Utilisateur/{id}/roles/{roleId}/primary` | PUT | **Actif** | Non | Admin | Rôle principal |
| `/api/Utilisateur/change-password` | POST | **Absent** | Oui | — | Utiliser `POST /api/Utilisateur/changer_mot_de_passe` |
| `/api/Role` | GET | **Actif** | Non | JWT | Catalogue rôles |
| `/api/Role/nomRole/{nomRole}` | GET | **Actif** | — | JWT | Alternative rôle par nom |

---

## 3. Agent

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes |
|----------|---------|--------|------------|---------|-------|
| `/api/Agent` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Agent` | POST | **Actif** | Non | Admin, Gérant | |
| `/api/Agent/{id}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Agent/{id}` | PUT | **Actif** | Non | Admin, Gérant | |
| `/api/Agent/toggle-statut/{id}` | PUT | **Actif** | Non | Admin, Gérant | |
| `/api/Agent/statut/{statut}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Agent/exists/{id}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Agent/batch` | POST | **Actif** | Non | Admin, Gérant | |
| `/api/Agent/{idAgent}/add-role` | POST | **Actif** | Non | Admin | |
| `/api/Agent/{idAgent}/replace-role` | PUT | **Actif** | Non | Admin | |
| `/api/Agent/{idAgent}/AffecterAgentSite` | PUT | **Actif** | Non | Admin, Gérant | Alias : `/site` |
| `/api/Agent/{idAgent}/site` | PUT | **Actif** | Non | Admin, Gérant | Alias de AffecterAgentSite |
| `/api/Agent/serial-number/{serialNumber}` | GET | **Actif** | Non | Agent, Admin | |
| `/api/Agent/{idAgent}/serial-number` | PUT | **Actif** | Non | Admin | |
| `/api/Agent/matricule/{matricule}/serial-number` | PUT | **Actif** | Non | Admin | |

---

## 4. Société, site, config multi-tenant

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes |
|----------|---------|--------|------------|---------|-------|
| `/api/Societe` | GET | **Actif** | Non | Super-Admin | Permission `Societe.ReadAll` |
| `/api/Societe` | POST | **Actif** | Non | Super-Admin | Body `{ societe, site }` |
| `/api/Societe/{id}` | GET | **Actif** | Non | Admin | Permission `Societe.Read` |
| `/api/Societe/{id}` | PUT | **Actif** | Non | Admin | Permission `Societe.Update` |
| `/api/Societe/{id}/config` | GET | **Actif** | Non | Admin, Gérant | Permission `ConfigSociete.Read` |
| `/api/Societe/{id}/config` | PUT | **Actif** | Non | Admin, Gérant | Permission `ConfigSociete.Update` |
| `/api/Societe/set-statut/{id}?Statut=` | PUT | **Actif** | Non | Super-Admin | Query `statut` (case-insensitive) |
| `/api/Site` | GET | **Actif** | Non | Admin | |
| `/api/Site` | POST | **Actif** | Non | Admin | |
| `/api/Site/{id}` | GET | **Actif** | Non | Admin | |
| `/api/Site/{id}` | PUT | **Actif** | Non | Admin | |
| `/api/Site/societe/{idSociete}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Site/toggle-statut/{id}` | PUT | **Actif** | Non | Admin | |
| `/api/InfoPaiementSociete` | GET | **Absent** | Oui | — | Pas de liste racine |
| `/api/InfoPaiementSociete` | POST | **Actif** | Non | Super-Admin | |
| `/api/InfoPaiementSociete/{id}` | GET | **Absent** | Oui | — | Utiliser `GET .../site/{idSite}` |
| `/api/InfoPaiementSociete/{id}` | PUT | **Actif** | Non | Super-Admin | |
| `/api/InfoPaiementSociete/{id}` | DELETE | **Actif** | Non | Super-Admin | |
| `/api/InfoPaiementSociete/site/{idSite}` | GET | **Actif** | Non | Super-Admin | |

---

## 5. Client

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes |
|----------|---------|--------|------------|---------|-------|
| `/api/Client` | GET | **Actif** | Non | Admin, Caissier | |
| `/api/Client` | POST | **Actif** | Non | Admin, Caissier | |
| `/api/Client/create` | POST | **Absent** | Oui | — | Utiliser `POST /api/Client` |
| `/api/Client/register` | POST | **Actif** | Non | Public | Inscription |
| `/api/Client/{id}` | GET | **Actif** | Non | Admin, Caissier, Client | |
| `/api/Client/{id}` | PUT | **Actif** | Non | Admin, Caissier, Client | |
| `/api/Client/paged` | GET | **Actif** | Non | Admin | |
| `/api/Client/societe/{idSociete}` | GET | **Actif** | Non | Admin, Caissier | Permission `Client.ReadAll` |
| `/api/Client/societe/{idSociete}/paged` | GET | **Actif** | Non | Admin, Caissier | |
| `/api/Client/societe/{idSociete}/recherche` | GET | **Actif** | Non | Admin, Caissier | |
| `/api/Client/toggle-statut/{id}` | PUT | **Actif** | Non | Admin | |
| `/api/Client/set-statut/{id}?Statut=` | PUT | **Actif** | Non | Admin | Query `statut` |

---

## 6. Voyage & planification

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes |
|----------|---------|--------|------------|---------|-------|
| `/api/Voyage` | GET | **Déprécié** | Non | Public | Préférer `/paged` |
| `/api/Voyage` | POST | **Actif** | Non | Admin, Gérant | Création |
| `/api/Voyage/get-all` | GET | **Absent** | Oui | — | Utiliser `GET /api/Voyage/paged` |
| `/api/Voyage/create` | POST | **Absent** | Oui | — | Utiliser `POST /api/Voyage` |
| `/api/Voyage/{id}` | GET | **Actif** | Non | Public | Détail |
| `/api/Voyage/{id}` | PUT | **Actif** | Non | Admin, Gérant | |
| `/api/Voyage/paged` | GET | **Actif** | Non | Public | Query : `pageNumber`, `pageSize`, `searchTerm`, `sortBy`, `sortDescending`, `date`, `periode` |
| `/api/Voyage/societe/{idSociete}` | GET | **Déprécié** | Non | Public | Préférer `.../paged` |
| `/api/Voyage/societe/{idSociete}/paged` | GET | **Actif** | Non | Public | |
| `/api/Voyage/{id}/sieges-disponibles` | GET | **Actif** | Non | Public | |
| `/api/Voyage/{id}/reporter` | POST | **Actif** | Non | Admin, Gérant | |
| `/api/PlanificationVoyage` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/PlanificationVoyage` | POST | **Actif** | Non | Admin, Gérant | |
| `/api/PlanificationVoyage/{id}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/PlanificationVoyage/{id}` | PUT | **Actif** | Non | Admin, Gérant | |
| `/api/PlanificationVoyage/{id}/toggle-statut` | PUT | **Actif** | Non | Admin, Gérant | |
| `/api/PlanificationVoyage/{id}/generer` | POST | **Actif** | Non | Admin, Gérant | |

---

## 7. Réservation & paiement

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes |
|----------|---------|--------|------------|---------|-------|
| `/api/Reservation` | GET | **Actif** | Non | Caissier, Admin | |
| `/api/Reservation` | POST | **Actif** | Non | Caissier, Client | |
| `/api/Reservation/create` | POST | **Absent** | Oui | — | Utiliser `POST /api/Reservation` |
| `/api/Reservation/{id}` | GET | **Actif** | Non | Caissier, Client | |
| `/api/Reservation/{id}` | PUT | **Actif** | Non | Caissier, Admin | |
| `/api/Reservation/client/{idClient}` | GET | **Actif** | Non | Caissier, Client | |
| `/api/Reservation/utilisateur/{idUtilisateur}` | GET | **Actif** | Non | Caissier | |
| `/api/Reservation/utilisateur/{idUtilisateur}/client/{idClient}` | GET | **Actif** | Non | Caissier | |
| `/api/Reservation/reservation_with_paiement` | POST | **Actif** | Non | Caissier | Guichet |
| `/api/Reservation/reservation_with_paiement_electronique` | POST | **Actif** | Non | Caissier, Client | FlexPay |
| `/api/Reservation/with-passengers-and-paiement` | POST | **Actif** | Non | Caissier | Alias même handler |
| `/api/Paiement` | GET | **Actif** | Non | Admin, Caissier | |
| `/api/Paiement` | POST | **Actif** | Non | Caissier | |
| `/api/Paiement/create` | POST | **Absent** | Oui | — | Utiliser `POST /api/Paiement` |
| `/api/Paiement/{id}` | GET | **Actif** | Non | Admin, Caissier | |
| `/api/Paiement/reservation/{idReservation}` | GET | **Actif** | Non | Caissier, Client | |
| `/api/Paiement/client/{idClient}` | GET | **Actif** | Non | Admin, Client | |
| `/api/Paiement/societe/{idSociete}` | GET | **Actif** | Non | Admin, Financier | |
| `/api/Paiement/societe/{idSociete}/paged` | GET | **Actif** | Non | Admin, Financier | |
| `/api/FlexPay/verifier/{orderNumber}` | GET | **Actif** | Non | JWT | Polling statut |

---

## 8. Billet, véhicule, destination, référentiels

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes |
|----------|---------|--------|------------|---------|-------|
| `/api/Billet` | GET | **Actif** | Non | Admin, Caissier | |
| `/api/Billet` | POST | **Actif** | Non | Caissier | |
| `/api/Billet/get-all` | GET | **Absent** | Oui | — | Utiliser `GET /api/Billet` |
| `/api/Billet/create` | POST | **Absent** | Oui | — | Utiliser `POST /api/Billet` |
| `/api/Billet/reservation/{idReservation}` | GET | **Actif** | Non | Caissier, Client | |
| `/api/Billet/qrcode/{qrCode}` | GET | **Actif** | Non | Caissier, Agent | |
| `/api/Billet/societe/{idSociete}/billet/{idBillet}/reaffecter` | PUT | **Breaking** | Oui | Caissier | Backend : **POST** (pas PUT) |
| `/api/Vehicule` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Vehicule` | POST | **Actif** | Non | Admin, Gérant | |
| `/api/Vehicule/get-all` | GET | **Absent** | Oui | — | Utiliser `GET /api/Vehicule` |
| `/api/Vehicule/create` | POST | **Absent** | Oui | — | Utiliser `POST /api/Vehicule` |
| `/api/Vehicule/{id}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Vehicule/{id}` | PUT | **Actif** | Non | Admin, Gérant | |
| `/api/Vehicule/societe/{idSociete}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Vehicule/{id}/photos` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Vehicule/{id}/photos` | POST | **Actif** | Non | Admin, Gérant | |
| `/api/Vehicule/{id}/photos/{idPhoto}` | GET | **Absent** | Oui | — | Pas de GET par photo ; liste via `GET .../photos` |
| `/api/Vehicule/{id}/photos/{idPhoto}` | DELETE | **Actif** | Non | Admin, Gérant | Param backend : `photoId` |
| `/api/Destination` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Destination` | POST | **Actif** | Non | Admin, Gérant | |
| `/api/Destination/get-all` | GET | **Absent** | Oui | — | Utiliser `GET /api/Destination` |
| `/api/Destination/create` | POST | **Absent** | Oui | — | Utiliser `POST /api/Destination` |
| `/api/Destination/{id}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Destination/{id}` | PUT | **Actif** | Non | Admin, Gérant | |
| `/api/Destination/societe/{idSociete}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/Destination/toggle-statut/{id}` | PUT | **Breaking** | Oui | Admin | Backend : `PUT /api/Destination/{id}/toggle-statut` |
| `/api/CategorieSiege` | GET | **Absent** | Oui | — | Utiliser `GET /api/CategorieSiege/societe/{idSociete}` |
| `/api/CategorieSiege` | POST | **Actif** | Non | Admin, Gérant | |
| `/api/CategorieSiege/societe/{idSociete}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/CategorieSiege/toggle-statut/{id}` | PUT | **Breaking** | Oui | Admin | Backend : `PUT /api/CategorieSiege/{id}/toggle-statut` |
| `/api/CategorieClient` | GET | **Supprimé** | Oui | — | Retirer du front |
| `/api/TypeVehicule` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/TypeVehicule` | POST | **Actif** | Non | Admin, Gérant | |
| `/api/TypeVehicule/{id}` | GET | **Actif** | Non | Admin, Gérant | |
| `/api/TypeVehicule/{id}` | PUT | **Actif** | Non | Admin, Gérant | |

---

## 9. Devises, facture, plaintes, audit, communication

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes |
|----------|---------|--------|------------|---------|-------|
| `/api/Devise/devises` | GET | **Actif** | Non | Admin, Financier | |
| `/api/Devise/devises` | POST | **Actif** | Non | Super-Admin | |
| `/api/Devise/devises/societe/{idSociete}` | GET | **Actif** | Non | Admin, Financier | |
| `/api/Devise/devises/{id}` | GET | **Actif** | Non | Admin, Financier | |
| `/api/Devise/devises/{id}` | PUT | **Actif** | Non | Super-Admin | |
| `/api/Devise/societe/{idSociete}/devise-principale/{code}` | PUT | **Actif** | Non | Admin | |
| `/api/Devise/taux-change` | GET | **Actif** | Non | Admin, Financier | |
| `/api/Devise/taux-change` | POST | **Actif** | Non | Super-Admin | |
| `/api/Devise/preview-conversion` | POST | **Breaking** | Oui | Admin | Backend : **GET** + query params |
| `/api/Facture` | GET | **Supprimé** | Oui | — | Remplacé par Reservation/Paiement |
| `/api/Facture` | POST | **Supprimé** | Oui | — | — |
| `/api/PlainteClient` | GET | **Actif** | Non | Admin, Client | |
| `/api/PlainteClient` | POST | **Actif** | Non | Client | |
| `/api/PlainteClient/{id}` | GET | **Actif** | Non | Admin, Client | |
| `/api/PlainteClient/{id}` | PUT | **Actif** | Non | Admin | |
| `/api/Audit/suspicious` | GET | **Actif** | Non | Super-Admin | |
| `/api/Audit/history/{tableName}/{recordId}` | GET | **Actif** | Non | Admin | |
| `/api/CommunicationCampaign` | GET | **Actif** | Non | Admin | |
| `/api/CommunicationCampaign` | POST | **Actif** | Non | Admin | |
| `/api/CommunicationCampaign/{id}` | GET | **Actif** | Non | Admin | |
| `/api/CommunicationCampaign/{id}` | PUT | **Actif** | Non | Admin | |

---

## 10. Statistiques & dashboards

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes / remplacement |
|----------|---------|--------|------------|---------|----------------------|
| `/api/Statistiques/{idSociete}` | GET | **Actif** | Non | Admin, Gérant | Réponse consolidée unique |
| `/api/Statistiques/generales/{idSociete}` | GET | **Supprimé** | Oui | — | → `.generales` du GET unique |
| `/api/Statistiques/financieres/{idSociete}` | GET | **Supprimé** | Oui | — | → `.financieres` |
| `/api/Statistiques/operationnelles/{idSociete}` | GET | **Supprimé** | Oui | — | → `.operationnelles` |
| `/api/Statistiques/performance/{idSociete}` | GET | **Supprimé** | Oui | — | → `.performance` |
| `/api/Statistiques/consolidees/{idSociete}` | GET | **Supprimé** | Oui | — | → GET unique |
| `/api/SuperAdminDashboard` | GET | **Actif** | Non | Super-Admin | |
| `/api/Dashboard/{idSociete}` | GET | **Actif** | Non | Admin | Remplace AdminDashboard |
| `/api/AdminDashboard/societe/{idSociete}` | GET | **Supprimé** | Oui | — | → `GET /api/Dashboard/{idSociete}` |
| `/api/GerantDashboard` | GET | **Actif** | Non | Gérant | DTO unifié |
| `/api/GerantDashboard/societe/{idSociete}` | GET | **Supprimé** | Oui | — | Scope JWT, pas d'URL société |
| `/api/GerantDashboard/statistiques` | GET | **Consolidé** | Oui | Gérant | → `.societeStatistiques` |
| `/api/GerantDashboard/alertes` | GET | **Consolidé** | Oui | Gérant | → `.alertesSociete` |
| `/api/GerantDashboard/societe-stats` | GET | **Consolidé** | Oui | Gérant | → `.societeStatistiques` |
| `/api/GerantDashboard/clients-stats` | GET | **Consolidé** | Oui | Gérant | → `.clientsStatistiques` |
| `/api/GerantDashboard/top-ca` | GET | **Consolidé** | Oui | Gérant | → `.top5ClientsCA` |
| `/api/GerantDashboard/top-arrieres` | GET | **Consolidé** | Oui | Gérant | → `.top5ClientsNonPayes` |
| `/api/GerantDashboard/alertes-societe` | GET | **Consolidé** | Oui | Gérant | → `.alertesSociete` |
| `/api/GerantDashboard/tendances` | GET | **Consolidé** | Oui | Gérant | → `.tendances` |
| `/api/GerantDashboard/paiements-stats` | GET | **Consolidé** | Oui | Gérant | → `.paiementsStatistiques` |
| `/api/GerantDashboard/statistiques-legacy` | GET | **Supprimé** | Oui | — | — |
| `/api/GerantDashboard/alertes-legacy` | GET | **Supprimé** | Oui | — | — |
| `/api/CaissierDashboard` | GET | **Actif** | Non | Caissier | DTO unifié |
| `/api/CaissierDashboard/statistiques-journalieres` | GET | **Consolidé** | Oui | Caissier | → `.statistiquesJournalieres` |
| `/api/CaissierDashboard/paiements-en-cours` | GET | **Consolidé** | Oui | Caissier | → `.paiementsEnCours` |
| `/api/CaissierDashboard/paiements-recents` | GET | **Consolidé** | Oui | Caissier | → `.paiementsRecents` |
| `/api/CaissierDashboard/recettes-journalieres` | GET | **Consolidé** | Oui | Caissier | → `.recettesJournalieres` |
| `/api/CaissierDashboard/alertes-caissier` | GET | **Consolidé** | Oui | Caissier | → `.alertesCaissier` |
| `/api/CaissierDashboard/resume-caisse` | GET | **Consolidé** | Oui | Caissier | → `.resumeCaisse` |
| `/api/CaissierDashboard/rapport-caisse` | GET | **Actif** | Non | Caissier | Route dédiée conservée |
| `/api/FinancierDashboard` | GET | **Actif** | Non | Financier | DTO unifié |
| `/api/FinancierDashboard/statistiques-globales` | GET | **Consolidé** | Oui | Financier | → `.globalStatistiques` |
| `/api/FinancierDashboard/societes-financieres` | GET | **Consolidé** | Oui | Financier | → `.societesFinancieres` |
| `/api/FinancierDashboard/transactions-recentes` | GET | **Consolidé** | Oui | Financier | → `.transactionsRecentes` |
| `/api/FinancierDashboard/alertes-financieres` | GET | **Consolidé** | Oui | Financier | → `.alertesFinancieres` |
| `/api/FinancierDashboard/tendances-financieres` | GET | **Consolidé** | Oui | Financier | → `.tendances` |
| `/api/ClientDashboard` | GET | **Actif** | Non | Client | DTO unifié |
| `/api/ClientDashboard/statistiques` | GET | **Consolidé** | Oui | Client | → `.statistiques` |
| `/api/ClientDashboard/reservations-recentes` | GET | **Consolidé** | Oui | Client | → `.reservationsRecentes` |
| `/api/ClientDashboard/paiements-recents` | GET | **Consolidé** | Oui | Client | → `.paiementsRecents` |
| `/api/ClientDashboard/voyages-client` | GET | **Consolidé** | Oui | Client | → `.voyagesClient` |
| `/api/ClientDashboard/alertes-client` | GET | **Consolidé** | Oui | Client | → `.alertesClient` |
| `/api/ClientDashboard/resume-client` | GET | **Consolidé** | Oui | Client | → `.resumeClient` |
| `/api/FinanceReporting/paiements/summary` | GET | **Actif** | Non | Financier, Admin | |
| `/api/FinanceReporting/rapport-caisse` | GET | **Actif** | Non | Financier, Admin | Query : `idSociete`, dates |

---

## 11. Temps réel

| Endpoint | Méthode | Statut | Breaking ? | Rôle(s) | Notes |
|----------|---------|--------|------------|---------|-------|
| `/hubs/notifications` | WS | **Actif** | Oui (URL) | JWT | Hub officiel ; ancienne doc : `/hubs/dashboard` |
| Événement `FlexPayPaymentConfirmed` | — | **Actif** | — | JWT | Groupe `user_{userId}` |
| Événement `FlexPayPaymentFailed` | — | **Actif** | — | JWT | |
| Événement `ReceiveNotification` | — | **Actif** | — | JWT | |
| Événement `NewPaiement` | — | **Actif** | — | JWT | Payload transport |
| Événement `SuperAdminDashboardUpdated` | — | **Actif** | — | Super-Admin | |

---

## Synthèse quantitative

| Statut | Nombre (inventaire front) |
|--------|---------------------------|
| **Actif** | ~95 |
| **Absent** (routes guide) | 18 |
| **Supprimé** | 12 |
| **Consolidé** (dashboards/statistiques) | 28 |
| **Breaking** (méthode/chemin) | 6 |
| **Déprécié** | 2 |

---

## Fichiers associés

| Fichier | Description |
|---------|-------------|
| `CHANGELOG_API_BREAKING_CHANGES.md` | Changelog détaillé + migrations |
| `postman/CongoTravel_API.postman_collection.json` | Collection Postman importable |
| `postman/CongoTravel_API.postman_environment.json` | Environnements dev/prod |

*Généré à partir du code source CongoTravel API — juillet 2026.*
