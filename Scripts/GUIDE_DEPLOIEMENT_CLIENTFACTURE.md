# 📋 Guide de Déploiement - ClientFacture

## 🎯 Vue d'ensemble

Ce guide explique comment déployer le modèle `ClientFacture` en production en utilisant le script SQL complet.

**Fichier principal :** `production_deploy_clientfacture_complete.sql`  
**Taille :** 18 KB (456 lignes)  
**Durée estimée :** 10-30 minutes selon le volume de données

---

## ⚠️ PRÉ-REQUIS IMPORTANTS

### 1. Backup de la base de données

**OBLIGATOIRE** - Faites un backup complet avant d'exécuter le script :

```bash
# Exemple avec mysqldump
mysqldump -u [user] -p [database_name] > backup_before_clientfacture_$(date +%Y%m%d_%H%M%S).sql

# Ou avec MySQL Workbench / phpMyAdmin
# Exportez la base de données complète
```

### 2. Test sur une base de données de test

**FORTEMENT RECOMMANDÉ** - Testez d'abord sur une copie de la base de données :

```bash
# Créer une base de test
mysql -u root -p -e "CREATE DATABASE kenergie_test;"

# Copier les données
mysqldump -u root -p kenergie | mysql -u root -p kenergie_test

# Tester le script sur la base de test
mysql -u root -p kenergie_test < Scripts/production_deploy_clientfacture_complete.sql
```

### 3. Vérifier la connexion

Assurez-vous d'être connecté à la **bonne base de données** :

```sql
-- Vérifier la base de données actuelle
SELECT DATABASE();

-- Vérifier les tables existantes
SHOW TABLES LIKE 'Factures';
SHOW TABLES LIKE 'Clients';
SHOW TABLES LIKE 'ClientUsages';
SHOW TABLES LIKE 'Paiements';
```

### 4. Période d'exécution

**RECOMMANDÉ** - Exécutez pendant une période de faible activité pour éviter les conflits.

---

## 🚀 Déploiement

### Option 1 : Via ligne de commande MySQL

```bash
# Se connecter à MySQL
mysql -u [user] -p [database_name]

# Ou directement
mysql -u [user] -p [database_name] < Scripts/production_deploy_clientfacture_complete.sql
```

### Option 2 : Via MySQL Workbench

1. Ouvrir MySQL Workbench
2. Se connecter à la base de données
3. Ouvrir le fichier `Scripts/production_deploy_clientfacture_complete.sql`
4. Exécuter le script (Ctrl+Shift+Enter)

### Option 3 : Via phpMyAdmin

1. Se connecter à phpMyAdmin
2. Sélectionner la base de données
3. Aller dans l'onglet "SQL"
4. Copier-coller le contenu du script
5. Cliquer sur "Exécuter"

---

## 📊 Ce que fait le script

Le script est divisé en **5 parties** :

### Partie 1 : Création de la table ClientFactures

- ✅ Crée la table `ClientFactures` si elle n'existe pas
- ✅ Ajoute les contraintes de clés étrangères
- ✅ Crée les index pour optimiser les requêtes
- ✅ Ajoute les commentaires sur les colonnes

**Durée :** ~1-2 secondes

### Partie 2 : Vérifications préalables

- ✅ Compte les factures existantes
- ✅ Compte les ClientFactures déjà créées
- ✅ Compte les paiements existants

**Durée :** ~1-2 secondes

### Partie 3 : Migration des données

- ✅ Crée une `ClientFacture` pour chaque combinaison Facture-Client
- ✅ Calcule le `Montant` (facture.Montant × nombreBatiment)
- ✅ Calcule le `MontantPaye` (somme des Paiements validés)
- ✅ Calcule le `MontantDu` (Montant - MontantPaye)
- ✅ Évite les doublons (idempotent)

**Durée :** 5-20 minutes selon le volume

### Partie 4 : Validation de la migration

- ✅ Vérifie la cohérence des montants (MontantPaye, MontantDu)
- ✅ Détecte les incohérences
- ✅ Vérifie que toutes les factures ont été migrées
- ✅ Détecte les doublons potentiels

**Durée :** 2-5 minutes

### Partie 5 : Commit et finalisation

- ✅ Valide la transaction (COMMIT)
- ✅ Affiche un résumé final
- ✅ Réactive les vérifications de clés étrangères

**Durée :** ~1 seconde

---

## ✅ Vérification post-déploiement

### 1. Vérifier que la table existe

