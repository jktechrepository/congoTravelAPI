# 🔍 **Analyse Expert - Demande Synchronisation Offline**

## 📋 **Vue d'ensemble de la demande**

Les développeurs frontend Flutter demandent une **API de synchronisation offline** pour supporter:
- **15 000 clients** avec leurs arriérés
- **Mode paiement totalement offline**
- **Synchronisation incrémentale (delta)**
- **Endpoints bulk paginés**
- **Idempotence des paiements**

---

## 🎯 **Analyse de la Spécification**

### **✅ Points Excellents dans la demande**

#### **1. Architecture bien pensée**
- **Bootstrap → Delta sync** pattern correct
- **Cursor-based pagination** (keyset) appropriée
- **Idempotence** bien spécifiée pour les paiements
- **Compression HTTP** mentionnée
- **Projections minces** recommandées

#### **2. SLA réalistes**
- **Bootstrap < 2 min** sur 5G
- **Delta < 10-30 s**
- **Resumable** après interruption
- **Payload maîtrisé**

#### **3. Sécurité bien considérée**
- **JWT tenant isolation**
- **Pas de confiance** en idSociete passé en query

---

## ❓ **Questions Techniques Essentielles**

### **🗄️ Base de Données & Schéma**

#### **1. Colonnes de tracking**
```sql
-- Avez-vous déjà ces colonnes ?
ALTER TABLE Clients ADD COLUMN UpdatedAt UTC_TIMESTAMP NULL;
ALTER TABLE ClientFactures ADD COLUMN UpdatedAt UTC_TIMESTAMP NULL;
ALTER TABLE Clients ADD COLUMN IsDeleted BIT DEFAULT 0;
ALTER TABLE ClientFactures ADD COLUMN IsDeleted BIT DEFAULT 0;
```

**Questions:**
- **Q1**: Les colonnes `UpdatedAt` existent-elles déjà dans vos tables ?
- **Q2**: Comment gérez-vous actuellement les suppressions (soft delete vs hard delete) ?
- **Q3**: Quelle est la structure exacte de votre table `ClientFacture` ?

#### **2. Indexation actuelle**
**Questions:**
- **Q4**: Quels index existent actuellement sur `Clients` et `ClientFactures` ?
- **Q5**: Quel est le volume moyen d'arriérés par client (estimation) ?

---

### **🔐 Authentification & Autorisation**

#### **3. Claims JWT actuels**
```csharp
// Claims actuels dans vos tokens JWT
var claims = new[]
{
    new Claim("idUtilisateur", user.IdUtilisateur.ToString()),
    new Claim("idSociete", user.IdSociete?.ToString() ?? ""),
    new Claim("role", user.Role?.Nom ?? ""),
    // Avez-vous déjà des claims de périmètre ?
};
```

**Questions:**
- **Q6**: Votre JWT contient-il déjà des claims de périmètre (zone/axe/cabine/agent) ?
- **Q7**: Comment filtrez-vous actuellement les données par agent/zone dans vos autres endpoints ?

---

### **📊 Architecture & Performance**

#### **4. Volume et charge**
**Questions:**
- **Q8**: Quelle est la taille moyenne d'un enregistrement Client (en KB) ?
- **Q9**: Quelle est la taille moyenne d'un enregistrement ClientFacture (en KB) ?
- **Q10**: Combien d'agents simultanés maximum peuvent synchroniser ?

#### **5. Infrastructure**
**Questions:**
- **Q11**: Quelle est votre configuration serveur (CPU/RAM) ?
- **Q12**: Utilisez-vous déjà du caching (Redis/Memory) ?
- **Q13**: Avez-vous des limitations de bande passante à considérer ?

---

### **🔄 Logique Métier**

#### **6. Gestion des conflits**
**Questions:**
- **Q14**: Comment gérez-vous les conflits de paiement (même facture payée par deux agents) ?
- **Q15**: Quelle est la règle métier pour les paiements partiels vs paiement total ?
- **Q16**: Comment validez-vous la solvabilité d'un client lors du paiement offline ?

#### **7. États des données**
**Questions:**
- **Q17**: Un client désactivé peut-il avoir des arriérés à payer ?
- **Q18**: Les arriérés pré-existants sont-ils traités différemment des factures normales ?
- **Q19**: Y a-t-il une période de prescription pour les arriérés ?

---

### **🔧 Implémentation Technique**

#### **8. Stack technique actuelle**
**Questions:**
- **Q20**: Quelle version d'EF Core utilisez-vous exactement ?
- **Q21**: Avez-vous déjà implémenté de la pagination cursor dans d'autres endpoints ?
- **Q22**: Utilisez-vous déjà des DTOs de projection dans vos services ?

