-- =====================================================
-- SCRIPT SQL - AJOUT DES INDEX UNIQUES SUR CLIENTS
-- =====================================================
-- Version: 1.0.0
-- Date: 24/04/2026
-- Description: Ajout des index uniques sur EmailClient et Telephone pour les endpoints publics
-- Base de données: MySQL
-- =====================================================

-- =====================================================
-- INSTRUCTIONS
-- =====================================================
-- 1. Exécuter ce script pour ajouter les index uniques
-- 2. Vérifier qu'aucun doublon n'existe avant l'exécution
-- 3. Monitorer la performance après l'ajout des index
-- =====================================================

-- =====================================================
-- DÉBUT DU SCRIPT
-- =====================================================

SELECT 'DÉBUT DE LA MIGRATION - AJOUT DES INDEX UNIQUES CLIENTS...' AS Message;
SELECT CONCAT('Timestamp: ', NOW()) AS Timestamp;

-- =====================================================
-- SECTION 1: VÉRIFICATION DES DONNÉES EXISTANTES
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 1: VÉRIFICATION DES DONNÉES EXISTANTES' AS Section;

-- Vérifier s'il existe des doublons d'email
SET @email_duplicates = (
    SELECT COUNT(*) - COUNT(DISTINCT EmailClient)
    FROM Clients 
    WHERE EmailClient IS NOT NULL
);

SELECT CONCAT('Doublons d\'email détectés: ', @email_duplicates) AS EmailDuplicates;

-- Vérifier s'il existe des doublons de téléphone
SET @phone_duplicates = (
    SELECT COUNT(*) - COUNT(DISTINCT Telephone)
    FROM Clients 
    WHERE Telephone IS NOT NULL
);

SELECT CONCAT('Doublons de téléphone détectés: ', @phone_duplicates) AS PhoneDuplicates;

-- Afficher les doublons d'email s'ils existent
IF @email_duplicates > 0 THEN
    SELECT 'Doublons d\'email à corriger:' AS Warning;
    SELECT 
        EmailClient,
        COUNT(*) AS Count,
        GROUP_CONCAT(IdClient) AS ClientIds
    FROM Clients 
    WHERE EmailClient IS NOT NULL
    GROUP BY EmailClient
    HAVING COUNT(*) > 1
    ORDER BY Count DESC;
END IF;

-- Afficher les doublons de téléphone s'ils existent
IF @phone_duplicates > 0 THEN
    SELECT 'Doublons de téléphone à corriger:' AS Warning;
    SELECT 
        Telephone,
        COUNT(*) AS Count,
        GROUP_CONCAT(IdClient) AS ClientIds
    FROM Clients 
    WHERE Telephone IS NOT NULL
    GROUP BY Telephone
    HAVING COUNT(*) > 1
    ORDER BY Count DESC;
END IF;

-- =====================================================
-- SECTION 2: VÉRIFICATION DES INDEX EXISTANTS
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 2: VÉRIFICATION DES INDEX EXISTANTS' AS Section;

-- Vérifier si l'index EmailClient existe déjà
SET @email_index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Clients' 
      AND index_name = 'IX_Clients_EmailClient_Unique'
);

IF @email_index_exists > 0 THEN
    SELECT '✓ Index unique sur EmailClient existe déjà' AS Status;
ELSE
    SELECT '⚠️ Index unique sur EmailClient non trouvé - sera créé' AS Status;
END IF;

-- Vérifier si l'index Telephone existe déjà
SET @phone_index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Clients' 
      AND index_name = 'IX_Clients_Telephone_Unique'
);

IF @phone_index_exists > 0 THEN
    SELECT '✓ Index unique sur Telephone existe déjà' AS Status;
ELSE
    SELECT '⚠️ Index unique sur Telephone non trouvé - sera créé' AS Status;
END IF;

-- =====================================================
-- SECTION 3: CRÉATION DES INDEX UNIQUES
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 3: CRÉATION DES INDEX UNIQUES' AS Section;

-- Créer l'index unique sur EmailClient
IF @email_index_exists = 0 AND @email_duplicates = 0 THEN
    SELECT 'Création de l index unique sur EmailClient...' AS Action;
    
    CREATE UNIQUE INDEX IX_Clients_EmailClient_Unique
    ON Clients (EmailClient)
    WHERE EmailClient IS NOT NULL;
    
    SELECT '✓ Index unique IX_Clients_EmailClient_Unique créé avec succès' AS Status;
ELSEIF @email_duplicates > 0 THEN
    SELECT '❌ IMPOSSIBLE: Des doublons d\'email existent - corrigez-les d\'abord' AS Error;
ELSE
    SELECT '✓ Index unique sur EmailClient existe déjà' AS Status;
END IF;

-- Créer l'index unique sur Telephone
IF @phone_index_exists = 0 AND @phone_duplicates = 0 THEN
    SELECT 'Création de l index unique sur Telephone...' AS Action;
    
    CREATE UNIQUE INDEX IX_Clients_Telephone_Unique
    ON Clients (Telephone)
    WHERE Telephone IS NOT NULL;
    
    SELECT '✓ Index unique IX_Clients_Telephone_Unique créé avec succès' AS Status;
