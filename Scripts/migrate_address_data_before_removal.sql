-- ============================================================================
-- SCRIPT DE MIGRATION DE DONNÉES : Adresses structurées vers AdresseResidence
-- ============================================================================
-- 
-- ⚠️ IMPORTANT : Exécuter ce script AVANT d'appliquer les migrations :
--    - RemoveAgentAddressFields
--    - RemoveUtilisateurAddressFields
--    - RemoveSocieteAddressFields
--
-- Ce script concatène les champs d'adresse structurés existants dans 
-- le champ AdresseResidence pour éviter la perte de données.
--
-- ============================================================================

USE `kenergie_db`; -- ⚠️ Remplacer par le nom de votre base de données

-- ============================================================================
-- 1. MIGRATION DES ADRESSES DES AGENTS
-- ============================================================================

-- Mettre à jour AdresseResidence avec les données existantes
-- Format : "Province, Ville, Commune, Quartier, Avenue, Numero"
UPDATE `Agents`
SET `AdresseResidence` = TRIM(
    CONCAT_WS(', ',
        NULLIF(TRIM(`Province`), ''),
        NULLIF(TRIM(`Ville`), ''),
        NULLIF(TRIM(`Commune`), ''),
        NULLIF(TRIM(`Quartier`), ''),
        NULLIF(TRIM(`Avenue`), ''),
        NULLIF(TRIM(`Numero`), '')
    )
)
WHERE `AdresseResidence` IS NULL 
  AND (
    `Province` IS NOT NULL OR 
    `Ville` IS NOT NULL OR 
    `Commune` IS NOT NULL OR 
    `Quartier` IS NOT NULL OR 
    `Avenue` IS NOT NULL OR 
    `Numero` IS NOT NULL
  );

-- Vérification : Afficher le nombre d'agents mis à jour
SELECT 
    COUNT(*) AS 'Agents mis à jour',
    COUNT(CASE WHEN `AdresseResidence` IS NOT NULL THEN 1 END) AS 'Agents avec AdresseResidence'
FROM `Agents`;

-- Aperçu des données migrées (premiers 10 agents)
SELECT 
    `IdAgent`,
    `NomComplet`,
    `AdresseResidence`,
    CONCAT_WS(', ',
        `Province`,
        `Ville`,
        `Commune`,
        `Quartier`,
        `Avenue`,
        `Numero`
    ) AS 'Ancienne adresse (pour vérification)'
FROM `Agents`
WHERE `AdresseResidence` IS NOT NULL
LIMIT 10;

-- ============================================================================
-- 2. MIGRATION DES ADRESSES DES UTILISATEURS
-- ============================================================================

-- Mettre à jour AdresseResidence avec les données existantes
UPDATE `Utilisateurs`
SET `AdresseResidence` = TRIM(
    CONCAT_WS(', ',
        NULLIF(TRIM(`Province`), ''),
        NULLIF(TRIM(`Ville`), ''),
        NULLIF(TRIM(`Commune`), ''),
        NULLIF(TRIM(`Quartier`), ''),
        NULLIF(TRIM(`Avenue`), ''),
        NULLIF(TRIM(`Numero`), '')
    )
)
WHERE `AdresseResidence` IS NULL 
  AND (
    `Province` IS NOT NULL OR 
    `Ville` IS NOT NULL OR 
    `Commune` IS NOT NULL OR 
    `Quartier` IS NOT NULL OR 
    `Avenue` IS NOT NULL OR 
    `Numero` IS NOT NULL
  );

-- Vérification : Afficher le nombre d'utilisateurs mis à jour
SELECT 
    COUNT(*) AS 'Utilisateurs totaux',
    COUNT(CASE WHEN `AdresseResidence` IS NOT NULL THEN 1 END) AS 'Utilisateurs avec AdresseResidence'
FROM `Utilisateurs`;

-- Aperçu des données migrées (premiers 10 utilisateurs)
SELECT 
    `IdUtilisateur`,
    `NomComplet`,
    `Email`,
    `AdresseResidence`,
    CONCAT_WS(', ',
        `Province`,
        `Ville`,
        `Commune`,
        `Quartier`,
        `Avenue`,
        `Numero`
    ) AS 'Ancienne adresse (pour vérification)'
FROM `Utilisateurs`
WHERE `AdresseResidence` IS NOT NULL
LIMIT 10;

-- ============================================================================
-- 3. MIGRATION DES ADRESSES DES SOCIETES
-- ============================================================================

-- Mettre à jour AdresseResidence avec les données existantes
UPDATE `Societes`
SET `AdresseResidence` = TRIM(
    CONCAT_WS(', ',
        NULLIF(TRIM(`Province`), ''),
        NULLIF(TRIM(`Ville`), ''),
        NULLIF(TRIM(`Commune`), ''),
        NULLIF(TRIM(`Quartier`), ''),
        NULLIF(TRIM(`Avenue`), ''),
        NULLIF(TRIM(`Numero`), '')
    )
)
WHERE `AdresseResidence` IS NULL 
  AND (
    `Province` IS NOT NULL OR 
    `Ville` IS NOT NULL OR 
    `Commune` IS NOT NULL OR 
    `Quartier` IS NOT NULL OR 
    `Avenue` IS NOT NULL OR 
    `Numero` IS NOT NULL
  );

