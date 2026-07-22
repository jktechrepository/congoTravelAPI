# 📖 Exemples Pratiques d'Utilisation des Endpoints de Synchronisation

## 🚀 Démarrage Rapide

### 1. Initialisation complète (Bootstrap)

```bash
# Étape 1: Obtenir un JWT
curl -X POST https://localhost:7110/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@kenergie.com",
    "password": "password123"
  }'

# Étape 2: Bootstrap (remplacer VOTRE_JWT)
curl -X GET "https://localhost:7110/api/sync/bootstrap" \
  -H "Authorization: Bearer VOTRE_JWT" \
  -H "Content-Type: application/json"
```

**Réponse attendue:**
```json
{
  "watermark": "2024-03-22T10:30:00.000Z_12345",
  "clients": [...],
  "arrears": [...]
}
```

---

## 📱 Scénarios d'Utilisation

### 🏠 Application Mobile - Première Installation

```javascript
class MobileAppSync {
  constructor() {
    this.jwt = null;
    this.lastWatermark = localStorage.getItem('lastWatermark');
  }

  // Première synchronisation complète
  async firstSync() {
    try {
      // 1. Authentification
      this.jwt = await this.authenticate();
      
      // 2. Bootstrap complet
      const bootstrap = await this.fetch('/api/sync/bootstrap');
      
      // 3. Stockage local
      await this.saveLocally(bootstrap.clients, 'clients');
      await this.saveLocally(bootstrap.arrears, 'arrears');
      
      // 4. Sauvegarder le watermark
      this.lastWatermark = bootstrap.watermark;
      localStorage.setItem('lastWatermark', this.lastWatermark);
      
      console.log('✅ Première synchronisation réussie');
    } catch (error) {
      console.error('❌ Erreur de synchronisation:', error);
    }
  }

  async authenticate() {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: 'user@example.com',
        password: 'password'
      })
    });
    const data = await response.json();
    return data.token;
  }

  async fetch(endpoint) {
    const response = await fetch(`https://api.kenergie.com${endpoint}`, {
      headers: { 'Authorization': `Bearer ${this.jwt}` }
    });
    return await response.json();
  }
}
```

### 🔄 Synchronisation Quotidienne

```javascript
class DailySync {
  async performDailySync() {
    const lastWatermark = localStorage.getItem('lastWatermark');
    
    try {
      // 1. Synchroniser les clients modifiés
      await this.syncClients(lastWatermark);
      
      // 2. Synchroniser les nouveaux arriérés
      await this.syncArrears(lastWatermark);
      
      // 3. Nettoyer les suppressions
      await this.syncDeletions(lastWatermark);
      
      // 4. Envoyer les paiements offline
      await this.syncPendingPayments();
      
    } catch (error) {
      console.error('Erreur de synchronisation quotidienne:', error);
    }
  }

  async syncClients(since) {
    let hasMore = true;
    let cursor = null;
    let allClients = [];

    while (hasMore) {
      const url = since 
        ? `/api/sync/clients?since=${since}&pageSize=100`
        : `/api/sync/clients?pageSize=100`;
      
      if (cursor) {
        url += `&cursor=${cursor}`;
      }

      const response = await this.fetch(url);
      allClients.push(...response.items);
      
      cursor = response.nextCursor;
      hasMore = response.hasMore;
    }

    // Mettre à jour la base de données locale
    await this.updateLocalClients(allClients);
    
    // Mettre à jour le watermark
    if (response.nextSince) {
      localStorage.setItem('lastWatermark', response.nextSince);
    }
  }
}
```

### 💳 Traitement des Paiements Offline

```javascript
class OfflinePaymentManager {
  constructor() {
    this.pendingPayments = JSON.parse(
      localStorage.getItem('pendingPayments') || '[]'
    );
  }

  // Enregistrer un paiement en mode offline
  recordPayment(paymentData) {
    const payment = {
      ...paymentData,
      clientRequestId: this.generateUUID(),
      timestamp: new Date().toISOString(),
      status: 'pending'
    };

    this.pendingPayments.push(payment);
    localStorage.setItem('pendingPayments', JSON.stringify(this.pendingPayments));
    
    // Essayer de synchroniser immédiatement si online
    if (navigator.onLine) {
      this.syncPendingPayments();
    }
  }

