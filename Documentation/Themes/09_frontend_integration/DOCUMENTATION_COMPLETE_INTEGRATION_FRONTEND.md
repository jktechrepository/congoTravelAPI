# Documentation complète — Intégration frontend (Vue.js + Flutter)

> **Point d'entrée unique** pour les équipes frontend CongoTravelAPI.
>
> Structure : ce document maître + **9 fiches modules** (voir [§5](#5-index-des-fiches-modules)).

---

## Table des matières

1. [Vue d'ensemble](#1-vue-densemble)
2. [Trois personas et parcours](#2-trois-personas-et-parcours)
3. [Fondations communes](#3-fondations-communes)
4. [Snippets réutilisables](#4-snippets-réutilisables)
5. [Index des fiches modules](#5-index-des-fiches-modules)
6. [Gestion des erreurs HTTP](#6-gestion-des-erreurs-http)
7. [Checklists d'intégration](#7-checklists-dintégration)
8. [Outils et références](#8-outils-et-références)

---

## 1. Vue d'ensemble

### Base URL

| Environnement | URL |
|---------------|-----|
| Dev local | `http://localhost:5000/api` ou `https://localhost:7110/api` |
| Production | `https://<votre-domaine>/api` |

### Headers standards

```
Content-Type: application/json
Authorization: Bearer <accessToken>    # endpoints protégés
X-Device-Id: <uuid-stable>             # inscription client (recommandé mobile)
```

### Conventions JSON

| Type | Format |
|------|--------|
| Dates (`DateTime`) | ISO-8601, ex. `2026-05-10T00:00:00` (souvent minuit pour une **date de voyage** ; le `Z` UTC n’est pas toujours présent) |
| Heures voyage / départ (`TimeSpan`) | String **`HH:mm:ss`** (`08:30:00`) — convertisseur global, **pas** un objet TimeSpan |
| Devises | Code ISO 3 lettres (`CDF`, `USD`) |
| Booléens | `true` / `false` |
| Pagination | `pageNumber`, `pageSize`, `sortBy`, `sortDescending` |

Scan billet (`dateDepartVoyage` + `heureDepartVoyage`, `dateVoyage` + `heureVoyage`) : détail dans [MODULE_03](MODULE_03_RESERVATION_BILLET.md#formats-date--heure-scan).

### Tenancy

- `idSociete` et `idSite` proviennent du JWT après login (`utilisateur.idSociete`, `utilisateur.idSite`).
- La plupart des endpoints transport filtrent automatiquement par société du token.
- Super-Admin peut passer `?idSociete=` sur certaines listes.
- **Événements** :
  - Catalogue Client / anonyme = sessions `Published` **toutes sociétés**.
  - Achat Client (`with-paiement` / `with-paiement-electronique`) = société **de la session** (organisateur), **pas** `utilisateur.idSociete` du JWT.
  - Staff guichet = société JWT uniquement.

---

## 2. Trois personas et parcours

### Persona A — Back-office admin (Vue.js)

| Étape | Module | Endpoints clés |
|-------|--------|----------------|
| 1. Login | [MODULE_01](MODULE_01_AUTH_ET_PERMISSIONS.md) | `POST /Utilisateur/authentifier` (Google/Apple : [guide Vue/Flutter](INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md)) |
| 2. Permissions | MODULE_01 | `permissions[]` → guards router |
| 3. Référentiels | [MODULE_09](MODULE_09_REFERENTIELS_ET_COMMUNICATION.md) | Société, Site, Agent, Véhicule |
| 4. Voyages | [MODULE_02](MODULE_02_TRANSPORT_VOYAGE.md) | CRUD Voyage, Destination, Planification |
| 5. Réservations | [MODULE_03](MODULE_03_RESERVATION_BILLET.md) | Réservation multi-passagers |
| 6. Paiements | [MODULE_04](MODULE_04_PAIEMENT_FLEXPAY.md) | Cash, FlexPay, multi-devise |
| 7. Reporting | [MODULE_07](MODULE_07_DASHBOARDS_ADMIN.md) | Dashboards gérant, financier |
| 8. Événements | [MODULE_05](MODULE_05_EVENEMENT_BILLETTERIE.md) | Guichet `with-paiement` / FlexPay, sessions, tickets |
| 9. Sites touristiques | [MODULE_10](MODULE_10_SITE_TOURISTIQUE.md) | Lieux, planification, journées, CASH/FlexPay, tickets |
| 10. Restaurants | [MODULE_11](MODULE_11_RESTAURANT.md) | Établissements, créneaux, zones, acompte CASH/FlexPay, dashboard |

**Stack recommandée** : Vue 3, Vue Router, Pinia, Axios, Chart.js.

### Persona B — Agent / caissier / embarquement (Flutter)

| Étape | Module | Endpoints clés |
|-------|--------|----------------|
| 1. Login agent | MODULE_01 | `POST /Utilisateur/authentifier` |
| 2. Voyages du jour | MODULE_02 | `GET /Voyage/site/{idSite}/paged?date=` |
| 3. Vente guichet | MODULE_03 + MODULE_04 | Réservation + paiement cash |
| 4. Scan QR | MODULE_03 | `GET /Billet/{qrCode}/check` |
| 5. Embarquement | MODULE_03 | `POST .../embarquer` |
| 6. Sync offline | [MODULE_08](MODULE_08_SYNC_OFFLINE_AGENT.md) | `/sync/*` |
| 7. FlexPay | MODULE_04 | Paiement électronique + `verifier` |
| 8. Événements (optionnel) | [MODULE_05](MODULE_05_EVENEMENT_BILLETTERIE.md) | Contrôle entrée tickets événement (`check` / `use`) |
| 9. Sites touristiques (optionnel) | [MODULE_10](MODULE_10_SITE_TOURISTIQUE.md) | Gate tickets site touristique (`check` / `use`) |

**Stack recommandée** : Flutter, Dio, flutter_secure_storage, mobile_scanner.

### Persona C — Client voyageur (Flutter)

| Étape | Module | Endpoints clés |
|-------|--------|----------------|
| 1. Inscription | [MODULE_06](MODULE_06_CLIENT_APP_VOYAGEUR.md) + [vérif email](INTEGRATION_VERIFICATION_EMAIL_VUE_FLUTTER.md) | `POST /client/register` + lien `verify-email` |
| 2. Login | MODULE_01 + [guide Google/Apple](INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md) | `authentifier` ou `auth/google` / `auth/apple` |
| 3. Recherche voyage | MODULE_02 | `GET /Voyage/search` |
| 4. Réservation | MODULE_03 | Multi-passagers |
| 5. Paiement FlexPay | MODULE_04 | Mobile Money |
| 6. Mes billets | MODULE_03 + MODULE_06 | QR codes, historique |
| 7. Dashboard client | MODULE_06 | `GET /ClientDashboard` |
| 8. Événements (optionnel) | [MODULE_05](MODULE_05_EVENEMENT_BILLETTERIE.md) | Catalogue → `with-paiement-electronique` → tickets QR |
| 9. Sites touristiques (optionnel) | [MODULE_10](MODULE_10_SITE_TOURISTIQUE.md) | Catalogue lieux/journées → FlexPay → QR |
| 10. Restaurants (optionnel) | [MODULE_11](MODULE_11_RESTAURANT.md) | Catalogue créneaux → acompte FlexPay (pas de gate QR V1) |

**Stack recommandée** : Flutter, Dio, SignalR (notifications paiement).

---

## 3. Fondations communes

### 3.1 Authentification JWT

Flux obligatoire :

```
POST /Utilisateur/authentifier  →  accessToken + refreshToken
       ↓
Requêtes API avec Authorization: Bearer <accessToken>
       ↓
401 → POST /Utilisateur/refresh-token  →  nouveaux tokens
       ↓
Échec refresh → logout + écran login
POST /Utilisateur/deconnecter  (optionnel)
```

Détails complets : [MODULE_01_AUTH_ET_PERMISSIONS.md](MODULE_01_AUTH_ET_PERMISSIONS.md)

### 3.2 Permissions RBAC

Le login retourne `permissions: string[]` (ex. `Voyage.Read`, `Agent.Update`).

**Vue.js** — guard router :

```js
function hasPermission(userPermissions, required) {
  return required.every(p => userPermissions.includes(p));
}
```

**Flutter** — avant navigation :

```dart
bool canAccess(List<String> perms, String required) => perms.contains(required);
```

### 3.3 SignalR (notifications temps réel)

Hub : `/hubs/notifications?access_token={jwt}`

Événements utiles :
- `FlexPayPaymentConfirmed` / `FlexPayPaymentFailed` — paiement **transport** et **événement** (mêmes noms ; IDs et routes HTTP différents)
- Notifications in-app

Références :
- Hub générique : [`Documentation/SignalR-Integration.md`](../../SignalR-Integration.md), [`docs/SIGNALR_FRONTEND_GUIDE.md`](../../../docs/SIGNALR_FRONTEND_GUIDE.md)
- Billetterie événement (Vue + Flutter) : [INTEGRATION_SIGNALR_EVENEMENT_FLEXPAY.md](INTEGRATION_SIGNALR_EVENEMENT_FLEXPAY.md)

### 3.4 Stockage tokens

| Plateforme | Recommandation |
|------------|----------------|
| Vue.js web | `localStorage` (access + refresh) ; éviter cookies non sécurisés en prod |
| Flutter | `flutter_secure_storage` |

---

## 4. Snippets réutilisables

### 4.1 Vue.js — Client Axios

```js
// src/services/api.js
import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL + '/api',
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (r) => r,
  async (error) => {
    if (error.response?.status === 401) {
      const refresh = localStorage.getItem('refreshToken');
      if (refresh) {
        try {
          const { data } = await axios.post(
            `${import.meta.env.VITE_API_BASE_URL}/api/Utilisateur/refresh-token`,
            { refreshToken: refresh }
          );
          localStorage.setItem('accessToken', data.accessToken);
          localStorage.setItem('refreshToken', data.refreshToken);
          error.config.headers.Authorization = `Bearer ${data.accessToken}`;
          return api.request(error.config);
        } catch {
          localStorage.clear();
          window.location.href = '/login';
        }
      }
    }
    return Promise.reject(error);
  }
);

export default api;
```

### 4.2 Flutter — Client Dio

```dart
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

final _storage = FlutterSecureStorage();
late final Dio api;

void initApi(String baseUrl) {
  api = Dio(BaseOptions(
    baseUrl: '$baseUrl/api',
    headers: {'Content-Type': 'application/json'},
  ));

  api.interceptors.add(InterceptorsWrapper(
    onRequest: (options, handler) async {
      final token = await _storage.read(key: 'accessToken');
      if (token != null) {
        options.headers['Authorization'] = 'Bearer $token';
      }
      handler.next(options);
    },
    onError: (e, handler) async {
      if (e.response?.statusCode == 401) {
        final refresh = await _storage.read(key: 'refreshToken');
        if (refresh != null) {
          try {
            final resp = await Dio().post(
              '${api.options.baseUrl}/Utilisateur/refresh-token',
              data: {'refreshToken': refresh},
            );
            await _storage.write(key: 'accessToken', value: resp.data['accessToken']);
            await _storage.write(key: 'refreshToken', value: resp.data['refreshToken']);
            e.requestOptions.headers['Authorization'] =
                'Bearer ${resp.data['accessToken']}';
            handler.resolve(await api.fetch(e.requestOptions));
            return;
          } catch (_) {
            await _storage.deleteAll();
          }
        }
      }
      handler.next(e);
    },
  ));
}
```

### 4.3 Login (commun aux deux stacks)

**Request** :

```json
{
  "emailOuTelephone": "user@example.com",
  "motDePasse": "secret",
  "fcmToken": "optional",
  "deviceType": "web",
  "deviceModel": "Chrome",
  "osVersion": "1.0"
}
```

**Response** (extrait) :

```json
{
  "success": true,
  "accessToken": "<jwt>",
  "refreshToken": "<refresh>",
  "expiresIn": 86400,
  "permissions": ["Voyage.Read", "Reservation.Create"],
  "utilisateur": {
    "idUtilisateur": 12,
    "nomComplet": "Agent Test",
    "idSociete": 1,
    "idSite": 3,
    "idAgent": 9
  },
  "agent": { "idAgent": 9, "idSite": 3 }
}
```

---

## 5. Index des fiches modules

| # | Fiche | Personas | Description |
|---|-------|----------|-------------|
| 01 | [MODULE_01_AUTH_ET_PERMISSIONS.md](MODULE_01_AUTH_ET_PERMISSIONS.md) | Tous | Login, refresh, RBAC, guards |
| — | [INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md](INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md) | Client (Vue + Flutter) | Se connecter avec Google / Apple |
| 02 | [MODULE_02_TRANSPORT_VOYAGE.md](MODULE_02_TRANSPORT_VOYAGE.md) | Admin, Agent, Client | Voyages, destinations, véhicules, tarifs |
| 03 | [MODULE_03_RESERVATION_BILLET.md](MODULE_03_RESERVATION_BILLET.md) | Tous | Réservation, billets, scan QR, embarquement |
| 04 | [MODULE_04_PAIEMENT_FLEXPAY.md](MODULE_04_PAIEMENT_FLEXPAY.md) | Admin, Agent, Client | Cash, FlexPay, multi-devise, remboursement |
| 05 | [MODULE_05_EVENEMENT_BILLETTERIE.md](MODULE_05_EVENEMENT_BILLETTERIE.md) | Admin, Client, Gate | Billetterie `api/events/*` — Vue guichet + Flutter catalogue/FlexPay/contrôle entrée |
| — | [INTEGRATION_SIGNALR_EVENEMENT_FLEXPAY.md](INTEGRATION_SIGNALR_EVENEMENT_FLEXPAY.md) | Client, Guichet (Vue + Flutter) | SignalR FlexPay événement + poll secours |
| 10 | [MODULE_10_SITE_TOURISTIQUE.md](MODULE_10_SITE_TOURISTIQUE.md) | Admin, Client, Gate | Billetterie `api/sites-touristiques/*` — lieu + journée + planification, CASH/FlexPay, gate |
| — | [DOCUMENTATION_WORKFLOW_SITE_TOURISTIQUE_V1.md](../05_transport_sync/DOCUMENTATION_WORKFLOW_SITE_TOURISTIQUE_V1.md) | Tous | Workflow métier complet Site Touristique (config → vente → entrée) |
| 11 | [MODULE_11_RESTAURANT.md](MODULE_11_RESTAURANT.md) | Admin, Client | Réservation `api/restaurants/*` — établissement + créneau + zones, acompte CASH/FlexPay, dashboard |
| — | [DOCUMENTATION_WORKFLOW_RESTAURANT_V1.md](../05_transport_sync/DOCUMENTATION_WORKFLOW_RESTAURANT_V1.md) | Tous | Workflow métier complet Restaurant (config → vente acompte) |
| 06 | [MODULE_06_CLIENT_APP_VOYAGEUR.md](MODULE_06_CLIENT_APP_VOYAGEUR.md) | Client | Inscription, dashboard, plaintes |
| — | [INTEGRATION_VERIFICATION_EMAIL_VUE_FLUTTER.md](INTEGRATION_VERIFICATION_EMAIL_VUE_FLUTTER.md) | Client (Vue + Flutter) | Vérification email par lien à l’inscription |
| 07 | [MODULE_07_DASHBOARDS_ADMIN.md](MODULE_07_DASHBOARDS_ADMIN.md) | Admin (Vue) | KPIs, reporting, graphiques |
| 08 | [MODULE_08_SYNC_OFFLINE_AGENT.md](MODULE_08_SYNC_OFFLINE_AGENT.md) | Agent (Flutter) | Sync offline, batch paiements |
| 09 | [MODULE_09_REFERENTIELS_ET_COMMUNICATION.md](MODULE_09_REFERENTIELS_ET_COMMUNICATION.md) | Admin | Société, site, agent, campagnes |

### Catalogue endpoints complet

Pour la liste exhaustive de toutes les routes : [`DOCUMENTATION_API_ENDPOINTS_COMPLETE.md`](../01_demarrage/DOCUMENTATION_API_ENDPOINTS_COMPLETE.md)

### Archives détaillées

- [`DOCUMENTATION_INTEGRATION_FRONTENDS_VUE_FLUTTER.md`](DOCUMENTATION_INTEGRATION_FRONTENDS_VUE_FLUTTER.md) — version détaillée historique
- [`DOCUMENTATION_BACKEND_CONTRACT_FRONTENDS.md`](DOCUMENTATION_BACKEND_CONTRACT_FRONTENDS.md) — contrats payload détaillés
- [`INTEGRATION_FLUTTER_FLEXPAY.md`](INTEGRATION_FLUTTER_FLEXPAY.md) — FlexPay transport approfondi
- [`INTEGRATION_SIGNALR_EVENEMENT_FLEXPAY.md`](INTEGRATION_SIGNALR_EVENEMENT_FLEXPAY.md) — SignalR + poll FlexPay événement (Vue + Flutter)
- [`INTEGRATION_VUEJS.md`](INTEGRATION_VUEJS.md) — dashboards Vue détaillés
- [`INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md`](INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md) — login social Google / Apple (Vue + Flutter)
- [`INTEGRATION_VERIFICATION_EMAIL_VUE_FLUTTER.md`](INTEGRATION_VERIFICATION_EMAIL_VUE_FLUTTER.md) — vérification email inscription (Vue + Flutter)

---

## 6. Gestion des erreurs HTTP

| Code | Signification | Action frontend |
|------|---------------|-----------------|
| 200 | Succès | Traiter la réponse |
| 201 | Créé | Navigation ou toast succès |
| 400 | Requête invalide | Afficher `message` du corps JSON |
| 401 | Non authentifié | Refresh token ou redirect login |
| 403 | Permission refusée | Message « accès refusé » |
| 404 | Non trouvé | Vérifier ID / route |
| 409 | Conflit métier | Afficher `message` (ex. billet déjà utilisé) |
| 429 | Rate limit | Lire `retryAfter` (secondes), désactiver bouton |
| 500 | Erreur serveur | Message générique + log |

**Format erreur typique** :

```json
{
  "success": false,
  "message": "Description lisible pour l'utilisateur",
  "retryAfter": 900
}
```

---

## 7. Checklists d'intégration

### Vue.js — Back-office

- [ ] Client Axios avec intercepteur refresh
- [ ] Store utilisateur (Pinia) : token, permissions, idSociete, idSite
- [ ] Guards router par permission
- [ ] Format `heureDepart` en string `HH:mm:ss`
- [ ] Module voyage + réservation + paiement branchés
- [ ] Dashboards selon rôle (Gérant, Financier, Super-Admin)
- [ ] Gestion 403 avec message utilisateur
- [ ] Événements guichet : CASH / FlexPay selon [MODULE_05](MODULE_05_EVENEMENT_BILLETTERIE.md)
- [ ] Sites touristiques : planification + vente selon [MODULE_10](MODULE_10_SITE_TOURISTIQUE.md) / [workflow](../05_transport_sync/DOCUMENTATION_WORKFLOW_SITE_TOURISTIQUE_V1.md)
- [ ] Restaurants : créneaux + acompte selon [MODULE_11](MODULE_11_RESTAURANT.md) / [workflow](../05_transport_sync/DOCUMENTATION_WORKFLOW_RESTAURANT_V1.md)

### Flutter — Agent

- [ ] Dio + secure storage
- [ ] Scan QR → `GET /Billet/{qr}/check`
- [ ] Afficher `nomClient` au scan (= **passager réel**, pas acheteur)
- [ ] Afficher départ : `dateDepartVoyage` (ISO) + `heureDepartVoyage` (`HH:mm:ss`) — voir MODULE_03
- [ ] Embarquement `POST .../embarquer`
- [ ] Sync offline si terrain sans réseau
- [ ] FlexPay caisse si paiement électronique

### Flutter — Client voyageur

- [ ] Inscription avec header `X-Device-Id`
- [ ] Si email fourni : parcours vérif lien ([guide](INTEGRATION_VERIFICATION_EMAIL_VUE_FLUTTER.md))
- [ ] Login classique et/ou Google / Apple ([guide](INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md))
- [ ] Gestion 429 inscription (ne pas boucler)
- [ ] Recherche voyage + réservation multi-passagers
- [ ] FlexPay : POST → attente → SignalR ou polling `verifier`
- [ ] Affichage billets QR après paiement confirmé
- [ ] SignalR notifications paiement (transport + événement)
- [ ] Événements : achat `with-paiement-electronique` + [SignalR guide](INTEGRATION_SIGNALR_EVENEMENT_FLEXPAY.md) (permissions `Evenement.Hold.Create` + `Evenement.Reservation.Confirm`)
- [ ] Sites touristiques : [MODULE_10](MODULE_10_SITE_TOURISTIQUE.md) (`/sites-touristiques/flexpay/verifier`, `domain: siteTouristique`)
- [ ] Restaurants : [MODULE_11](MODULE_11_RESTAURANT.md) (`/restaurants/flexpay/verifier`, `domain: restaurant`)

---

## 8. Outils et références

| Outil | URL / Fichier |
|-------|---------------|
| Swagger UI | `https://localhost:7110/swagger` |
| Postman | `CongoTravel_API_Collection.postman_collection.json` (racine projet) |
| Index thématique | [`INDEX_DOCUMENTATION_THEMATIQUE.md`](../../INDEX_DOCUMENTATION_THEMATIQUE.md) |
| SignalR (hub générique) | [`Documentation/SignalR-Integration.md`](../../SignalR-Integration.md) |
| SignalR FlexPay événement | [INTEGRATION_SIGNALR_EVENEMENT_FLEXPAY.md](INTEGRATION_SIGNALR_EVENEMENT_FLEXPAY.md) |

---

## Historique

| Date | Version | Description |
|------|---------|-------------|
| 2026-07-28 | 1.1 | Conventions date/heure scan, tenancy events Client, checklists OAuth / vérif email / events |
| 2026-07-07 | 1.0 | Document maître initial — structure modulaire Vue + Flutter |
