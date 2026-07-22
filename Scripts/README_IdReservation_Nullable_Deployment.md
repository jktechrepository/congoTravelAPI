# 📋 Guide de Déploiement Production - IdReservation Nullable

## 🎯 Objectif
Rendre le champ `IdReservation` nullable dans la table `Billets` pour permettre des billets autonomes (sans réservation associée).

## 📁 Fichiers
- **Script principal** : `Production_Make_IdReservation_Nullable.sql`
- **Script rollback** : `Rollback_Make_IdReservation_Nullable.sql`
- **Ce guide** : `README_IdReservation_Nullable_Deployment.md`

## ⚠️ Instructions de Sécurité

### 🔒 Avant l'exécution
1. **SAUVEGARDE OBLIGATOIRE** de la base de données complète
2. **NOTIFIER** les équipes concernées de la maintenance imminente
3. **PLANIFIER** pendant une période de faible trafic
4. **TESTER** sur un environnement de staging d'abord

### 🛡️ Pendant l'exécution
1. **SURVEILLER** les logs d'erreurs
2. **VALIDER** chaque étape du script
3. **CONSERVER** le script de rollback prêt
4. **VÉRIFIER** que l'application fonctionne avec les nouveaux cas d'usage

## 🚀 Procédure de Déploiement

### Étape 1: Préparation
```bash
# 1. Connecter à la base de données de production
mysql -h [host] -u [username] -p [database]

# 2. Vérifier la connexion et la version
SELECT DATABASE(), VERSION();

# 3. Vérifier l'état actuel de la colonne
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Billets' 
  AND COLUMN_NAME = 'IdReservation';
```

### Étape 2: Exécution du Script
```bash
# Exécuter le script principal
mysql -h [host] -u [username] -p [database] < Scripts/Production_Make_IdReservation_Nullable.sql
```

### Étape 3: Validation
```sql
-- Vérifier que la colonne est maintenant nullable
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Billets' 
  AND COLUMN_NAME = 'IdReservation';

-- Le résultat doit montrer IS_NULLABLE = 'YES'
```

### Étape 4: Tests d'Intégration
```sql
-- Test 1: Insérer un billet sans réservation (NULL)
INSERT INTO Billets (
    IdReservation,
    QrCode,
    dateGeneration,
    IdSociete,
    IdClient
) VALUES (
    NULL,
    'TEST_DEPLOYMENT_NULL',
    NOW(),
    1,
    1
);

-- Vérifier l'insertion
SELECT Id, IdReservation, QrCode 
FROM Billets 
WHERE QrCode = 'TEST_DEPLOYMENT_NULL';

-- Test 2: Insérer un billet avec réservation (non NULL)
INSERT INTO Billets (
    IdReservation,
    QrCode,
    dateGeneration,
    IdSociete,
    IdClient
) VALUES (
    1,
    'TEST_DEPLOYMENT_NOTNULL',
    NOW(),
    1,
    1
);

-- Vérifier l'insertion
SELECT Id, IdReservation, QrCode 
FROM Billets 
WHERE QrCode = 'TEST_DEPLOYMENT_NOTNULL';

-- Nettoyer les tests
DELETE FROM Billets 
WHERE QrCode IN ('TEST_DEPLOYMENT_NULL', 'TEST_DEPLOYMENT_NOTNULL');
```

### Étape 5: Validation de l'Application
1. **Redémarrer** l'application API
2. **Tester** les endpoints CRUD pour les billets
3. **Valider** que les billets peuvent être créés avec et sans réservation
4. **Vérifier** que la logique métier fonctionne correctement

## 🔄 Procédure de Rollback (en cas de problème)

### Si problème détecté
```bash
# Exécuter le script de rollback
mysql -h [host] -u [username] -p [database] < Scripts/Rollback_Make_IdReservation_Nullable.sql
```

### Validation du rollback
```sql
-- Vérifier que la colonne est NOT NULL
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Billets' 
  AND COLUMN_NAME = 'IdReservation';

-- Le résultat doit montrer IS_NULLABLE = 'NO'
```

## 📊 Impact sur les Données

### 🔄 Modifications apportées
| Table | Champ | Ancien type | Nouveau type | Impact |
|-------|-------|-------------|--------------|--------|
| Billets | IdReservation | INT NOT NULL | INT NULL | Permet NULL |

### 📋 Cas d'usage nouveaux
- **Billets autonomes** : Billets sans réservation associée
- **Flexibilité** : Support pour différents types de billets
- **Compatibilité** : Données existantes non affectées

### 🔍 Validation des données
```sql
-- Statistiques post-déploiement
SELECT 
    COUNT(*) as total_billets,
    COUNT(CASE WHEN IdReservation IS NULL THEN 1 END) as billets_sans_reservation,
    COUNT(CASE WHEN IdReservation IS NOT NULL THEN 1 END) as billets_avec_reservation
FROM Billets;
```

## ✅ Checklist de Validation

- [ ] Sauvegarde effectuée
- [ ] Script testé sur staging
- [ ] Maintenance planifiée
- [ ] Équipes notifiées
- [ ] Script principal exécuté
- [ ] Colonne nullable validée
- [ ] Tests d'intégration passés
- [ ] Application redémarrée
- [ ] Endpoints API testés
- [ ] Logique métier validée
- [ ] Monitoring activé
- [ ] Documentation mise à jour

## 🚨 En cas d'urgence

1. **STOPPER** immédiatement l'application
2. **EXÉCUTER** le script de rollback
3. **RESTAURER** la sauvegarde si nécessaire
4. **NOTIFIER** l'équipe technique
5. **ANALYSER** les logs d'erreurs

## 📝 Notes importantes

### 🔄 Compatibilité ascendante
- Les billets existants avec `IdReservation` non NULL ne sont pas affectés
- L'application doit gérer les deux cas (NULL et non NULL)
- La logique de validation dans `BilletService` a été mise à jour

### 🎯 Nouveaux cas d'usage
```csharp
// Billet autonome (sans réservation)
var billetAutonome = new Billet
{
    IdReservation = null, // Nouveau cas d'usage
    QrCode = "QR123456",
    DateGeneration = DateTime.Now,
    IdSociete = 1,
    IdClient = 1
};
```

### 🔍 Validation métier
- La validation d'unicité par réservation ne s'applique que si `IdReservation` n'est pas NULL
- Les billets autonomes peuvent avoir le même QR Code si ils n'ont pas de réservation
- La logique métier doit être testée avec les deux scénarios

## 📞 Support

Pour toute question ou problème technique :
- **Équipe DevOps** : devops@congotravel.com
- **Administrateur DBA** : dba@congotravel.com
- **Chef de projet** : pm@congotravel.com

---

**Version**: 1.0  
**Date**: 2025-04-23  
**Auteur**: Équipe CongoTravel API
