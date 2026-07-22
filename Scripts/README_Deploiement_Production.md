# 📋 Guide de Déploiement Production - Nouveaux Champs

## 🎯 Objectif
Ajouter les nouveaux champs suivants à la base de données de production :
- `Reservations.nombreDePlace` (INT NOT NULL DEFAULT 1)
- `Destinations.HeureDepart` (TIME NULL)
- `Destinations.jourDepart` (VARCHAR(50) NULL)

## 📁 Fichiers
- **Script principal** : `Production_Add_New_Fields.sql`
- **Script rollback** : `Rollback_Production_Add_New_Fields.sql`
- **Ce guide** : `README_Deploiement_Production.md`

## ⚠️ Instructions de Sécurité

### 🔒 Avant l'exécution
1. **SAUVEGARDE OBLIGATOIRE** de la base de données complète
2. **NOTIFIER** les utilisateurs de la maintenance imminente
3. **PLANIFIER** pendant une période de faible trafic
4. **TESTER** sur un environnement de staging d'abord

### 🛡️ Pendant l'exécution
1. **EXÉCUTER** en mode maintenance
2. **SURVEILLER** les logs d'erreurs
3. **VALIDER** chaque étape
4. **CONSERVER** le script de rollback prêt

## 🚀 Procédure de Déploiement

### Étape 1: Préparation
```bash
# 1. Connecter à la base de données de production
mysql -h [host] -u [username] -p [database]

# 2. Vérifier la connexion
SELECT DATABASE(), VERSION();

# 3. Faire une sauvegarde (si pas déjà faite)
mysqldump -h [host] -u [username] -p [database] > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Étape 2: Exécution du Script
```bash
# Exécuter le script principal
mysql -h [host] -u [username] -p [database] < Scripts/Production_Add_New_Fields.sql
```

### Étape 3: Validation
```sql
-- Vérifier que les colonnes existent
DESCRIBE Reservations;
DESCRIBE Destinations;

-- Vérifier les valeurs par défaut
SELECT COUNT(*) as test_reservation 
FROM Reservations 
WHERE nombreDePlace = 1 
LIMIT 1;

-- Vérifier les types de données
SELECT 
    TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND COLUMN_NAME IN ('nombreDePlace', 'HeureDepart', 'jourDepart');
```

### Étape 4: Tests d'Intégration
```sql
-- Test 1: Créer une réservation
INSERT INTO Reservations (
    IdUtilisateur, IdClient, IdVoyage, StatutReservation, 
    Statut, DateReservation, IdSociete
) VALUES (1, 1, 1, 'TEST_INTEGRATION', 1, NOW(), 1);

-- Vérifier que nombreDePlace = 1 par défaut
SELECT IdReservation, nombreDePlace 
FROM Reservations 
WHERE StatutReservation = 'TEST_INTEGRATION';

-- Nettoyer
DELETE FROM Reservations WHERE StatutReservation = 'TEST_INTEGRATION';

-- Test 2: Créer une destination
INSERT INTO Destinations (
    VilleDepart, VilleArrivee, Montant, Statut, 
    DateCreation, IdSociete, HeureDepart, jourDepart
) VALUES (
    'TEST_CITY', 'TEST_DEST', 100.00, 1, NOW(), 1, '14:30:00', 'Mardi'
);

-- Vérifier les nouveaux champs
SELECT IdDestination, HeureDepart, jourDepart 
FROM Destinations 
WHERE VilleDepart = 'TEST_CITY';

-- Nettoyer
DELETE FROM Destinations WHERE VilleDepart = 'TEST_CITY';
```

## 🔄 Procédure de Rollback (en cas de problème)

### Si problème détecté
```bash
# Exécuter le script de rollback
mysql -h [host] -u [username] -p [database] < Scripts/Rollback_Production_Add_New_Fields.sql
```

### Validation du rollback
```sql
-- Vérifier que les colonnes ont été supprimées
SELECT COUNT(*) as should_be_zero
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND COLUMN_NAME IN ('nombreDePlace', 'HeureDepart', 'jourDepart');
```

## 📊 Résumé des Modifications

| Table | Champ | Type | Nullable | Default | Description |
|-------|-------|------|----------|---------|-------------|
| Reservations | nombreDePlace | INT | NOT NULL | 1 | Nombre de places réservées |
| Destinations | HeureDepart | TIME | OUI | NULL | Heure de départ HH:mm |
| Destinations | jourDepart | VARCHAR(50) | OUI | NULL | Jour de départ |

## ✅ Checklist de Validation

- [ ] Sauvegarde effectuée
- [ ] Script testé sur staging
- [ ] Maintenance planifiée
- [ ] Utilisateurs notifiés
- [ ] Script principal exécuté
- [ ] Validation des colonnes réussie
- [ ] Tests d'intégration passés
- [ ] Application redémarrée
- [ ] Monitoring activé
- [ ] Documentation mise à jour

## 🚨 En cas d'urgence

1. **STOPPER** immédiatement l'application
2. **EXÉCUTER** le script de rollback
3. **RESTAURER** la sauvegarde si nécessaire
4. **NOTIFIER** l'équipe technique
5. **ANALYSER** les logs d'erreurs

## 📞 Support

Pour toute question ou problème technique :
- **Équipe DevOps** : devops@congotravel.com
- **Administrateur DBA** : dba@congotravel.com
- **Chef de projet** : pm@congotravel.com

---

**Version**: 1.0  
**Date**: 2025-04-23  
**Auteur**: Équipe CongoTravel API
