-- =============================================================================
-- Rattrapage : IdSite des comptes Admin (agent + utilisateur) → site principal actif
-- À exécuter sur UAT/prod pour les sociétés créées avant le correctif bootstrap.
-- =============================================================================

-- Prévisualisation (agents Admin sans IdSite mais société avec principal actif)
SELECT
    a.IdAgent,
    a.IdSociete,
    a.NomComplet,
    a.RoleAgent,
    a.IdSite AS agent_id_site_actuel,
    s.IdSite AS site_principal_id,
    s.NomSite AS site_principal_nom
FROM Agents a
INNER JOIN Sites s ON s.IdSociete = a.IdSociete AND s.IsSitePrincipal = 1 AND s.Statut = 1
WHERE a.RoleAgent = 'Admin'
  AND a.IdSite IS NULL
ORDER BY a.IdSociete, a.IdAgent;

START TRANSACTION;

UPDATE Agents a
INNER JOIN Sites s ON s.IdSociete = a.IdSociete AND s.IsSitePrincipal = 1 AND s.Statut = 1
SET a.IdSite = s.IdSite
WHERE a.RoleAgent = 'Admin'
  AND a.IdSite IS NULL;

UPDATE Utilisateurs u
INNER JOIN Agents a ON a.IdAgent = u.IdAgent
INNER JOIN Sites s ON s.IdSociete = a.IdSociete AND s.IsSitePrincipal = 1 AND s.Statut = 1
SET u.IdSite = s.IdSite
WHERE a.RoleAgent = 'Admin'
  AND u.IdSite IS NULL;

COMMIT;

-- Contrôle post-fix
SELECT a.IdAgent, a.IdSociete, a.IdSite AS agent_id_site, u.IdUtilisateur, u.IdSite AS user_id_site
FROM Agents a
LEFT JOIN Utilisateurs u ON u.IdAgent = a.IdAgent
WHERE a.RoleAgent = 'Admin'
ORDER BY a.IdSociete, a.IdAgent;