ELSEIF @phone_duplicates > 0 THEN
    SELECT '❌ IMPOSSIBLE: Des doublons de téléphone existent - corrigez-les d\'abord' AS Error;
ELSE
    SELECT '✓ Index unique sur Telephone existe déjà' AS Status;
END IF;

-- =====================================================
-- SECTION 4: VALIDATION DES INDEX CRÉÉS
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 4: VALIDATION DES INDEX CRÉÉS' AS Section;

-- Afficher tous les index sur la table Clients
SELECT 'Index actuels sur la table Clients:' AS Title;

SELECT 
    index_name,
    index_type,
    non_unique,
    GROUP_CONCAT(column_name ORDER BY seq_in_index) AS columns,
    index_comment
FROM information_schema.statistics 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients'
GROUP BY index_name, index_type, non_unique, index_comment
ORDER BY index_name;

-- =====================================================
-- SECTION 5: TEST DE PERFORMANCE
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 5: TEST DE PERFORMANCE' AS Section;

-- Test de recherche par email
SELECT 'Test de performance - Recherche par email:' AS TestTitle;

-- Expliquer la requête de recherche email
EXPLAIN FORMAT=JSON
SELECT * FROM Clients WHERE EmailClient = 'test@example.com';

-- Test de recherche par téléphone
SELECT 'Test de performance - Recherche par téléphone:' AS TestTitle;

-- Expliquer la requête de recherche téléphone
EXPLAIN FORMAT=JSON
SELECT * FROM Clients WHERE Telephone = '+243123456789';

-- =====================================================
-- SECTION 6: STATISTIQUES FINALES
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 6: STATISTIQUES FINALES' AS Section;

-- Statistiques de la table Clients
SELECT 'Statistiques de la table Clients:' AS StatsTitle;

SELECT 
    COUNT(*) AS TotalClients,
    COUNT(EmailClient) AS ClientsWithEmail,
    COUNT(Telephone) AS ClientsWithPhone,
    COUNT(DISTINCT EmailClient) AS UniqueEmails,
    COUNT(DISTINCT Telephone) AS UniquePhones,
    NOW() AS Timestamp
FROM Clients;

-- Vérifier l'intégrité des données
SELECT 'Vérification de l intégrité des données:' AS IntegrityTitle;

SELECT 
    CASE 
        WHEN COUNT(EmailClient) = COUNT(DISTINCT EmailClient) THEN '✅ Aucun doublon d\'email'
        ELSE '❌ Doublons d\'email détectés'
    END AS EmailIntegrity,
    CASE 
        WHEN COUNT(Telephone) = COUNT(DISTINCT Telephone) THEN '✅ Aucun doublon de téléphone'
        ELSE '❌ Doublons de téléphone détectés'
    END AS PhoneIntegrity
FROM Clients;

-- =====================================================
-- SECTION 7: INSTRUCTIONS POST-MIGRATION
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 7: INSTRUCTIONS POST-MIGRATION' AS Section;
SELECT '' AS Separator;
SELECT '=====================================================' AS Separator;
SELECT 'MIGRATION TERMINÉE AVEC SUCCÈS!' AS Status;
SELECT '=====================================================' AS Separator;
SELECT '' AS Separator;
SELECT 'Actions requises après la migration:' AS Actions;
SELECT '1. Redémarrer l application CongoTravel API' AS Step1;
SELECT '2. Tester les endpoints d inscription publique' AS Step2;
SELECT '3. Vérifier les performances des recherches email/téléphone' AS Step3;
SELECT '4. Monitorer les logs pour détecter les erreurs' AS Step4;
SELECT '5. Tester la création de doublons (doit échouer)' AS Step5;
SELECT '' AS Separator;
SELECT 'Endpoints à tester:' AS Endpoints;
SELECT '- POST /api/client/register' AS Endpoint1;
SELECT '- POST /api/client/check-email' AS Endpoint2;
SELECT '' AS Separator;
SELECT 'Bénéfices attendus:' AS Benefits;
SELECT '- Performance améliorée des recherches email/téléphone' AS Benefit1;
SELECT '- Protection contre les doublons d\'email et téléphone' AS Benefit2;
SELECT '- Intégrité des données garantie' AS Benefit3;
SELECT '- Support optimal pour les endpoints publics' AS Benefit4;
SELECT '' AS Separator;
SELECT CONCAT('Timestamp final: ', NOW()) AS Timestamp;
SELECT '=====================================================' AS Separator;

-- =====================================================
-- FIN DU SCRIPT
-- =====================================================

-- Afficher un résumé final
SELECT 
    'Migration Index Uniques Clients' as Nom,
    '1.0.0' as Version,
    NOW() as DateExecution,
    CASE 
        WHEN @email_duplicates = 0 AND @phone_duplicates = 0 THEN 'SUCCÈS' 
        ELSE 'PARTIEL' 
    END as Statut,
    @email_duplicates as EmailDuplicates,
    @phone_duplicates as PhoneDuplicates,
    CASE 
        WHEN @email_duplicates = 0 AND @phone_duplicates = 0 
        THEN 'Index uniques créés avec succès'
        ELSE 'Corrigez les doublons avant de continuer'
    END as Message;

SELECT '' AS Separator;
SELECT '🚀 Les index uniques sur EmailClient et Telephone sont maintenant prêts!' AS FinalMessage;
