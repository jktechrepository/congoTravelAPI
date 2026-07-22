-- =====================================================
-- Script de Test : Facturation avec le nouveau modèle Usage
-- =====================================================
-- Ce script crée des données de test pour valider la facturation
-- =====================================================

-- 1. Créer une société de test (si elle n'existe pas)
INSERT IGNORE INTO `Societes` (`IdSociete`, `NomSociete`, `AdresseSociete`, `Telephone`, `EmailSociete`, `Statut`, `DateCreation`)
VALUES (1, 'Société Test', 'Adresse Test', '+243900000000', 'test@societe.com', true, NOW());

-- 2. Créer une catégorie de test
INSERT IGNORE INTO `CategorieClients` (`IdCategorie`, `NomCategorie`, `Description`, `Actif`, `IdSociete`, `DateCreation`)
VALUES (1, 'Catégorie Test', 'Catégorie pour les tests de facturation', true, 1, NOW());

-- 3. Créer des usages pour cette catégorie
INSERT IGNORE INTO `Usages` (`IdUsage`, `Libelle`, `Description`, `IdCategorieClient`, `DateCreation`)
VALUES 
    (1, 'Résidentiel', 'Usage résidentiel pour les particuliers', 1, NOW()),
    (2, 'Commercial', 'Usage commercial pour les entreprises', 1, NOW()),
    (3, 'Industriel', 'Usage industriel pour les grandes industries', 1, NOW());

-- 4. Créer un client de test
INSERT IGNORE INTO `Clients` (`IdClient`, `NomClient`, `AdresseClient`, `Telephone`, `EmailClient`, `Statut`, `IsActif`, `DateCreation`)
VALUES (1, 'Client Test Facturation', 'Adresse Client Test', '+243900000001', 'client.test@email.com', true, true, NOW());

-- 5. Assigner des usages au client (avec nombreBatiment)
INSERT IGNORE INTO `ClientUsages` (`IdClientUsage`, `IdClient`, `IdUsage`, `nombreBatiment`, `DateAttribution`)
VALUES 
    (1, 1, 1, 1, NOW()),  -- Client a 1 bâtiment résidentiel
    (2, 1, 2, 2, NOW());  -- Client a 2 bâtiments commerciaux

-- 6. Créer des factures pour ces usages
-- Facture résidentielle
INSERT INTO `Factures` (
    `NumeroFacture`, 
    `Montant`, 
    `DateEmission`, 
    `MoisEmission`, 
    `AnneesEmission`, 
    `IdUsage`, 
    `Statut`, 
    `DateCreation`
)
VALUES (
    'FAC-RES-1224-0001',
    50000.00,
    '2024-12-01',
    12,
    2024,
    1,  -- Usage Résidentiel
    true,
    NOW()
);

-- Facture commerciale
INSERT INTO `Factures` (
    `NumeroFacture`, 
    `Montant`, 
    `DateEmission`, 
    `MoisEmission`, 
    `AnneesEmission`, 
    `IdUsage`, 
    `Statut`, 
    `DateCreation`
)
VALUES (
    'FAC-COM-1224-0001',
    100000.00,
    '2024-12-01',
    12,
    2024,
    2,  -- Usage Commercial
    true,
    NOW()
);

-- 7. Vérifier les données créées
SELECT '=== VÉRIFICATION DES DONNÉES ===' AS Info;

-- Vérifier les usages
SELECT 'Usages créés:' AS Info;
SELECT u.IdUsage, u.Libelle, u.Description, cc.NomCategorie AS Categorie
FROM Usages u
INNER JOIN CategorieClients cc ON u.IdCategorieClient = cc.IdCategorie
WHERE u.IdCategorieClient = 1;

-- Vérifier les relations ClientUsage
SELECT 'Relations Client-Usage:' AS Info;
SELECT 
    cu.IdClientUsage,
    c.NomClient,
    u.Libelle AS Usage,
    cu.nombreBatiment
FROM ClientUsages cu
INNER JOIN Clients c ON cu.IdClient = c.IdClient
INNER JOIN Usages u ON cu.IdUsage = u.IdUsage
WHERE cu.IdClient = 1;

-- Vérifier les factures
SELECT 'Factures créées:' AS Info;
SELECT 
    f.IdFacture,
    f.NumeroFacture,
    f.Montant,
    f.DateEmission,
    u.Libelle AS Usage,
    cc.NomCategorie AS Categorie
FROM Factures f
INNER JOIN Usages u ON f.IdUsage = u.IdUsage
INNER JOIN CategorieClients cc ON u.IdCategorieClient = cc.IdCategorie
WHERE f.IdUsage IN (1, 2);

-- 8. Test de calcul des arriérés (avec nombreBatiment)
SELECT '=== TEST CALCUL ARRIÉRÉS ===' AS Info;
SELECT 
    c.IdClient,
    c.NomClient,
    u.Libelle AS Usage,
    cu.nombreBatiment,
    f.NumeroFacture,
    f.Montant AS MontantFacture,
    (f.Montant * cu.nombreBatiment) AS MontantTotalAvecBatiments,
    COALESCE(SUM(p.MontantPaye), 0) AS MontantPaye,
    ((f.Montant * cu.nombreBatiment) - COALESCE(SUM(p.MontantPaye), 0)) AS MontantDu
FROM Clients c
INNER JOIN ClientUsages cu ON c.IdClient = cu.IdClient
INNER JOIN Usages u ON cu.IdUsage = u.IdUsage
INNER JOIN Factures f ON u.IdUsage = f.IdUsage
LEFT JOIN Paiements p ON f.IdFacture = p.IdFacture AND (p.Statut = 'Validé' OR p.Statut = 'true')
WHERE c.IdClient = 1
  AND f.Statut = true
GROUP BY c.IdClient, c.NomClient, u.Libelle, cu.nombreBatiment, f.IdFacture, f.NumeroFacture, f.Montant
ORDER BY f.DateEmission DESC;

-- =====================================================
-- Script terminé
-- =====================================================
-- Vous pouvez maintenant tester les endpoints API :
-- 1. GET /api/Facture - Lister les factures
-- 2. GET /api/Facture/usage/{idUsage} - Factures par usage
-- 3. POST /api/Facture - Créer une nouvelle facture
-- 4. GET /api/Arrieres/client/{idClient} - Calculer les arriérés
-- =====================================================
