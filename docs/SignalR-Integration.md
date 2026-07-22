# 📊 Dashboard & Statistiques SignalR - Documentation d'Intégration

## 🎯 Vue d'Ensemble

Ce document explique comment intégrer le hub SignalR pour les notifications temps réel du dashboard et des statistiques dans vos applications frontend.

### **📡 Hub SignalR**
- **Endpoint** : `https://localhost:7110/hubs/dashboard`
- **Authentification** : JWT Bearer Token requis
- **Protocole** : WebSocket avec SignalR

---

## 🔐 Authentification

### **JWT Token Required**
Toutes les connexions au hub nécessitent un token JWT valide :

```http
Authorization: Bearer <votre_jwt_token>
```

Le token doit contenir les claims suivants :
- `NameIdentifier` : ID de l'utilisateur
- `Name` : Nom de l'utilisateur  
- `Role` : Rôle de l'utilisateur
- `SocieteId` : ID de la société (optionnel)

---

## 📡 Événements Disponibles

### **📊 Dashboard Events**

#### `DashboardUpdated`
Déclenché lors d'une mise à jour complète du dashboard
```json
{
  "societeId": 1,
  "dashboard": {
    "totalAgents": 15,
    "totalClientsActifs": 250,
    "montantTotalPaiements": 150000.00,
    "montantTotalArrieres": 25000.00,
    "top5AgentsCollecteurs": [...]
  },
  "timestamp": "2026-02-14T16:30:00Z",
  "type": "full_update"
}
```

#### `NewPaiement`
Déclenché lors de la création d'un nouveau paiement
```json
{
  "societeId": 1,
  "paiement": {
    "id": 123,
    "montant": 5000.00,
    "date": "2026-02-14T16:30:00Z",
    "mode": "Mobile Money",
    "statut": "Validé",
    "estPaiementArriere": false,
    "idClient": 45,
    "idFacture": 78,
    "idClientFacture": null
  },
  "timestamp": "2026-02-14T16:30:00Z",
  "type": "new_paiement"
}
```

#### `NewClient`
Déclenché lors de la création d'un nouveau client
```json
{
  "societeId": 1,
  "client": {
    "id": 456,
    "nom": "Jean Dupont",
    "telephone": "+2250700000000",
    "dateCreation": "2026-02-14T16:30:00Z"
  },
  "timestamp": "2026-02-14T16:30:00Z",
  "type": "new_client"
}
```

#### `DashboardStatusChanged`
Déclenché lors d'un changement de statut
```json
{
  "societeId": 1,
  "entityType": "paiement",
  "entityId": 123,
  "newStatus": "mis_à_jour",
  "timestamp": "2026-02-14T16:30:00Z"
}
```

### **📈 Statistiques Events**

#### `StatistiquesGeneralesUpdated`
```json
{
  "societeId": 1,
  "statistiques": {
    "totalClients": 250,
    "totalFactures": 180,
    "totalArrieres": 25000.00,
    "totalPaiements": 150000.00,
    "tauxRecouvrement": 85.5,
    "totalPaiementsCount": 125,
    "dateGeneration": "2026-02-14T16:30:00Z"
  },
  "timestamp": "2026-02-14T16:30:00Z",
  "type": "generales_update"
}
```

#### `StatistiquesFinancieresUpdated`
```json
{
  "societeId": 1,
  "statistiques": {
    "chiffreAffaires": 500000.00,
    "montantArrieres": 25000.00,
    "montantPaye": 150000.00,
    "montantDu": 75000.00,
    "evolutionMensuelle": [...],
    "repartitionPaiements": [...],
    "dateGeneration": "2026-02-14T16:30:00Z"
  },
  "timestamp": "2026-02-14T16:30:00Z",
  "type": "financieres_update"
}
```

#### `StatistiquesOperationnellesUpdated`
```json
{
  "societeId": 1,
  "statistiques": {
    "repartitionClientsParCategorie": [...],
    "repartitionClientsParAxe": [...],
    "statistiquesFacturesMois": [...],
    "clientActivite": {...},
    "dateGeneration": "2026-02-14T16:30:00Z"
  },
  "timestamp": "2026-02-14T16:30:00Z",
  "type": "operationnelles_update"
}
```

