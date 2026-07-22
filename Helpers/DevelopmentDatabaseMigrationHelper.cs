using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using CongoTravel.Data;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Applique les migrations EF en Development en gérant une base déjà peuplée
    /// mais sans historique EF complet (schéma créé hors EF, autre environnement, etc.).
    /// </summary>
    public static class DevelopmentDatabaseMigrationHelper
    {
        private const string ProductVersion = "6.0.25";
        private const string InitialMigrationId = "20260507163135_InitialMigration";

        /// <summary>Première migration à appliquer réellement si le schéma existe déjà (FlexPay+).</summary>
        private const string FirstAutoApplyMigrationId = "20260524142738_FlexPayRegressionFoundation";

        public static async Task MigrateSafelyAsync(
            CongoTravelDbContext context,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            var allMigrations = context.Database.GetMigrations().ToList();
            var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToHashSet();
            var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            var schemaExists = await SchemaAlreadyExistsAsync(context, cancellationToken);
            logger.LogDebug(
                "EF migrations — schéma existant: {SchemaExists}, appliquées: {AppliedCount}, en attente: {PendingCount}",
                schemaExists,
                applied.Count,
                pending.Count);

            if (schemaExists)
            {
                var baselineIds = allMigrations
                    .TakeWhile(m => string.CompareOrdinal(m, FirstAutoApplyMigrationId) < 0)
                    .Where(m => !applied.Contains(m))
                    .ToList();

                if (baselineIds.Count > 0)
                {
                    logger.LogWarning(
                        "Base déjà peuplée : enregistrement de {Count} migration(s) dans __EFMigrationsHistory " +
                        "(sans réexécuter le SQL) : {Migrations}",
                        baselineIds.Count,
                        string.Join(", ", baselineIds));

                    await EnsureMigrationsHistoryTableAsync(context, cancellationToken);
                    await InsertBaselineHistoryAsync(context, logger, baselineIds, cancellationToken);
                }
            }

            pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count == 0)
            {
                logger.LogInformation("Aucune migration EF en attente.");
                return;
            }

            if (schemaExists && pending.Contains(InitialMigrationId))
            {
                logger.LogError(
                    "Impossible d'aligner l'historique EF : la migration initiale est toujours en attente alors que " +
                    "des tables existent déjà. Exécutez le script Scripts/sync-ef-migration-history-before-flexpay.sql " +
                    "sur la base, puis relancez l'API.");
                return;
            }

            logger.LogInformation(
                "Application de {Count} migration(s) EF : {Migrations}",
                pending.Count,
                string.Join(", ", pending));

            try
            {
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Migrations EF appliquées avec succès.");
            }
            catch (MySqlException ex) when (IsTableAlreadyExists(ex))
            {
                logger.LogError(
                    ex,
                    "Migration EF interrompue : table déjà existante ({Message}). " +
                    "Exécutez Scripts/reconcile_ef_migrations_history.sql puis " +
                    "dotnet ef database update --project CongoTravel.csproj",
                    ex.Message);
            }
            catch (MySqlException ex) when (IsDuplicateColumn(ex))
            {
                logger.LogError(
                    ex,
                    "Migration EF interrompue : colonne déjà existante ({Message}). " +
                    "Le schéma physique est en avance sur __EFMigrationsHistory. " +
                    "Exécutez Scripts/reconcile_ef_migrations_history.sql sur la base, puis relancez l'API.",
                    ex.Message);
            }
        }

        private static bool IsDuplicateColumn(MySqlException ex) =>
            ex.Number == 1060
            || ex.Message.Contains("Duplicate column", StringComparison.OrdinalIgnoreCase);

        private static bool IsTableAlreadyExists(MySqlException ex) =>
            ex.Number == 1050
            || ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Détection insensible à la casse (MySQL Linux : <c>auditlogs</c> vs <c>AuditLogs</c>).
        /// </summary>
        private static async Task<bool> SchemaAlreadyExistsAsync(
            CongoTravelDbContext context,
            CancellationToken cancellationToken)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;
            if (shouldClose)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES " +
                    "WHERE TABLE_SCHEMA = DATABASE() " +
                    "AND LOWER(TABLE_NAME) IN ('auditlogs', 'societes', '__efmigrationshistory')";
                var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
                return count >= 1;
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }

        private static async Task EnsureMigrationsHistoryTableAsync(
            CongoTravelDbContext context,
            CancellationToken cancellationToken)
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (" +
                "`MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL, " +
                "`ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL, " +
                "CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)" +
                ") CHARACTER SET=utf8mb4;",
                cancellationToken);
        }

        private static async Task InsertBaselineHistoryAsync(
            CongoTravelDbContext context,
            ILogger logger,
            IReadOnlyList<string> migrationIds,
            CancellationToken cancellationToken)
        {
            foreach (var migrationId in migrationIds)
            {
                var rows = await context.Database.ExecuteSqlRawAsync(
                    "INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ({0}, {1})",
                    new object[] { migrationId, ProductVersion },
                    cancellationToken: cancellationToken);

                if (rows > 0)
                    logger.LogInformation("Baseline enregistré : {MigrationId}", migrationId);
            }
        }
    }
}
