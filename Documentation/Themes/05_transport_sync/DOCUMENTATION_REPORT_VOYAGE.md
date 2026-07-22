# API Report de voyage

## Endpoint

| Méthode | Route | Permission |
|---------|-------|------------|
| POST | `/api/Voyage/{id}/reporter` | `Voyage.Update` (Admin, Gérant) |

## Request

```json
{
  "dateDepart": "2026-06-15",
  "heureDepart": "08:00:00",
  "motif": "Panne véhicule",
  "notifierClients": true,
  "confirmerAvecBilletsUtilises": false
}
```

| Champ | Description |
|-------|-------------|
| `dateDepart` | Nouvelle date de départ (jour civil) |
| `heureDepart` | Nouvelle heure de départ |
| `motif` | Raison du report (optionnel, inclus dans audit + notifications) |
| `notifierClients` | Envoyer in-app / SMS / email selon préférences (défaut `true`) |
| `confirmerAvecBilletsUtilises` | Requis si des billets ont déjà été scannés (`IsUsed=true`) |

## Response 200

```json
{
  "idVoyage": 42,
  "ancienneDateDepart": "2026-06-10T00:00:00",
  "ancienneHeureDepart": "14:00:00",
  "nouvelleDateDepart": "2026-06-15T00:00:00",
  "nouvelleHeureDepart": "08:00:00",
  "nombreReservationsImpactees": 12,
  "nombreBilletsRecalcules": 15,
  "notificationsEnvoyees": 10,
  "notificationsEchecs": 0,
  "avertissements": []
}
```

## Règles métier

1. **Report in-place** : même `IdVoyage`, réservations et sièges conservés.
2. **Validités billet** recalculées (`DateValiditeDebut/Fin`) pour les réservations `CONFIRMEE` / `CONFIRME`.
3. **Refus 409** si :
   - date/heure de départ **actuelle** déjà passées (UTC) ;
   - nouvelle date/heure passée ou identique à l'actuelle ;
   - conflit véhicule (même bus, même créneau) ;
   - holds FlexPay actifs sur le voyage ;
   - billets déjà utilisés sans confirmation explicite.
4. **Avancement autorisé** : la nouvelle date peut être **antérieure** à l'horaire actuel du voyage, tant qu'elle reste strictement dans le futur (ex. départ prévu J+5 → J+3).
5. **Audit** : action `REPORT` sur table `Voyage`.
6. **Notifications** : clients distincts avec réservation active ; canaux selon `NotificationPreference` (`OptOutGlobal` respecté).

## Note front / admin

- **Ne pas** changer `dateDepart` / `heureDepart` via `PUT /api/Voyage/{id}` lorsque des réservations existent — utiliser **`POST /api/Voyage/{id}/reporter`**.
- Prévoir un écran « Reporter le voyage » avec confirmation si billets embarqués.
- Le mobile client peut écouter les notifications in-app de type `VOYAGE_REPORT`.

## Fichiers backend

- [`Services/VoyageReportService.cs`](../../Services/VoyageReportService.cs)
- [`Services/VoyageReportNotificationService.cs`](../../Services/VoyageReportNotificationService.cs)
- [`Controllers/VoyageController.cs`](../../Controllers/VoyageController.cs)