#### `StatistiquesPerformanceUpdated`
```json
{
  "societeId": 1,
  "statistiques": {
    "tauxRecouvrementGlobal": 85.5,
    "tauxRecouvrementParCategorie": [...],
    "topAgents": [...],
    "performanceMensuelle": [...],
    "dateGeneration": "2026-02-14T16:30:00Z"
  },
  "timestamp": "2026-02-14T16:30:00Z",
  "type": "performance_update"
}
```

---

## 📱 Flutter Integration

### **📦 Dépendances**

Ajoutez à votre `pubspec.yaml` :
```yaml
dependencies:
  signalr_core: ^1.0.0
  flutter_secure_storage: ^8.0.0
  http: ^1.1.0
```

### **🔧 Service SignalR**

```dart
import 'dart:convert';
import 'package:signalr_core/signalr_core.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class DashboardSignalRService {
  late HubConnection _hubConnection;
  final FlutterSecureStorage _storage = FlutterSecureStorage();
  
  // Callbacks
  Function(Map<String, dynamic>)? onDashboardUpdated;
  Function(Map<String, dynamic>)? onNewPaiement;
  Function(Map<String, dynamic>)? onNewClient;
  Function(Map<String, dynamic>)? onStatistiquesGeneralesUpdated;
  Function(Map<String, dynamic>)? onStatistiquesFinancieresUpdated;
  Function(Map<String, dynamic>)? onStatistiquesOperationnellesUpdated;
  Function(Map<String, dynamic>)? onStatistiquesPerformanceUpdated;
  Function(Map<String, dynamic>)? onDashboardStatusChanged;

  Future<void> connect() async {
    try {
      // Récupérer le token JWT
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) {
        throw Exception('Token JWT non trouvé');
      }

      // Configuration de la connexion
      _hubConnection = HubConnectionBuilder()
          .withUrl('https://localhost:7110/hubs/dashboard', options: {
            HttpMessageHandlerFactory: () => JwtAuthProvider(token),
          })
          .withAutomaticReconnect()
          .build();

      // Inscription aux événements Dashboard
      _hubConnection.on('DashboardUpdated', (data) {
        if (onDashboardUpdated != null) {
          onDashboardUpdated!(Map<String, dynamic>.from(data));
        }
      });

      _hubConnection.on('NewPaiement', (data) {
        if (onNewPaiement != null) {
          onNewPaiement!(Map<String, dynamic>.from(data));
        }
      });

      _hubConnection.on('NewClient', (data) {
        if (onNewClient != null) {
          onNewClient!(Map<String, dynamic>.from(data));
        }
      });

      _hubConnection.on('DashboardStatusChanged', (data) {
        if (onDashboardStatusChanged != null) {
          onDashboardStatusChanged!(Map<String, dynamic>.from(data));
        }
      });

      // Inscription aux événements Statistiques
      _hubConnection.on('StatistiquesGeneralesUpdated', (data) {
        if (onStatistiquesGeneralesUpdated != null) {
          onStatistiquesGeneralesUpdated!(Map<String, dynamic>.from(data));
        }
      });

      _hubConnection.on('StatistiquesFinancieresUpdated', (data) {
        if (onStatistiquesFinancieresUpdated != null) {
          onStatistiquesFinancieresUpdated!(Map<String, dynamic>.from(data));
        }
      });

      _hubConnection.on('StatistiquesOperationnellesUpdated', (data) {
        if (onStatistiquesOperationnellesUpdated != null) {
          onStatistiquesOperationnellesUpdated!(Map<String, dynamic>.from(data));
        }
      });

      _hubConnection.on('StatistiquesPerformanceUpdated', (data) {
        if (onStatistiquesPerformanceUpdated != null) {
          onStatistiquesPerformanceUpdated!(Map<String, dynamic>.from(data));
        }
      });

      // Démarrer la connexion
      await _hubConnection.start();
      print('✅ Connecté au DashboardHub');
      
    } catch (e) {
      print('❌ Erreur de connexion: $e');
      rethrow;
    }
  }

  Future<void> disconnect() async {
    await _hubConnection.stop();
    print('🔌 Déconnecté du DashboardHub');
  }

  Future<void> subscribeToDashboardUpdates(int societeId) async {
    await _hubConnection.invoke('SubscribeToDashboardUpdates', arguments: [societeId]);
  }

  Future<void> subscribeToStatistiquesUpdates(int societeId) async {
    await _hubConnection.invoke('SubscribeToStatistiquesUpdates', arguments: [societeId]);
  }

  Future<void> requestDashboardUpdate(int societeId) async {
    await _hubConnection.invoke('RequestDashboardUpdate', arguments: [societeId]);
  }

  Future<void> requestStatistiquesUpdate(int societeId) async {
    await _hubConnection.invoke('RequestStatistiquesUpdate', arguments: [societeId]);
  }
}

// Provider JWT pour l'authentification
class JwtAuthProvider extends HttpMessageHandler {
  final String token;

  JwtAuthProvider(this.token);

  @override
  Future<HttpResponseMessage> send(HttpRequestMessage request) async {
    request.headers['Authorization'] = 'Bearer $token';
    return super.send(request);
  }
}
```

