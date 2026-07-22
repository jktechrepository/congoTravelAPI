/**
 * Exemple complet d'implémentation SignalR pour Vue.js
 * 
 * Installation:
 * npm install @microsoft/signalr
 * 
 * Utilisation:
 * 1. Copiez ce fichier dans votre projet (ex: src/services/signalr.service.js)
 * 2. Importez-le dans vos composants
 * 3. Utilisez-le comme montré dans les exemples ci-dessous
 */

import * as signalR from '@microsoft/signalr'

class SignalRService {
  constructor() {
    this.connection = null
    this.isConnected = false
    this.reconnectAttempts = 0
    this.maxReconnectAttempts = 5
  }

  /**
   * Récupère le token JWT depuis votre store d'authentification
   * Adaptez cette méthode selon votre système d'authentification
   */
  getToken() {
    // Exemple avec Pinia
    // const authStore = useAuthStore()
    // return authStore.token

    // Exemple avec localStorage
    return localStorage.getItem('auth_token')

    // Exemple avec sessionStorage
    // return sessionStorage.getItem('auth_token')
  }

  /**
   * Démarre la connexion SignalR
   */
  async start() {
    if (this.connection && this.isConnected) {
      console.log('⚠️ Déjà connecté à SignalR')
      return this.connection
    }

    try {
      const token = this.getToken()
      if (!token) {
        throw new Error('❌ Token JWT manquant. Veuillez vous connecter.')
      }

      console.log('🔄 Démarrage de la connexion SignalR...')

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('https://localhost:7110/hubs/notifications', {
          accessTokenFactory: () => token,
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            // Stratégie de reconnexion progressive
            if (retryContext.previousRetryCount === 0) return 0
            if (retryContext.previousRetryCount === 1) return 2000
            if (retryContext.previousRetryCount === 2) return 10000
            return 30000 // Max 30 secondes
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build()

      // ============================================
      // GESTION DES ÉVÉNEMENTS DE CONNEXION
      // ============================================

      this.connection.onclose((error) => {
        console.log('🔴 Déconnexion de SignalR', error ? `Erreur: ${error.message}` : '')
        this.isConnected = false
        this.reconnectAttempts = 0
      })

      this.connection.onreconnecting((error) => {
        console.log('🔄 Reconnexion en cours...', error ? `Erreur: ${error.message}` : '')
        this.isConnected = false
        this.reconnectAttempts++
      })

      this.connection.onreconnected((connectionId) => {
        console.log('✅ Reconnecté à SignalR. ConnectionId:', connectionId)
        this.isConnected = true
        this.reconnectAttempts = 0
      })

      // ============================================
      // DÉMARRAGE DE LA CONNEXION
      // ============================================

      await this.connection.start()
      this.isConnected = true
      this.reconnectAttempts = 0
      console.log('✅ Connecté à SignalR avec succès!')
      console.log('📡 ConnectionId:', this.connection.connectionId)

      return this.connection
    } catch (error) {
      console.error('❌ Erreur de connexion SignalR:', error)
      this.isConnected = false
      
      if (error.statusCode === 401) {
        throw new Error('Token invalide ou expiré. Veuillez vous reconnecter.')
      } else if (error.statusCode === 404) {
        throw new Error('Hub SignalR introuvable. Vérifiez l\'URL du serveur.')
      } else {
        throw error
      }
    }
  }

  /**
   * Arrête la connexion SignalR
   */
  async stop() {
    if (this.connection) {
      try {
        await this.connection.stop()
        this.isConnected = false
        console.log('🛑 Déconnecté de SignalR')
      } catch (error) {
        console.error('Erreur lors de la déconnexion:', error)
      } finally {
        this.connection = null
      }
    }
  }

  // ============================================
  // MÉTHODES POUR APPELER LE SERVEUR
  // ============================================

  /**
   * Marque une notification comme lue
   * @param {number} notificationId - ID de la notification
   */
  async markNotificationAsRead(notificationId) {
    if (!this.isConnected) {
      throw new Error('Non connecté à SignalR')
    }

    try {
      await this.connection.invoke('MarkNotificationAsRead', notificationId)
      console.log(`✅ Notification ${notificationId} marquée comme lue`)
    } catch (error) {
      console.error('Erreur lors du marquage comme lu:', error)
      throw error
    }
  }

  /**
   * Obtient le statut de la connexion
   */
  async getConnectionStatus() {
    if (!this.isConnected) {
      throw new Error('Non connecté à SignalR')
    }

    try {
      return await this.connection.invoke('GetConnectionStatus')
    } catch (error) {
      console.error('Erreur lors de la récupération du statut:', error)
      throw error
    }
  }

  /**
   * Rejoint un groupe spécifique
   * @param {string} groupName - Nom du groupe (ex: "societe_1", "classe_5")
   */
  async joinGroup(groupName) {
    if (!this.isConnected) {
      throw new Error('Non connecté à SignalR')
    }

    try {
      await this.connection.invoke('JoinGroup', groupName)
      console.log(`✅ Rejoint le groupe: ${groupName}`)
    } catch (error) {
      console.error(`Erreur lors de la jointure au groupe ${groupName}:`, error)
      throw error
    }
  }

  /**
   * Quitte un groupe spécifique
   * @param {string} groupName - Nom du groupe
   */
  async leaveGroup(groupName) {
    if (!this.isConnected) {
      throw new Error('Non connecté à SignalR')
    }

    try {
      await this.connection.invoke('LeaveGroup', groupName)
      console.log(`✅ Quitté le groupe: ${groupName}`)
    } catch (error) {
      console.error(`Erreur lors de la sortie du groupe ${groupName}:`, error)
      throw error
    }
  }

  // ============================================
  // MÉTHODES POUR ÉCOUTER LES ÉVÉNEMENTS
  // ============================================

  /**
   * Écoute les nouvelles notifications
   * @param {Function} callback - Fonction appelée lors de la réception d'une notification
   */
  onNotificationReceived(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée. Appelez start() d\'abord.')
    }

    this.connection.on('ReceiveNotification', (notification) => {
      console.log('📨 Notification reçue:', notification)
      callback(notification)
    })
  }

