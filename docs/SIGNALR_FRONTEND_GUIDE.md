# Guide SignalR pour les Développeurs Frontend - CongoTravel API

## 📋 Table des matières

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Configuration de base](#configuration-de-base)
4. [Authentification](#authentification)
5. [Méthodes disponibles](#méthodes-disponibles)
6. [Événements à écouter](#événements-à-écouter)
7. [Exemples de code](#exemples-de-code)
8. [Gestion des erreurs](#gestion-des-erreurs)
9. [Tests](#tests)
10. [Bonnes pratiques](#bonnes-pratiques)

---

## 🎯 Introduction

SignalR permet la communication en temps réel bidirectionnelle entre le serveur et les clients. Le hub `NotificationHub` de CongoTravel API permet de recevoir des notifications en temps réel, gérer les groupes, et interagir avec le serveur.

### URL du Hub

```
https://localhost:7110/hubs/notifications
```

**En production :** Remplacez `localhost:7110` par l'URL de votre serveur.

### Authentification requise

⚠️ **Important :** Le hub nécessite une authentification JWT. Vous devez être connecté et fournir un token valide pour vous connecter.

---

## 📦 Installation

### Vue.js / Nuxt.js

```bash
npm install @microsoft/signalr
# ou
yarn add @microsoft/signalr
```

### React

```bash
npm install @microsoft/signalr
# ou
yarn add @microsoft/signalr
```

### Angular

```bash
npm install @microsoft/signalr
# ou
yarn add @microsoft/signalr
```

---

## ⚙️ Configuration de base

### Vue.js / Nuxt.js

```javascript
// services/signalr.service.js
import * as signalR from '@microsoft/signalr'

class SignalRService {
  constructor() {
    this.connection = null
    this.isConnected = false
  }

  // Récupérer le token JWT depuis votre store/auth
  getToken() {
    // Exemple avec Pinia/Vuex
    const authStore = useAuthStore()
    return authStore.token
  }

  async start() {
    if (this.connection && this.isConnected) {
      console.log('Déjà connecté à SignalR')
      return
    }

    try {
      const token = this.getToken()
      if (!token) {
        throw new Error('Token JWT manquant')
      }

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('https://localhost:7110/hubs/notifications', {
          accessTokenFactory: () => token,
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            // Reconnexion progressive : 0s, 2s, 10s, 30s
            if (retryContext.previousRetryCount === 0) return 0
            if (retryContext.previousRetryCount === 1) return 2000
            if (retryContext.previousRetryCount === 2) return 10000
            return 30000
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build()

      // Gestion des événements de connexion
      this.connection.onclose(() => {
        console.log('Déconnexion de SignalR')
        this.isConnected = false
      })

      this.connection.onreconnecting(() => {
        console.log('Reconnexion en cours...')
        this.isConnected = false
      })

      this.connection.onreconnected(() => {
        console.log('Reconnecté à SignalR')
        this.isConnected = true
      })

      // Démarrer la connexion
      await this.connection.start()
      this.isConnected = true
      console.log('✅ Connecté à SignalR')

      return this.connection
    } catch (error) {
      console.error('Erreur de connexion SignalR:', error)
      throw error
    }
  }

  async stop() {
    if (this.connection) {
      await this.connection.stop()
      this.isConnected = false
      console.log('Déconnecté de SignalR')
    }
  }

  // Méthodes pour appeler le serveur
  async markNotificationAsRead(notificationId) {
    if (!this.isConnected) {
      throw new Error('Non connecté à SignalR')
    }
    return await this.connection.invoke('MarkNotificationAsRead', notificationId)
  }

  async getConnectionStatus() {
    if (!this.isConnected) {
      throw new Error('Non connecté à SignalR')
    }
    return await this.connection.invoke('GetConnectionStatus')
  }

  async joinGroup(groupName) {
    if (!this.isConnected) {
      throw new Error('Non connecté à SignalR')
    }
    return await this.connection.invoke('JoinGroup', groupName)
  }

  async leaveGroup(groupName) {
    if (!this.isConnected) {
      throw new Error('Non connecté à SignalR')
    }
    return await this.connection.invoke('LeaveGroup', groupName)
  }

  // Méthodes pour écouter les événements
  onNotificationReceived(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée')
    }
    this.connection.on('ReceiveNotification', callback)
  }

  onNotificationMarkedAsRead(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée')
    }
    this.connection.on('NotificationMarkedAsRead', callback)
  }

  onConnectionStatus(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée')
    }
    this.connection.on('ConnectionStatus', callback)
  }

  onStatusChanged(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée')
    }
    this.connection.on('StatusChanged', callback)
  }

  onNewMessage(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée')
    }
    this.connection.on('NewMessage', callback)
  }

  onNewGrade(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée')
    }
    this.connection.on('NewGrade', callback)
  }
}

export default new SignalRService()
```

### Utilisation dans un composant Vue

```vue
<template>
  <div>
    <div v-if="!isConnected" class="alert alert-warning">
      Connexion SignalR en cours...
    </div>
    <div v-else class="alert alert-success">
      ✅ Connecté à SignalR
    </div>

    <div v-for="notification in notifications" :key="notification.id" class="notification">
      <h4>{{ notification.title }}</h4>
      <p>{{ notification.message }}</p>
      <button @click="markAsRead(notification.id)">Marquer comme lu</button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import signalRService from '@/services/signalr.service'

const isConnected = ref(false)
const notifications = ref([])

onMounted(async () => {
  try {
    // Démarrer la connexion
    await signalRService.start()
    isConnected.value = signalRService.isConnected

    // Écouter les notifications
    signalRService.onNotificationReceived((notification) => {
      console.log('Nouvelle notification reçue:', notification)
      notifications.value.unshift(notification)
      
      // Afficher une notification toast (exemple avec une librairie)
      // toast.success(notification.title)
    })

    // Écouter la confirmation de lecture
    signalRService.onNotificationMarkedAsRead((notificationId) => {
      console.log('Notification marquée comme lue:', notificationId)
      const index = notifications.value.findIndex(n => n.id === notificationId)
      if (index !== -1) {
        notifications.value[index].isRead = true
      }
    })

    // Écouter les changements de statut
    signalRService.onStatusChanged((statusData) => {
      console.log('Changement de statut:', statusData)
      // Mettre à jour l'interface selon le type d'entité
    })

    // Obtenir le statut de connexion
    const status = await signalRService.getConnectionStatus()
    console.log('Statut de connexion:', status)
  } catch (error) {
    console.error('Erreur lors de la connexion SignalR:', error)
  }
})

onUnmounted(async () => {
  await signalRService.stop()
})

const markAsRead = async (notificationId) => {
  try {
    await signalRService.markNotificationAsRead(notificationId)
  } catch (error) {
    console.error('Erreur lors du marquage comme lu:', error)
  }
}
</script>
```

---

## 🔐 Authentification

Le hub utilise l'authentification JWT. Le token doit être fourni lors de la connexion via `accessTokenFactory`.

### Format du token

Le token doit être au format Bearer et être valide. Il est automatiquement inclus dans les headers de la requête.

```javascript
const token = 'votre_token_jwt_ici' // Sans le préfixe "Bearer "

this.connection = new signalR.HubConnectionBuilder()
  .withUrl('https://localhost:7110/hubs/notifications', {
    accessTokenFactory: () => token
  })
  .build()
```

### Gestion du token expiré

Si le token expire, vous devez le renouveler et reconnecter :

```javascript
// Dans votre service SignalR
async refreshToken() {
  try {
    // Appeler votre API pour renouveler le token
    const response = await api.post('/api/Utilisateur/refresh-token')
    const newToken = response.data.token
    
    // Mettre à jour le token dans votre store
    const authStore = useAuthStore()
    authStore.setToken(newToken)
    
    // Reconnecter avec le nouveau token
    await this.stop()
    await this.start()
  } catch (error) {
    console.error('Erreur lors du renouvellement du token:', error)
    // Rediriger vers la page de connexion
  }
}
```

---

## 📞 Méthodes disponibles

Ces méthodes peuvent être appelées depuis le client pour interagir avec le serveur.

### `MarkNotificationAsRead(notificationId: number)`

Marque une notification comme lue.

```javascript
await signalRService.markNotificationAsRead(123)
```

**Réponse :** Le serveur envoie un événement `NotificationMarkedAsRead` avec l'ID de la notification.

### `GetConnectionStatus()`

Obtient le statut de la connexion actuelle.

```javascript
const status = await signalRService.getConnectionStatus()
// Retourne: { IsConnected, UserId, UserName, ConnectionId, Timestamp }
```

### `JoinGroup(groupName: string)`

Rejoint un groupe spécifique pour recevoir des notifications de ce groupe.

```javascript
// Rejoindre le groupe d'une société
await signalRService.joinGroup('societe_1')

// Rejoindre le groupe d'une classe
await signalRService.joinGroup('classe_5')
```

### `LeaveGroup(groupName: string)`

Quitte un groupe spécifique.

```javascript
await signalRService.leaveGroup('societe_1')
```

---

## 📨 Événements à écouter

Ces événements sont émis par le serveur et doivent être écoutés côté client.

### `ReceiveNotification`

Reçoit une nouvelle notification.

```javascript
signalRService.onNotificationReceived((notification) => {
  console.log('Notification:', notification)
  /*
  Structure de la notification:
  {
    id: number,
    title: string,
    message: string,
    type: string, // "info", "success", "warning", "error"
    isRead: boolean,
    dateCreation: string,
    expediteur: {
      id: number,
      nomComplet: string,
      photo: string | null
    } | null,
    metadata: {
      societeId: number | null,
      agentId: number | null
    }
  }
  */
})
```

### `NotificationMarkedAsRead`

Confirmation qu'une notification a été marquée comme lue.

```javascript
signalRService.onNotificationMarkedAsRead((notificationId) => {
  console.log('Notification marquée comme lue:', notificationId)
})
```

### `ConnectionStatus`

Statut de la connexion (réponse à `GetConnectionStatus()`).

```javascript
signalRService.onConnectionStatus((status) => {
  console.log('Statut:', status)
  /*
  {
    IsConnected: boolean,
    UserId: string,
    UserName: string,
    ConnectionId: string,
    Timestamp: string
  }
  */
})
```

### `StatusChanged`

Notification de changement de statut d'une entité.

```javascript
signalRService.onStatusChanged((statusData) => {
  console.log('Changement de statut:', statusData)
  /*
  {
    entityType: string, // "Client", "Facture", etc.
    entityId: number,
    newStatus: string,
    timestamp: string
  }
  */
})
```

### `NewMessage`

Notification d'un nouveau message.

```javascript
signalRService.onNewMessage((messageData) => {
  console.log('Nouveau message:', messageData)
  /*
  {
    senderId: number,
    senderName: string,
    message: string,
    timestamp: string
  }
  */
})
```

### `NewGrade`

Notification d'une nouvelle note publiée.

```javascript
signalRService.onNewGrade((gradeData) => {
  console.log('Nouvelle note:', gradeData)
  /*
  {
    courseName: string,
    grade: number | null,
    timestamp: string
  }
  */
})
```

---

## 💻 Exemples de code

### React avec Hooks

```jsx
// hooks/useSignalR.js
import { useEffect, useState, useRef } from 'react'
import * as signalR from '@microsoft/signalr'

export const useSignalR = (token) => {
  const [connection, setConnection] = useState(null)
  const [isConnected, setIsConnected] = useState(false)
  const [notifications, setNotifications] = useState([])

  useEffect(() => {
    if (!token) return

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7110/hubs/notifications', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build()

    newConnection.start()
      .then(() => {
        console.log('✅ Connecté à SignalR')
        setIsConnected(true)
      })
      .catch(err => console.error('Erreur de connexion:', err))

    // Écouter les notifications
    newConnection.on('ReceiveNotification', (notification) => {
      setNotifications(prev => [notification, ...prev])
    })

    newConnection.onclose(() => setIsConnected(false))
    newConnection.onreconnecting(() => setIsConnected(false))
    newConnection.onreconnected(() => setIsConnected(true))

    setConnection(newConnection)

    return () => {
      newConnection.stop()
    }
  }, [token])

  const markAsRead = async (notificationId) => {
    if (connection && isConnected) {
      await connection.invoke('MarkNotificationAsRead', notificationId)
    }
  }

  return { connection, isConnected, notifications, markAsRead }
}

// Utilisation dans un composant
function NotificationComponent() {
  const { token } = useAuth() // Votre hook d'authentification
  const { isConnected, notifications, markAsRead } = useSignalR(token)

  return (
    <div>
      <p>Statut: {isConnected ? '✅ Connecté' : '❌ Déconnecté'}</p>
      {notifications.map(notif => (
        <div key={notif.id}>
          <h4>{notif.title}</h4>
          <p>{notif.message}</p>
          <button onClick={() => markAsRead(notif.id)}>
            Marquer comme lu
          </button>
        </div>
      ))}
    </div>
  )
}
```

### Angular avec Service

```typescript
// services/signalr.service.ts
import { Injectable } from '@angular/core'
import * as signalR from '@microsoft/signalr'
import { BehaviorSubject, Observable } from 'rxjs'
import { AuthService } from './auth.service'

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private connection: signalR.HubConnection | null = null
  private isConnected$ = new BehaviorSubject<boolean>(false)
  private notifications$ = new BehaviorSubject<any[]>([])

  constructor(private authService: AuthService) {}

  async start(): Promise<void> {
    if (this.connection && this.isConnected$.value) {
      return
    }

    const token = this.authService.getToken()
    if (!token) {
      throw new Error('Token manquant')
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7110/hubs/notifications', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build()

    // Écouter les notifications
    this.connection.on('ReceiveNotification', (notification) => {
      const current = this.notifications$.value
      this.notifications$.next([notification, ...current])
    })

    this.connection.onclose(() => this.isConnected$.next(false))
    this.connection.onreconnecting(() => this.isConnected$.next(false))
    this.connection.onreconnected(() => this.isConnected$.next(true))

    await this.connection.start()
    this.isConnected$.next(true)
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop()
      this.isConnected$.next(false)
    }
  }

  async markNotificationAsRead(notificationId: number): Promise<void> {
    if (this.connection && this.isConnected$.value) {
      await this.connection.invoke('MarkNotificationAsRead', notificationId)
    }
  }

  getIsConnected(): Observable<boolean> {
    return this.isConnected$.asObservable()
  }

  getNotifications(): Observable<any[]> {
    return this.notifications$.asObservable()
  }
}

// Utilisation dans un composant
@Component({
  selector: 'app-notifications',
  template: `
    <div>
      <p>Statut: {{ isConnected ? '✅ Connecté' : '❌ Déconnecté' }}</p>
      <div *ngFor="let notif of notifications">
        <h4>{{ notif.title }}</h4>
        <p>{{ notif.message }}</p>
        <button (click)="markAsRead(notif.id)">Marquer comme lu</button>
      </div>
    </div>
  `
})
export class NotificationsComponent implements OnInit, OnDestroy {
  isConnected = false
  notifications: any[] = []

  constructor(private signalRService: SignalRService) {}

  async ngOnInit() {
    await this.signalRService.start()
    
    this.signalRService.getIsConnected().subscribe(connected => {
      this.isConnected = connected
    })

    this.signalRService.getNotifications().subscribe(notifications => {
      this.notifications = notifications
    })
  }

  async ngOnDestroy() {
    await this.signalRService.stop()
  }

  async markAsRead(notificationId: number) {
    await this.signalRService.markNotificationAsRead(notificationId)
  }
}
```

---

## ⚠️ Gestion des erreurs

### Erreurs de connexion

```javascript
this.connection.start()
  .then(() => {
    console.log('✅ Connecté')
  })
  .catch(error => {
    console.error('Erreur de connexion:', error)
    
    if (error.statusCode === 401) {
      // Token invalide ou expiré
      console.error('Token invalide, reconnexion nécessaire')
      // Renouveler le token et reconnecter
    } else if (error.statusCode === 404) {
      // Hub introuvable
      console.error('Hub SignalR introuvable')
    } else {
      // Autre erreur
      console.error('Erreur inconnue:', error)
    }
  })
```

### Gestion de la reconnexion automatique

SignalR gère automatiquement la reconnexion, mais vous pouvez personnaliser le comportement :

```javascript
.withAutomaticReconnect({
  nextRetryDelayInMilliseconds: retryContext => {
    // Stratégie de reconnexion personnalisée
    if (retryContext.previousRetryCount === 0) return 0
    if (retryContext.previousRetryCount === 1) return 2000
    if (retryContext.previousRetryCount === 2) return 10000
    return 30000 // Max 30 secondes entre les tentatives
  }
})
```

### Gestion des erreurs lors de l'invocation

```javascript
try {
  await this.connection.invoke('MarkNotificationAsRead', notificationId)
} catch (error) {
  console.error('Erreur lors de l\'invocation:', error)
  
  if (error.message.includes('not connected')) {
    // Reconnecter
    await this.start()
  }
}
```

---

## 🧪 Tests

### Test de connexion

```javascript
// Tester la connexion
async function testConnection() {
  try {
    await signalRService.start()
    const status = await signalRService.getConnectionStatus()
    console.log('✅ Connexion réussie:', status)
  } catch (error) {
    console.error('❌ Erreur de connexion:', error)
  }
}
```

### Test de réception de notifications

```javascript
// Écouter toutes les notifications pour le débogage
signalRService.onNotificationReceived((notification) => {
  console.log('📨 Notification reçue:', notification)
  console.log('Type:', notification.type)
  console.log('Titre:', notification.title)
  console.log('Message:', notification.message)
})
```

### Test avec Postman ou un client HTTP

Vous ne pouvez pas tester SignalR directement avec Postman, mais vous pouvez utiliser des outils comme :

- **SignalR Client** (extension Chrome)
- **SignalR Test Client** (outil en ligne)
- **wscat** (pour WebSocket)

---

## ✅ Bonnes pratiques

### 1. Gérer le cycle de vie de la connexion

```javascript
// Démarrer lors de la connexion de l'utilisateur
onUserLogin() {
  signalRService.start()
}

// Arrêter lors de la déconnexion
onUserLogout() {
  signalRService.stop()
}
```

### 2. Vérifier l'état de connexion avant d'invoquer

```javascript
if (!signalRService.isConnected) {
  console.warn('Non connecté à SignalR')
  await signalRService.start()
}
await signalRService.markNotificationAsRead(notificationId)
```

### 3. Nettoyer les listeners lors du démontage

```javascript
onUnmounted(() => {
  // Supprimer tous les listeners
  signalRService.connection.off('ReceiveNotification')
  signalRService.connection.off('NotificationMarkedAsRead')
  // ...
  signalRService.stop()
})
```

### 4. Gérer les notifications en double

```javascript
const notificationIds = new Set()

signalRService.onNotificationReceived((notification) => {
  // Éviter les doublons
  if (!notificationIds.has(notification.id)) {
    notificationIds.add(notification.id)
    notifications.value.unshift(notification)
  }
})
```

### 5. Limiter le nombre de notifications affichées

```javascript
const MAX_NOTIFICATIONS = 50

signalRService.onNotificationReceived((notification) => {
  notifications.value.unshift(notification)
  if (notifications.value.length > MAX_NOTIFICATIONS) {
    notifications.value = notifications.value.slice(0, MAX_NOTIFICATIONS)
  }
})
```

### 6. Afficher un indicateur de connexion

```vue
<template>
  <div class="connection-indicator" :class="{ connected: isConnected }">
    <span v-if="isConnected">🟢 Connecté</span>
    <span v-else>🔴 Déconnecté</span>
  </div>
</template>
```

---

## 📚 Ressources supplémentaires

- [Documentation officielle SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [SignalR JavaScript Client](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client)
- [Guide de migration SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/version-differences)

---

## 🆘 Support

Pour toute question ou problème :

1. Vérifiez les logs de la console du navigateur
2. Vérifiez les logs du serveur
3. Vérifiez que votre token JWT est valide
4. Vérifiez que l'URL du hub est correcte
5. Contactez l'équipe backend pour plus d'aide

---

**Dernière mise à jour :** Décembre 2025  
**Version de l'API :** 2.0

