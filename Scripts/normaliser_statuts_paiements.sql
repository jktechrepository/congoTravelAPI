-- Script pour normaliser les statuts des paiements
-- Convertit "true", "True", "TRUE" en "Validé"
-- À exécuter une seule fois pour corriger les données existantes

UPDATE `Paiements`
SET `Statut` = 'Validé'
WHERE `Statut` IN ('true', 'True', 'TRUE', '1', 'True');

-- Vérification
SELECT 
    `Statut`,
    COUNT(*) as `Nombre`
FROM `Paiements`
GROUP BY `Statut`;

-- Résultat attendu : tous les paiements doivent avoir le statut "Validé" (ou d'autres statuts valides comme "En attente", "Rejeté", etc.)

