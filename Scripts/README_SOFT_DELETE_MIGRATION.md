# 📋 Guide de Migration - Soft Delete

## 🎯 Objectif

Ce script SQL ajoute les colonnes nécessaires pour implémenter le soft delete sur les entités suivantes :
- **Axe** : colonne `Statut`
- **Cabine** : colonne `Statut`
- **CommunicationCampaign** : colonne `Statut`
- **PlainteClient** : colonne `Statut`
- **Paiement** : colonne `IsDeleted`

---

## 📝 Prérequis

1. **Sauvegarde de la base de données** : ⚠️ **OBLIGATOIRE** avant d'exécuter le script
2. **Accès administrateur** à la base de données MySQL/MariaDB
3. **Maintenance prévue** : Exécuter pendant une fenêtre de maintenance

---

## 🚀 Exécution du Script

### Option 1 : Via MySQL CLI

```bash
mysql -u root -p nom_de_la_base < Scripts/production_add_soft_delete_fields.sql
```

### Option 2 : Via MySQL Workbench / phpMyAdmin

1. Ouvrir le fichier `Scripts/production_add_soft_delete_fields.sql`
2. Sélectionner la base de données cible
3. Exécuter le script complet

### Option 3 : Via ligne de commande MySQL

```sql
SOURCE /chemin/vers/Scripts/production_add_soft_delete_fields.sql;
```

---

## ✅ Vérification Post-Migration

### 1. Vérifier les colonnes ajoutées

```sql
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    COLUMN_TYPE,
    COLUMN_DEFAULT,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
AND (
    (TABLE_NAME = 'Axes' AND COLUMN_NAME = 'Statut') OR
    (TABLE_NAME = 'Cabines' AND COLUMN_NAME = 'Statut') OR
    (TABLE_NAME = 'CommunicationCampaigns' AND COLUMN_NAME = 'Statut') OR
    (TABLE_NAME = 'PlainteClients' AND COLUMN_NAME = 'Statut') OR
    (TABLE_NAME = 'Paiements' AND COLUMN_NAME = 'IsDeleted')
)
ORDER BY TABLE_NAME, COLUMN_NAME;
```

### 2. Vérifier les index créés

```sql
SELECT 
    TABLE_NAME,
    INDEX_NAME,
    COLUMN_NAME
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
AND (
    INDEX_NAME = 'IX_Axes_Statut' OR
    INDEX_NAME = 'IX_Cabines_Statut' OR
    INDEX_NAME = 'IX_CommunicationCampaigns_Statut' OR
    INDEX_NAME = 'IX_PlainteClients_Statut' OR
    INDEX_NAME = 'IX_Paiements_IsDeleted'
)
ORDER BY TABLE_NAME, INDEX_NAME;
```

### 3. Vérifier les valeurs par défaut

```sql
-- Vérifier que toutes les entités existantes ont Statut = 1
SELECT 
    'Axes' AS table_name,
    COUNT(*) AS total,
    SUM(CASE WHEN Statut = 1 THEN 1 ELSE 0 END) AS actifs,
    SUM(CASE WHEN Statut = 0 THEN 1 ELSE 0 END) AS inactifs
FROM Axes
UNION ALL
SELECT 
    'Cabines' AS table_name,
    COUNT(*) AS total,
    SUM(CASE WHEN Statut = 1 THEN 1 ELSE 0 END) AS actifs,
    SUM(CASE WHEN Statut = 0 THEN 1 ELSE 0 END) AS inactifs
FROM Cabines
UNION ALL
SELECT 
    'CommunicationCampaigns' AS table_name,
    COUNT(*) AS total,
    SUM(CASE WHEN Statut = 1 THEN 1 ELSE 0 END) AS actifs,
    SUM(CASE WHEN Statut = 0 THEN 1 ELSE 0 END) AS inactifs
FROM CommunicationCampaigns
UNION ALL
SELECT 
    'PlainteClients' AS table_name,
    COUNT(*) AS total,
    SUM(CASE WHEN Statut = 1 THEN 1 ELSE 0 END) AS actifs,
    SUM(CASE WHEN Statut = 0 THEN 1 ELSE 0 END) AS inactifs
FROM PlainteClients
UNION ALL
SELECT 
    'Paiements' AS table_name,
    COUNT(*) AS total,
    SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) AS non_supprimes,
    SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS supprimes
FROM Paiements;
```