### **🏗️ Widget Flutter**

```dart
import 'package:flutter/material.dart';

class DashboardScreen extends StatefulWidget {
  @override
  _DashboardScreenState createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  final DashboardSignalRService _signalRService = DashboardSignalRService();
  Map<String, dynamic>? _dashboardData;
  Map<String, dynamic>? _statistiquesData;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _initializeSignalR();
  }

  Future<void> _initializeSignalR() async {
    try {
      // Configuration des callbacks
      _signalRService.onDashboardUpdated = (data) {
        setState(() {
          _dashboardData = data;
          _isLoading = false;
        });
        _showNotification('Dashboard mis à jour', 'Les données du dashboard ont été rafraîchies');
      };

      _signalRService.onNewPaiement = (data) {
        _showNotification('Nouveau paiement', 'Un paiement de ${data['paiement']['montant']} a été enregistré');
      };

      _signalRService.onNewClient = (data) {
        _showNotification('Nouveau client', '${data['client']['nom']} a été ajouté');
      };

      _signalRService.onStatistiquesGeneralesUpdated = (data) {
        setState(() {
          _statistiquesData = data['statistiques'];
        });
      };

      // Connexion au hub
      await _signalRService.connect();
      
      // Abonnement aux mises à jour (remplacer 1 par l'ID de la société)
      await _signalRService.subscribeToDashboardUpdates(1);
      await _signalRService.subscribeToStatistiquesUpdates(1);
      
    } catch (e) {
      setState(() => _isLoading = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Erreur de connexion: $e')),
      );
    }
  }

  void _showNotification(String title, String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('$title: $message'),
        duration: Duration(seconds: 3),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Dashboard Temps Réel'),
        actions: [
          IconButton(
            icon: Icon(Icons.refresh),
            onPressed: () async {
              await _signalRService.requestDashboardUpdate(1);
            },
          ),
        ],
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : _buildDashboardContent(),
    );
  }

  Widget _buildDashboardContent() {
    if (_dashboardData == null) {
      return Center(child: Text('Aucune donnée disponible'));
    }

    return SingleChildScrollView(
      padding: EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildStatCard('Agents', _dashboardData!['totalAgents'].toString(), Icons.people),
          _buildStatCard('Clients Actifs', _dashboardData!['totalClientsActifs'].toString(), Icons.person),
          _buildStatCard('Paiements', '${_dashboardData!['montantTotalPaiements']} FCFA', Icons.payment),
          _buildStatCard('Arriérés', '${_dashboardData!['montantTotalArrieres']} FCFA', Icons.warning),
          
          SizedBox(height: 20),
          
          Text('Top Agents', style: Theme.of(context).textTheme.headline6),
          SizedBox(height: 10),
          _buildTopAgentsList(),
        ],
      ),
    );
  }

  Widget _buildStatCard(String title, String value, IconData icon) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icon, color: Theme.of(context).primaryColor),
                SizedBox(width: 8),
                Text(title, style: Theme.of(context).textTheme.subtitle1),
              ],
            ),
            SizedBox(height: 8),
            Text(value, style: Theme.of(context).textTheme.headline5),
          ],
        ),
      ),
    );
  }

  Widget _buildTopAgentsList() {
    final topAgents = _dashboardData!['top5AgentsCollecteurs'] as List;
    
    return Card(
      child: ListView.builder(
        shrinkWrap: true,
        physics: NeverScrollableScrollPhysics(),
        itemCount: topAgents.length,
        itemBuilder: (context, index) {
          final agent = topAgents[index];
          return ListTile(
            leading: CircleAvatar(child: Text('${index + 1}')),
            title: Text(agent['nomComplet'] ?? 'Agent ${agent['idAgent']}'),
            subtitle: Text('${agent['montantCollecte']} FCFA'),
            trailing: Text('${agent['nombrePaiements']} paiements'),
          );
        },
      ),
    );
  }

  @override
  void dispose() {
    _signalRService.disconnect();
    super.dispose();
  }
}
```

