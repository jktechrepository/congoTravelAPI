# Déploiement production — Billetterie événement V1

Module **autonome** du transport : tables `Evenement*`, routes `api/events/*`.

## Migrations EF concernées

| MigrationId | Contenu |
|-------------|---------|
| `20260703101713_EvenementTicketingV1` | 11 tables Evenement*, `ConfigSocietes.DureeHoldEvenementMinutes` |
| `20260703120104_EvenementSessionGlobalQuotaPricing` | `PrixUnitaire` + `CodeDevise` sur `EvenementSessionGlobalQuotas` |

Prérequis : la base doit déjà avoir toutes les migrations transport jusqu'à `20260619134037_ClientAdresseClientOptional`.

## Fichiers

| Fichier | Rôle |
|---------|------|
| `deploy_evenement_ticketing_production.sh` | Orchestration (recommandé) |
| `generated_evenement_migrations.sql` | SQL idempotent EF (sans `dotnet` sur le serveur) |
| `production_evenement_hold_expiration_procedure_only.sql` | `sp_ExpireEvenementHolds` |
| `production_evenement_hold_expiration_job.sql` | Procédure + event scheduler MariaDB (optionnel) |
| `production_evenement_triggers_v1.sql` | Triggers cohérence `EvenementReservationLines` |
| `preflight_evenement_ticketing_v1.sql` | Contrôles avant déploiement |
| `verify_evenement_api_db_contract.sql` | Validation post-déploiement |
| `rollback_evenement_ticketing_v1.sql` | Rollback destructif |
| `test_concurrency_evenement_ticketing_v1.sql` | Tests manuels concurrence |

> `production_evenement_ticketing_v1.sql` est **obsolète** (manque pricing GlobalQuota + stamp EF). Utiliser `generated_evenement_migrations.sql`.

## Avant déploiement

1. **Sauvegarde complète** de la base MariaDB.
2. Fenêtre de maintenance (DDL + redémarrage API).
3. Vérifier que l'API déployée inclut `AddEvenementTicketing()` et `PermissionSeeder` Evenement.*.

```bash
export DB_HOST=...
export DB_PORT=3306
export DB_USER=...
export DB_NAME=...
export DB_PASSWORD=...

mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" \
  < Scripts/preflight_evenement_ticketing_v1.sql
```

## Option A — Script orchestré (recommandé)

```bash
chmod +x Scripts/deploy_evenement_ticketing_production.sh

# Via dotnet ef (machine avec SDK + accès DB)
./Scripts/deploy_evenement_ticketing_production.sh

# SQL pur (serveur DB sans dotnet)
USE_SQL_SCRIPT=1 ./Scripts/deploy_evenement_ticketing_production.sh
```

Variables utiles :

| Variable | Défaut | Description |
|----------|--------|-------------|
| `SKIP_EF` | `0` | `1` = ne pas lancer `dotnet ef database update` |
| `USE_SQL_SCRIPT` | `0` | `1` = exécuter `generated_evenement_migrations.sql` |
| `SKIP_TRIGGERS` | `0` | `1` = ignorer les triggers |
| `SKIP_VERIFY` | `0` | `1` = ignorer la vérification contrat |
| `INSTALL_EVENT_SCHEDULER` | `0` | `1` = event MariaDB au lieu du hosted service .NET |

## Option B — Manuel pas à pas

### 1. Migrations DDL

**Via EF :**

```bash
dotnet ef database update \
  --project CongoTravel.csproj \
  --context CongoTravelDbContext \
  --connection "Server=HOST;Port=3306;Database=DB;User=USER;Password=PWD;"
```

**Via SQL idempotent :**

```bash
mysql ... < Scripts/generated_evenement_migrations.sql
```

### 2. Procédure expiration holds

```bash
# Recommandé : hosted service .NET (EvenementHoldExpirationHostedService)
mysql ... < Scripts/production_evenement_hold_expiration_procedure_only.sql

# Alternative : scheduler MariaDB (désactiver le hosted service pour éviter double expiration)
mysql ... < Scripts/production_evenement_hold_expiration_job.sql
# SET GLOBAL event_scheduler = ON;
```

### 3. Triggers

```bash
mysql ... < Scripts/production_evenement_triggers_v1.sql
```

### 4. Vérification

```bash
mysql ... < Scripts/verify_evenement_api_db_contract.sql
```

Contrôles attendus :

- 11 tables `Evenement*` présentes
- `__EFMigrationsHistory` contient les 2 migrations Evenement
- Index `IX_EvenementReservations_Status_ExpiresAtUtc` présent
- `RowsInvalides` = 0 sur les LineType (si données)

## Post-déploiement API

1. Redéployer / redémarrer l'API (binaire avec module Evenement).
2. Au démarrage : `PermissionSeeder` crée les permissions `Evenement.*`.
3. Vérifier les rôles Admin / Gérant / Financier (dashboard widget inclus).
4. Smoke API :

```bash
dotnet test Tests/CongoTravel.Tests.csproj --filter "FullyQualifiedName~Evenement"
```

5. Callback FlexPay événement : URL `POST /api/events/flexpay/callback` configurée côté FlexPay.

## Rollback

```bash
mysql ... < Scripts/rollback_evenement_ticketing_v1.sql
```

Supprime toutes les tables Evenement*, la procédure, les triggers, la colonne `DureeHoldEvenementMinutes` et les entrées `__EFMigrationsHistory`. **Irréversible** si des ventes ont eu lieu.

## Régénérer le SQL EF

```bash
dotnet ef migrations script \
  20260619134037_ClientAdresseClientOptional \
  20260703120104_EvenementSessionGlobalQuotaPricing \
  --project CongoTravel.csproj \
  --context CongoTravelDbContext \
  --idempotent \
  -o Scripts/generated_evenement_migrations.sql
```

## Voir aussi

- [`DOCUMENTATION_DASHBOARD_EVENEMENT_V1.md`](../Documentation/Themes/05_transport_sync/DOCUMENTATION_DASHBOARD_EVENEMENT_V1.md)
- [`DOCUMENTATION_FLEXPAY_EVENEMENT_V1.md`](../Documentation/Themes/05_transport_sync/DOCUMENTATION_FLEXPAY_EVENEMENT_V1.md)
- [`ANALYSE_V1_BILLETTERIE_3_MODES_INVENTAIRE.md`](../Documentation/Themes/11_analyses_plans/ANALYSE_V1_BILLETTERIE_3_MODES_INVENTAIRE.md)
