# MODULE 13 — Photos & stockage AWS S3 (V1)

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)  
> Guide pratique Vue + Flutter : [`INTEGRATION_PHOTOS_S3_VUE_FLUTTER.md`](INTEGRATION_PHOTOS_S3_VUE_FLUTTER.md)  
> Domaines concernés : [MODULE_02](MODULE_02_TRANSPORT_VOYAGE.md) (véhicule) · [MODULE_05](MODULE_05_EVENEMENT_BILLETTERIE.md) · [MODULE_10](MODULE_10_SITE_TOURISTIQUE.md) · [MODULE_11](MODULE_11_RESTAURANT.md)

---

## Objectif

Les photos sont stockées en **binaire sur AWS S3** côté serveur (`StorageKey`). Le front **ne parle pas à S3** (pas d’URL présignée client).

Flux cible :

1. Créer l’entité parent en JSON **sans** `photos` (ou `photos: null`).
2. Attacher 0–3 images via **multipart** (`POST` unitaire ou `PUT` replace-all).
3. Afficher via **`photoUrl`** → `GET .../photos/{photoId}/content` (stream JPEG/PNG).

`photoBase64` / `photos[]` embarqués à la création restent acceptés mais **dépréciés** (compat).

---

## Bases d’URL (4 domaines)

| Domaine | Base parent |
|---------|-------------|
| Véhicule | `/api/Vehicule/{id}` |
| Événement (session) | `/api/events/sessions/{id}` |
| Restaurant | `/api/restaurants/etablissements/{id}` |
| Site touristique | `/api/sites-touristiques/lieux/{id}` |

Sous chaque base :

| Méthode | Route relative | Rôle |
|---------|----------------|------|
| GET | `/photos?includePhotoBase64=false` | Liste métadonnées (+ `photoUrl`) |
| GET | `/photos/{photoId}/content` | Stream image (JPEG/PNG) |
| POST | `/photos` | Ajout 1 photo (`application/json` **ou** `multipart/form-data`) |
| PUT | `/photos` | Remplacement complet galerie (`multipart` uniquement) |
| PUT | `/photos/{photoId}/ordre` | Changer l’ordre 1–3 |
| DELETE | `/photos/{photoId}` | Supprimer une photo |

Exemples `photoUrl` (chemins relatifs API) :

- `/api/Vehicule/12/photos/5/content`
- `/api/events/sessions/3/photos/9/content`
- `/api/restaurants/etablissements/4/photos/2/content`
- `/api/sites-touristiques/lieux/7/photos/1/content`

Préfixer avec la base URL d’env (`https://localhost:7110` ou prod).

---

## Auth / permissions

| Domaine | GET list + `/content` | Writes (POST / PUT / DELETE) |
|---------|----------------------|------------------------------|
| **Véhicule** | JWT (`[Authorize]`) | JWT (pas de permission nommée) |
| **Événement** | `[AllowAnonymous]` | `Evenement.Session.Write` + JWT |
| **Restaurant** | `[AllowAnonymous]` | `Restaurant.Etablissement.Write` + JWT |
| **Site** | `[AllowAnonymous]` | `SiteTouristique.Lieu.Write` + JWT |

Cache stream : `Cache-Control: private, max-age=300`.

---

## Contraintes communes

| Règle | Valeur |
|-------|--------|
| Max photos / parent | **3** |
| Formats | **JPG / JPEG / PNG** uniquement |
| Taille max | **1 Mo** par fichier (`MaxImageBytes`) |
| Ordre | Entier **1–3** |

---

## Lecture

### Liste

`GET .../photos?includePhotoBase64=false` (défaut recommandé)

Champs utiles (camelCase JSON) :

| Champ | Rôle |
|-------|------|
| `idPhoto*` / ids parent | Identifiants (noms varient par domaine) |
| **`photoUrl`** | URL relative à utiliser pour l’affichage |
| `photoBase64` | **Vide** par défaut ; rempli seulement si `includePhotoBase64=true` |
| `ordre` | 1–3 |
| `originalFileName`, `typeMIME`, `fileSize` | Métadonnées |
| `statut`, dates | Cycle de vie |

**Ne pas** demander `includePhotoBase64=true` en listes catalogue (payload lourd).

### Stream

`GET .../photos/{photoId}/content` → bytes image + `Content-Type` `image/jpeg` ou `image/png`.

- Événement / restaurant / site : souvent accessible sans JWT.
- Véhicule : envoyer `Authorization: Bearer …` (ex. header sur `<img>` via blob fetch, ou `Image.network` + headers Dio).

Les listes / détails parent exposent aussi `photoCouverture` / `photos[]` / `photosVehicules` avec le même contrat (`photoUrl` préféré).

---

## Écriture canonique — multipart

### POST — une photo

`Content-Type: multipart/form-data`

| Champ form | Requis | Notes |
|------------|--------|--------|
| `file` | oui | Fichier image |
| `ordre` | non | 1–3 ; sinon 1re place libre |
| `fileName` | non | Suggestion d’extension / nom |

### PUT — replace-all

`Content-Type: multipart/form-data`

| Champ form | Requis | Notes |
|------------|--------|--------|
| `files` | non (0–3) | **0 fichier = vider la galerie** |
| `ordres` | non | Liste parallèle d’entiers 1–3 ; sinon 1..n |

Sémantique = remplacement complet (anciennes clés S3 nettoyées côté serveur).

Swagger documente le **multipart** en cas de conflit avec le POST JSON (même path).

---

## Écriture legacy (dépréciée, toujours live)

### POST JSON

```json
{
  "photoBase64": "data:image/jpeg;base64,...",
  "ordre": 1,
  "fileName": "cover.jpg"
}
```

Aliases éventuels : `photo`, `filePath`, `image`, `base64`.

### Create / update parent avec `photos[]`

Toujours accepté sur create véhicule / session / établissement / lieu (et update véhicule) mais marqué **LEGACY** dans les DTO.  
**Préférer** : create sans photos → `POST` / `PUT` multipart.

---

## Ordre & suppression

```http
PUT .../photos/{photoId}/ordre
Content-Type: application/json

{ "ordre": 2 }
```

```http
DELETE .../photos/{photoId}
```

---

## Codes HTTP (écritures / lecture)

| Code | Cas |
|------|-----|
| 200 / 201 | Succès |
| 400 | > 3 fichiers, type non JPG/PNG, > 1 Mo, ordre invalide, galerie pleine (POST) |
| 401 | JWT manquant (écritures ; lecture véhicule) |
| 403 | Permission domaine manquante |
| 404 | Parent ou photo introuvable |
| 409 | Conflit métier éventuel (ex. ordre déjà pris selon domaine) |

---

## Hors scope front

- Upload direct S3 (presigned PUT)
- Admin backfill : `POST /api/admin/photo-storage/backfill` (Admin / Super-Admin)
- Configuration credentials AWS (serveur uniquement)

---

## Guide d’intégration pratique

Snippets Vue / Flutter, affichage authentifié, replace-all, checklist QA :

→ [`INTEGRATION_PHOTOS_S3_VUE_FLUTTER.md`](INTEGRATION_PHOTOS_S3_VUE_FLUTTER.md)