  // Synchroniser tous les paiements en attente
  async syncPendingPayments() {
    if (this.pendingPayments.length === 0) return;

    try {
      const response = await fetch('/api/sync/payments/batch', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.jwt}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          payments: this.pendingPayments
        })
      });

      const result = await response.json();

      // Traiter les succès
      result.processed.forEach(payment => {
        if (payment.statut === 'success') {
          this.removePendingPayment(payment.clientRequestId);
          this.updateLocalPaymentStatus(payment.clientRequestId, 'paid');
        }
      });

      // Gérer les échecs
      result.failed.forEach(payment => {
        console.error(`Paiement échoué: ${payment.clientRequestId}`, payment.message);
      });

    } catch (error) {
      console.error('Erreur de synchronisation des paiements:', error);
    }
  }

  generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }
}
```

---

## 🌐 Exemples par Langage

### Python

```python
import requests
import json
import time
from typing import List, Dict, Optional

class CongoTravelSyncClient:
    def __init__(self, base_url: str, email: str, password: str):
        self.base_url = base_url.rstrip('/')
        self.jwt = None
        self.last_watermark = None
        self.authenticate(email, password)

    def authenticate(self, email: str, password: str):
        """Authentification et récupération du JWT"""
        response = requests.post(
            f"{self.base_url}/api/auth/login",
            json={"email": email, "password": password}
        )
        response.raise_for_status()
        data = response.json()
        self.jwt = data["token"]

    def bootstrap(self) -> Dict:
        """Synchronisation initiale complète"""
        response = requests.get(
            f"{self.base_url}/api/sync/bootstrap",
            headers={"Authorization": f"Bearer {self.jwt}"}
        )
        response.raise_for_status()
        data = response.json()
        self.last_watermark = data["watermark"]
        return data

    def sync_clients(self, since: Optional[str] = None, page_size: int = 100) -> List[Dict]:
        """Synchronisation incrémentielle des clients"""
        all_clients = []
        cursor = None
        has_more = True

        while has_more:
            params = {"pageSize": page_size}
            if since:
                params["since"] = since
            if cursor:
                params["cursor"] = cursor

            response = requests.get(
                f"{self.base_url}/api/sync/clients",
                params=params,
                headers={"Authorization": f"Bearer {self.jwt}"}
            )
            response.raise_for_status()
            data = response.json()

            all_clients.extend(data["items"])
            cursor = data.get("nextCursor")
            has_more = data.get("hasMore", False)
            
            # Mettre à jour le watermark
            if data.get("nextSince"):
                self.last_watermark = data["nextSince"]

        return all_clients

    def submit_payments(self, payments: List[Dict]) -> Dict:
        """Soumission batch de paiements"""
        response = requests.post(
            f"{self.base_url}/api/sync/payments/batch",
            json={"payments": payments},
            headers={
                "Authorization": f"Bearer {self.jwt}",
                "Content-Type": "application/json"
            }
        )
        response.raise_for_status()
        return response.json()

# Utilisation
if __name__ == "__main__":
    client = CongoTravelSyncClient(
        "https://api.kenergie.com",
        "user@example.com",
        "password123"
    )

    # Bootstrap initial
    bootstrap_data = client.bootstrap()
    print(f"Sync initial: {len(bootstrap_data['clients'])} clients")

    # Synchronisation quotidienne
    clients = client.sync_clients(since=client.last_watermark)
    print(f"Sync quotidien: {len(clients)} clients modifiés")

    # Paiement batch
    payments = [
        {
            "clientRequestId": "payment-001",
            "idClient": 1,
            "idClientFacture": 1,
            "idFacture": 1,
            "montantPaye": 5000,
            "datePaiementUtc": "2024-03-22T10:30:00.000Z",
            "methodePaiement": "Mobile Money",
            "referenceTransaction": "TX123456",
            "commentaire": "Paiement en ligne"
        }
    ]
    
    result = client.submit_payments(payments)
    print(f"Paiements traités: {result['summary']['success']}/{result['summary']['total']}")