```sql
SHOW TABLES LIKE 'ClientFactures';

-- Vérifier la structure
DESCRIBE ClientFactures;
```

### 2. Vérifier les données migrées

```sql
-- Statistiques générales
SELECT 
    COUNT(*) as Total_ClientFactures,
    COUNT(DISTINCT IdFacture) as Factures_Migrees,
    COUNT(DISTINCT IdClient) as Clients_Concernes,
    SUM(MontantDu) as Total_Arrieres
FROM ClientFactures
WHERE Statut = 1;
```

### 3. Tester les endpoints API

```bash
# Tester le calcul des arriérés (devrait être plus rapide)
curl -X GET "https://api.example.com/api/Client/1/arrieres" \
  -H "Authorization: Bearer {token}"

# Vérifier qu'il n'y a pas d'erreurs dans les logs
```

### 4. Comparer les résultats

Vérifiez que les calculs d'arriérés sont identiques avant/après :

```sql
-- Avant (via l'ancien système - si vous avez gardé une trace)
-- Comparer avec les résultats de l'API après migration
```

---

## 🔧 Dépannage

### Problème : Erreur de clé étrangère

**Solution :** Vérifiez que les tables `Clients` et `Factures` existent :

```sql
SHOW TABLES LIKE 'Clients';
SHOW TABLES LIKE 'Factures';
```

### Problème : Doublons détectés

**Solution :** Le script évite automatiquement les doublons. Si des doublons existent, supprimez-les :

```sql
-- Identifier les doublons
SELECT IdFacture, IdClient, COUNT(*) as Nombre
FROM ClientFactures
WHERE IdFacture IS NOT NULL
GROUP BY IdFacture, IdClient
HAVING COUNT(*) > 1;

-- Supprimer les doublons (garder le plus récent)
DELETE cf1 FROM ClientFactures cf1
INNER JOIN ClientFactures cf2
WHERE cf1.IdFacture = cf2.IdFacture
  AND cf1.IdClient = cf2.IdClient
  AND cf1.IdClientFacture < cf2.IdClientFacture;
```

### Problème : Incohérences de montants

**Solution :** Recalculez les montants :

```sql
-- Recalculer MontantPaye
UPDATE ClientFactures cf
SET MontantPaye = (
    SELECT COALESCE(SUM(p.MontantPaye), 0)
    FROM Paiements p
    WHERE p.IdFacture = cf.IdFacture
      AND p.IdClient = cf.IdClient
      AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
)
WHERE cf.IdFacture IS NOT NULL;

-- Recalculer MontantDu
UPDATE ClientFactures
SET MontantDu = Montant - COALESCE(MontantPaye, 0)
WHERE Montant IS NOT NULL;
```

### Problème : Rollback nécessaire

**Solution :** Si vous devez annuler la migration :

```sql
-- Annuler la transaction (si elle est encore ouverte)
ROLLBACK;

-- Ou supprimer la table (ATTENTION : perte de données)
DROP TABLE IF EXISTS ClientFactures;

-- Restaurer depuis le backup
mysql -u [user] -p [database_name] < backup_before_clientfacture_YYYYMMDD_HHMMSS.sql
```

---

## 📝 Notes importantes

1. **Idempotence** : Le script peut être exécuté plusieurs fois sans erreur. Il évite automatiquement les doublons.

2. **Transaction** : Le script utilise une transaction. Si une erreur survient, vous pouvez faire un ROLLBACK.

3. **Performance** : La migration peut prendre du temps selon le volume de données. Surveillez la progression.

4. **Validation** : Le script valide automatiquement la migration. Vérifiez les résultats affichés.

5. **Arriérés pré-existants** : Après la migration, vous pouvez créer des arriérés pré-existants via l'API :
   ```bash
   POST /api/ClientFacture/pre-existant
   ```

---

## 🎯 Checklist de déploiement

- [ ] Backup de la base de données effectué
- [ ] Test sur une base de test réussi
- [ ] Vérification de la connexion à la bonne base
- [ ] Période de faible activité choisie
- [ ] Script SQL exécuté
- [ ] Résultats de validation vérifiés
- [ ] Endpoints API testés
- [ ] Performance vérifiée
- [ ] Documentation mise à jour

---

## 📞 Support

En cas de problème :

1. Consultez les logs MySQL pour les erreurs détaillées
2. Vérifiez les résultats de validation dans le script
3. Utilisez le script de validation séparé : `production_validate_clientfacture_migration.sql`
4. Restaurez depuis le backup si nécessaire

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0
