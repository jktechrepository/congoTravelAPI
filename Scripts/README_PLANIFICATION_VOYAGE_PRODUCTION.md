# Déploiement production — PlanificationVoyageV1

## Objectif

Corriger l'erreur API :

```
Unknown column 'v.IdPlanificationVoyage' in 'SELECT'
```

sur `GET /api/Voyage/societe/{idSociete}/paged` et tous les endpoints interrogeant la table `Voyages`.

## Migration EF

| Champ | Valeur |
|-------|--------|
| MigrationId | `20260531142422_PlanificationVoyageV1` |
| Migration source (prod attendue) | `20260530121511_ConfigSocietePenalitePourcentage` |
| ProductVersion | `6.0.25` |

## Fichiers

| Fichier | Rôle |
|---------|------|
| [`verify_planification_voyage_pre_prod.sql`](verify_planification_voyage_pre_prod.sql) | Pré-vérifications |
| [`production_planification_voyage_v1.sql`](production_planification_voyage_v1.sql) | Script principal (DDL) |
| [`verify_planification_voyage_post_prod.sql`](verify_planification_voyage_post_prod.sql) | Post-vérifications |
| [`production_planification_voyage_v1_rollback.sql`](production_planification_voyage_v1_rollback.sql) | Rollback (urgence) |
| [`production_planification_voyage_v1_patch_voyages_column.sql`](production_planification_voyage_v1_patch_voyages_column.sql) | Patch si tables créées sans colonne `Voyages` |

## Cas fréquent : tables créées sans colonne Voyages

Si les 4 tables `Planification*` existent mais `SHOW COLUMNS FROM Voyages LIKE 'IdPlanificationVoyage'` retourne **0 ligne** :

```bash
mysql -h [host] -u [user] -p [database] < Scripts/production_planification_voyage_v1_patch_voyages_column.sql
```

Puis relancer `verify_planification_voyage_post_prod.sql`.

## Procédure DBA

### 1. Sauvegarde (obligatoire)

```bash
mysqldump -h [host] -u [user] -p [database] > backup_planification_$(date +%Y%m%d_%H%M%S).sql
```

### 2. Pré-vérifications

```bash
mysql -h [host] -u [user] -p [database] < Scripts/verify_planification_voyage_pre_prod.sql
```

**Contrôles :**
- `IdPlanificationVoyage` absent de `Voyages` → OK pour migrer
- Dernière migration = `20260530121511_ConfigSocietePenalitePourcentage` (ou adapter le script si différent)
- `20260531142422_PlanificationVoyageV1` absent de `__EFMigrationsHistory`

**Si des migrations intermédiaires manquent** (ex. `ConfigSocieteCentralizedRules`), appliquer d'abord leurs scripts respectifs avant celui-ci.

### 3. Exécution

```bash
mysql -h [host] -u [user] -p [database] < Scripts/production_planification_voyage_v1.sql
```

Le script est transactionnel (`START TRANSACTION` / `COMMIT`).

### 4. Post-vérifications

```bash
mysql -h [host] -u [user] -p [database] < Scripts/verify_planification_voyage_post_prod.sql
```

### 5. Tests API

Après redémarrage ou sans redémarrage de l'API (schéma seul) :

```http
GET /api/Voyage/societe/{idSociete}/paged?pageNumber=1&pageSize=10
Authorization: Bearer {token}
```

Attendu : **HTTP 200** (plus d'exception MySQL).

Tests complémentaires :
- `GET /api/Voyage/{id}` sur un voyage existant
- `GET /api/PlanificationVoyage/societe/{idSociete}` → `[]` si aucun template

## Impact métier

- Aucune modification des voyages existants (`IdPlanificationVoyage` reste `NULL`).
- Nouvelles tables vides au départ.
- Module planification utilisable après déploiement du code associé.

## Rollback

**Uniquement si aucune planification n'a été créée en prod.**

```bash
mysql -h [host] -u [user] -p [database] < Scripts/production_planification_voyage_v1_rollback.sql
```

## Régénération du script (dev)

Si la base prod n'est pas à jour jusqu'à `ConfigSocietePenalitePourcentage` :

```bash
cd "/Users/mac/Documents/Developpement/Projet Kansa/CongoTravelAPI"

dotnet ef migrations script \
  [DERNIERE_MIGRATION_PROD] \
  20260531142422_PlanificationVoyageV1 \
  --project CongoTravel.csproj \
  --context CongoTravelDbContext \
  --output Scripts/production_planification_voyage_v1.sql
```

## Rappel

L'API **n'applique pas** les migrations EF en production (`Program.cs`, environnement Production). Toute migration doit passer par script SQL ou `dotnet ef database update` explicite sur la base cible.
