# Changelog API — 15 août 2026  
## Restaurant + Site Touristique (intégration Vue.js & Flutter)

**Destinataires :** équipes front web (Vue 3) et mobile (Flutter)  
**Nature :** changements **additifs** (rétrocompatibles pour les clients qui ignorent les nouveaux champs)  
**Guides modules :** [MODULE_10](MODULE_10_SITE_TOURISTIQUE.md) · [MODULE_11](MODULE_11_RESTAURANT.md)  
**Tickets restaurant (référence complète) :** [DOCUMENTATION_API_TICKETS_RESTAURANT_V1.md](../05_transport_sync/DOCUMENTATION_API_TICKETS_RESTAURANT_V1.md)

---

## 1. Résumé exécutif

| Thème | Impact front |
|-------|----------------|
| **Restaurant — tickets d’entrée** | Nouveau `GET/POST /api/restaurants/tickets/*` ; `tickets[]` sur réservation confirmée ; cancel renvoie `ticketsVoided` ; gate Check/Use |
| **Restaurant — photos établissement** | Max 3 photos base64 à la création + CRUD `/etablissements/{id}/photos` |
| **Restaurant — config entrée** | `heuresOuvertureEntreeRestaurantAvantDebut` sur ConfigSociété (défaut **1**, clamp 0–72) |
| **Site touristique — photos lieu** | Max 3 photos base64 + CRUD `/lieux/{id}/photos` |
| **Site touristique — localisation** | `province`, `ville`, `adresse`, `telephone` sur lieu (create/update/list/detail) |
| **Site touristique — horaires** | `heureOuverture`, `heureFermeture` (`"HH:mm:ss"`), `jourOuverture` |

**Breaking soft :** [MODULE_11](MODULE_11_RESTAURANT.md) annonçait « pas de gate V1 » — **obsolète**. Le gate restaurant est disponible (même pattern Event / Site Touristique).

JSON API en **camelCase**.

---

## 2. Déploiement SQL (ops / backend)

Appliquer **une fois** (vérifier `__EFMigrationsHistory` avant) :

| Script | Objet |
|--------|--------|
| [`create_restaurant_photos_production.sql`](../../../Scripts/create_restaurant_photos_production.sql) | Table `RestaurantPhotos` |
| [`create_restaurant_tickets_production.sql`](../../../Scripts/create_restaurant_tickets_production.sql) | Table `RestaurantTickets` + config heures entrée resto |
| [`create_site_touristique_lieu_photos_production.sql`](../../../Scripts/create_site_touristique_lieu_photos_production.sql) | Table `SiteTouristiqueLieuPhotos` |
| [`add_site_touristique_lieu_localisation_production.sql`](../../../Scripts/add_site_touristique_lieu_localisation_production.sql) | Colonnes localisation lieu |
| [`add_site_touristique_lieu_horaires_production.sql`](../../../Scripts/add_site_touristique_lieu_horaires_production.sql) | Colonnes horaires lieu |

Permissions nouvelles (seed / base déjà peuplée) : `Restaurant.Ticket.Check`, `Restaurant.Ticket.Use` (Caissier Check+Use ; Financier Check). Relancer le PermissionSeeder ou équivalent ops.

---

## 3. Restaurant — tickets

### 3.1 Émission

À la confirmation CASH ou FlexPay `SUCCEEDED` :

- **1 ticket** par unité de `quantite` sur chaque ligne de réservation
- Statut initial : `ISSUED`
- Code : `REST-TKT-{idSociete}-{yyyyMMddHHmmss}-{4 digits}`
- Présents dans `reservation.tickets[]` des réponses confirm / get réservation

### 3.2 Fenêtre d’entrée (gate)

`[creneau.startAtUtc − heuresAvant, creneau.endAtUtc]`  
`heuresAvant` = `ConfigSociete.heuresOuvertureEntreeRestaurantAvantDebut` (défaut 1).

Ne **pas** réutiliser `heuresOuvertureEntreeEvenementAvantDebut`.

