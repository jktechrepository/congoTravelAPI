# API tickets événement — CongoTravelAPI V1

Base route : `api/events/tickets`

Complète les actions check/use existantes avec la lecture inspirée de `api/Billet`, adaptée au modèle événement.

## Permissions

| Action | Permission |
|--------|------------|
| Tous les GET (liste, détail) | `Evenement.Session.Read` |
| `GET /{ticketCode}/check` | `Evenement.Ticket.Check` |
| `POST /{ticketCode}/use` | `Evenement.Ticket.Use` |

Tenancy : société du JWT via la réservation liée ; Super-Admin peut passer `?idSociete=` sur la liste.

## Endpoints GET

### Liste (société JWT ou query Super-Admin)

```
GET /api/events/tickets?idSociete={optional}&status={optional}&idEvenementReservation={optional}&idEvenementSession={optional}
```

Retourne `EvenementTicketListItemDto[]` triés par `issuedAtUtc` desc.

`status` : `ISSUED`, `USED`, `VOID` (insensible à la casse).

### Liste par société (alias explicite)

```
GET /api/events/tickets/societe/{idSociete}
```

### Liste par réservation

```
GET /api/events/tickets/reservation/{idEvenementReservation}
```

### Liste société + réservation

```
GET /api/events/tickets/societe/{idSociete}/reservation/{idEvenementReservation}
```

`404` si la réservation est absente ou n'appartient pas à la société.

### Liste par session

```
GET /api/events/tickets/session/{idEvenementSession}
```

### Liste par statut

```
GET /api/events/tickets/status/{status}
```

### Détail par code ticket

```
GET /api/events/tickets/code/{ticketCode}
```

Équivalent transport de `GET /api/Billet/qrcode/{qrCode}`. Retourne `EvenementTicketDetailResponseDto`.

### Liste par date d'émission

```
GET /api/events/tickets/date/{date}
```

Filtre sur `issuedAtUtc` (jour UTC).

### Liste par plage de dates

```
GET /api/events/tickets/daterange?dateDebut={date}&dateFin={date}
```

`400` si `dateFin < dateDebut`.

### Détail par identifiant

```
GET /api/events/tickets/{id}
```

## Endpoints existants (contrôle entrée)

| Méthode | Route | Permission |
|---------|-------|------------|
| GET | `/{ticketCode}/check` | `Evenement.Ticket.Check` |
| POST | `/{ticketCode}/use` | `Evenement.Ticket.Use` |

## Mapping transport → événement

| Transport (`api/Billet`) | Événement (`api/events/tickets`) |
|--------------------------|----------------------------------|
| `GET /` | `GET /` |
| `GET /{id}` | `GET /{id}` |
| `GET /{qr}/check` | `GET /{ticketCode}/check` |
| `GET /reservation/{id}` | `GET /reservation/{idEvenementReservation}` |
| `GET /qrcode/{qr}` | `GET /code/{ticketCode}` |
| `GET /date/{date}` | `GET /date/{date}` |
| `GET /daterange` | `GET /daterange` |
| — | `GET /session/{idEvenementSession}` |
| — | `GET /status/{status}` |

Hors périmètre V1 : compteurs `/count`, pagination `POST .../paged`, embarquement, réaffectation.

## DTOs

- **Liste** : `EvenementTicketListItemDto` — ticket + `idEvenementReservation`, `referenceReservation`, `idEvenementSession`.
- **Détail** : `EvenementTicketDetailResponseDto` — ticket + contexte réservation et session.

## Exemple

```http
GET /api/events/tickets/reservation/42
Authorization: Bearer {token}
```

```http
GET /api/events/tickets/code/EVT-TKT-001-20260706120000-1234
Authorization: Bearer {token}
```
