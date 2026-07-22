# 🚀 Guide de Déploiement - Workflow Automatique Paiement→Billet

> **Version 1.3.0** | Date: 23/04/2026  
> Guide complet pour le déploiement du workflow d'émission automatique de billets

---

## 📋 Table des Matières

1. [🎯 Vue d'Ensemble](#vue-densemble)
2. [🔄 Prérequis](#prérequis)
3. [📦 Déploiement](#déploiement)
4. [✅ Validation](#validation)
5. [📊 Monitoring](#monitoring)
6. [🛠️ Dépannage](#dépannage)
7. [🔄 Rollback](#rollback)

---

## 🎯 Vue d'Ensemble

Le workflow automatique Paiement→Billet permet d'émettre automatiquement un billet dès qu'un paiement est complété :

```
Réservation → Paiement Complet → Billet Automatique ✅
```

### **Composants Implémentés**
- **QrCodeService** : Génération de QR Codes uniques
- **BilletEmissionService** : Logique d'émission automatique
- **PaiementService** : Intégration du workflow
- **Migration DB** : `AddBilletTrackingToPaiement`

---

## 🔄 Prérequis

### **Base de Données**
```sql
-- Vérifier que la migration a été appliquée
SELECT name FROM sys.tables WHERE name = 'Paiements' AND OBJECT_ID('Paiements') IS NOT NULL;

-- Vérifier les nouveaux champs
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Paiements' 
  AND COLUMN_NAME IN ('DateEmissionBillet', 'IdBilletEmis');
```

### **Configuration Application**
```csharp
// Program.cs - Services requis
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<BilletEmissionService>();
builder.Services.AddScoped<IPaiementRepository, PaiementService>();
builder.Services.AddScoped<IBilletRepository, BilletService>();
```

### **Permissions**
- **SQL Server** : `ALTER TABLE` permissions
- **Application** : Accès aux services d'injection
- **Réseau** : Connexion à la base de données

---

## 📦 Déploiement

### **Phase 1: Préparation**

#### **1.1 Backup de la Base de Données**
```sql
-- Backup complet avant déploiement
BACKUP DATABASE CongoTravel 
TO DISK = 'C:\Backups\CongoTravel_PreWorkflow_' + CONVERT(VARCHAR, GETDATE(), 120) + '.bak'
WITH FORMAT, INIT, COMPRESSION, STATS;
```

#### **1.2 Arrêt de l'Application**
```bash
# Arrêter l'application
sudo systemctl stop congotravel-api

# Vérifier l'arrêt
sudo systemctl status congotravel-api
```

### **Phase 2: Migration**

#### **2.1 Appliquer la Migration**
```bash
# Appliquer la migration
dotnet ef database update --connection "Server=your_server;Database=CongoTravel;User Id=your_user;Password=your_password;"

# Vérifier la migration
dotnet ef database list
```

#### **2.2 Validation de la Migration**
```sql
-- Vérifier les nouveaux champs
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Paiements' 
  AND COLUMN_NAME IN ('DateEmissionBillet', 'IdBilletEmis');

-- Résultat attendu :
-- DateEmissionBillet | datetime  | YES    | NULL
-- IdBilletEmis     | int       | YES    | NULL
```

### **Phase 3: Déploiement Application**

#### **3.1 Déploiement du Code**
```bash
# Déployer la nouvelle version
cd /var/www/CongoTravel
git pull origin main
dotnet restore
dotnet build --configuration Release
dotnet publish --configuration Release --output /var/www/CongoTravel/published

# Redémarrer l'application
sudo systemctl start congotravel-api
sudo systemctl enable congotravel-api
```

#### **3.2 Vérification du Démarrage**
```bash
# Vérifier les logs
sudo journalctl -u congotravel-api -f

# Vérifier le statut
sudo systemctl status congotravel-api

# Vérifier l'endpoint
curl -X GET "https://api.congotravel.cd/api/health" -H "accept: application/json"
```

---

## ✅ Validation

### **Tests Fonctionnels**

#### **Test 1: Paiement Complet**
```bash
# Créer un paiement complet
curl -X POST "https://api.congotravel.cd/api/Paiement/create" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "montantAPaye": 100.00,
    "montantPaye": 100.00,
    "methodePaiement": "Mobile Money",
    "referenceTransaction": "TEST_TXN_001",
    "idReservation": 1,
    "idSociete": 1,
    "idUtilisateur": 1
  }'

# Vérifier la réponse
# Doit contenir : idBilletEmis, dateEmissionBillet, billetEmis
```

#### **Test 2: Paiement Partiel**
```bash
# Créer un paiement partiel
curl -X POST "https://api.congotravel.cd/api/Paiement/create" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "montantAPaye": 100.00,
    "montantPaye": 50.00,
    "methodePaiement": "Mobile Money",
    "referenceTransaction": "TEST_TXN_002",
    "idReservation": 2,
    "idSociete": 1,
    "idUtilisateur": 1
  }'

# Vérifier la réponse
# Ne doit PAS contenir : idBilletEmis, dateEmissionBillet
```

#### **Test 3: Billet Autonome**
```bash
# Créer un paiement sans réservation
curl -X POST "https://api.congotravel.cd/api/Paiement/create" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "montantAPaye": 75.00,
    "montantPaye": 75.00,
    "methodePaiement": "Mobile Money",
    "referenceTransaction": "TEST_TXN_003",
    "idReservation": null,
    "idSociete": 1,
    "idUtilisateur": 1
  }'

# Vérifier la réponse
# Doit contenir un billet autonome (idReservation = null)
```

### **Tests de Performance**

#### **Test de Charge**
```bash
# Script de test de charge
for i in {1..100}; do
  curl -X POST "https://api.congotravel.cd/api/Paiement/create" \
    -H "Authorization: Bearer YOUR_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{
      "montantAPaye": 100.00,
      "montantPaye": 100.00,
      "methodePaiement": "Mobile Money",
      "referenceTransaction": "LOAD_TEST_'$i'",
      "idReservation": '$i',
      "idSociete": 1,
      "idUtilisateur": 1
    }' &
done

wait

# Vérifier les logs pour les erreurs
sudo journalctl -u congotravel-api --since "1 minute ago" | grep -i "error\|exception"
```

#### **Test d'Unicité QR Code**
```sql
-- Vérifier l'unicité des QR Codes
SELECT QrCode, COUNT(*) as Count
FROM Billets
WHERE DateGeneration >= DATEADD(day, -1, GETDATE())
GROUP BY QrCode
HAVING COUNT(*) > 1;

-- Doit retourner 0 lignes
```

---

## 📊 Monitoring

### **Logs Structurés**

#### **Logs d'Émission**
```json
{
  "timestamp": "2026-04-23T21:17:38Z",
  "level": "Information",
  "message": "Billet émis automatiquement avec succès",
  "data": {
    "paiementId": 1,
    "billetId": 1,
    "qrCode": "RT-001-20260423211738-1234",
    "reservationId": 5,
    "clientId": 10,
    "societeId": 1,
    "tempsEmission": "45ms"
  }
}
```

#### **Logs d'Erreur**
```json
{
  "timestamp": "2026-04-23T21:18:15Z",
  "level": "Error",
  "message": "Erreur lors de l'émission automatique du billet",
  "data": {
    "paiementId": 2,
    "erreurType": "CollisionQRCode",
    "tentatives": 10,
    "exceptionMessage": "Impossible de générer un QR Code unique après 10 tentatives"
  }
}
```

### **Métriques de Performance**

#### **Requêtes SQL**
```sql
-- Monitoring des performances d'émission
SELECT 
    COUNT(*) as NombreEmissions,
    AVG(DATEDIFF(millisecond, DateCreation, DateEmissionBillet)) as TempsMoyenEmission,
    MAX(DATEDIFF(millisecond, DateCreation, DateEmissionBillet)) as TempsMaxEmission
FROM Paiements
WHERE DateEmissionBillet IS NOT NULL
  AND DateCreation >= DATEADD(day, -7, GETDATE());

-- Objectifs :
-- TempsMoyenEmission < 100ms
-- TempsMaxEmission < 500ms
```

#### **Taux de Succès**
```sql
-- Taux de succès d'émission automatique
SELECT 
    COUNT(*) as TotalPaiements,
    SUM(CASE WHEN IdBilletEmis IS NOT NULL THEN 1 ELSE 0 END) as EmissionsReussies,
    CAST(SUM(CASE WHEN IdBilletEmis IS NOT NULL THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) as DECIMAL(5,2)) as TauxSucces
FROM Paiements
WHERE DateCreation >= DATEADD(day, -1, GETDATE());

-- Objectif : TauxSucces > 99%
```

---

## 🛠️ Dépannage

### **Problèmes Communs**

#### **Problème 1: Aucun Billet Émis**
**Symptôme** : Paiement créé mais pas de billet
```bash
# Vérifier les logs
sudo journalctl -u congotravel-api --since "10 minutes ago" | grep -i "billet\|emission"

# Vérifier la base de données
SELECT IdPaiement, MontantAPaye, MontantPaye, EstComplet, IdBilletEmis, DateEmissionBillet
FROM Paiements
WHERE DateCreation >= DATEADD(hour, -1, GETDATE())
ORDER BY DateCreation DESC;
```

**Causes Possibles** :
- Paiement non complet (`MontantPaye < MontantAPaye`)
- Erreur dans `BilletEmissionService`
- Problème de connexion à la base de données

#### **Problème 2: QR Code Dupliqué**
**Symptôme** : Erreur d'unicité QR Code
```sql
-- Vérifier les doublons
SELECT QrCode, COUNT(*) as Count
FROM Billets
WHERE QrCode IS NOT NULL
GROUP BY QrCode
HAVING COUNT(*) > 1;
```

**Solution** :
```sql
-- Régénérer les QR Codes dupliqués
UPDATE Billets
SET QrCode = 'RT-' + CAST(IdSociete AS VARCHAR(3)) + '-' + FORMAT(GETDATE(), 'yyyyMMddHHmmss') + '-' + RIGHT('0000' + CAST(Id AS VARCHAR(4)), 4)
WHERE QrCode IN (
    SELECT QrCode
    FROM Billets
    GROUP BY QrCode
    HAVING COUNT(*) > 1
);
```

#### **Problème 3: Performance Lente**
**Symptôme** : Temps d'émission > 1 seconde
```sql
-- Identifier les requêtes lentes
SELECT 
    total_elapsed_time / 1000.0 as TempsExecutionSecondes,
    total_logical_reads,
    total_physical_reads,
    SUBSTRING(qt.text, 1, 200) as Requete
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.text LIKE '%BilletEmissionService%'
  AND total_elapsed_time > 1000000; -- > 1 seconde
ORDER BY total_elapsed_time DESC;
```

### **Scripts de Dépannage**

#### **Script de Validation Complète**
```sql
-- Script complet de validation du workflow
DECLARE @DateTest DATETIME = GETDATE();

-- 1. Vérifier les paiements récents
SELECT 
    'Paiements Récents' as Type,
    COUNT(*) as Nombre,
    SUM(CASE WHEN EstComplet = 1 THEN 1 ELSE 0 END) as Complets,
    SUM(CASE WHEN IdBilletEmis IS NOT NULL THEN 1 ELSE 0 END) as BilletsEmis
FROM Paiements
WHERE DateCreation >= DATEADD(hour, -1, @DateTest);

-- 2. Vérifier les billets récents
SELECT 
    'Billets Récents' as Type,
    COUNT(*) as Nombre,
    COUNT(DISTINCT QrCode) as QrCodesUniques
FROM Billets
WHERE DateGeneration >= DATEADD(hour, -1, @DateTest);

-- 3. Vérifier les erreurs récentes (si table de logs existe)
SELECT 
    'Erreurs Récentes' as Type,
    COUNT(*) as Nombre
FROM LogsApplication
WHERE Timestamp >= DATEADD(hour, -1, @DateTest)
  AND LogLevel = 'Error'
  AND Message LIKE '%billet%';
```

---

## 🔄 Rollback

### **Scénario de Rollback Complet**

#### **Phase 1: Arrêt et Backup**
```bash
# Arrêter l'application
sudo systemctl stop congotravel-api

# Backup avant rollback
BACKUP DATABASE CongoTravel 
TO DISK = 'C:\Backups\CongoTravel_Rollback_' + CONVERT(VARCHAR, GETDATE(), 120) + '.bak'
WITH FORMAT, INIT, COMPRESSION, STATS;
```

#### **Phase 2: Rollback Migration**
```sql
-- Supprimer les nouveaux champs
ALTER TABLE Paiements DROP COLUMN DateEmissionBillet;
ALTER TABLE Paiements DROP COLUMN IdBilletEmis;

-- Nettoyer les billets automatiques
DELETE FROM Billets
WHERE DateGeneration >= DATEADD(day, -1, GETDATE())
  AND IdReservation IN (
    SELECT IdReservation 
    FROM Paiements 
    WHERE DateCreation >= DATEADD(day, -1, GETDATE())
      AND IdBilletEmis IS NOT NULL
  );
```

#### **Phase 3: Redémarrage**
```bash
# Redémarrer avec l'ancienne version
cd /var/www/CongoTravel
git checkout previous-stable-tag
dotnet restore
dotnet build --configuration Release
dotnet publish --configuration Release --output /var/www/CongoTravel/published

sudo systemctl start congotravel-api
```

### **Validation Post-Rollback**
```bash
# Vérifier que l'ancien comportement est restauré
curl -X POST "https://api.congotravel.cd/api/Paiement/create" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "montantAPaye": 100.00,
    "montantPaye": 100.00,
    "idReservation": 1,
    "idSociete": 1,
    "idUtilisateur": 1
  }'

# Doit retourner un paiement SANS idBilletEmis
```

---

## 📞 Support et Contact

### **Équipe de Support**
- **Développeur Principal** : tech-lead@congotravel.cd
- **Administrateur Système** : admin@congotravel.cd
- **Support Production** : support@congotravel.cd

### **Documentation Complémentaire**
- **Guide API** : `/API_INTEGRATION_GUIDE.md`
- **Tests Unitaires** : `/Tests/Services/`
- **Architecture** : `/ANALYSE_EXPERT_SYSTEME_KENERGIE.md`

### **Alertes et Monitoring**
- **Status Page** : https://status.congotravel.cd
- **Logs en Temps Réel** : https://logs.congotravel.cd
- **Métriques** : https://metrics.congotravel.cd

---

## ✅ Checklist de Déploiement

### **Pré-Déploiement**
- [ ] Backup base de données effectué
- [ ] Tests en environnement de staging validés
- [ ] Migration SQL préparée
- [ ] Documentation mise à jour
- [ ] Équipe informée du déploiement

### **Déploiement**
- [ ] Application arrêtée proprement
- [ ] Migration appliquée avec succès
- [ ] Nouvelle version déployée
- [ ] Services redémarrés
- [ ] Logs de démarrage vérifiés

### **Post-Déploiement**
- [ ] Tests fonctionnels validés
- [ ] Monitoring vérifié
- [ ] Performance mesurée
- [ ] Utilisateurs notifiés
- [ ] Documentation finale publiée

---

**🎯 Le workflow automatique Paiement→Billet est maintenant prêt pour la production !**
