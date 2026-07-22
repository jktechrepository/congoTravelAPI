#!/usr/bin/env bash
# Déploiement billetterie événementielle V1 (3 modes A/B/C)
# Usage:
#   export DB_HOST=localhost DB_USER=rusa DB_NAME=congotravel DB_PASSWORD=secret
#   ./Scripts/deploy_evenement_ticketing_production.sh
#
# Modes migrations :
#   défaut     → dotnet ef database update
#   USE_SQL_SCRIPT=1 → Scripts/generated_evenement_migrations.sql (idempotent)
#   SKIP_EF=1  → ignorer les migrations (déjà appliquées)
#
# Doc complète : Scripts/README_DEPLOIEMENT_EVENEMENT_TICKETING_V1.md

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-3306}"
DB_USER="${DB_USER:-}"
DB_NAME="${DB_NAME:-}"
DB_PASSWORD="${DB_PASSWORD:-}"
SKIP_EF="${SKIP_EF:-0}"
USE_SQL_SCRIPT="${USE_SQL_SCRIPT:-0}"
SKIP_TRIGGERS="${SKIP_TRIGGERS:-0}"
SKIP_VERIFY="${SKIP_VERIFY:-0}"
SKIP_PREFLIGHT="${SKIP_PREFLIGHT:-0}"
INSTALL_EVENT_SCHEDULER="${INSTALL_EVENT_SCHEDULER:-0}"

echo "=== Déploiement Evenement Ticketing V1 ==="
echo "Base: ${DB_USER}@${DB_HOST}:${DB_PORT}/${DB_NAME}"
echo "Date: $(date -u +"%Y-%m-%dT%H:%M:%SZ")"
echo "Mode migrations: $(
  if [[ "$SKIP_EF" == "1" ]]; then echo 'SKIP';
  elif [[ "$USE_SQL_SCRIPT" == "1" ]]; then echo 'SQL idempotent';
  else echo 'dotnet ef'; fi
)"

if ! command -v mysql >/dev/null 2>&1; then
  echo "AVERTISSEMENT: client mysql absent — étapes SQL à exécuter manuellement"
fi

if [[ -z "$DB_USER" || -z "$DB_NAME" ]]; then
  echo "ERREUR: DB_USER et DB_NAME sont obligatoires"
  exit 1
fi

mysql_exec() {
  MYSQL_PWD="$DB_PASSWORD" mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" "$DB_NAME" "$@"
}

if [[ "$SKIP_PREFLIGHT" != "1" ]] && command -v mysql >/dev/null 2>&1; then
  echo ""
  echo ">> 0/5 Pré-vol"
  mysql_exec < Scripts/preflight_evenement_ticketing_v1.sql
  echo "OK pré-vol (vérifier les lignes STATUS = KO)"
else
  echo ">> 0/5 Pré-vol ignoré"
fi

if [[ "$SKIP_EF" != "1" ]]; then
  echo ""
  if [[ "$USE_SQL_SCRIPT" == "1" ]]; then
    echo ">> 1/5 Migrations SQL idempotentes (generated_evenement_migrations.sql)"
    if command -v mysql >/dev/null 2>&1; then
      mysql_exec < Scripts/generated_evenement_migrations.sql
      echo "OK migrations SQL"
    else
      echo "ERREUR: mysql requis pour USE_SQL_SCRIPT=1"
      exit 1
    fi
  else
    echo ">> 1/5 Migrations EF Core"
    if ! command -v dotnet >/dev/null 2>&1; then
      echo "ERREUR: dotnet CLI introuvable — utiliser USE_SQL_SCRIPT=1"
      exit 1
    fi
    if [[ -n "${ConnectionStrings__DefaultConnection:-}" ]]; then
      dotnet ef database update --project CongoTravel.csproj -c CongoTravelDbContext
    else
      CONN="Server=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};User=${DB_USER};Password=${DB_PASSWORD};"
      dotnet ef database update --project CongoTravel.csproj -c CongoTravelDbContext --connection "$CONN"
    fi
    echo "OK migrations EF"
  fi
else
  echo ">> 1/5 Migrations ignorées (SKIP_EF=1)"
fi

echo ""
echo ">> 2/5 Procédure sp_ExpireEvenementHolds"
if command -v mysql >/dev/null 2>&1; then
  if [[ "$INSTALL_EVENT_SCHEDULER" == "1" ]]; then
    mysql_exec < Scripts/production_evenement_hold_expiration_job.sql
    echo "OK procédure + event scheduler MariaDB"
  else
    mysql_exec < Scripts/production_evenement_hold_expiration_procedure_only.sql
    echo "OK procédure (hosted service .NET recommandé pour l'expiration)"
  fi
else
  echo "AVERTISSEMENT: exécuter Scripts/production_evenement_hold_expiration_procedure_only.sql"
fi

echo ""
echo ">> 3/5 Triggers EvenementReservationLines"
if [[ "$SKIP_TRIGGERS" != "1" ]]; then
  if command -v mysql >/dev/null 2>&1; then
    mysql_exec < Scripts/production_evenement_triggers_v1.sql
    echo "OK triggers"
  else
    echo "AVERTISSEMENT: exécuter manuellement Scripts/production_evenement_triggers_v1.sql"
  fi
else
  echo "Ignoré (SKIP_TRIGGERS=1)"
fi

echo ""
echo ">> 4/5 Vérification contrat API/DB"
if [[ "$SKIP_VERIFY" != "1" ]] && command -v mysql >/dev/null 2>&1; then
  mysql_exec < Scripts/verify_evenement_api_db_contract.sql
  echo "OK vérification (voir résultats ci-dessus)"
else
  echo "Ignoré ou mysql absent"
fi

echo ""
echo ">> 5/5 Historique EF Evenement"
if command -v mysql >/dev/null 2>&1; then
  mysql_exec -e "
    SELECT MigrationId, ProductVersion
    FROM __EFMigrationsHistory
    WHERE MigrationId LIKE '%Evenement%'
    ORDER BY MigrationId;
  "
fi

echo ""
echo "=== Déploiement terminé ==="
echo "Rappels post-déploiement:"
echo "  - Redémarrer l'API (AddEvenementTicketing + PermissionSeeder Evenement.*)"
echo "  - Doc: Scripts/README_DEPLOIEMENT_EVENEMENT_TICKETING_V1.md"
echo "  - Tests: dotnet test --filter FullyQualifiedName~Evenement"
echo "  - Concurrence: Scripts/test_concurrency_evenement_ticketing_v1.sql"
echo "  - Rollback: Scripts/rollback_evenement_ticketing_v1.sql (destructif)"