### 3.3 Annulation

- Tickets `ISSUED` → `VOID`
- Réponse cancel : `ticketsVoided` (int)
- **Bloqué** si un ticket est déjà `USED`

### 3.4 Endpoints (aperçu)

Base : `/api/restaurants/tickets`

| Méthode | Route | Permission |
|---------|-------|------------|
| GET | `/`, `/reservation/{id}`, `/creneau/{id}`, `/code/{code}`, `/{id}`, … | `Restaurant.Etablissement.Read` |
| GET | `/{ticketCode}/check` | `Restaurant.Ticket.Check` |
| POST | `/{ticketCode}/use` | `Restaurant.Ticket.Use` |

Détail : [DOCUMENTATION_API_TICKETS_RESTAURANT_V1.md](../05_transport_sync/DOCUMENTATION_API_TICKETS_RESTAURANT_V1.md).

### 3.5 Exemple check / use

```http
GET /api/restaurants/tickets/REST-TKT-001-20260815120000-1234/check
Authorization: Bearer {token}
```

```json
{
  "idRestaurantTicket": 12,
  "ticketCode": "REST-TKT-001-20260815120000-1234",
  "status": "ISSUED",
  "statut": "Valide",
  "message": "Ticket valide. Entrée autorisée.",
  "entreeAutorisee": true,
  "idRestaurantReservation": 5,
  "idRestaurantCreneau": 3,
  "startAtUtc": "2026-08-15T18:00:00Z"
}
```

`statut` possibles : `NonReconnu` | `DejaUtilise` | `Invalide` | `CreneauInactif` | `HorsFenetre` | `Valide`.

```http
POST /api/restaurants/tickets/REST-TKT-001-20260815120000-1234/use
```

```json
{
  "ticket": {
    "idRestaurantTicket": 12,
    "ticketCode": "REST-TKT-001-20260815120000-1234",
    "status": "USED",
    "issuedAtUtc": "…",
    "usedAtUtc": "…"
  },
  "alreadyUsed": false
}
```

Second `use` : HTTP 200, `alreadyUsed: true` (idempotent).

---

## 4. Restaurant — photos établissement

| Action | Endpoint |
|--------|----------|
| Create avec photos | `POST /api/restaurants/etablissements` body `photos: [{ photoBase64, fileName?, ordre? }]` (max 3) |
| Liste | `GET /api/restaurants/etablissements/{id}/photos` |
| Ajouter | `POST /api/restaurants/etablissements/{id}/photos` |
| Ordre | `PUT /api/restaurants/etablissements/{id}/photos/{photoId}/ordre` |
| Supprimer | `DELETE /api/restaurants/etablissements/{id}/photos/{photoId}` |

Réponses list/detail : `photoCouverture` + `photos[]` (data-URL `data:image/…;base64,…`).

Permissions : Write pour muter ; Read pour lister (même pattern Lieu ST).

---

## 5. Site touristique — fiche lieu enrichie

### 5.1 Create / update (exemple)

```json
POST /api/sites-touristiques/lieux
{
  "codeLieu": "PARC-01",
  "nom": "Parc National",
  "description": "Visite journalière",
  "province": "Kinshasa",
  "ville": "Mont Ngafula",
  "adresse": "Route de Kasangulu",
  "telephone": "+243810000001",
  "heureOuverture": "08:00:00",
  "heureFermeture": "17:30:00",
  "jourOuverture": "Lun-Dim",
  "idSite": 1,
  "photos": [
    { "photoBase64": "<base64 ou data-URL>", "fileName": "cover.jpg", "ordre": 1 }
  ]
}
```

`PUT /api/sites-touristiques/lieux/{id}` accepte les mêmes champs localisation / horaires (sans `codeLieu`).

### 5.2 Règles

| Champ | Type JSON | Notes |
|-------|-----------|--------|
| `province`, `ville` | string? | max 120 |
| `adresse` | string? | max 500 |
| `telephone` | string? | max 30 |
| `heureOuverture`, `heureFermeture` | `"HH:mm:ss"` ou null | `TimeOnly` |
| `jourOuverture` | string? | max 100, libre (ex. `Lun-Dim`) |

