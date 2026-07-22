# Analyse des failles de sécurité — CongoTravelAPI

**Date :** mai 2026  
**Périmètre :** authentification JWT, RBAC, multi-tenant, FlexPay, endpoints publics, secrets, QR billets  
**Contexte :** analyse statique du code (revue manuelle, sans pentest externe)

---

## Synthèse exécutive

CongoTravelAPI dispose de **fondations solides** (BCrypt, RBAC seedé, rate limit partiel, audit FlexPay, CORS prod configurable). En revanche, plusieurs failles **critiques** subsistent sur l’**isolation multi-tenant** et l’**application réelle du RBAC** sur le cœur métier (réservations, paiements, billets).

| Niveau    | Nb | Exemples |
|-----------|----|----------|
| Critique  | 4  | IDOR inter-sociétés, callbacks FlexPay, RBAC absent sur le cœur métier |
| Élevé     | 5  | JWT faible, QR codes prévisibles, secrets par défaut dans le code |
| Moyen     | 6  | Swagger prod, rate limiting incomplet, fuite d’infos via métriques |
| Faible    | 3  | Debug logs, endpoints de test, CORS dev |

---

## Forces existantes (à conserver)

- **Mots de passe** : vérification BCrypt à l’authentification (`UtilisateurController.Authentifier`)
- **Inscription client** : rate limit dédié (`ClientRegistrationRateLimitAttribute` — 3 req / 10 min)
- **Upload photos véhicule** : validation taille (1 Mo) et type (`VehiculePhotoBase64Helper`)
- **SignalR** : hub `NotificationHub` protégé par `[Authorize]`
- **FlexPay** : audit des callbacks (payload, IP, headers) + idempotence métier
- **CORS production** : origines configurables via `Cors:AllowedOrigins`
- **Amélioration récente** : filtrage `IdSociete` sur `GET` listes Reservation / Paiement / Billet (`TenantGuard`, `GetAllBySocieteAsync`)

---

## Failles critiques

### C1 — IDOR multi-tenant (accès inter-sociétés)

**Description :** un utilisateur authentifié peut accéder à des ressources d’une autre société en devinant ou en incrémentant les identifiants.

**Endpoints concernés (exemples) :**

| Endpoint | Problème |
|----------|----------|
| `GET /api/Paiement/{id}` | Pas de vérification `paiement.IdSociete == jwt.SocieteId` |
| `GET /api/Billet/{id}` | Idem |
| `GET /api/Reservation/{id}` | Idem |
| `GET /api/Reservation/Societe/{idSociete}` | Aucun `TenantGuard` — liste complète réservations + passagers |
| `GET /api/Reservation/Societe/{idSociete}/voyage/{idVoyage}` | Idem |

**Fichiers clés :**

- `Controllers/ReservationController.cs` — routes `/Societe/{idSociete}`
- `Controllers/PaiementController.cs` — `GetById`
- `Controllers/BilletController.cs` — `GetById`
- `Helpers/TenantGuard.cs` — présent mais appliqué uniquement sur certaines listes

**Impact :** fuite de données personnelles (clients, passagers, montants, QR billets) entre sociétés (violation multi-tenant).

**Recommandation :**

1. Appeler `TenantGuard.EnsureRouteSocieteMatchesJwt()` sur **toutes** les routes avec `idSociete` dans l’URL.
2. Sur chaque `GetById`, vérifier `entity.IdSociete == _currentUserService.SocieteId` (sauf SuperAdmin).
3. Introduire un middleware ou filtre global `ITenantContext` à terme.

---

### C2 — RBAC incomplet sur le cœur métier

**Description :** `ReservationController`, `PaiementController` et `BilletController` n’utilisent **aucun** attribut `[Permission(...)]` — seulement `[Authorize]`.

**Conséquence :** tout utilisateur disposant d’un JWT valide (y compris rôle **Client**) peut théoriquement :

- créer / modifier des réservations et paiements ;
- réaffecter des billets (`POST .../reaffecter`) ;
- enregistrer des embarquements ;
- consulter des billets via `GET /api/Billet/{QrCode}/check`.

Le système RBAC existe (`Attributes/PermissionAttribute.cs`, permissions seedées via `PermissionSeeder`) mais n’est appliqué que sur une **minorité** de controllers (Sync, Societe, Metrics, Init, etc.).

**Impact :** élévation de privilèges horizontale et verticale selon le rôle du token.

**Recommandation :** ajouter des permissions granulaires, par exemple :

- `Reservation.Read`, `Reservation.Create`, `Reservation.Update`
- `Paiement.Read`, `Paiement.Create`
- `Billet.Read`, `Billet.Embarquer`, `Billet.Reaffecter`, `Billet.Check`

---

### C3 — Callbacks FlexPay publics sans authentification forte

**Description :** les endpoints callback sont `[AllowAnonymous]` :

- `POST /api/FlexPay/callback`
- `POST /api/FlexPay/payout/callback`