---

## 🌐 Vue.js Integration

### **📦 Dépendances**

```bash
npm install @microsoft/signalr @microsoft/signalr-protocol-msgpack axios
```

### **🔧 Service SignalR**

```javascript
// src/services/dashboardSignalR.js
import * as signalR from '@microsoft/signalr';
import axios from 'axios';

class DashboardSignalRService {
  constructor() {
    this.connection = null;
    this.token = localStorage.getItem('jwt_token');
    this.callbacks = {};
  }

  // Configuration des callbacks
  on(event, callback) {
    this.callbacks[event] = callback;
  }

  off(event) {
    delete this.callbacks[event];
  }

  async connect() {
    try {
      if (!this.token) {
        throw new Error('Token JWT non trouvé');
      }

      // Configuration de la connexion
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('https://localhost:7110/hubs/dashboard', {
          accessTokenFactory: () => this.token,
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Inscription aux événements Dashboard
      this.connection.on('DashboardUpdated', (data) => {
        if (this.callbacks.onDashboardUpdated) {
          this.callbacks.onDashboardUpdated(data);
        }
      });

      this.connection.on('NewPaiement', (data) => {
        if (this.callbacks.onNewPaiement) {
          this.callbacks.onNewPaiement(data);
        }
      });

      this.connection.on('NewClient', (data) => {
        if (this.callbacks.onNewClient) {
          this.callbacks.onNewClient(data);
        }
      });

      this.connection.on('DashboardStatusChanged', (data) => {
        if (this.callbacks.onDashboardStatusChanged) {
          this.callbacks.onDashboardStatusChanged(data);
        }
      });

      // Inscription aux événements Statistiques
      this.connection.on('StatistiquesGeneralesUpdated', (data) => {
        if (this.callbacks.onStatistiquesGeneralesUpdated) {
          this.callbacks.onStatistiquesGeneralesUpdated(data);
        }
      });

      this.connection.on('StatistiquesFinancieresUpdated', (data) => {
        if (this.callbacks.onStatistiquesFinancieresUpdated) {
          this.callbacks.onStatistiquesFinancieresUpdated(data);
        }
      });

      this.connection.on('StatistiquesOperationnellesUpdated', (data) => {
        if (this.callbacks.onStatistiquesOperationnellesUpdated) {
          this.callbacks.onStatistiquesOperationnellesUpdated(data);
        }
      });

      this.connection.on('StatistiquesPerformanceUpdated', (data) => {
        if (this.callbacks.onStatistiquesPerformanceUpdated) {
          this.callbacks.onStatistiquesPerformanceUpdated(data);
        }
      });

      // Démarrer la connexion
      await this.connection.start();
      console.log('✅ Connecté au DashboardHub');
      
    } catch (error) {
      console.error('❌ Erreur de connexion:', error);
      throw error;
    }
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop();
      console.log('🔌 Déconnecté du DashboardHub');
    }
  }

  async subscribeToDashboardUpdates(societeId) {
    if (this.connection) {
      await this.connection.invoke('SubscribeToDashboardUpdates', societeId);
    }
  }

  async subscribeToStatistiquesUpdates(societeId) {
    if (this.connection) {
      await this.connection.invoke('SubscribeToStatistiquesUpdates', societeId);
    }
  }

  async requestDashboardUpdate(societeId) {
    if (this.connection) {
      await this.connection.invoke('RequestDashboardUpdate', societeId);
    }
  }

  async requestStatistiquesUpdate(societeId) {
    if (this.connection) {
      await this.connection.invoke('RequestStatistiquesUpdate', societeId);
    }
  }

  getConnectionState() {
    return this.connection ? this.connection.state : 'Disconnected';
  }
}

export default new DashboardSignalRService();
```

### **🏗️ Composant Vue.js**