#### **9. Monitoring & Observabilité**
**Questions:**
- **Q23**: Avez-vous déjà des métriques sur vos endpoints existants ?
- **Q24**: Comment souhaitez-vous tracker les performances de synchronisation ?
- **Q25**: Souhaitez-vous des logs détaillés pour le debugging des sync ?

---

### **📱 Spécifications Mobile**

#### **10. Stratégie de retry**
**Questions:**
- **Q26**: Quelle est la taille maximale d'un batch de paiements que le mobile peut envoyer ?
- **Q27**: Comment le mobile gère-t-il les conflits de version pendant la sync ?
- **Q28**: Y a-t-il une priorité dans les données à synchroniser (clients vs arriérés) ?

---

## 🏗️ **Proposition d'Architecture Backend**

### **Structure des Controllers**
```csharp
[ApiController]
[Route("api/sync")]
[Authorize] // JWT tenant isolation
public class SyncController : ControllerBase
{
    // GET /api/sync/bootstrap
    // GET /api/sync/clients
    // GET /api/sync/arrears  
    // GET /api/sync/deletions
    // POST /api/sync/payments/batch
}
```

### **Services recommandés**
```csharp
public interface ISyncService
{
    Task<SyncBootstrapDto> GetBootstrapAsync(int societeId);
    Task<SyncPageDto<ClientSyncDto>> GetClientsAsync(SyncRequest request);
    Task<SyncPageDto<ArrearSyncDto>> GetArrearsAsync(SyncRequest request);
    Task<SyncDeletionsDto> GetDeletionsAsync(SyncDeletionsRequest request);
    Task<PaymentBatchResultDto> ProcessPaymentsBatchAsync(PaymentBatchRequest request);
}
```

---

## 📊 **Estimations de Performance**

### **Calculs de volume**
```
Clients: 15 000 × ~200 bytes = ~3 MB (compressé ~1 MB)
Arriérés: 42 000 × ~150 bytes = ~6 MB (compressé ~2 MB)
Total bootstrap: ~3 MB compressé
```

### **Estimations de temps**
```
- 1000 clients/page: ~5-8 requêtes = 15-30s
- 2000 arrears/page: ~21 requêtes = 30-45s  
- Total bootstrap: 45-75s (dans les SLA)
```

---

## 🚨 **Risques Identifiés**

### **🔴 Critiques**
1. **Timeout sur gros volumes** si pagination mal optimisée
2. **Consommation mémoire** si projections mal faites
3. **Incohérences** si snapshot mal géré

### **🟡 Modérés**
1. **Complexité du delta sync**
2. **Gestion des suppressions**
3. **Performance sous charge**

---

## 📋 **Checklist Pré-Implémentation**

### **Database**
- [ ] Ajouter colonnes `UpdatedAt` et `IsDeleted`
- [ ] Créer index composites
- [ ] Tester les requêtes de pagination

### **Backend**
- [ ] Implémenter les DTOs de sync
- [ ] Créer les services de synchronisation
- [ ] Ajouter la compression
- [ ] Implémenter l'idempotence

### **Tests**
- [ ] Tests de charge avec 15 000 clients
- [ ] Tests de résilience (réseau coupé)
- [ ] Tests de concurrence (multi-agents)

---

## 🎯 **Recommandations Initiales**

### **1. Commencer petit**
- Implémenter d'abord `/sync/clients` seul
- Tester avec 1000 clients
- Puis ajouter les autres endpoints

### **2. Monitoring dès le début**
- Ajouter des métriques sur chaque endpoint
- Logger les temps de réponse
- Surveiller la mémoire

### **3. Approche itérative**
- Version 1: Bootstrap complet
- Version 2: Delta sync
- Version 3: Optimisations avancées

---

## 🔄 **Prochaines Étapes**

Une fois que vous aurez répondu aux questions ci-dessus, je pourrai:

1. **Proposer une implémentation détaillée**
2. **Fournir les scripts SQL nécessaires**
3. **Donner les exemples de code complets**
4. **Estimer plus précisément les performances**

---

## ❓ **Questions Prioritaires**

Pour commencer rapidement, les questions les plus importantes sont:

**Q1, Q2, Q4, Q6, Q8, Q14, Q20**

Vos réponses à ces questions me permettront de vous proposer une architecture adaptée à votre contexte existant.

---

*Analyse préparée le 21 mars 2026 - En attente de vos réponses techniques*