Le traitement dans `FlexPayCallbackService.ProcessCallbackAsync` repose sur `OrderNumber` / `Reference` et l’idempotence. **Aucune vérification** de signature HMAC, token partagé ou whitelist IP n’est visible dans le code.

**Impact :** un attaquant pourrait tenter d’envoyer un callback forgé (`code=0`) pour finaliser une commande en attente s’il obtient ou devine un `OrderNumber`.

**Recommandation :**

1. Whitelist IP des serveurs FlexPay (ou validation réseau au reverse proxy).
2. Vérification signature / secret partagé si l’API FlexPay le supporte.
3. Rate limit strict sur les routes callback.
4. Ne jamais finaliser une commande sans recoupement montant + statut côté API FlexPay (`VerifierStatutTransactionAsync`).

---

### C4 — Modèle `Client` global (sans `IdSociete`)

**Description :** l’entité `Client` n’a pas de colonne `IdSociete`. L’isolation repose uniquement sur le contexte applicatif (réservations, paiements).

**Impact :** en cas d’IDOR ou de requête mal filtrée, un client « partagé » peut être visible ou manipulé par plusieurs opérateurs.

**Recommandation :** décision produit explicite :

- **Option A :** ajouter `IdSociete` sur `Client` + migration ;
- **Option B :** documenter le modèle « client global » et renforcer tous les points d’accès (sync, export, recherche).

---

## Failles élevées

### E1 — Configuration JWT fragile

**Fichier :** `Program.cs`

| Paramètre | État actuel | Risque |
|-----------|-------------|--------|
| `ValidateIssuer` | `false` | Rejeu cross-environnement si même secret |
| `ValidateAudience` | `false` | Idem |
| `RequireHttpsMetadata` | `false` | MITM si TLS mal configuré |
| Secret par défaut | Chaîne en dur si config absente | Forge de tokens si prod mal configurée |

**Fichiers :** `Program.cs`, `Services/SimpleJwtService.cs`

**Recommandation :**

- Secret ≥ 256 bits via vault / variables d’environnement (jamais en code).
- Activer `ValidateIssuer` et `ValidateAudience`.
- Supprimer les fallbacks `"CongoTravel-SecretKey-..."` et `"Kenergie_SecretKey_..."`.

---

### E2 — QR codes billets : entropie faible

**Format :** `RT-{societe}-{yyyyMMddHHmmss}-{1000-9999}`  
**Génération :** `System.Random()` (non cryptographique) — voir `Services/QrCodeService.cs`.

**Surface d’attaque :** `GET /api/Billet/{QrCode}/check` requiert seulement `[Authorize]`, pas de permission dédiée → énumération possible (~9 000 valeurs par seconde et par société).

**Recommandation :**

- Utiliser `RandomNumberGenerator` (crypto).
- Suffixe aléatoire ≥ 128 bits (ou UUID).
- Rate limit agressif sur `/check`.
- Restreindre à la permission `Billet.Check` / rôle embarquement.

---

### E3 — `PermissionAttribute` synchrone

**Fichier :** `Attributes/PermissionAttribute.cs`

```csharp
permissionService.UserHasPermissionAsync(...).GetAwaiter().GetResult();
```

**Risque :** blocage thread pool sous charge, deadlock potentiel avec certains providers EF/async.

**Recommandation :** migrer vers `IAsyncAuthorizationFilter` (priorité P3 du plan architectural).

---

### E4 — Endpoints publics métier (scraping)

**AllowAnonymous** sur de nombreuses routes `VoyageController` (catalogue voyages paginé). Choix produit acceptable pour un site de réservation public, mais facilite le scraping (tarifs, fréquences, structure réseau).

**Recommandation :** rate limit + captcha optionnel sur les endpoints les plus sollicités ; documenter le contrat « public » vs « staff ».

---

### E5 — FlexPay `GET /api/FlexPay/verifier/{orderNumber}`

Protégé par `[Authorize]` mais **sans contrôle** que l’utilisateur est lié à la commande. Tout token valide peut déclencher une vérification/finalisation sur un `orderNumber` connu.

**Recommandation :** lier `orderNumber` à l’utilisateur / société / session de commande en attente.

---

## Failles moyennes

### M1 — Swagger ouvert en production

`Program.cs` active Swagger dans **tous** les environnements (`/swagger`). Élargit la surface d’attaque (énumération endpoints, schémas DTO).

**Recommandation :** `if (IsDevelopment())` ou authentification basique sur `/swagger` en prod.

---

### M2 — Métriques exposées anonymement

`GET /api/Metrics/health` — `[AllowAnonymous]` — retourne statut, uptime, environnement.

**Recommandation :** réserver aux endpoints `/health/ready` (déjà en place) ou protéger Metrics par permission / réseau interne.

---

### M3 — Rate limiting probablement incomplet

`AspNetCoreRateLimit` est câblé dans `Program.cs`, mais la section `IpRateLimiting` peut être absente de la configuration active → règles vides par défaut.

