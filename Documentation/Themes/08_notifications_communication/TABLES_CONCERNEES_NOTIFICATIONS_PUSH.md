# 📊 Tables et migrations concernées par les notifications push

**Date :** 2025-11-05  
**Question :** Quelles migrations sont nécessaires pour les notifications push ?

---

## 🎯 **Réponse courte**

**1 seule table critique** pour les notifications push :
- ✅ `UserDevices` → Contient les tokens FCM et le champ `Statut`/`IsActive`

**Mais TOUTES les tables avec `Statut`** sont concernées car les notifications utilisent les données des élèves, tuteurs, etc.

---

## 📋 **Tables DIRECTEMENT concernées par les notifications push**

### **1. Table `UserDevices` (CRITIQUE)** 🔴

**Rôle :** Stocke les tokens FCM des appareils mobiles.

**Requête problématique (ligne 57 `UserDeviceService.cs`) :**
```csharp
return await _context.UserDevices
    .Where(ud => ud.IdUtilisateur == idUtilisateur && ud.Statut == true)  // ❌ ÉCHOUE
    .Select(ud => ud.FcmToken)
    .ToListAsync();
```

**Erreur actuelle :**
```
System.InvalidCastException: Unable to cast object of type 'System.String' to type 'System.Boolean'
```

**Champs concernés :**
- `Statut` (ou `IsActive`) : Doit être **TINYINT(1)** au lieu de **VARCHAR**

**Correction dans le script SQL :**
```sql
ALTER TABLE UserDevices MODIFY COLUMN Statut TINYINT(1) NULL;
-- ou
ALTER TABLE UserDevices MODIFY COLUMN IsActive TINYINT(1) NULL;
```

---

### **2. Table `Utilisateurs` (CRITIQUE)** 🔴

**Rôle :** Lien entre tuteur et son compte utilisateur pour récupérer les devices.

**Requête problématique (ligne 39 `UtilisateurService.cs`) :**
```csharp
return await _context.Utilisateurs
    .Include(u => u.Ecole)
    .Where(u => u.Statut == true)  // ❌ ÉCHOUE
    .FirstOrDefaultAsync(u => u.Email == email);
```

**Champs concernés :**
- `Statut` : Doit être **TINYINT(1)** au lieu de **VARCHAR**

---

### **3. Table `Eleves` (IMPORTANT)** 🟠

**Rôle :** Pour récupérer l'élève et vérifier son statut avant d'envoyer notification.

**Requête problématique (ligne 450 `PresenceService.cs`) :**
```csharp
var eleve = await _context.Eleves
    .Include(e => e.Tuteur)
    .Include(e => e.Classe)
    .FirstOrDefaultAsync(e => e.IdEleve == presence.IdEleve.Value);

// Plus tard : filtrer par statut actif
if (eleve.Statut == true)  // ❌ ÉCHOUE si Statut est VARCHAR
```

**Champs concernés :**
- `Statut` : Doit être **TINYINT(1)**

---

### **4. Table `Tuteurs` (IMPORTANT)** 🟠

**Rôle :** Pour vérifier que le tuteur est actif avant d'envoyer notification.

**Requête problématique :**
```csharp
var tuteur = await _context.Tuteurs
    .Where(t => t.Statut == true)  // ❌ ÉCHOUE
    .FirstOrDefaultAsync(t => t.IdTuteur == idTuteur);
```

**Champs concernés :**
- `Statut` : Doit être **TINYINT(1)**

---

### **5. Table `Ecoles` (IMPORTANT)** 🟠

**Rôle :** Vérifier si l'école accepte les notifications SMS (fallback).

**Requête problématique (ligne 485 `PresenceService.cs`) :**
```csharp
var direction = await _context.Directions
    .Include(d => d.Ecole)
    .Where(d => d.IdEcole == idEcole && d.Statut == true)  // ❌ ÉCHOUE
    .FirstOrDefaultAsync();

bool acceptNotification = direction.Ecole.AcceptNotification == true;  // ✅ OK (bool nullable)
```

**Champs concernés :**
- `Statut` : Doit être **TINYINT(1)**

---

### **6. Table `Paiements` (SECONDAIRE)** 🟡

**Rôle :** Pour les notifications de paiement.

**Requête problématique :**
```csharp
var paiement = await _context.Paiements
    .Include(p => p.Eleve)
    .Where(p => p.Statut == true)  // ❌ ÉCHOUE
    .FirstOrDefaultAsync(p => p.IdPaiement == idPaiement);
```

---

### **7. Table `Presences` (SECONDAIRE)** 🟡

**Rôle :** Pour les notifications de présence.

**Requête problématique :**
```csharp
var presence = await _context.Presences
    .Include(p => p.Eleve)
    .Where(p => p.Statut == true)  // ❌ ÉCHOUE
    .FirstOrDefaultAsync(p => p.IdPresence == idPresence);
```

---

### **8. Table `Inscriptions` (SECONDAIRE)** 🟡

**Rôle :** Pour les notifications d'inscription.

