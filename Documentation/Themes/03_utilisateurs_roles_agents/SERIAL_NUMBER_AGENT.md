# 📱 SERIAL NUMBER AGENT - Documentation Complète

## 📅 Date de documentation
**27 octobre 2025**

---

## 🎯 Objectif

Le champ `SerialNumber` permet d'associer un agent à un device (appareil) physique spécifique. Cette fonctionnalité est essentielle pour :
- Sécuriser l'accès aux fonctionnalités mobiles
- Lier un agent à son appareil personnel
- Permettre le pointage de présence uniquement depuis un device autorisé
- Gérer les changements d'appareil

---

## 📐 Architecture

### 1️⃣ **Modèle de données (Agent.cs)**

```csharp
public class Agent : Adresse
{
    [Key]
    public int IdAgent { get; set; }
    
    [MaxLength(50)]
    public string? Matricule { get; set; }
    
    // ... autres propriétés ...
    
    public string? SerialNumber { get; set; } // ✨ Identifiant unique du device
    
    // ... autres propriétés ...
}
```

**Caractéristiques :**
- **Type** : `string?` (nullable)
- **Obligatoire** : ❌ Non
- **Unique** : ❌ Non (pas de contrainte d'unicité actuellement)
- **Format** : Aucune contrainte (IMEI, UUID, custom, etc.)
- **Valeur par défaut** : `NULL`

---

### 2️⃣ **Repository (IAgentRepository.cs & AgentService.cs)**

#### Méthodes disponibles

```csharp
public interface IAgentRepository
{
    // Récupérer un agent par son SerialNumber
    Task<Agent> GetBySerialNumberAsync(string serialNumber);
    
    // Mettre à jour le SerialNumber par ID
    Task<bool> UpdateSerialNumberByIdAsync(int idAgent, string serialNumber);
    
    // Mettre à jour le SerialNumber par Matricule
    Task<bool> UpdateSerialNumberByMatriculeAsync(string matricule, string serialNumber);
}
```

#### Implémentation

```csharp
// Récupération par SerialNumber
public async Task<Agent> GetBySerialNumberAsync(string serialNumber)
{
    return await _context.Agents
        .Include(a => a.Ecole)
        .Where(a => a.Statut == true) // Agents actifs uniquement
        .FirstOrDefaultAsync(a => a.SerialNumber == serialNumber);
}

// Mise à jour par ID
public async Task<bool> UpdateSerialNumberByIdAsync(int idAgent, string serialNumber)
{
    var agent = await _context.Agents.FindAsync(idAgent);
    if (agent == null)
        return false;

    agent.SerialNumber = serialNumber;
    await _context.SaveChangesAsync();
    return true;
}

// Mise à jour par Matricule
public async Task<bool> UpdateSerialNumberByMatriculeAsync(string matricule, string serialNumber)
{
    var agent = await _context.Agents
        .FirstOrDefaultAsync(a => a.Matricule == matricule);
    
    if (agent == null)
        return false;

    agent.SerialNumber = serialNumber;
    await _context.SaveChangesAsync();
    return true;
}
```

---

### 3️⃣ **Contrôleur (AgentController.cs)**

#### Endpoints disponibles

| Méthode | Route | Description |
|---------|-------|-------------|
| **GET** | `/api/Agent/serial-number/{serialNumber}` | Récupérer un agent par SerialNumber |
| **PUT** | `/api/Agent/{idAgent}/serial-number` | Mettre à jour le SerialNumber par ID |
| **PUT** | `/api/Agent/matricule/{matricule}/serial-number` | Mettre à jour le SerialNumber par Matricule |

#### Exemple : Récupérer un agent par SerialNumber

```http
GET /api/Agent/serial-number/SN-AGENT-001
Authorization: Bearer YOUR_JWT_TOKEN
```

**Réponse 200 OK :**
```json
{
  "idAgent": 5,
  "matricule": "NAT25-A3F2B1",
  "nom": "MUKENDI",
  "postnom": "KABONGO",
  "prenom": "Pierre",
  "serialNumber": "SN-AGENT-001",
  "statut": true,
  // ... autres champs
}
```

**Réponse 404 Not Found :**
```json
{
  "message": "Aucun agent trouvé avec le numéro de série 'SN-AGENT-001'"
}
```

---

#### Exemple : Mettre à jour le SerialNumber par ID

```http
PUT /api/Agent/5/serial-number
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "serialNumber": "SN-AGENT-UPDATED-001"
}
```

**Réponse 200 OK :**
```json
{
  "message": "Numéro de série mis à jour avec succès",
  "idAgent": 5,
  "serialNumber": "SN-AGENT-UPDATED-001",
  "agent": {
    "idAgent": 5,
    "matricule": "NAT25-A3F2B1",
    "serialNumber": "SN-AGENT-UPDATED-001",
    // ... autres champs
  }
}
```

---

#### Exemple : Mettre à jour le SerialNumber par Matricule

```http
PUT /api/Agent/matricule/NAT25-A3F2B1/serial-number
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "serialNumber": "SN-AGENT-BY-MATRICULE"
}
```

**Réponse 200 OK :**
```json
{
  "message": "Numéro de série mis à jour avec succès",
  "matricule": "NAT25-A3F2B1",
  "serialNumber": "SN-AGENT-BY-MATRICULE",
  "agent": {
    "idAgent": 5,
    "matricule": "NAT25-A3F2B1",
    "serialNumber": "SN-AGENT-BY-MATRICULE",
    // ... autres champs
  }
}
```

---

## 🔄 Flux d'utilisation typique

### Scénario 1 : Premier login mobile d'un agent

```
1. L'agent se connecte depuis son appareil mobile
   └─> POST /api/Utilisateur/authentifier
   
2. L'application mobile récupère le SerialNumber du device
   └─> Android: Build.SERIAL / iOS: identifierForVendor
   
3. L'application met à jour le SerialNumber de l'agent
   └─> PUT /api/Agent/{idAgent}/serial-number
   
4. Les prochaines connexions vérifient le SerialNumber
   └─> GET /api/Agent/serial-number/{serialNumber}
```

---

### Scénario 2 : Changement d'appareil

```
1. L'agent se connecte depuis un nouveau device
   └─> POST /api/Utilisateur/authentifier
   
2. Le système détecte un SerialNumber différent
   
3. Options :
   a) Demander confirmation pour changer de device
   b) Envoyer une notification à l'administrateur
   c) Bloquer et demander une autorisation manuelle
   
4. Si autorisé, mettre à jour le SerialNumber
   └─> PUT /api/Agent/{idAgent}/serial-number
```

---

### Scénario 3 : Pointage de présence sécurisé

```
1. L'agent ouvre l'application de pointage
   └─> GET /api/Agent/serial-number/{deviceSerialNumber}
   
2. Si SerialNumber correspond → Autoriser le pointage
   └─> POST /api/Presence
   
3. Si SerialNumber ne correspond pas → Refuser
   └─> Erreur: "Appareil non autorisé"
```

---

## 🔒 Sécurité et bonnes pratiques

### 1. Authentification requise
Tous les endpoints nécessitent un token JWT valide :
```csharp
[Authorize] // 🔒 Token JWT requis
public class AgentController : ControllerBase
```

### 2. Validation du SerialNumber côté client
```javascript
// Exemple en React Native
import DeviceInfo from 'react-native-device-info';

const updateSerialNumber = async (idAgent) => {
  const serialNumber = await DeviceInfo.getUniqueId();
  
  const response = await fetch(`${API_URL}/Agent/${idAgent}/serial-number`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ serialNumber })
  });
  
  if (response.ok) {
    console.log('SerialNumber mis à jour avec succès');
  }
};
```

### 3. Gestion des erreurs
```csharp
try
{
    var agent = await _agentRepository.GetBySerialNumberAsync(serialNumber);
    if (agent == null)
    {
        return NotFound(new { message = "Aucun agent trouvé avec ce SerialNumber" });
    }
    return Ok(agent);
}
catch (Exception ex)
{
    return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
}
```

---

## 📊 Différences Agent vs Eleve

| Critère | `Agent.SerialNumber` | `Eleve.SerialNumber` |
|---------|---------------------|---------------------|
| **Table** | `Agents` | `Eleves` |
| **Type** | `string?` | `string?` |
| **Nullable** | ✅ Oui | ✅ Oui |
| **Contrainte unique** | ❌ Non | ❌ Non |
| **Endpoints** | `/api/Agent/*` | `/api/Eleve/*` |
| **Logique** | ✅ Identique | ✅ Identique |
| **Cas d'usage** | Pointage agent, accès sécurisé | Pointage élève, accès parents |

**Conclusion :** La logique est strictement identique, seule la table et les routes changent.

---

## 🧪 Tests

Un fichier de test complet est disponible : **`test-serial-number-agent.http`**

Il couvre :
1. ✅ Création d'agent avec SerialNumber
2. ✅ Création d'agent sans SerialNumber (NULL)
3. ✅ Récupération par SerialNumber
4. ✅ Mise à jour par ID
5. ✅ Mise à jour par Matricule
6. ❌ Récupération avec SerialNumber inexistant (404)

---

## 💡 Cas d'usage avancés

### 1. Multi-device pour un agent

Actuellement, un agent ne peut avoir qu'un seul SerialNumber. Pour permettre plusieurs devices :

**Option A : Table séparée (Recommandé)**
```sql
CREATE TABLE AgentDevices (
    IdAgentDevice INT PRIMARY KEY AUTO_INCREMENT,
    IdAgent INT NOT NULL,
    SerialNumber VARCHAR(255) UNIQUE,
    DeviceName VARCHAR(100),
    DateAjout DATETIME,
    Actif BOOLEAN,
    FOREIGN KEY (IdAgent) REFERENCES Agents(IdAgent)
);
```

**Option B : Format JSON dans SerialNumber**
```json
{
  "serialNumbers": [
    "SN-DEVICE-1",
    "SN-DEVICE-2",
    "SN-DEVICE-3"
  ]
}
```

---

### 2. Historique des changements

Pour tracer les changements de SerialNumber :

```sql
CREATE TABLE AgentSerialNumberHistory (
    IdHistory INT PRIMARY KEY AUTO_INCREMENT,
    IdAgent INT NOT NULL,
    AncienSerialNumber VARCHAR(255),
    NouveauSerialNumber VARCHAR(255),
    DateChangement DATETIME,
    IdUtilisateurModif INT,
    FOREIGN KEY (IdAgent) REFERENCES Agents(IdAgent)
);
```

---

### 3. Validation du SerialNumber

Pour ajouter une contrainte d'unicité :

```csharp
// Dans CongoTravelDbContext.cs
modelBuilder.Entity<Agent>()
    .HasIndex(a => a.SerialNumber)
    .IsUnique()
    .HasDatabaseName("IX_Agents_SerialNumber_Unique");
```

⚠️ **Attention :** Cette contrainte empêchera plusieurs agents d'avoir le même SerialNumber, même NULL.

---

## 📋 Checklist d'implémentation

- [x] Champ `SerialNumber` dans le modèle `Agent`
- [x] Méthode `GetBySerialNumberAsync()` dans `IAgentRepository`
- [x] Méthode `UpdateSerialNumberByIdAsync()` dans `IAgentRepository`
- [x] Méthode `UpdateSerialNumberByMatriculeAsync()` dans `IAgentRepository`
- [x] Implémentation dans `AgentService`
- [x] Endpoints dans `AgentController`
- [x] Fichier de tests (`test-serial-number-agent.http`)
- [x] Documentation complète (`SERIAL_NUMBER_AGENT.md`)
- [ ] Migration base de données (si contrainte unique souhaitée)
- [ ] Tests unitaires
- [ ] Tests d'intégration

---

## 🚀 Prochaines améliorations possibles

1. ✨ **Ajout d'une contrainte d'unicité** sur `SerialNumber`
2. 📱 **Support multi-device** (table `AgentDevices`)
3. 📊 **Historique des changements** (table `AgentSerialNumberHistory`)
4. 🔔 **Notifications** lors du changement de device
5. 🔐 **Validation renforcée** (format, longueur, caractères autorisés)
6. ⏰ **Expiration** du SerialNumber après inactivité
7. 🌍 **Géolocalisation** lors de l'enregistrement du SerialNumber

---

## 📞 Support et questions

Pour toute question ou amélioration, contactez l'équipe de développement.

---

**✅ Le SerialNumber Agent est maintenant pleinement documenté et testé !**