**Endpoints sensibles à protéger :**

- `POST /api/Utilisateur/authentifier`
- `POST /api/Utilisateur/mot-de-passe-oublie`
- `POST /api/FlexPay/callback`
- `GET /api/Billet/{QrCode}/check`

**Recommandation :** reprendre la config de `appsettings.json.backup` (section `IpRateLimiting`) et l’activer en prod.

---

### M4 — JWT SignalR via query string

Le token JWT est accepté via `?access_token=` pour les hubs SignalR (`Program.cs`). Risque de fuite via logs proxy, historique, Referer.

**Recommandation :** préférer l’en-tête `Authorization` ; si query string obligatoire, durée de vie courte + logs sans token.

---

### M5 — Secrets et fallbacks code

- `appsettings.json` est **gitignoré** (correct).
- Fallbacks secrets dans `Program.cs` et `SimpleJwtService` restent dangereux si déploiement sans config.
- `Console.WriteLine` de debug dans `SimpleJwtService.GenerateToken` (fuite d’infos en logs).

**Recommandation :** échouer au démarrage si `Jwt:SecretKey` absent en production ; supprimer les logs debug.

---

### M6 — Endpoints de test en production

- `AuthTestController` — `/api/AuthTest/public` sans authentification.
- `FlexPayController` — `/approve`, `/cancel`, `/decline` en AllowAnonymous (pages retour utilisateur — faible risque métier).

**Recommandation :** désactiver ou restreindre `AuthTestController` hors Development.

---

## Scénarios d’attaque probables

| # | Scénario | Prérequis | Impact |
|---|----------|-----------|--------|
| 1 | Agent société A lit `GET /api/Reservation/Societe/2` | JWT valide (n’importe quel rôle) | Fuite PII société B |
| 2 | Client authentifié crée un paiement ou réaffecte un billet | JWT Client + pas de `[Permission]` | Fraude / manipulation |
| 3 | Callback FlexPay forgé | OrderNumber intercepté | Finalisation paiement non légitime |
| 4 | Brute-force QR billet | JWT + endpoint `/check` | Contrefaçon / usurpation embarquement |

---

## Plan de remédiation priorisé

| Priorité | Action | Effort | Impact |
|----------|--------|--------|--------|
| P0 | IDOR : `TenantGuard` sur toutes routes `{id}` et `{idSociete}` | Moyen | Très haut |
| P0 | `[Permission]` sur Reservation / Paiement / Billet | Moyen | Très haut |
| P0 | Sécuriser callbacks FlexPay (IP + signature + rate limit) | Moyen | Très haut |
| P1 | Renforcer JWT (issuer, audience, secret vault, supprimer fallbacks) | Faible | Haut |
| P1 | QR codes crypto + rate limit `/check` + permission dédiée | Faible | Haut |
| P2 | Désactiver Swagger / AuthTest en prod | Faible | Moyen |
| P2 | Configurer `IpRateLimiting` (login, callback, reset pwd) | Faible | Moyen |
| P3 | `IAsyncAuthorizationFilter` pour `PermissionAttribute` | Faible | Moyen |
| P3 | Décision Client multi-tenant (IdSociete ou modèle documenté) | Élevé | Haut |

---

## Références code

| Sujet | Fichier |
|-------|---------|
| JWT / auth pipeline | `Program.cs` |
| Génération token | `Services/SimpleJwtService.cs` |
| Permissions RBAC | `Attributes/PermissionAttribute.cs`, `Data/PermissionSeeder.cs` |
| Contexte utilisateur | `Services/CurrentUserService.cs` |
| Garde tenant | `Helpers/TenantGuard.cs`, `Helpers/TenantQueryExtensions.cs` |
| Callbacks FlexPay | `Controllers/FlexPayController.cs`, `Services/FlexPayCallbackService.cs` |
| QR codes | `Services/QrCodeService.cs` |
| Rate limit inscription | `Models/DTOs/Client/RateLimitAttribute.cs` |

---

## Documents liés

- [PLAN_TRAVAIL_SECURITE.md](./PLAN_TRAVAIL_SECURITE.md) — plan de travail par vagues (implémentation à planifier)
- [DOCUMENTATION_AUTHENTIFICATION.md](./DOCUMENTATION_AUTHENTIFICATION.md)
- [SECURISATION_COMPLETE_JWT.md](./SECURISATION_COMPLETE_JWT.md)
- [GUIDE_TEST_AUTO_BEARER.md](./GUIDE_TEST_AUTO_BEARER.md)
- [../11_analyses_plans/ANALYSE_EXPERT_SYSTEME_KENERGIE.md](../11_analyses_plans/ANALYSE_EXPERT_SYSTEME_KENERGIE.md)

---

*Document généré dans le cadre de l’analyse architecturale CongoTravelAPI — à mettre à jour après chaque vague de correctifs sécurité.*
