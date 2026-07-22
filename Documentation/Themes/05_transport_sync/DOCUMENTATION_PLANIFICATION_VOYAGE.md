# Planification de voyages v1

Module de génération batch de voyages à partir de templates récurrents.

## Concept

1. **Template** (`PlanificationVoyage`) : véhicule, site, heure, prix, jours de semaine, étapes destinations.
2. **Génération** : expansion des dates sur une période → création de voyages réels (`Voyage.IdPlanificationVoyage` renseigné).
3. **Idempotence** : créneau déjà occupé (même véhicule + date + heure) → ignoré avec rapport.

## Endpoint principal

```
POST /api/PlanificationVoyage/{id}/generer
```

Body exemple :

```json
{
  "mode": "MoisCourant"
}
```

Ou période personnalisée :

```json
{
  "mode": "PeriodePersonnalisee",
  "dateDebut": "2026-06-01",
  "dateFin": "2026-06-30"
}
```

## Jours de semaine

Valeurs .NET `DayOfWeek` : `0` = Dimanche, `1` = Lundi, … `6` = Samedi.

Exemple multi-jours : `"joursSemaine": [1, 3, 5]` (lun/mer/ven).

## Réponse génération

```json
{
  "idGeneration": 1,
  "planification": { "id": 1, "libelle": "Kin-Gom semaine" },
  "periode": { "dateDebut": "2026-06-01", "dateFin": "2026-06-30" },
  "resume": { "creees": 12, "ignorees": 1, "echecs": 0 },
  "avertissements": ["2 voyage(s) dépassent l'horizon de réservation (60 jours)"],
  "details": [
    { "dateDepart": "2026-06-02", "statut": "Cree", "idVoyage": 101 },
    { "dateDepart": "2026-06-09", "statut": "Ignore", "message": "Créneau véhicule déjà occupé" }
  ]
}
```

## Règles métier

- Modification du template **ne modifie pas** les voyages déjà générés.
- Suppression impossible si des voyages générés ont des réservations → soft-disable (`Statut=false`).
- `POST /api/Voyage` reste disponible pour les voyages ponctuels exceptionnels.

## Déploiement production

La migration EF **`20260531142422_PlanificationVoyageV1`** doit être appliquée sur la base MySQL de production **avant** ou **en même temps** que le déploiement du code planification.

Sans cette migration, les endpoints `Voyage` échouent avec :

```
Unknown column 'v.IdPlanificationVoyage' in 'SELECT'
```

### Fichiers SQL

| Script | Usage |
|--------|-------|
| [`Scripts/verify_planification_voyage_pre_prod.sql`](../../../Scripts/verify_planification_voyage_pre_prod.sql) | Pré-vérifications |
| [`Scripts/production_planification_voyage_v1.sql`](../../../Scripts/production_planification_voyage_v1.sql) | Migration DDL |
| [`Scripts/verify_planification_voyage_post_prod.sql`](../../../Scripts/verify_planification_voyage_post_prod.sql) | Post-vérifications |
| [`Scripts/production_planification_voyage_v1_rollback.sql`](../../../Scripts/production_planification_voyage_v1_rollback.sql) | Rollback |
| [`Scripts/production_planification_voyage_v1_patch_voyages_column.sql`](../../../Scripts/production_planification_voyage_v1_patch_voyages_column.sql) | Patch colonne `Voyages` si tables seules créées |
| [`Scripts/README_PLANIFICATION_VOYAGE_PRODUCTION.md`](../../../Scripts/README_PLANIFICATION_VOYAGE_PRODUCTION.md) | Guide DBA complet |

Si les tables planification existent mais `GET /api/Voyage/.../paged` échoue encore, exécuter le **patch colonne Voyages** ci-dessus.

### Procédure résumée

1. Sauvegarde base prod
2. Exécuter `verify_planification_voyage_pre_prod.sql`
3. Exécuter `production_planification_voyage_v1.sql`
4. Exécuter `verify_planification_voyage_post_prod.sql`
5. Tester `GET /api/Voyage/societe/{idSociete}/paged`

L'API n'applique **pas** les migrations automatiquement en production.

## Voir aussi

- [`DOCUMENTATION_API_ENDPOINTS_COMPLETE.md`](../01_demarrage/DOCUMENTATION_API_ENDPOINTS_COMPLETE.md) — routes CRUD
- [`SPEC_TECHNIQUE_WORKFLOW_RESERVATION_VOYAGE_V2.md`](SPEC_TECHNIQUE_WORKFLOW_RESERVATION_VOYAGE_V2.md) — workflow voyage/réservation
