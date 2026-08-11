-- =============================================================================
-- Job d'expiration HOLD — Site Touristique V1 (procédure + event scheduler)
-- =============================================================================

SOURCE production_site_touristique_hold_expiration_procedure_only.sql;

SET GLOBAL event_scheduler = ON;

DROP EVENT IF EXISTS `ev_ExpireSiteTouristiqueHolds`;
CREATE EVENT `ev_ExpireSiteTouristiqueHolds`
ON SCHEDULE EVERY 1 MINUTE
DO CALL `sp_ExpireSiteTouristiqueHolds`();