-- Vérification : Afficher le nombre de sociétés mises à jour
SELECT 
    COUNT(*) AS 'Societes totales',
    COUNT(CASE WHEN `AdresseResidence` IS NOT NULL THEN 1 END) AS 'Societes avec AdresseResidence'
FROM `Societes`;

-- Aperçu des données migrées (premières 10 sociétés)
SELECT 
    `IdSociete`,
    `Nom`,
    `AdresseResidence`,
    CONCAT_WS(', ',
        `Province`,
        `Ville`,
        `Commune`,
        `Quartier`,
        `Avenue`,
        `Numero`
    ) AS 'Ancienne adresse (pour vérification)'
FROM `Societes`
WHERE `AdresseResidence` IS NOT NULL
LIMIT 10;

-- ============================================================================
-- 4. STATISTIQUES DE MIGRATION
-- ============================================================================

-- Statistiques globales
SELECT 
    'Agents' AS 'Table',
    COUNT(*) AS 'Total',
    COUNT(CASE WHEN `AdresseResidence` IS NOT NULL THEN 1 END) AS 'Avec AdresseResidence',
    COUNT(CASE WHEN `Province` IS NOT NULL OR `Ville` IS NOT NULL THEN 1 END) AS 'Avec ancienne adresse'
FROM `Agents`
UNION ALL
SELECT 
    'Utilisateurs' AS 'Table',
    COUNT(*) AS 'Total',
    COUNT(CASE WHEN `AdresseResidence` IS NOT NULL THEN 1 END) AS 'Avec AdresseResidence',
    COUNT(CASE WHEN `Province` IS NOT NULL OR `Ville` IS NOT NULL THEN 1 END) AS 'Avec ancienne adresse'
FROM `Utilisateurs`
UNION ALL
SELECT 
    'Societes' AS 'Table',
    COUNT(*) AS 'Total',
    COUNT(CASE WHEN `AdresseResidence` IS NOT NULL THEN 1 END) AS 'Avec AdresseResidence',
    COUNT(CASE WHEN `Province` IS NOT NULL OR `Ville` IS NOT NULL THEN 1 END) AS 'Avec ancienne adresse'
FROM `Societes`;

-- ============================================================================
-- 5. VÉRIFICATION DES DONNÉES MANQUANTES
-- ============================================================================

-- Agents avec ancienne adresse mais sans AdresseResidence (devrait être 0 après migration)
SELECT 
    `IdAgent`,
    `NomComplet`,
    `Province`,
    `Ville`,
    `Commune`,
    `Quartier`,
    `Avenue`,
    `Numero`
FROM `Agents`
WHERE `AdresseResidence` IS NULL
  AND (
    `Province` IS NOT NULL OR 
    `Ville` IS NOT NULL OR 
    `Commune` IS NOT NULL OR 
    `Quartier` IS NOT NULL OR 
    `Avenue` IS NOT NULL OR 
    `Numero` IS NOT NULL
  );

-- Utilisateurs avec ancienne adresse mais sans AdresseResidence (devrait être 0 après migration)
SELECT 
    `IdUtilisateur`,
    `NomComplet`,
    `Email`,
    `Province`,
    `Ville`,
    `Commune`,
    `Quartier`,
    `Avenue`,
    `Numero`
FROM `Utilisateurs`
WHERE `AdresseResidence` IS NULL
  AND (
    `Province` IS NOT NULL OR 
    `Ville` IS NOT NULL OR 
    `Commune` IS NOT NULL OR 
    `Quartier` IS NOT NULL OR 
    `Avenue` IS NOT NULL OR 
    `Numero` IS NOT NULL
  );

-- Societes avec ancienne adresse mais sans AdresseResidence (devrait être 0 après migration)
SELECT 
    `IdSociete`,
    `Nom`,
    `Province`,
    `Ville`,
    `Commune`,
    `Quartier`,
    `Avenue`,
    `Numero`
FROM `Societes`
WHERE `AdresseResidence` IS NULL
  AND (
    `Province` IS NOT NULL OR 
    `Ville` IS NOT NULL OR 
    `Commune` IS NOT NULL OR 
    `Quartier` IS NOT NULL OR 
    `Avenue` IS NOT NULL OR 
    `Numero` IS NOT NULL
  );

-- ============================================================================
-- NOTES IMPORTANTES
-- ============================================================================
--
-- 1. Ce script utilise CONCAT_WS qui ignore les valeurs NULL
-- 2. Les virgules sont ajoutées automatiquement entre les valeurs non nulles
-- 3. TRIM() est utilisé pour supprimer les espaces en début/fin
-- 4. Le format final sera : "Province, Ville, Commune, Quartier, Avenue, Numero"
--    (sans les valeurs NULL)
--
-- Exemple de résultat :
--   Avant : Province="Kinshasa", Ville="Gombe", Commune=NULL, Quartier="Centre-ville"
--   Après : "Kinshasa, Gombe, Centre-ville"
--
-- ============================================================================

