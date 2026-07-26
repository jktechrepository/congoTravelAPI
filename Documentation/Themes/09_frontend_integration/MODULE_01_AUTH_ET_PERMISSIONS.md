# MODULE 01 — Authentification et permissions

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)  
> Login social (Vue + Flutter) : [INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md](INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md)

---

## Endpoints

| Méthode | Route | Auth | Description |
|---------|-------|------|-------------|
| POST | `/api/Utilisateur/authentifier` | Non | Login |
| POST | `/api/Utilisateur/auth/google` | Non | Login / inscription Google (même body de réponse que `authentifier`) |
| POST | `/api/Utilisateur/auth/apple` | Non | Login / inscription Apple (même body de réponse que `authentifier`) |
| POST | `/api/Utilisateur/refresh-token` | Non | Renouvellement JWT |
| POST | `/api/Utilisateur/deconnecter` | Oui | Logout |
| POST | `/api/Utilisateur/changer-mot-de-passe` | Oui | Changement MDP |
| GET | `/api/Permission` | Oui | Liste permissions (admin) |

---

## POST `/api/Utilisateur/authentifier`

### Request

```json
{
  "emailOuTelephone": "agent@congotravel.cd",
  "motDePasse": "secret",
  "fcmToken": "optional-fcm-token",
  "deviceType": "web",
  "deviceModel": "Chrome 124",
  "osVersion": "macOS 14"
}
```

`emailOuTelephone` accepte : email, username par défaut, ou téléphone.

### Response 200 (extrait)

```json
{
  "success": true,
  "message": "Authentification reussie",
  "accessToken": "<jwt>",
  "refreshToken": "<refresh>",
  "tokenType": "Bearer",
  "expiresIn": 86400,
  "expiresAt": "2026-05-09T08:00:00Z",
  "doitChangerMotDePasse": false,
  "nomRole": "Agent",
  "nomSociete": "CongoTravel",
  "permissions": ["Voyage.Read", "Reservation.Create", "Billet.Read"],
  "roles": [],
  "primaryRole": null,
  "utilisateur": {
    "idUtilisateur": 12,
    "nomComplet": "Agent Test",
    "email": "agent@congotravel.cd",
    "telephone": "+243900000",
    "idSociete": 1,
    "idSite": 3,
    "idAgent": 9,
    "idClient": null,
    "statut": true
  },
  "agent": {
    "idAgent": 9,
    "nomComplet": "Agent Test",
    "idSociete": 1,
    "idSite": 3,
    "roleAgent": "Caissier"
  },
  "client": null
}
```

### Notes frontend

- **`utilisateur.idSite`** : source canonique pour filtrer voyages / caisse par site.
- **`doitChangerMotDePasse`** : rediriger vers écran changement MDP si `true`.
- **`permissions`** : stocker en mémoire pour guards UI.
- Login **client** : `client` est renseigné, `agent` null.

### Erreurs

| Code | Cas |
|------|-----|
| 400 | Payload invalide |
| 401 | Identifiants incorrects / compte désactivé |
| 404 | Utilisateur introuvable après auth |
| 500 | Erreur serveur |

---

## POST `/api/Utilisateur/auth/google`

> Guide d’intégration Vue.js / Flutter : [INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md](INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md)

Connexion / première inscription via **ID token Google** (vérifié côté serveur).

### Request

```json
{ "idToken": "<Google ID token>" }
```

### Response 200

**Strictement le même contrat** que `POST /api/Utilisateur/authentifier` (`AuthentificationResponse`). Réutiliser le même handler de session côté front.

Différences de **valeurs** typiques :
- `doitChangerMotDePasse` : `false` (compte Google)
- `utilisateur.telephone` / `client.telephone` : souvent `null` au premier login (compléter plus tard)

Config API : `GoogleAuth:ClientIds` (audiences Android / iOS / Web).

### Erreurs

| Code | Cas |
|------|-----|
| 400 | Email manquant / non vérifié pour create-link |
| 401 | ID token invalide ou expiré |
| 403 | Compte désactivé |
| 409 | Conflit email / lien Google |

---

## POST `/api/Utilisateur/auth/apple`

> Guide d’intégration Vue.js / Flutter : [INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md](INTEGRATION_LOGIN_GOOGLE_APPLE_VUE_FLUTTER.md)

Connexion / première inscription via **identity token Apple** (JWT vérifié côté serveur via JWKS Apple).

### Request

```json
{ "idToken": "<Apple identity token>" }
```

### Response 200

**Même contrat** que `authentifier` / `auth/google`.

Particularités Apple :
- L’email peut être un relay `privaterelay.appleid.com` et n’est souvent envoyé **qu’à la première** connexion — le `sub` reste la clé stable.
- `doitChangerMotDePasse` : `false` ; téléphone souvent `null`.

Config API : `AppleAuth:ClientIds` = Services ID / Bundle IDs (claim `aud` du token).

Obtenir les IDs : [Apple Developer](https://developer.apple.com/) → Identifiers → **Services IDs** / App IDs avec Sign in with Apple.

### Erreurs

| Code | Cas |
|------|-----|
| 400 | Email manquant à la 1ʳᵉ connexion / non vérifié |
| 401 | Identity token invalide |
| 403 | Compte désactivé |
| 409 | Conflit email / lien |

---

## POST `/api/Utilisateur/refresh-token`

```json
{ "refreshToken": "<refresh>", "deviceInfo": "web-chrome" }
```

Réponse : même structure que login avec nouveaux tokens.

---

## POST `/api/Utilisateur/deconnecter`

Désactive le device courant ou tous les devices selon le payload.

---

## Permissions RBAC

Format : `Module.Action` (ex. `Voyage.Read`, `Evenement.Session.Write`).

### Vue.js — guard router

```js
// router/index.js
router.beforeEach((to, from, next) => {
  const perms = JSON.parse(localStorage.getItem('permissions') || '[]');
  if (to.meta.permissions && !to.meta.permissions.every(p => perms.includes(p))) {
    return next('/forbidden');
  }
  next();
});

// route
{ path: '/voyages', meta: { permissions: ['Voyage.Read'] } }
```

### Flutter — widget guard

```dart
class PermissionGate extends StatelessWidget {
  final String permission;
  final Widget child;
  const PermissionGate({required this.permission, required this.child});

  @override
  Widget build(BuildContext context) {
    final perms = context.watch<AuthProvider>().permissions;
    if (!perms.contains(permission)) return const SizedBox.shrink();
    return child;
  }
}
```

---

## Multi-rôles

Un utilisateur peut avoir plusieurs rôles. `permissions[]` est l'union agrégée. Utiliser `primaryRole` ou `nomRole` pour l'affichage UI principal.

---

## Références backend

- [`DOCUMENTATION_AUTHENTIFICATION.md`](../02_securite_auth/DOCUMENTATION_AUTHENTIFICATION.md)
- [`DOCUMENTATION_BACKEND_CONTRACT_FRONTENDS.md`](DOCUMENTATION_BACKEND_CONTRACT_FRONTENDS.md) §1
- [`SECURISATION_COMPLETE_JWT.md`](../02_securite_auth/SECURISATION_COMPLETE_JWT.md)