Si **les deux** heures sont renseignées et `heureFermeture <= heureOuverture` → **400** / erreur métier.

### 5.3 Photos lieu

Même contrat que restaurant, sous `/api/sites-touristiques/lieux/{id}/photos`.

---

## 6. Config société (Vue admin)

Sur update/get ConfigSociété, nouveau champ :

```json
"heuresOuvertureEntreeRestaurantAvantDebut": 1
```

Distinct de `heuresOuvertureEntreeEvenementAvantDebut`. Clamp backend **0–72**.

---

## 7. Notes Vue 3

### Types (extrait TS)

```ts
interface SiteTouristiqueLieu {
  idSiteTouristique: number
  codeLieu: string
  nom: string
  province?: string | null
  ville?: string | null
  adresse?: string | null
  telephone?: string | null
  heureOuverture?: string | null  // "08:00:00"
  heureFermeture?: string | null
  jourOuverture?: string | null
  photoCouverture?: LieuPhoto | null
  photos?: LieuPhoto[]
}

interface RestaurantTicket {
  idRestaurantTicket: number
  ticketCode: string
  status: 'ISSUED' | 'USED' | 'VOID'
  issuedAtUtc: string
  usedAtUtc?: string | null
}
```

### UI recommandée

- **Admin lieu / resto** : formulaire localisation + horaires + uploader 3 photos (preview data-URL).
- **Guichet resto** : après CASH/FlexPay, afficher QR `ticketCode` (comme Event/ST).
- **Gate resto** : écran scan → `check` → confirmer → `use` ; gérer `HorsFenetre` / `DejaUtilise`.
- Guards Pinia : `Restaurant.Ticket.Check` / `Use` pour le gate ; masquer si absent.

### TimeOnly dans les forms

Préférer `<input type="time">` puis sérialiser en `"HH:mm:ss"` (ajouter `:00` si besoin).

---

## 8. Notes Flutter

### Models

```dart
class SiteTouristiqueLieu {
  final String? province;
  final String? ville;
  final String? adresse;
  final String? telephone;
  final String? heureOuverture; // "08:00:00"
  final String? heureFermeture;
  final String? jourOuverture;
  // fromJson: map['heureOuverture'] as String?
}

class RestaurantTicket {
  final String ticketCode;
  final String status;
  // …
}
```

### Écrans

| Écran | Actions |
|-------|---------|
| Catalogue lieux | Afficher ville/province, horaires, cover photo |
| Fiche lieu | Carte adresse / téléphone / `jourOuverture` |
| Post-paiement resto | Liste QR `tickets` |
| Gate resto | `mobile_scanner` → Dio `GET …/check` → `POST …/use` |

Parser les heures avec `TimeOfDay` si besoin d’affichage local (`HH:mm`).

---

## 9. Checklist intégration

### Vue

- [ ] Formulaire lieu : localisation + horaires + photos
- [ ] Formulaire établissement : photos (max 3)
- [ ] Config société : champ heures entrée restaurant
- [ ] Affichage `tickets[]` après confirmation resto
- [ ] Écran gate resto (check/use) + permissions
- [ ] Cancel resto : afficher `ticketsVoided` / gérer erreur ticket `USED`

### Flutter

- [ ] Models + UI catalogue lieu enrichi
- [ ] QR tickets restaurant post-achat
- [ ] Gate restaurant (si app agent)
- [ ] Gestion `statut` check (`HorsFenetre`, `DejaUtilise`, …)

### QA

- [ ] Confirm 2 couverts → 2 tickets `ISSUED`
- [ ] Check hors fenêtre → `HorsFenetre`
- [ ] Use ×2 → second `alreadyUsed: true`
- [ ] Cancel après use → erreur métier
- [ ] Create lieu `heureFermeture < heureOuverture` → erreur
