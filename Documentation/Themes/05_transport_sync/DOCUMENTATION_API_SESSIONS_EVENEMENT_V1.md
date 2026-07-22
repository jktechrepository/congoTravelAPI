# API sessions événement — CongoTravelAPI V1

Base route : `api/events/sessions`

Complète les actions existantes (création, publication, disponibilité, holds) avec la lecture inspirée de `api/Voyage`.

## Permissions

| Action | Permission |
|--------|------------|
| Tous les GET (liste, détail, availability) | `Evenement.Session.Read` |
| Création, publication | `Evenement.Session.Write` |
| Holds | `Evenement.Hold.Create` |

Tenancy : société du JWT ; Super-Admin peut passer `?idSociete=` sur la liste.

## Endpoints GET

### Liste (société JWT ou query Super-Admin)

```
GET /api/events/sessions?idSociete={optional}&status={optional}&inventoryMode={optional}
```

Retourne `EvenementSessionListItemDto[]` triés par `startAtUtc` desc.

`status` : `Draft`, `Published`, `Closed`, `Cancelled` (insensible à la casse).

`inventoryMode` : `GlobalQuota`, `ClassQuota`, `SeatNumbered`.

### Liste par société (alias explicite)

```
GET /api/events/sessions/societe/{idSociete}
```

### Liste par statut

```
GET /api/events/sessions/status/{status}
```

### Liste par mode d'inventaire

```
GET /api/events/sessions/inventory-mode/{inventoryMode}
```

### Détail par code session

```
GET /api/events/sessions/code/{codeSession}
```

Correspondance exacte sur `codeSession` (unique par société). Retourne `EvenementSessionResponseDto` complet (inventaire inclus).

### Liste par date de début

```
GET /api/events/sessions/date/{date}
```

Filtre sur `startAtUtc` (jour UTC).

### Liste par plage de dates

```
GET /api/events/sessions/daterange?dateDebut={date}&dateFin={date}
```

`400` si `dateFin < dateDebut`.

### Détail par identifiant

```
GET /api/events/sessions/{id}
```

### Disponibilité inventaire

```
GET /api/events/sessions/{id}/availability
```

## Endpoints POST/PUT (existants)

| Méthode | Route | Permission |
|---------|-------|------------|
| POST | `/` | `Evenement.Session.Write` |
| PUT | `/{id}/publish` | `Evenement.Session.Write` |
| POST | `/{id}/holds` | `Evenement.Hold.Create` |

## Mapping transport → événement

| Transport (`api/Voyage`) | Événement (`api/events/sessions`) |
|--------------------------|-----------------------------------|
| `GET /` | `GET /` |
| `GET /{id}` | `GET /{id}` |
| `GET /societe/{idSociete}` | `GET /societe/{idSociete}` |
| `GET /date/{date}` | `GET /date/{date}` |
| `GET /daterange` | `GET /daterange` |
| `GET /statut/{statut}` | `GET /status/{status}` |
| — | `GET /code/{codeSession}` |
| — | `GET /inventory-mode/{inventoryMode}` |
| `GET /{id}/sieges-disponibles` | `GET /{id}/availability` |

Hors périmètre V1 : pagination `paged`, compteurs `/count`, filtres site/véhicule/destination.

## DTOs

- **Liste** : `EvenementSessionListItemDto` — en-tête sans quotas / sièges.
- **Détail** : `EvenementSessionResponseDto` — inventaire complet selon `inventoryMode`.

## Exemple

```http
GET /api/events/sessions?status=Published&inventoryMode=GlobalQuota
Authorization: Bearer {token}
```

```http
GET /api/events/sessions/code/GALA-2026
Authorization: Bearer {token}
```