```

### C# (.NET)

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class CongoTravelSyncClient
{
    private readonly HttpClient _httpClient;
    private string _jwt;
    private string _lastWatermark;

    public CongoTravelSyncClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<bool> AuthenticateAsync(string email, string password)
    {
        var loginData = new { email, password };
        var content = new StringContent(
            JsonSerializer.Serialize(loginData),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/api/auth/login", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResponse>(responseContent);
        
        _jwt = authResult.Token;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _jwt);

        return true;
    }

    public async Task<BootstrapResponse> BootstrapAsync()
    {
        var response = await _httpClient.GetAsync("/api/sync/bootstrap");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BootstrapResponse>(content);
        
        _lastWatermark = result.Watermark;
        return result;
    }

    public async Task<SyncResponse<ClientDto>> SyncClientsAsync(
        string since = null, 
        int pageSize = 100)
    {
        var allItems = new List<ClientDto>();
        var hasMore = true;
        var cursor = (string)null;

        while (hasMore)
        {
            var url = $"/api/sync/clients?pageSize={pageSize}";
            if (!string.IsNullOrEmpty(since))
                url += $"&since={since}";
            if (!string.IsNullOrEmpty(cursor))
                url += $"&cursor={Uri.EscapeDataString(cursor)}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SyncResponse<ClientDto>>(content);

            allItems.AddRange(result.Items);
            cursor = result.NextCursor;
            hasMore = result.HasMore;

            if (!string.IsNullOrEmpty(result.NextSince))
                _lastWatermark = result.NextSince;
        }

        return new SyncResponse<ClientDto>
        {
            Items = allItems,
            NextSince = _lastWatermark,
            HasMore = false
        };
    }

    public async Task<PaymentBatchResponse> SubmitPaymentsAsync(
        IEnumerable<PaymentRequestDto> payments)
    {
        var requestData = new { payments };
        var content = new StringContent(
            JsonSerializer.Serialize(requestData),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/api/sync/payments/batch", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaymentBatchResponse>(responseContent);
    }
}

// DTOs
public class AuthResponse
{
    public string Token { get; set; }
}

public class BootstrapResponse
{
    public string Watermark { get; set; }
    public List<ClientDto> Clients { get; set; }
    public List<ArrearDto> Arrears { get; set; }
}

public class SyncResponse<T>
{
    public List<T> Items { get; set; }
    public string NextCursor { get; set; }
    public string NextSince { get; set; }
    public bool HasMore { get; set; }
}
```

---

## 🧪 Tests d'Intégration

### Script de Test Bash

```bash
#!/bin/bash

# Configuration
API_URL="https://localhost:7110"
EMAIL="test@kenergie.com"
PASSWORD="test123"

# Couleurs pour l'affichage
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🧪 Démarrage des tests d'intégration Sync API${NC}"

# 1. Authentification
echo -e "\n${YELLOW}1️⃣ Test d'authentification...${NC}"
AUTH_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")

JWT=$(echo $AUTH_RESPONSE | jq -r '.token')

if [ "$JWT" != "null" ] && [ "$JWT" != "" ]; then
    echo -e "${GREEN}✅ Authentification réussie${NC}"
else
    echo -e "${RED}❌ Authentification échouée${NC}"
    exit 1
fi

# 2. Test Bootstrap
echo -e "\n${YELLOW}2️⃣ Test Bootstrap...${NC}"
BOOTSTRAP_RESPONSE=$(curl -s -X GET "$API_URL/api/sync/bootstrap" \
  -H "Authorization: Bearer $JWT")

WATERMARK=$(echo $BOOTSTRAP_RESPONSE | jq -r '.watermark')
CLIENTS_COUNT=$(echo $BOOTSTRAP_RESPONSE | jq '.clients | length')

if [ "$WATERMARK" != "null" ]; then
    echo -e "${GREEN}✅ Bootstrap réussi - Watermark: $WATERMARK${NC}"
    echo -e "${GREEN}📊 $CLIENTS_COUNT clients récupérés${NC}"
else
    echo -e "${RED}❌ Bootstrap échoué${NC}"
fi

# 3. Test Sync Clients avec pagination
echo -e "\n${YELLOW}3️⃣ Test Sync Clients (pagination)...${NC}"
SYNC_RESPONSE=$(curl -s -X GET "$API_URL/api/sync/clients?pageSize=5" \
  -H "Authorization: Bearer $JWT")

ITEMS_COUNT=$(echo $SYNC_RESPONSE | jq '.items | length')
HAS_MORE=$(echo $SYNC_RESPONSE | jq '.hasMore')

if [ "$ITEMS_COUNT" -eq 5 ] && [ "$HAS_MORE" = "true" ]; then
    echo -e "${GREEN}✅ Pagination clients réussie${NC}"
else
    echo -e "${RED}❌ Pagination clients échouée${NC}"
fi

# 4. Test Paiement Batch
echo -e "\n${YELLOW}4️⃣ Test Paiement Batch...${NC}"
PAYMENT_DATA='{
  "payments": [
    {
      "clientRequestId": "test-payment-'$(date +%s)'",
      "idClient": 1,
      "idClientFacture": 1,
      "idFacture": 1,
      "montantPaye": 1000,
      "datePaiementUtc": "'$(date -u +%Y-%m-%dT%H:%M:%S.000Z)'",
      "methodePaiement": "Test",
      "referenceTransaction": "TEST-'$(date +%s)'",
      "commentaire": "Paiement de test"
    }
  ]
}'

PAYMENT_RESPONSE=$(curl -s -X POST "$API_URL/api/sync/payments/batch" \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d "$PAYMENT_DATA")

SUCCESS_COUNT=$(echo $PAYMENT_RESPONSE | jq '.summary.success')

if [ "$SUCCESS_COUNT" -eq 1 ]; then
    echo -e "${GREEN}✅ Paiement batch réussi${NC}"
else
    echo -e "${RED}❌ Paiement batch échoué${NC}"
    echo $PAYMENT_RESPONSE | jq .
fi

echo -e "\n${GREEN}🎉 Tests d'intégration terminés!${NC}"
```

