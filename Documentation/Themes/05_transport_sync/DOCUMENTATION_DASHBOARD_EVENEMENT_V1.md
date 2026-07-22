# Dashboard billetterie événement — CongoTravelAPI V1

Module **autonome** du transport : routes sous `api/events/dashboard`, métriques basées sur les tables `Evenement*`.

## Permission

| Permission | Rôles typiques |
|------------|----------------|
| `Evenement.Dashboard.Read` | Admin, Gérant, Financier, Super-Admin |

## Endpoints

### Dashboard société

```
GET /api/events/dashboard?idSociete={optional}&month=yyyy-MM
```

- **Tenancy** : société du JWT ; Super-Admin peut passer `idSociete`.
- **month** : période calendaire UTC (défaut = mois courant).

Réponse (extrait) :

```json
{
  "idSociete": 1,
  "nomSociete": "Rusa Events",
  "summary": {
    "sessionsPubliees": 3,
    "sessionsActives": 1,
    "reservationsConfirmeesMois": 12,
    "reservationsConfirmeesJour": 2,
    "ticketsEmisMois": 28,
    "ticketsUtilisesMois": 15,
    "holdsEnCours": 1
  },
  "reservations": { "hold": 1, "confirmed": 45, "cancelled": 2, "expired": 5 },
  "revenuParProvider": [
    { "provider": "CASH", "montant": 1200.00, "nombrePaiements": 8 },
    { "provider": "FLEXPAY", "montant": 800.00, "nombrePaiements": 4 }
  ],
  "revenuParDevise": [
    { "codeDevise": "USD", "montant": 2000.00, "nombrePaiements": 12 }
  ],
  "top5SessionsCa": [
    {
      "rang": 1,
      "idEvenementSession": 10,
      "codeSession": "GALA-2026",
      "libelle": "Gala annuel",
      "chiffreAffaires": 1500.00,
      "codeDevise": "USD",
      "ticketsVendus": 30
    }
  ],
  "reservationsRecentes": [],
  "paiementsRecents": []
}
```

### Dashboard Super-Admin (multi-sociétés)

```
GET /api/events/dashboard/super-admin?month=yyyy-MM
```

Réservé au **Super-Admin** : agrégation par société ayant au moins une session événement.

## Notes V1

- **Multi-devise** : les montants sont regroupés par `CodeDevise` (pas de conversion).
- **CA** : somme des `EvenementPayments` au statut `SUCCEEDED` sur la période.

## Widgets dans les dashboards transport

Les dashboards transport existants exposent un bloc optionnel `evenementStatistiques` (camelCase JSON) :

| Dashboard | Route | Type du widget |
|-----------|-------|----------------|
| Admin société | `GET /api/Dashboard/stats` | `EvenementDashboardWidgetDto` |
| Gérant | `GET /api/GerantDashboard` | `EvenementDashboardWidgetDto` |
| Financier | `GET /api/FinancierDashboard` | `EvenementDashboardWidgetDto` (agrégé sur le scope sociétés) |
| Super-Admin | `GET /api/SuperAdminDashboard` | `EvenementDashboardGlobalSummaryDto` |

- **Permission** : `Evenement.Dashboard.Read` requis (Super-Admin : toujours inclus).
- **Valeur `null`** : permission absente — le reste du dashboard transport est inchangé.
- **Période** : mois courant UTC (aligné sur les KPIs transport).
- **Détail complet** : route dédiée `GET /api/events/dashboard`.

## Tests

- `EvenementDashboardServiceTests`
- `EvenementDashboardEnrichmentTests`
- `GerantDashboardTests` (widget avec/sans permission)
- Filtre : `dotnet test --filter "FullyQualifiedName~EvenementDashboard"`

## Voir aussi

- [`DOCUMENTATION_API_CLASSES_EVENEMENT_V1.md`](DOCUMENTATION_API_CLASSES_EVENEMENT_V1.md) — référentiel classes (CRUD)
- [`DOCUMENTATION_FLEXPAY_EVENEMENT_V1.md`](DOCUMENTATION_FLEXPAY_EVENEMENT_V1.md) — paiement électronique
- [`ANALYSE_V1_BILLETTERIE_3_MODES_INVENTAIRE.md`](../11_analyses_plans/ANALYSE_V1_BILLETTERIE_3_MODES_INVENTAIRE.md) — roadmap globale
