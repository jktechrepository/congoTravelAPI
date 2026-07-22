-- =============================================================================
-- Audit pré-migration ConfigSociete : divergences des règles par société
-- Exécuter en staging/prod AVANT Scripts/production_configsociete.sql
-- =============================================================================

-- 1. Sociétés où les voyages ont des valeurs différentes
SELECT v.IdSociete,
       s.Nom AS NomSociete,
       COUNT(*) AS nb_voyages,
       COUNT(DISTINCT v.PenaliteReaffectation) AS nb_penalites_distinctes,
       COUNT(DISTINCT v.DureeValiditeBilletJours) AS nb_durees_distinctes,
       COUNT(DISTINCT v.HeuresLimiteReaffectation) AS nb_heures_distinctes,
       MIN(v.PenaliteReaffectation) AS penalite_min,
       MAX(v.PenaliteReaffectation) AS penalite_max,
       MIN(v.DureeValiditeBilletJours) AS duree_min,
       MAX(v.DureeValiditeBilletJours) AS duree_max,
       MIN(v.HeuresLimiteReaffectation) AS heures_min,
       MAX(v.HeuresLimiteReaffectation) AS heures_max
FROM Voyages v
INNER JOIN Societes s ON s.IdSociete = v.IdSociete
GROUP BY v.IdSociete, s.Nom
HAVING nb_penalites_distinctes > 1
    OR nb_durees_distinctes > 1
    OR nb_heures_distinctes > 1
ORDER BY v.IdSociete;

-- 2. Valeurs qui seront retenues par le backfill (voyage le plus récent par DateCreation, puis Id max)
SELECT v.IdSociete,
       v.Id AS IdVoyageRetenu,
       v.DateCreation,
       v.PenaliteReaffectation,
       v.DureeValiditeBilletJours,
       v.HeuresLimiteReaffectation
FROM Voyages v
INNER JOIN (
    SELECT v1.IdSociete, MAX(v1.Id) AS IdVoyageRetenu
    FROM Voyages v1
    INNER JOIN (
        SELECT IdSociete, MAX(DateCreation) AS MaxDateCreation
        FROM Voyages
        GROUP BY IdSociete
    ) m ON m.IdSociete = v1.IdSociete AND v1.DateCreation = m.MaxDateCreation
    GROUP BY v1.IdSociete
) pick ON pick.IdVoyageRetenu = v.Id
ORDER BY v.IdSociete;

-- 3. Sociétés sans voyage (recevront les défauts 0 / 0 / 2)
SELECT s.IdSociete, s.Nom
FROM Societes s
WHERE NOT EXISTS (SELECT 1 FROM Voyages v WHERE v.IdSociete = s.IdSociete);
