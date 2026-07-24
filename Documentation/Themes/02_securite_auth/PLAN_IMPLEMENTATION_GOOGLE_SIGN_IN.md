# Plan d’implémentation — Google Sign-In

**Statut :** implémenté (endpoint + services + migration)  
**Référence décisions :** Connexion Google — décisions figées (consolidation)

## Objectif

`POST` auth Google : vérifier l’ID token côté serveur → retrouver ou créer `Client` + `Utilisateur` → renvoyer `AuthentificationResponse` (JWT), comme `POST /api/Utilisateur/authentifier`.

## Endpoint

- **Route :** `POST /api/Utilisateur/auth/google` (`[AllowAnonymous]`)
- **Body :** `{ "idToken": "<Google ID token>" }`
- **200 :** [`AuthentificationResponse`](../../Models/AuthentificationResponse.cs) (`AccessToken`, `RefreshToken`, `Utilisateur`, permissions, etc.)
- **401** token invalide · **400** email manquant / non vérifié pour create-link · **403** compte inactif · **409** conflit métier rare

## 1. Modèle + migration

Sur [`Utilisateur`](../../Models/Utilisateur.cs) :

| Champ | Type | Notes |
|--------|------|--------|
| `AuthProvider` | `string?` max 32 | `null` / `Local` = classique ; `Google` une fois lié |
| `ExternalSubjectId` | `string?` max 128 | Google `sub` |
| `EmailVerified` | `bool?` | claim Google |

- Index unique `(AuthProvider, ExternalSubjectId)` (non null).
- Migration EF + script SQL prod idempotent.
- `MotDePasseHash` reste **Required** (pas de nullable en v1).

## 2. Configuration

```json
"GoogleAuth": {
  "ClientIds": [
    "xxx-android.apps.googleusercontent.com",
    "yyy-ios.apps.googleusercontent.com",
    "zzz-web.apps.googleusercontent.com"
  ]
}
```

Options DI `GoogleAuthOptions` ; audiences = liste des Client IDs.

## 3. Vérification token

- Package : `Google.Apis.Auth` → `GoogleJsonWebSignature.ValidateAsync`.
- Service `IGoogleTokenValidator` / `GoogleTokenValidator` :
  - signature, audience ∈ ClientIds, expiration ;
  - retourne `GoogleIdentity { Sub, Email, EmailVerified, Name, Picture }` ;
  - refuse create/link si email absent ou `EmailVerified != true`.

## 4. Orchestration — `IGoogleAuthService`

Ordre (anti-doublon) :

1. Valider ID token.
2. Find `Utilisateur` par `AuthProvider=Google` + `ExternalSubjectId=sub`.
3. Sinon find par email normalisé (Utilisateur, puis Client → user lié).
4. Trouvé sans lien Google + email verified → **link** (`AuthProvider`, `ExternalSubjectId`, `EmailVerified`) — **pas** de 2ᵉ Client.
5. Introuvable → créer `Client` (nom, email ; **téléphone null**) + utilisateur variante Google :
   - factoriser / étendre `CreateDefaultClientUserAsync` ;
   - hash = BCrypt(mot de passe **aléatoire fort**, non communiqué) ;
   - `DoitChangerMotDePasse = false` ;
   - rôle Client + société (mêmes règles que register).
6. Émettre JWT + refresh comme [`Authentifier`](../../Controllers/UtilisateurController.cs) (extraire helper commun si possible).
7. Retourner `AuthentificationResponse`.

## 5. Contrôleur

Méthode dans `UtilisateurController` près de `Authentifier`, ou petit contrôleur Auth dédié. Rate-limit léger si le pattern registration est réutilisable sans friction.

## 6. Tests

- Create → Client + User + champs Google + JWT.
- 2ᵉ appel même `sub` → même user, pas de 2ᵉ Client.
- Email local existant + token verified → link.
- Audience invalide → 401.
- Create sans téléphone OK.

## 7. Doc front

MODULE auth / matrice endpoints : body `idToken`, réponse = login, téléphone optionnel après auth (réservation / FlexPay).

## Hors scope v1

- Apple / table multi-providers.
- Refonte du MDP défaut `"123456"` du register classique.
- Forcer téléphone au Sign-In.
- Flux « définir un MDP » post-Google.

## Ordre de livraison

1. Migration + config `GoogleAuth`
2. Validator + `GoogleAuthService` + endpoint
3. Factorisation émission JWT (si duplication login)
4. Tests + doc

## Todos d’exécution

- [ ] Migration Utilisateur + config GoogleAuth
- [ ] IGoogleTokenValidator + GoogleAuthService (lookup/link/create + JWT)
- [ ] Endpoint POST auth/google + tests + doc MODULE auth
