# 📚 Documentation des Endpoints de Synchronisation Offline

## 🎯 Vue d'ensemble

L'API de synchronisation offline permet aux applications mobiles/desktop de synchroniser les données avec le serveur de manière efficace et sécurisée. Elle utilise une pagination cursor-based pour gérer de grands volumes de données et un système de watermark pour le delta sync.

---

## 🔐 Authentification

**Tous les endpoints nécessitent un JWT valide dans l'en-tête Authorization:**

```http
Authorization: Bearer VOTRE_JETON_JWT
```

---

## 📡 Endpoints Disponibles

### 1. 🚀 Bootstrap - Initialisation complète

**Endpoint:** `GET /api/sync/bootstrap`

**Description:** Récupère toutes les données initiales pour une première synchronisation complète.

**Paramètres:**
- `idSociete` (optionnel) : ID de la société pour l'isolation multi-tenant

**Réponse:**
```json
{
  "watermark": "2024-03-21T10:30:00.000Z_12345",
  "clients": [
    {
      "idClient": 1,
      "nomClient": "Jean Dupont",
      "adresseClient": "123 Rue de la République",
      "telephone": "0123456789",
      "idSociete": 1,
      "updatedAt": "2024-03-21T10:30:00.000Z"
    }
  ],
  "arrears": [
    {
      "idClientFacture": 1,
      "idFacture": 1,
      "idClient": 1,
      "numeroFacture": "F2024-001",
      "montantTotal": 15000,
      "montantPaye": 5000,
      "montantDu": 10000,
      "mois": 3,
      "annees": 2024,
      "dateEmission": "2024-03-01T00:00:00.000Z",
      "estArrierePreExistant": false,
      "libelleUsage": "Électricité",
      "dateModification": "2024-03-21T10:30:00.000Z"
    }
  ]
}
```

**Cas d'usage:**
- Première installation de l'application
- Réinitialisation complète des données locales
- Synchronisation forcée après longue période d'inactivité

---

### 2. 👥 Clients - Synchronisation incrémentielle

**Endpoint:** `GET /api/sync/clients`

**Description:** Synchronise les clients ayant au moins une réservation non supprimée dans la société du JWT, avec pagination cursor-based et delta sync (aligné sur `GET /api/Client/societe/{idSociete}`).

**Paramètres:**
- `since` (optionnel) : Watermark pour récupérer uniquement les modifications
- `cursor` (optionnel) : Curseur pour la pagination
- `pageSize` (optionnel) : Nombre d'éléments par page (défaut: 50, max: 1000)

**Exemples:**

*Première page:*
```http
GET /api/sync/clients?pageSize=100
```

*Page suivante avec cursor:*
```http
GET /api/sync/clients?cursor=eyJ1c2VySWQiOjEsInVwZGF0ZWRBdCI6IjIwMjQtMDMtMjFUMTA6MzA6MDAuMDAwWiJ9&pageSize=100
```

*Delta sync (modifications récentes):*
```http
GET /api/sync/clients?since=2024-03-21T10:30:00.000Z_12345
```

**Réponse:**
```json
{
  "items": [
    {
      "idClient": 1,
      "nomClient": "Jean Dupont",
      "adresseClient": "123 Rue de la République",
      "telephone": "0123456789",
      "idSociete": 1,
      "updatedAt": "2024-03-21T10:30:00.000Z",
      "isDeleted": false
    }
  ],
  "nextCursor": "eyJ1c2VySWQiOjIsInVwZGF0ZWRBdCI6IjIwMjQtMDMtMjFUMTE6NDU6MDAuMDAwWiJ9",
  "nextSince": "2024-03-21T10:30:00.000Z_12345",
  "hasMore": true
}
```

**Cas d'usage:**
- Synchronisation quotidienne des modifications
- Mise à jour progressive des données
- Gestion de grands volumes de clients

---

### 3. 💰 Arrears - Synchronisation des factures impayées

**Endpoint:** `GET /api/sync/arrears`

**Description:** Synchronise les factures impayées et arriérés.

**Paramètres:**
- `since` (optionnel) : Watermark pour le delta sync
- `cursor` (optionnel) : Curseur pour pagination
- `pageSize` (optionnel) : Taille de page (défaut: 50, max: 1000)

