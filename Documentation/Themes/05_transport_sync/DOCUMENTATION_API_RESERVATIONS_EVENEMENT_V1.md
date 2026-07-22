# API réservations événement — CongoTravelAPI V1

Base route : `api/events/reservations`

Complète les actions POST existantes (confirm-payment, initiate-flexpay, cancel) avec la lecture inspirée de `api/Reservation`, adaptée au modèle événement.

## Permissions

| Action | Permission |
|--------|------------|
| Tous les GET | `Evenement.Session.Read` |
| confirm-payment, initiate-flexpay, cancel | `Evenement.Reservation.Confirm` |

Tenancy : société du JWT ; Super-Admin peut passer `?idSociete=` sur la liste.

## Endpoints GET

### Liste (société JWT ou query Super-Admin)

```
GET /api/events/reservations?idSociete={optional}&status={optional}&idEvenementSession={optional}&customerRef={optional}
```

Retourne `EvenementReservationListItemDto[]` triés par `dateCreation` desc.

`status` : `HOLD`, `CONFIRMED`, `CANCELLED`, `EXPIRED` (insensible à la casse).

### Liste par société (alias explicite)

```
GET /api/events/reservations/societe/{idSociete}
```

### Liste par session

```
GET /api/events/reservations/session/{idEvenementSession}
```

### Liste société + session

```
GET /api/events/reservations/societe/{idSociete}/session/{idEvenementSession}
```

`404` si la session est absente ou n'appartient pas à la société.

### Liste par statut

```
GET /api/events/reservations/status/{status}
```

### Détail par référence

```
GET /api/events/reservations/reference/{reference}
```

Correspondance exacte sur `referenceReservation` (unique par société). Retourne `EvenementReservationResponseDto` complet.

### Liste par date

```
GET /api/events/reservations/date/{date}
```

Filtre sur `dateCreation` (jour UTC).

### Liste par plage de dates

```
GET /api/events/reservations/daterange?dateDebut={date}&dateFin={date}
```

`400` si `dateFin < dateDebut`.

### Détail par identifiant

```
GET /api/events/reservations/{id}
```

Retourne lignes, tickets et paiements (`EvenementReservationResponseDto`).

### Tickets d'une réservation

```
GET /api/events/reservations/{id}/tickets
```

Équivalent transport de `GET /api/Reservation/{id}/billets`.

## Endpoints POST (existants)

| Méthode | Route | Permission |
|---------|-------|------------|
| POST | `/{id}/confirm-payment` | `Evenement.Reservation.Confirm` |
| POST | `/{id}/initiate-flexpay` | `Evenement.Reservation.Confirm` |
| POST | `/{id}/cancel` | `Evenement.Reservation.Confirm` |

La création de hold reste sur `POST /api/events/sessions/{id}/holds` (`Evenement.Hold.Create`).

## Mapping transport → événement

| Transport (`api/Reservation`) | Événement (`api/events/reservations`) |
|-------------------------------|---------------------------------------|
| `GET /` | `GET /` |
| `GET /{id}` | `GET /{id}` |
| `GET /Societe/{idSociete}` | `GET /societe/{idSociete}` |
| `GET /voyage/{idVoyage}` | `GET /session/{idEvenementSession}` |
| `GET /Societe/{id}/voyage/{idVoyage}` | `GET /societe/{id}/session/{idSession}` |
| `GET /statutreservation/{statut}` | `GET /status/{status}` |
| `GET /date/{date}` | `GET /date/{date}` |
| `GET /daterange` | `GET /daterange` |
| `GET /{id}/billets` | `GET /{id}/tickets` |
| — | `GET /reference/{reference}` |

Hors périmètre V1 : filtres `utilisateur`, `client`, compteurs `/count`, pagination `POST .../paged` (pas de `IdUtilisateur` / `IdClient` sur `EvenementReservation`).

## DTOs

- **Liste** : `EvenementReservationListItemDto` — en-tête sans collections imbriquées.
- **Détail** : `EvenementReservationResponseDto` — lignes, tickets, paiements.

## Exemple

```http
GET /api/events/reservations?status=HOLD&idEvenementSession=12
Authorization: Bearer {token}
```

```http
GET /api/events/reservations/42
Authorization: Bearer {token}
```