---

## 🔄 Rollback (En cas de problème)

Si vous devez annuler les modifications, exécutez le script suivant :

```sql
-- ⚠️ ATTENTION : Ce script supprime les colonnes et les index
-- Assurez-vous d'avoir une sauvegarde avant d'exécuter

-- Supprimer les index
DROP INDEX IF EXISTS `IX_Axes_Statut` ON `Axes`;
DROP INDEX IF EXISTS `IX_Cabines_Statut` ON `Cabines`;
DROP INDEX IF EXISTS `IX_CommunicationCampaigns_Statut` ON `CommunicationCampaigns`;
DROP INDEX IF EXISTS `IX_PlainteClients_Statut` ON `PlainteClients`;
DROP INDEX IF EXISTS `IX_Paiements_IsDeleted` ON `Paiements`;

-- Supprimer les colonnes
ALTER TABLE `Axes` DROP COLUMN IF EXISTS `Statut`;
ALTER TABLE `Cabines` DROP COLUMN IF EXISTS `Statut`;
ALTER TABLE `CommunicationCampaigns` DROP COLUMN IF EXISTS `Statut`;
ALTER TABLE `PlainteClients` DROP COLUMN IF EXISTS `Statut`;
ALTER TABLE `Paiements` DROP COLUMN IF EXISTS `IsDeleted`;
```

---

## 📊 Détails des Modifications

### Colonnes Ajoutées

| Table | Colonne | Type | Défaut | Description |
|-------|---------|------|--------|-------------|
| `Axes` | `Statut` | TINYINT(1) | 1 | Statut de l'axe (actif/inactif) |
| `Cabines` | `Statut` | TINYINT(1) | 1 | Statut de la cabine (actif/inactif) |
| `CommunicationCampaigns` | `Statut` | TINYINT(1) | 1 | Statut de la campagne (actif/inactif) |
| `PlainteClients` | `Statut` | TINYINT(1) | 1 | Statut de la plainte (actif/inactif) |
| `Paiements` | `IsDeleted` | TINYINT(1) | 0 | Indique si le paiement est supprimé |

### Index Créés

| Table | Index | Colonne | Objectif |
|-------|-------|---------|----------|
| `Axes` | `IX_Axes_Statut` | `Statut` | Optimiser les requêtes filtrées |
| `Cabines` | `IX_Cabines_Statut` | `Statut` | Optimiser les requêtes filtrées |
| `CommunicationCampaigns` | `IX_CommunicationCampaigns_Statut` | `Statut` | Optimiser les requêtes filtrées |
| `PlainteClients` | `IX_PlainteClients_Statut` | `Statut` | Optimiser les requêtes filtrées |
| `Paiements` | `IX_Paiements_IsDeleted` | `IsDeleted` | Optimiser les requêtes filtrées |

---

## ⚠️ Notes Importantes

1. **Idempotence** : Le script est conçu pour être exécuté plusieurs fois sans erreur. Il vérifie l'existence des colonnes et index avant de les créer.

2. **Valeurs par défaut** : Toutes les entités existantes seront automatiquement mises à jour avec :
   - `Statut = 1` (actif) pour Axe, Cabine, CommunicationCampaign, PlainteClient
   - `IsDeleted = 0` (non supprimé) pour Paiement

3. **Performance** : Les index créés optimiseront les requêtes qui filtrent par `Statut` ou `IsDeleted`.

4. **Compatibilité** : Le script est compatible avec MySQL 5.7+ et MariaDB 10.2+.

---

## 📞 Support

En cas de problème lors de l'exécution du script, vérifiez :
1. Les logs MySQL pour les erreurs détaillées
2. Les permissions de l'utilisateur de base de données
3. L'espace disque disponible
4. La version de MySQL/MariaDB

---

**Date de création :** 2025-01-05  
**Version du script :** 1.0.0