**Requête problématique :**
```csharp
var inscription = await _context.Inscriptions
    .Include(i => i.Eleve)
    .Where(i => i.Statut == true)  // ❌ ÉCHOUE
    .FirstOrDefaultAsync(i => i.IdInscription == idInscription);
```

---

## 🔍 **Flux complet d'envoi de notification**

### **Exemple : Notification de présence**

```
1. Élève pointe sa présence
   ↓ Table: Presences (Statut = true)

2. Récupérer l'élève
   ↓ Table: Eleves (Statut = true)

3. Récupérer le tuteur
   ↓ Table: Tuteurs (Statut = true)

4. Récupérer l'utilisateur du tuteur
   ↓ Table: Utilisateurs (Statut = true, IdTuteur = X)

5. Récupérer les devices actifs
   ↓ Table: UserDevices (Statut = true, IdUtilisateur = Y)

6. Envoyer notification FCM
   ↓ Firebase Cloud Messaging

7. Si pas de device : SMS
   ↓ Twilio SMS
```

**❌ Chaque étape peut échouer** si `Statut` est en VARCHAR !

---

## 📊 **Priorité des migrations pour les notifications**

| Table | Priorité | Impact si non migrée |
|-------|----------|---------------------|
| **UserDevices** | 🔴 CRITIQUE | Aucune notification push ne peut être envoyée |
| **Utilisateurs** | 🔴 CRITIQUE | Impossible de récupérer l'utilisateur du tuteur |
| **Eleves** | 🟠 HAUTE | Notifications présence/paiement échouent |
| **Tuteurs** | 🟠 HAUTE | Impossible de vérifier le tuteur actif |
| **Ecoles** | 🟠 HAUTE | Impossible de vérifier AcceptNotification |
| Presences | 🟡 MOYENNE | Affecte les requêtes de présences |
| Paiements | 🟡 MOYENNE | Affecte les requêtes de paiements |
| Inscriptions | 🟡 MOYENNE | Affecte les requêtes d'inscriptions |

---

## ✅ **Script SQL concerné**

### **`APPLIQUER_MIGRATION_STATUT_NULLABLE.sql`**

Ce script convertit le champ `Statut` de **TOUTES** les tables, incluant :

```sql
-- Tables CRITIQUES pour notifications push
ALTER TABLE UserDevices MODIFY COLUMN Statut TINYINT(1) NULL;
ALTER TABLE Utilisateurs MODIFY COLUMN Statut TINYINT(1) NULL;
ALTER TABLE Eleves MODIFY COLUMN Statut TINYINT(1) NULL;
ALTER TABLE Tuteurs MODIFY COLUMN Statut TINYINT(1) NULL;
ALTER TABLE Ecoles MODIFY COLUMN Statut TINYINT(1) NULL;

-- Tables SECONDAIRES
ALTER TABLE Presences MODIFY COLUMN Statut TINYINT(1) NULL;
ALTER TABLE Paiements MODIFY COLUMN Statut TINYINT(1) NULL;
ALTER TABLE Inscriptions MODIFY COLUMN Statut TINYINT(1) NULL;

-- + 19 autres tables...
```

---

## 🧪 **Test après migration**

### **1. Vérifier que UserDevices est corrigé :**
```sql
SHOW COLUMNS FROM UserDevices WHERE Field IN ('Statut', 'IsActive');
```

**Résultat attendu :**
```
Field     | Type        | Null
----------|-------------|------
Statut    | tinyint(1)  | YES
```

### **2. Tester l'envoi de notification :**
```http
POST /api/NotificationPush/test
Authorization: Bearer {token}
Body: {
  "idUtilisateur": 223,
  "titre": "Test notification",
  "corps": "Test après migration"
}
```

**Résultat attendu :**
- ✅ Code 200
- ✅ Message : "Notification envoyée avec succès"
- 📱 Mobile reçoit la notification

---

## 🎯 **Résumé**

### **Tables critiques pour notifications push :**

```
UserDevices    🔴 CRITIQUE (stocke les FCM tokens)
    ↓
Utilisateurs   🔴 CRITIQUE (lien tuteur → user → devices)
    ↓
Eleves         🟠 HAUTE (données pour notification)
    ↓
Tuteurs        🟠 HAUTE (destinataire)
    ↓
Ecoles         🟠 HAUTE (AcceptNotification)
```

### **Migration requise :**

**UN SEUL script corrige TOUT :**
```
APPLIQUER_MIGRATION_STATUT_NULLABLE.sql
```

**Ce script convertit `Statut` dans les 27 tables**, incluant toutes celles nécessaires aux notifications push.

---

## ✅ **Après migration**

```
✅ UserDevices.Statut = TINYINT(1)
✅ Récupération des tokens FCM fonctionne
✅ Notifications push envoyées
✅ Fallback SMS si pas de device
✅ Tout fonctionne à 100% !
```

---

**🎯 Action immédiate : Exécute `APPLIQUER_MIGRATION_STATUT_NULLABLE.sql` et les notifications push fonctionneront !** 🚀

