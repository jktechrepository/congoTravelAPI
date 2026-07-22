-- =============================================================================
-- BACKFILL — Utilisateurs.IdSite depuis Agents.IdSite (idempotent MySQL 8+)
-- =============================================================================
--
-- Contexte : à la création automatique du compte utilisateur (POST /api/Agent),
-- IdSite n'était pas toujours copié depuis l'agent lié.
--
-- EXÉCUTION :
--   1. USE nom_de_votre_base;
--   2. Exécuter la section « Vérification avant »
--   3. Exécuter la section « Mise à jour »
--   4. Exécuter la section « Vérification après » (0 ligne attendue en désalignement)
-- =============================================================================

SET @db := DATABASE();

-- -----------------------------------------------------------------------------
-- Vérification avant — utilisateurs liés à un agent avec IdSite désaligné
-- -----------------------------------------------------------------------------
SELECT
    u.IdUtilisateur,
    u.IdAgent,
    u.IdSite AS UtilisateurIdSite,
    a.IdSite AS AgentIdSite,
    u.IdSociete,
    a.IdSociete AS AgentIdSociete
FROM `Utilisateurs` u
INNER JOIN `Agents` a ON a.IdAgent = u.IdAgent
WHERE a.IdSite IS NOT NULL
  AND (u.IdSite IS NULL OR u.IdSite <> a.IdSite);

-- -----------------------------------------------------------------------------
-- Mise à jour
-- -----------------------------------------------------------------------------
UPDATE `Utilisateurs` u
INNER JOIN `Agents` a ON a.IdAgent = u.IdAgent
SET u.IdSite = a.IdSite
WHERE a.IdSite IS NOT NULL
  AND (u.IdSite IS NULL OR u.IdSite <> a.IdSite);

-- -----------------------------------------------------------------------------
-- Vérification après — doit retourner 0 ligne
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS LignesEncoreDesalignees
FROM `Utilisateurs` u
INNER JOIN `Agents` a ON a.IdAgent = u.IdAgent
WHERE a.IdSite IS NOT NULL
  AND (u.IdSite IS NULL OR u.IdSite <> a.IdSite);
