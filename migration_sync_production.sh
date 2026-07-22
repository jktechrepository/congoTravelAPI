#!/bin/bash

# Script de migration sécurisée pour la synchronisation offline
# Auteur: CongoTravel Team
# Date: 21 mars 2026

set -e  # Arrêter en cas d'erreur

# Configuration
DB_HOST="${DB_HOST:-localhost}"
DB_USER="${DB_USER:-kenergie}"
DB_NAME="${DB_NAME:-kenergie}"
DB_PASSWORD="${DB_PASSWORD:-}"

echo "🔴 DÉBUT MIGRATION SYNCHRONISATION OFFLINE"
echo "⏰ $(date)"
echo "📊 Base: $DB_HOST/$DB_NAME"

# Vérification des prérequis
echo "🔍 Vérification des prérequis..."

if ! command -v dotnet &> /dev/null; then
    echo "❌ dotnet CLI non trouvé"
    exit 1
fi

if ! command -v mysql &> /dev/null; then
    echo "❌ mysql client non trouvé"
    exit 1
fi

# 1. Backup de la base de données
echo "💾 Backup de la base de données..."
BACKUP_FILE="backup_pre_sync_$(date +%Y%m%d_%H%M%S).sql"
mysqldump --single-transaction --routines --triggers \
    -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" \
    "$DB_NAME" > "./backups/$BACKUP_FILE"

if [ $? -eq 0 ]; then
    echo "✅ Backup créé: $BACKUP_FILE"
else
    echo "❌ Échec du backup"
    exit 1
fi

# 2. Migration EF Core
echo "🔄 Migration EF Core..."
dotnet ef database update --no-build --verbose --project CongoTravel.csproj

if [ $? -eq 0 ]; then
    echo "✅ Migration EF Core réussie"
else
    echo "❌ Échec migration EF Core"
    echo "🔄 Restauration du backup..."
    mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" < "./backups/$BACKUP_FILE"
    exit 1
fi

# 3. Vérification des colonnes ajoutées
echo "🔍 Vérification des colonnes ajoutées..."
echo "Tables et colonnes attendues:"

mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -e "
    SELECT 
        TABLE_NAME as 'Table',
        COLUMN_NAME as 'Column',
        DATA_TYPE as 'Type',
        IS_NULLABLE as 'Nullable',
        COLUMN_DEFAULT as 'Default'
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = '$DB_NAME' 
    AND TABLE_NAME IN ('Clients', 'Paiements')
    AND COLUMN_NAME IN ('UpdatedAt', 'IsDeleted', 'ClientRequestId')
    ORDER BY TABLE_NAME, ORDINAL_POSITION;
"

# Compter les colonnes attendues
EXPECTED_COLUMNS=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -sN -e "
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = '$DB_NAME' 
    AND TABLE_NAME IN ('Clients', 'Paiements')
    AND COLUMN_NAME IN ('UpdatedAt', 'IsDeleted', 'ClientRequestId');
")

echo "📊 Colonnes trouvées: $EXPECTED_COLUMNS/4 attendues"

if [ "$EXPECTED_COLUMNS" -eq 4 ]; then
    echo "✅ Toutes les colonnes attendues sont présentes"
else
    echo "⚠️ Colonnes manquantes: $((4 - EXPECTED_COLUMNS))"
fi

# 4. Vérification des index créés
echo "📊 Vérification des index créés..."
mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -e "
    SELECT 
        TABLE_NAME as 'Table',
        INDEX_NAME as 'Index',
        COLUMN_NAME as 'Column',
        NON_UNIQUE as 'Unique'
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = '$DB_NAME' 
    AND TABLE_NAME IN ('Clients', 'Paiements', 'ClientFactures')
    AND INDEX_NAME LIKE '%Sync%' OR INDEX_NAME LIKE '%Idempotent%'
    ORDER BY TABLE_NAME, INDEX_NAME;
"

# Compter les index attendus
EXPECTED_INDEXES=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -sN -e "
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = '$DB_NAME' 
    AND TABLE_NAME IN ('Clients', 'Paiements', 'ClientFactures')
    AND (INDEX_NAME LIKE '%Sync%' OR INDEX_NAME LIKE '%Idempotent%');
")

echo "📊 Index trouvés: $EXPECTED_INDEXES/3 attendues"

if [ "$EXPECTED_INDEXES" -eq 3 ]; then
    echo "✅ Tous les index attendus sont présents"
else
    echo "⚠️ Index manquants: $((3 - EXPECTED_INDEXES))"
fi

# 5. Validation des données
echo "✅ Validation des données..."

# Clients
TOTAL_CLIENTS=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -sN -e "SELECT COUNT(*) FROM Clients;")
CLIENTS_WITH_UPDATEDAT=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -sN -e "SELECT COUNT(*) FROM Clients WHERE UpdatedAt IS NOT NULL;")
CLIENTS_NOT_DELETED=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -sN -e "SELECT COUNT(*) FROM Clients WHERE IsDeleted = 0;")

echo "📊 Clients: $TOTAL_CLIENTS total, $CLIENTS_WITH_UPDATEDAT avec UpdatedAt, $CLIENTS_NOT_DELETED non supprimés"

# Paiements
TOTAL_PAIEMENTS=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -sN -e "SELECT COUNT(*) FROM Paiements;")
PAIEMENTS_WITH_REQUESTID=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -sN -e "SELECT COUNT(*) FROM Paiements WHERE ClientRequestId IS NOT NULL;")

echo "📊 Paiements: $TOTAL_PAIEMENTS total, $PAIEMENTS_WITH_REQUESTID avec ClientRequestId"

# 6. Test de l'application
echo "🧪 Test de l'application..."
dotnet build --configuration Release

if [ $? -eq 0 ]; then
    echo "✅ Build de l'application réussie"
else
    echo "❌ Échec du build de l'application"
    exit 1
fi

# 7. Nettoyage
echo "🧹 Nettoyage des fichiers temporaires..."

# Conserver le backup le plus récent, supprimer les anciens (garder 5)
mkdir -p ./backups
cd ./backups
ls -t *.sql | tail -n +6 | xargs -r rm -f

echo "✅ MIGRATION SYNCHRONISATION TERMINÉE"
echo "⏰ $(date)"
echo "📊 Résumé:"
echo "  - Backup: $BACKUP_FILE"
echo "  - Colonnes: $EXPECTED_COLUMNS/4"
echo "  - Index: $EXPECTED_INDEXES/3"
echo "  - Clients: $TOTAL_CLIENTS"
echo "  - Paiements: $TOTAL_PAIEMENTS"
echo ""
echo "🎯 Prochaine étape: Déploiement blue-green"
echo "📋 Commande: ./deploy_sync_production.sh"
