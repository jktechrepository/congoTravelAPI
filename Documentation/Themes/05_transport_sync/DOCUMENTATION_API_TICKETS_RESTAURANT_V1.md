# API tickets restaurant — CongoTravelAPI V1

Base route : `api/restaurants/tickets`

Miroir de [`DOCUMENTATION_API_TICKETS_EVENEMENT_V1.md`](DOCUMENTATION_API_TICKETS_EVENEMENT_V1.md), adapté au créneau restaurant.

Intégration Vue / Flutter : [MODULE_11_RESTAURANT.md](../09_frontend_integration/MODULE_11_RESTAURANT.md) · [CHANGELOG 15 août 2026](../09_frontend_integration/CHANGELOG_2026-08-15_RESTAURANT_ET_SITE_TOURISTIQUE.md)

## Émission

- À la confirmation CASH / FlexPay (`ConfirmHoldAndEmitTicketsAsync`)
- **1 ticket** par unité de `Quantite` sur chaque `RestaurantReservationLine`
- Préfixe code : `REST-TKT-{idSociete}-{yyyyMMddHHmmss}-{rand4}`
- Statuts : `ISSUED` | `USED` | `VOID`

## Fenêtre d’entrée

`[Creneau.StartAtUtc − heuresAvant, Creneau.EndAtUtc]`

`heuresAvant` = `ConfigSocietes.HeuresOuvertureEntreeRestaurantAvantDebut` (défaut **1**, clamp 0–72).  
Champ **distinct** de `HeuresOuvertureEntreeEvenementAvantDebut`.

## Permissions

| Action | Permission |
|--------|------------|
| Tous les GET (liste, détail) | `Restaurant.Etablissement.Read` |
| `GET /{ticketCode}/check` | `Restaurant.Ticket.Check` |
| `POST /{ticketCode}/use` | `Restaurant.Ticket.Use` |

Tenancy : société du JWT via la réservation liée ; Super-Admin peut passer `?idSociete=` sur la liste.

## Endpoints GET

### Liste (société JWT ou query Super-Admin)

```
GET /api/restaurants/tickets?idSociete={optional}&status={optional}&idRestaurantReservation={optional}&idRestaurantCreneau={optional}
```

Retourne `RestaurantTicketListItemDto[]` triés par `issuedAtUtc` desc.

`status` : `ISSUED`, `USED`, `VOID` (insensible à la casse).

### Liste par société (alias explicite)

```
GET /api/restaurants/tickets/societe/{idSociete}
```

### Liste par réservation

```
GET /api/restaurants/tickets/reservation/{idRestaurantReservation}
```

### Liste société + réservation

```
GET /api/restaurants/tickets/societe/{idSociete}/reservation/{idRestaurantReservation}
```

`404` si la réservation est absente ou n'appartient pas à la société.

### Liste par créneau

```
GET /api/restaurants/tickets/creneau/{idRestaurantCreneau}
```

### Liste par statut

```
GET /api/restaurants/tickets/status/{status}
```

### Détail par code ticket

```
GET /api/restaurants/tickets/code/{ticketCode}
```

Retourne `RestaurantTicketDetailResponseDto` (réservation + créneau : `dateService`, `startAtUtc`, `endAtUtc`).

### Liste par date d'émission

```
GET /api/restaurants/tickets/date/{date}
```

Filtre sur `issuedAtUtc` (jour UTC).

### Liste par plage de dates

```
GET /api/restaurants/tickets/daterange?dateDebut={date}&dateFin={date}
```

`400` si `dateFin < dateDebut`.

### Détail par identifiant

```
GET /api/restaurants/tickets/{id}
```

## Contrôle entrée

| Méthode | Route | Permission |
|---------|-------|------------|
| GET | `/{ticketCode}/check` | `Restaurant.Ticket.Check` |
| POST | `/{ticketCode}/use` | `Restaurant.Ticket.Use` |

### Check — synthèse `statut`

`NonReconnu` | `DejaUtilise` | `Invalide` | `CreneauInactif` | `HorsFenetre` | `Valide`

Champ `entreeAutorisee` (bool) + `message`.

### Use

- Transition atomique `ISSUED` → `USED`
- Déjà `USED` : HTTP 200, `alreadyUsed: true` (idempotent)
- Hors éligibilité : statut HTTP suggéré + message (pas de body ticket)

## Annulation réservation

- `ISSUED` → `VOID`
- Réponse : `ticketsVoided`
- Refus si au moins un ticket `USED`

## Mapping Event → Restaurant

| Événement (`api/events/tickets`) | Restaurant (`api/restaurants/tickets`) |
|----------------------------------|----------------------------------------|
| `GET /session/{id}` | `GET /creneau/{id}` |
| `Evenement.Session.Read` | `Restaurant.Etablissement.Read` |
| `Evenement.Ticket.Check` / `Use` | `Restaurant.Ticket.Check` / `Use` |
| Préfixe `EVT-TKT-` | `REST-TKT-` |

## DTOs

- **Liste** : `RestaurantTicketListItemDto` — ticket + `idRestaurantReservation`, `referenceReservation`, `idRestaurantCreneau`
- **Détail** : `RestaurantTicketDetailResponseDto` — + `customerRef`, `reservationStatus`, `dateService`, `startAtUtc`, `endAtUtc`
- **Sur réservation** : `RestaurantTicketResponseDto` dans `reservation.tickets[]`

## Exemples

```http
GET /api/restaurants/tickets/reservation/42
Authorization: Bearer {token}
```

```http
GET /api/restaurants/tickets/code/REST-TKT-001-20260815120000-1234
Authorization: Bearer {token}
```

```http
GET /api/restaurants/tickets/REST-TKT-001-20260815120000-1234/check
Authorization: Bearer {token}
```

```http
POST /api/restaurants/tickets/REST-TKT-001-20260815120000-1234/use
Authorization: Bearer {token}
```