```vue
<!-- src/components/Dashboard.vue -->
<template>
  <div class="dashboard">
    <div class="dashboard-header">
      <h1>Dashboard Temps Réel</h1>
      <div class="connection-status">
        <span :class="['status', connectionState.toLowerCase()]">
          {{ connectionState }}
        </span>
        <button @click="refreshDashboard" :disabled="connectionState !== 'Connected'">
          🔄 Rafraîchir
        </button>
      </div>
    </div>

    <div v-if="loading" class="loading">
      <div class="spinner"></div>
      <p>Chargement des données...</p>
    </div>

    <div v-else-if="dashboardData" class="dashboard-content">
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon">👥</div>
          <div class="stat-content">
            <h3>{{ dashboardData.totalAgents }}</h3>
            <p>Agents</p>
          </div>
        </div>
        
        <div class="stat-card">
          <div class="stat-icon">👤</div>
          <div class="stat-content">
            <h3>{{ dashboardData.totalClientsActifs }}</h3>
            <p>Clients Actifs</p>
          </div>
        </div>
        
        <div class="stat-card">
          <div class="stat-icon">💰</div>
          <div class="stat-content">
            <h3>{{ formatCurrency(dashboardData.montantTotalPaiements) }}</h3>
            <p>Paiements</p>
          </div>
        </div>
        
        <div class="stat-card">
          <div class="stat-icon">⚠️</div>
          <div class="stat-content">
            <h3>{{ formatCurrency(dashboardData.montantTotalArrieres) }}</h3>
            <p>Arriérés</p>
          </div>
        </div>
      </div>

      <div class="top-agents">
        <h2>Top Agents Collecteurs</h2>
        <div class="agents-list">
          <div v-for="(agent, index) in dashboardData.top5AgentsCollecteurs" 
               :key="agent.idAgent" 
               class="agent-item">
            <div class="agent-rank">{{ index + 1 }}</div>
            <div class="agent-info">
              <h4>{{ agent.nomComplet || `Agent ${agent.idAgent}` }}</h4>
              <p>{{ formatCurrency(agent.montantCollecte) }} • {{ agent.nombrePaiements }} paiements</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Notifications -->
    <div v-if="notifications.length > 0" class="notifications">
      <div v-for="notification in notifications" 
           :key="notification.id" 
           :class="['notification', notification.type]">
        <span class="notification-icon">{{ notification.icon }}</span>
        <span class="notification-text">{{ notification.message }}</span>
        <button @click="removeNotification(notification.id)" class="close-btn">×</button>
      </div>
    </div>
  </div>
</template>

<script>
import DashboardSignalRService from '@/services/dashboardSignalR';

export default {
  name: 'Dashboard',
  data() {
    return {
      signalRService: new DashboardSignalRService(),
      dashboardData: null,
      loading: true,
      connectionState: 'Disconnected',
      notifications: [],
      notificationId: 0
    };
  },
  async mounted() {
    await this.initializeSignalR();
  },
  beforeUnmount() {
    this.signalRService.disconnect();
  },
  methods: {
    async initializeSignalR() {
      try {
        // Configuration des callbacks
        this.signalRService.on('onDashboardUpdated', (data) => {
          this.dashboardData = data.dashboard;
          this.loading = false;
          this.addNotification('Dashboard mis à jour', 'Les données ont été rafraîchies', 'success');
        });

        this.signalRService.on('onNewPaiement', (data) => {
          this.addNotification('Nouveau paiement', 
            `Un paiement de ${this.formatCurrency(data.paiement.montant)} a été enregistré`, 'info');
        });

        this.signalRService.on('onNewClient', (data) => {
          this.addNotification('Nouveau client', 
            `${data.client.nom} a été ajouté`, 'success');
        });

        this.signalRService.on('onDashboardStatusChanged', (data) => {
          this.addNotification('Changement de statut', 
            `${data.entityType} #${data.entityId}: ${data.newStatus}`, 'warning');
        });

        // Connexion au hub
        await this.signalRService.connect();
        
        // Abonnement aux mises à jour (remplacer 1 par l'ID de la société)
        await this.signalRService.subscribeToDashboardUpdates(1);
        
        // Écouter les changements de connexion
        this.connectionState = this.signalRService.getConnectionState();
        
      } catch (error) {
        console.error('Erreur d\'initialisation SignalR:', error);
        this.loading = false;
        this.addNotification('Erreur de connexion', error.message, 'error');
      }
    },

    async refreshDashboard() {
      try {
        await this.signalRService.requestDashboardUpdate(1);
      } catch (error) {
        console.error('Erreur de rafraîchissement:', error);
      }
    },

    addNotification(title, message, type = 'info') {
      const notification = {
        id: ++this.notificationId,
        title,
        message,
        type,
        icon: type === 'success' ? '✅' : type === 'error' ? '❌' : type === 'warning' ? '⚠️' : 'ℹ️'
      };
      
      this.notifications.push(notification);
      
      // Auto-suppression après 5 secondes
      setTimeout(() => {
        this.removeNotification(notification.id);
      }, 5000);
    },

    removeNotification(id) {
      const index = this.notifications.findIndex(n => n.id === id);
      if (index > -1) {
        this.notifications.splice(index, 1);
      }
    },

    formatCurrency(amount) {
      return new Intl.NumberFormat('fr-FR', {
        style: 'currency',
        currency: 'XOF',
        minimumFractionDigits: 0
      }).format(amount);
    }
  }
};
</script>