**Réponse:**
```json
{
  "items": [
    {
      "idClientFacture": 1,
      "idFacture": 1,
      "idClient": 1,
      "numeroFacture": "F2024-001",
      "montantTotal": 15000,
      "montantPaye": 5000,
      "montantDu": 10000,
      "mois": 3,
      "annees": 2024,
      "dateEmission": "2024-03-01T00:00:00.000Z",
      "estArrierePreExistant": false,
      "libelleUsage": "Électricité",
      "dateModification": "2024-03-21T10:30:00.000Z"
    }
  ],
  "nextCursor": "eyJpZENsaWVudEZhY3R1cmUiOjIsImRhdGVNb2RpZmljYXRpb24iOiIyMDI0LTAzLTIxVDExOjQ1OjAwLjAwMFoifQ==",
  "nextSince": "2024-03-21T10:30:00.000Z_12345",
  "hasMore": true
}
```

**Cas d'usage:**
- Mise à jour des états de paiement
- Calcul des soldes dus
- Affichage des factures en retard

---

### 4. 🗑️ Deletions - Gestion des suppressions

**Endpoint:** `GET /api/sync/deletions`

**Description:** Récupère la liste des éléments supprimés (soft delete).

**Paramètres:**
- `since` (optionnel) : Watermark pour les suppressions récentes
- `cursor` (optionnel) : Curseur pour pagination
- `pageSize` (optionnel) : Taille de page (défaut: 50, max: 1000)
- `type` (optionnel) : Type d'élément (`client`, `arrears`)

**Réponse:**
```json
{
  "items": [
    {
      "id": 123,
      "type": "client",
      "deletedAt": "2024-03-21T10:30:00.000Z",
      "idSociete": 1
    }
  ],
  "nextCursor": "eyJkZWxldGVkQXQiOiIyMDI0LTAzLTIxVDEwOjMwOjAwLjAwMFoifQ==",
  "nextSince": "2024-03-21T10:30:00.000Z_12345",
  "hasMore": false
}
```

**Cas d'usage:**
- Nettoyage des données locales supprimées
- Synchronisation des suppressions
- Maintien de la cohérence des données

---

### 5. 💳 Payments - Traitement batch des paiements

**Endpoint:** `POST /api/sync/payments/batch`

**Description:** Soumet un lot de paiements de manière idempotente.

**Corps de la requête:**
```json
{
  "payments": [
    {
      "clientRequestId": "uuid-client-123",
      "idClient": 1,
      "idClientFacture": 1,
      "idFacture": 1,
      "montantPaye": 5000,
      "datePaiementUtc": "2024-03-21T10:30:00.000Z",
      "methodePaiement": "Mobile Money",
      "referenceTransaction": "TX123456789",
      "commentaire": "Paiement partiel"
    }
  ]
}
```

**Réponse:**
```json
{
  "processed": [
    {
      "clientRequestId": "uuid-client-123",
      "idPaiement": 456,
      "statut": "success",
      "message": "Paiement traité avec succès"
    }
  ],
  "failed": [
    {
      "clientRequestId": "uuid-client-456",
      "statut": "error",
      "message": "Facture déjà payée",
      "errorCode": "ALREADY_PAID"
    }
  ],
  "summary": {
    "total": 2,
    "success": 1,
    "failed": 1
  }
}
```

**Cas d'usage:**
- Synchronisation des paiements offline
 Traitement en lot pour optimiser les appels
- Gestion des paiements dupliqués (idempotence)

---

## 🔧 Concepts Techniques

### 🎯 Watermark

Le watermark est une chaîne encodée qui représente le point de synchronisation:
- Format: `{timestamp}_{id}`
- Exemple: `2024-03-21T10:30:00.000Z_12345`
- Usage: Passé dans le paramètre `since` pour le delta sync

### 📍 Cursor

Le cursor permet la pagination efficace:
- Encodé en Base64 avec HMAC-SHA256
- Contient les informations de tri (updatedAt + id)
- Usage: Passé dans le paramètre `cursor` pour les pages suivantes

### ♻️ Idempotence

Le système garantit l'idempotence:
- Chaque paiement a un `clientRequestId` unique
- Les doublons sont automatiquement détectés
- Le même paiement ne peut être traité qu'une seule fois

---

## 📊 Performance et Limites

### 🚀 Limites configurées

- **Taille de page**: Maximum 1000 éléments par requête
- **Timeout**: 30 secondes par requête
- **Rate limiting**: 100 requêtes/minute par utilisateur

### ⚡ Optimisations

- **Compression GZIP**: Activée automatiquement
- **Cache**: 5 minutes pour les données statiques
- **Index de base de données**: Optimisés pour les requêtes de synchronisation

### 📈 Performance attendue