---

## 📊 Monitoring et Débogage

### Logs de Synchronisation

```javascript
class SyncLogger {
  static log(operation, data, duration = null) {
    const logEntry = {
      timestamp: new Date().toISOString(),
      operation,
      data: JSON.stringify(data),
      duration: duration ? `${duration}ms` : null
    };

    console.log(`[SYNC] ${operation}:`, logEntry);
    
    // Envoyer au serveur de monitoring
    if (navigator.onLine) {
      this.sendToMonitoring(logEntry);
    }
  }

  static async sendToMonitoring(logEntry) {
    try {
      await fetch('/api/monitoring/logs', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(logEntry)
      });
    } catch (error) {
      console.warn('Impossible d\'envoyer les logs de monitoring:', error);
    }
  }
}

// Utilisation
const startTime = performance.now();
const result = await syncClients();
const duration = performance.now() - startTime;

SyncLogger.log('sync_clients', {
  itemCount: result.items.length,
  watermark: result.nextSince
}, duration);
```

---

## 🚨 Gestion des Erreurs Avancée

```javascript
class SyncErrorHandler {
  static async handleSyncError(error, context) {
    const errorInfo = {
      timestamp: new Date().toISOString(),
      context,
      error: error.message,
      stack: error.stack,
      userAgent: navigator.userAgent,
      online: navigator.onLine
    };

    // Classification des erreurs
    if (error.message.includes('401')) {
      return this.handleAuthError(errorInfo);
    } else if (error.message.includes('429')) {
      return this.handleRateLimitError(errorInfo);
    } else if (error.message.includes('500')) {
      return this.handleServerError(errorInfo);
    } else {
      return this.handleUnknownError(errorInfo);
    }
  }

  static async handleAuthError(errorInfo) {
    console.error('Erreur d\'authentification:', errorInfo);
    
    // Rafraîchir le token
    try {
      await this.refreshToken();
      return { retry: true, delay: 0 };
    } catch (refreshError) {
      // Rediriger vers la page de login
      window.location.href = '/login';
      return { retry: false };
    }
  }

  static async handleRateLimitError(errorInfo) {
    console.warn('Rate limit atteint:', errorInfo);
    
    // Attendre avec backoff exponentiel
    const delay = Math.min(1000 * Math.pow(2, this.retryCount), 30000);
    return { retry: true, delay };
  }

  static async handleServerError(errorInfo) {
    console.error('Erreur serveur:', errorInfo);
    
    // Réessayer avec un délai progressif
    const delay = Math.min(5000 * this.retryCount, 60000);
    return { retry: true, delay };
  }
}
```

---

## 📈 Performance Tips

### 1. Pagination Optimale

```javascript
// ✅ Bon : utiliser des pages de taille raisonnable
const PAGE_SIZE = 100; // Optimal pour la plupart des cas

// ❌ Éviter : pages trop petites ou trop grandes
const BAD_PAGE_SIZE_1 = 5;   // Trop d'appels réseau
const BAD_PAGE_SIZE_2 = 10000; // Timeout et mémoire élevée
```

### 2. Cache Intelligent

```javascript
class SyncCache {
  constructor() {
    this.cache = new Map();
    this.cacheTimeout = 5 * 60 * 1000; // 5 minutes
  }

  async get(key) {
    const cached = this.cache.get(key);
    if (cached && Date.now() - cached.timestamp < this.cacheTimeout) {
      return cached.data;
    }
    return null;
  }

  set(key, data) {
    this.cache.set(key, {
      data,
      timestamp: Date.now()
    });
  }
}
```

### 3. Compression

```javascript
// Activer la compression gzip automatiquement
const response = await fetch(url, {
  headers: {
    'Accept-Encoding': 'gzip, deflate',
    'Authorization': `Bearer ${jwt}`
  }
});
```

---

*Document généré le 22 mars 2024 - Pour les développeurs intégrateurs*