  /**
   * Écoute la confirmation de lecture d'une notification
   * @param {Function} callback - Fonction appelée avec l'ID de la notification
   */
  onNotificationMarkedAsRead(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée.')
    }

    this.connection.on('NotificationMarkedAsRead', (notificationId) => {
      console.log('✅ Notification marquée comme lue:', notificationId)
      callback(notificationId)
    })
  }

  /**
   * Écoute le statut de connexion
   * @param {Function} callback - Fonction appelée avec le statut
   */
  onConnectionStatus(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée.')
    }

    this.connection.on('ConnectionStatus', (status) => {
      console.log('📊 Statut de connexion:', status)
      callback(status)
    })
  }

  /**
   * Écoute les changements de statut
   * @param {Function} callback - Fonction appelée avec les données de statut
   */
  onStatusChanged(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée.')
    }

    this.connection.on('StatusChanged', (statusData) => {
      console.log('🔄 Changement de statut:', statusData)
      callback(statusData)
    })
  }

  /**
   * Écoute les nouveaux messages
   * @param {Function} callback - Fonction appelée avec les données du message
   */
  onNewMessage(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée.')
    }

    this.connection.on('NewMessage', (messageData) => {
      console.log('💬 Nouveau message:', messageData)
      callback(messageData)
    })
  }

  /**
   * Écoute les nouvelles notes
   * @param {Function} callback - Fonction appelée avec les données de la note
   */
  onNewGrade(callback) {
    if (!this.connection) {
      throw new Error('Connexion SignalR non initialisée.')
    }

    this.connection.on('NewGrade', (gradeData) => {
      console.log('📝 Nouvelle note:', gradeData)
      callback(gradeData)
    })
  }

  /**
   * Supprime tous les listeners d'un événement
   * @param {string} eventName - Nom de l'événement
   */
  off(eventName) {
    if (this.connection) {
      this.connection.off(eventName)
      console.log(`🗑️ Listener supprimé pour: ${eventName}`)
    }
  }

  /**
   * Supprime tous les listeners
   */
  removeAllListeners() {
    if (this.connection) {
      this.connection.off('ReceiveNotification')
      this.connection.off('NotificationMarkedAsRead')
      this.connection.off('ConnectionStatus')
      this.connection.off('StatusChanged')
      this.connection.off('NewMessage')
      this.connection.off('NewGrade')
      console.log('🗑️ Tous les listeners ont été supprimés')
    }
  }
}

// Exporte une instance singleton
export default new SignalRService()