<style scoped>
.dashboard {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
}

.connection-status {
  display: flex;
  align-items: center;
  gap: 15px;
}

.status {
  padding: 5px 10px;
  border-radius: 15px;
  font-size: 12px;
  font-weight: bold;
}

.status.connected {
  background-color: #4caf50;
  color: white;
}

.status.connecting {
  background-color: #ff9800;
  color: white;
}

.status.disconnected {
  background-color: #f44336;
  color: white;
}

button {
  padding: 8px 16px;
  border: none;
  border-radius: 5px;
  background-color: #2196f3;
  color: white;
  cursor: pointer;
}

button:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}

.loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 400px;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #2196f3;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.stat-card {
  background: white;
  border-radius: 10px;
  padding: 20px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  display: flex;
  align-items: center;
  gap: 15px;
}

.stat-icon {
  font-size: 2rem;
}

.stat-content h3 {
  margin: 0;
  font-size: 1.5rem;
  color: #333;
}

.stat-content p {
  margin: 5px 0 0 0;
  color: #666;
}

.top-agents {
  background: white;
  border-radius: 10px;
  padding: 20px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.1);
}

.top-agents h2 {
  margin-top: 0;
  color: #333;
}

.agents-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.agent-item {
  display: flex;
  align-items: center;
  gap: 15px;
  padding: 15px;
  border-radius: 8px;
  background: #f8f9fa;
}

.agent-rank {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background: #2196f3;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
}

.agent-info h4 {
  margin: 0;
  color: #333;
}

.agent-info p {
  margin: 5px 0 0 0;
  color: #666;
  font-size: 0.9rem;
}

.notifications {
  position: fixed;
  top: 20px;
  right: 20px;
  z-index: 1000;
  max-width: 400px;
}

.notification {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 15px;
  margin-bottom: 10px;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0,0,0,0.2);
  animation: slideIn 0.3s ease-out;
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

.notification.success {
  background: #4caf50;
  color: white;
}

.notification.error {
  background: #f44336;
  color: white;
}

.notification.warning {
  background: #ff9800;
  color: white;
}

.notification.info {
  background: #2196f3;
  color: white;
}

.close-btn {
  background: none;
  border: none;
  color: inherit;
  font-size: 18px;
  cursor: pointer;
  margin-left: auto;
}
</style>
```

---

## 🔧 Configuration de l'API

### **CORS Configuration**
Assurez-vous que votre API autorise les connexions WebSocket depuis votre domaine frontend.

### **HTTPS en Production**
En production, utilisez HTTPS et configurez les certificats SSL appropriés.

### **Gestion des Erreurs**
Implémentez une gestion robuste des erreurs :
- Tentatives de reconnexion automatique
- Notifications utilisateur en cas d'échec
- Fallback vers des requêtes HTTP classiques

---

## 🎯 Bonnes Pratiques

### **Frontend**
1. **Gestion d'état** : Utilisez un store (Redux, Vuex, Pinia) pour gérer les données temps réel
2. **Optimisation** : Limitez le nombre d'abonnements simultanés
3. **Sécurité** : Stockez les tokens JWT de manière sécurisée
4. **Performance** : Désabonnez les événements non utilisés

### **Backend**
1. **Logging** : Surveillez les connexions et déconnexions
2. **Scaling** : Prévoyez un scaling horizontal pour les hubs
3. **Monitoring** : Surveillez la charge des WebSocket
4. **Fallback** : Prévoyez une alternative HTTP si SignalR échoue

---

## 📞 Support

Pour toute question sur l'intégration SignalR :
- Consultez la documentation officielle Microsoft SignalR
- Vérifiez les logs de l'API pour les erreurs de connexion
- Testez avec l'outil SignalR Client de Microsoft

**🚀 Vos applications sont maintenant prêtes pour recevoir des mises à jour en temps réel du dashboard et des statistiques !**
