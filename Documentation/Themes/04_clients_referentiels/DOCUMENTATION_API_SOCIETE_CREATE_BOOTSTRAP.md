# POST `/api/Societe` — Création société + site + gérant

## Vue d’ensemble

La création d’une société provisionne en une seule requête :

1. La société (`Societe`).
2. Les catégories siège par défaut (comportement existant du service).
3. Le type de véhicule par défaut **`Terrestre`** (`TypeVehicule`, actif).
4. Un **administrateur** automatique (agent « Manager général », `RoleAgent` Admin, utilisateur rôle **Admin**, email = `societe.emailContact` si disponible et unique) — **`IdSite`** = site principal créé à l’étape 5.
5. Un **site** initial (`Site`) pour cette société.
6. Un **gérant** : agent (`RoleAgent` Gerant, `Fonction` Gérant) + utilisateur rôle **Gerant**, tous deux avec `IdSite` renseigné — **créés automatiquement à partir des champs du bloc `site`** (comme `POST /api/Site`), sans bloc `gerant` dans le JSON.

En cas de conflit métier (voir ci‑dessous), l’API répond **`409 Conflict`** avec `{ "code": "<RaisonEnum>", "message": "..." }`.

Si **`site.email`** et **`site.telephone`** sont tous deux absents ou vides, l’API répond **`400 Bad Request`** avec `{ "message": "..." }`.

## Corps JSON (DTO imbriqué obligatoire)

Structure **`{ "societe", "site" }`** uniquement.

```json
{
  "societe": {
    "nom": "Ma Société",
    "devise": "Qualité",
    "type": "Privée",
    "telephone": "+243900000000",
    "emailContact": "contact@masociete.cd",
    "siteWeb": "https://example.cd",
    "nomCompletResponsable": "Patron Patron",
    "genreResponsable": "Masculin",
    "description": "…",
    "adresseResidence": "…",
    "statut": true
  },
  "site": {
    "codeSite": "MAIN",
    "nomSite": "Siège principal",
    "ville": "Kinshasa",
    "adresse": "…",
    "nomResponsableSite": "Jean Gérant",
    "genre": "Masculin",
    "telephone": "+243900000001",
    "email": "gerant@masociete.cd",
    "statut": true
  }
}
```

### Règle d’identifiant gérant (contact)

- Si **`site.email`** est renseigné → il sert d’identifiant (`Utilisateur.Email`, `Agent.EmailAgent`). Un email de bienvenue peut être planifié.
- Sinon → **`site.telephone`** doit être renseigné ; il sert d’identifiant à la place (pas d’email de bienvenue automatique).
- Le contact choisi doit être **distinct** de **`societe.emailContact`** (réservé au compte Admin auto).
- Mot de passe gérant par défaut : **`123456`**.
- Matricule gérant : **toujours généré** automatiquement (format habituel des agents).

## Réponse `201 Created` (extrait)

```json
{
  "societe": { "...": "..." },
  "site": {
    "id": 1,
    "code": "MAIN",
    "nom": "Siège principal",
    "idSociete": 1
  },
  "adminUser": {
    "email": "contact@masociete.cd",
    "telephone": "+243900000000",
    "motDePasse": "123456",
    "nomComplet": "…",
    "idSite": 1,
    "message": "…"
  },
  "gerantUser": {
    "email": "gerant@masociete.cd",
    "telephone": "+243900000001",
    "username": "…",
    "motDePasse": "123456",
    "nomComplet": "Jean Gérant",
    "idSite": 1,
    "idAgent": 2,
    "message": "Email de bienvenue envoyé automatiquement au gérant"
  }
}
```

Si l’identifiant gérant est le téléphone (pas d’email site), `gerantUser.email` contient ce numéro et `message` indique l’absence d’email de bienvenue automatique.

## Codes `409` (`code`)

| Valeur `code` | Signification |
|----------------|---------------|
| `SiteCodeAlreadyExists` | `codeSite` déjà utilisé pour cette société. |
| `GerantEmailAlreadyExists` | Contact gérant déjà présent sur un utilisateur (`Email`). |
| `GerantEmailSameAsSocieteContact` | Contact gérant = email de contact société (interdit). |
| `AgentGerantEmailAlreadyExists` | Contact déjà utilisé comme `EmailAgent`. |
| `SocieteContactEmailAlreadyUsed` | Email de contact déjà utilisé par un utilisateur (admin auto impossible). |

> **Note :** la raison historique `AgentGerantMatriculeAlreadyExists` n’est plus émise sur ce flux (plus de matricule manuel dans le payload).

## Fichiers principaux

- DTO : [`Models/DTOs/CreateSocieteBootstrapDtos.cs`](Models/DTOs/CreateSocieteBootstrapDtos.cs), [`Models/DTOs/SocieteBootstrapCreationResult.cs`](Models/DTOs/SocieteBootstrapCreationResult.cs)
- Exception : [`Models/SocieteBootstrapConflictException.cs`](Models/SocieteBootstrapConflictException.cs)
- Service : [`Services/SocieteService.cs`](Services/SocieteService.cs) — `CreateWithBootstrapAsync`
- Contrôleur : [`Controllers/SocieteController.cs`](Controllers/SocieteController.cs)

## Note compatibilité client

Le corps du **POST** doit être **`{ "societe", "site" }`**. Le bloc **`gerant`** n’est plus accepté : utiliser **`site.nomResponsableSite`**, **`site.genre`**, **`site.email`** / **`site.telephone`**.
