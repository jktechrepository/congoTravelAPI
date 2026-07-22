-- =============================================================================
-- CongoTravel — EvenementPayment devise de paiement (D_t / D_p)
-- Migration : 20260717171358_EvenementPaymentDevisePaiement
-- =============================================================================
-- Prérequis : table EvenementPayments déjà présente.
-- Vérifier avant :
--   SELECT * FROM `__EFMigrationsHistory`
--     WHERE `MigrationId` = '20260717171358_EvenementPaymentDevisePaiement';
-- =============================================================================

START TRANSACTION;

ALTER TABLE `EvenementPayments`
    ADD `CodeDeviseTarif` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    ADD `MontantTarif` decimal(18,2) NOT NULL DEFAULT 0.00,
    ADD `TauxVersDevisePaiement` decimal(18,8) NOT NULL DEFAULT 1.00000000;

UPDATE `EvenementPayments`
SET `MontantTarif` = `Montant`,
    `CodeDeviseTarif` = `CodeDevise`,
    `TauxVersDevisePaiement` = 1;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260717171358_EvenementPaymentDevisePaiement', '6.0.25');

COMMIT;