| Opération | Temps de réponse | Volume typique |
|-----------|------------------|----------------|
| Bootstrap | 2-5 secondes | 1000-5000 éléments |
| Sync clients | 500ms-2s | 100-1000 éléments/page |
| Sync arrears | 500ms-2s | 100-1000 éléments/page |
| Batch payments | 1-3 secondes | 10-100 paiements |

---

## 🔄 Stratégies de Synchronisation

### 🏁 Première synchronisation (Bootstrap)

1. Appeler `/api/sync/bootstrap`
2. Stocker le watermark retourné
3. Afficher les données à l'utilisateur

### 📆 Synchronisation quotidienne (Delta Sync)

1. Utiliser le dernier watermark connu
2. Appeler `/api/sync/clients?since={watermark}`
3. Appeler `/api/sync/arrears?since={watermark}`
4. Appeler `/api/sync/deletions?since={watermark}`
5. Mettre à jour le watermark

### 📱 Synchronisation en temps réel

1. Écouter les événements de modification locale
2. Envoyer les paiements via `/api/sync/payments/batch`
3. Récupérer les modifications du serveur périodiquement

---

## 🚨 Gestion des Erreurs

### 📋 Codes d'erreur courants

| Code | Message | Action |
|------|----------|---------|
| 400 | Requête invalide | Vérifier les paramètres |
| 401 | Non autorisé | Vérifier le JWT |
| 403 | Accès refusé | Vérifier idSociete |
| 429 | Trop de requêtes | Attendre et réessayer |
| 500 | Erreur serveur | Réessayer plus tard |

### 🔄 Stratégie de retry

```javascript
const retryStrategy = {
  maxRetries: 3,
  baseDelay: 1000,
  maxDelay: 10000,
  backoff: 'exponential'
};
```

---

## 🔐 Sécurité

### 🛡️ Tokens JWT

- Durée de vie: 24 heures
- Algorithme: HS256
- Contient: userId, idSociete, permissions

### 🔒 Validation des données

- Tous les curseurs sont signés avec HMAC-SHA256
- Les timestamps sont validés côté serveur
- Protection contre les injections SQL

---

## 📱 Exemples d'implémentation

### 📲 JavaScript/TypeScript

```typescript
class SyncService {
  private baseUrl = 'https://api.kenergie.com/api/sync';
  private jwt: string;
  private lastWatermark?: string;

  async bootstrap(): Promise<BootstrapResponse> {
    const response = await fetch(`${this.baseUrl}/bootstrap`, {
      headers: { 'Authorization': `Bearer ${this.jwt}` }
    });
    const data = await response.json();
    this.lastWatermark = data.watermark;
    return data;
  }

  async syncClients(since?: string): Promise<ClientSyncResponse> {
    const url = since 
      ? `${this.baseUrl}/clients?since=${since}`
      : `${this.baseUrl}/clients`;
    
    const response = await fetch(url, {
      headers: { 'Authorization': `Bearer ${this.jwt}` }
    });
    return await response.json();
  }
}
```

### 📱 Swift (iOS)

```swift
class SyncService {
    private let baseURL = "https://api.kenergie.com/api/sync"
    private var jwt: String
    private var lastWatermark: String?
    
    func bootstrap() async throws -> BootstrapResponse {
        var request = URLRequest(url: URL(string: "\(baseURL)/bootstrap")!)
        request.setValue("Bearer \(jwt)", forHTTPHeaderField: "Authorization")
        
        let (data, _) = try await URLSession.shared.data(for: request)
        let response = try JSONDecoder().decode(BootstrapResponse.self, from: data)
        lastWatermark = response.watermark
        return response
    }
}
```

---

## 📞 Support et Débogage

### 🔍 Outils de débogage

1. **Logs détaillés**: Activer les logs de synchronisation
2. **Watermark tracking**: Surveiller l'évolution des watermarks
3. **Performance monitoring**: Mesurer les temps de réponse

### 📧 Contact support

- **Email**: support@kenergie.com
- **Documentation**: https://docs.kenergie.com/sync
- **Status**: https://status.kenergie.com

---

## 📝 Changelog

### v1.0.0 (2024-03-21)
- ✅ Implémentation des 5 endpoints de base
- ✅ Pagination cursor-based
- ✅ Delta sync avec watermark
- ✅ Idempotence des paiements
- ✅ Support multi-tenant

### Prochaines versions
- 🔄 Support de la synchronisation bidirectionnelle
- 📊 Analytics de synchronisation
- 🔔 Notifications push pour les mises à jour

---

*Document généré le 22 mars 2024 - Version 1.0.0*
