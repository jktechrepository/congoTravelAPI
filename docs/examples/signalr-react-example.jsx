/**
 * Exemple complet d'implémentation SignalR pour React
 * 
 * Installation:
 * npm install @microsoft/signalr
 * 
 * Utilisation:
 * 1. Créez un hook personnalisé useSignalR
 * 2. Utilisez-le dans vos composants comme montré ci-dessous
 */

import { useEffect, useState, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'

/**
 * Hook personnalisé pour gérer SignalR
 * @param {string} token - Token JWT pour l'authentification
 * @param {string} hubUrl - URL du hub SignalR (optionnel)
 */
export const useSignalR = (token, hubUrl = 'https://localhost:7110/hubs/notifications') => {
  const [connection, setConnection] = useState(null)
  const [isConnected, setIsConnected] = useState(false)
  const [notifications, setNotifications] = useState([])
  const connectionRef = useRef(null)

  // Démarrer la connexion
  const start = useCallback(async () => {
    if (!token) {
      console.warn('⚠️ Token manquant, impossible de se connecter à SignalR')
      return
    }

    if (connectionRef.current && isConnected) {
      console.log('⚠️ Déjà connecté à SignalR')
      return
    }

    try {
      console.log('🔄 Démarrage de la connexion SignalR...')

      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => token,
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            if (retryContext.previousRetryCount === 0) return 0
            if (retryContext.previousRetryCount === 1) return 2000
            if (retryContext.previousRetryCount === 2) return 10000
            return 30000
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build()

      // Gestion des événements de connexion
      newConnection.onclose((error) => {
        console.log('🔴 Déconnexion de SignalR', error ? `Erreur: ${error.message}` : '')
        setIsConnected(false)
      })

      newConnection.onreconnecting((error) => {
        console.log('🔄 Reconnexion en cours...', error ? `Erreur: ${error.message}` : '')
        setIsConnected(false)
      })

      newConnection.onreconnected((connectionId) => {
        console.log('✅ Reconnecté à SignalR. ConnectionId:', connectionId)
        setIsConnected(true)
      })

      // Écouter les notifications
      newConnection.on('ReceiveNotification', (notification) => {
        console.log('📨 Notification reçue:', notification)
        setNotifications(prev => [notification, ...prev])
      })

      newConnection.on('NotificationMarkedAsRead', (notificationId) => {
        console.log('✅ Notification marquée comme lue:', notificationId)
        setNotifications(prev =>
          prev.map(notif =>
            notif.id === notificationId ? { ...notif, isRead: true } : notif
          )
        )
      })

      newConnection.on('ConnectionStatus', (status) => {
        console.log('📊 Statut de connexion:', status)
      })

      newConnection.on('StatusChanged', (statusData) => {
        console.log('🔄 Changement de statut:', statusData)
      })

      newConnection.on('NewMessage', (messageData) => {
        console.log('💬 Nouveau message:', messageData)
      })

      newConnection.on('NewGrade', (gradeData) => {
        console.log('📝 Nouvelle note:', gradeData)
      })

      // Démarrer la connexion
      await newConnection.start()
      setIsConnected(true)
      setConnection(newConnection)
      connectionRef.current = newConnection
      console.log('✅ Connecté à SignalR avec succès!')
      console.log('📡 ConnectionId:', newConnection.connectionId)

    } catch (error) {
      console.error('❌ Erreur de connexion SignalR:', error)
      setIsConnected(false)
      
      if (error.statusCode === 401) {
        console.error('Token invalide ou expiré. Veuillez vous reconnecter.')
      } else if (error.statusCode === 404) {
        console.error('Hub SignalR introuvable. Vérifiez l\'URL du serveur.')
      }
    }
  }, [token, hubUrl, isConnected])

  // Arrêter la connexion
  const stop = useCallback(async () => {
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop()
        setIsConnected(false)
        setConnection(null)
        connectionRef.current = null
        console.log('🛑 Déconnecté de SignalR')
      } catch (error) {
        console.error('Erreur lors de la déconnexion:', error)
      }
    }
  }, [])

  // Marquer une notification comme lue
  const markNotificationAsRead = useCallback(async (notificationId) => {
    if (connectionRef.current && isConnected) {
      try {
        await connectionRef.current.invoke('MarkNotificationAsRead', notificationId)
        console.log(`✅ Notification ${notificationId} marquée comme lue`)
      } catch (error) {
        console.error('Erreur lors du marquage comme lu:', error)
        throw error
      }
    } else {
      throw new Error('Non connecté à SignalR')
    }
  }, [isConnected])

  // Obtenir le statut de connexion
  const getConnectionStatus = useCallback(async () => {
    if (connectionRef.current && isConnected) {
      try {
        return await connectionRef.current.invoke('GetConnectionStatus')
      } catch (error) {
        console.error('Erreur lors de la récupération du statut:', error)
        throw error
      }
    } else {
      throw new Error('Non connecté à SignalR')
    }
  }, [isConnected])

  // Rejoindre un groupe
  const joinGroup = useCallback(async (groupName) => {
    if (connectionRef.current && isConnected) {
      try {
        await connectionRef.current.invoke('JoinGroup', groupName)
        console.log(`✅ Rejoint le groupe: ${groupName}`)
      } catch (error) {
        console.error(`Erreur lors de la jointure au groupe ${groupName}:`, error)
        throw error
      }
    } else {
      throw new Error('Non connecté à SignalR')
    }
  }, [isConnected])

  // Quitter un groupe
  const leaveGroup = useCallback(async (groupName) => {
    if (connectionRef.current && isConnected) {
      try {
        await connectionRef.current.invoke('LeaveGroup', groupName)
        console.log(`✅ Quitté le groupe: ${groupName}`)
      } catch (error) {
        console.error(`Erreur lors de la sortie du groupe ${groupName}:`, error)
        throw error
      }
    } else {
      throw new Error('Non connecté à SignalR')
    }
  }, [isConnected])

  // Effet pour démarrer/arrêter la connexion
  useEffect(() => {
    if (token) {
      start()
    }

    return () => {
      stop()
    }
  }, [token, start, stop])

  return {
    connection,
    isConnected,
    notifications,
    start,
    stop,
    markNotificationAsRead,
    getConnectionStatus,
    joinGroup,
    leaveGroup
  }
}

/**
 * Exemple d'utilisation dans un composant React
 */
function NotificationComponent() {
  // Récupérer le token depuis votre contexte/auth
  const { token } = useAuth() // Adaptez selon votre système d'authentification
  
  const {
    isConnected,
    notifications,
    markNotificationAsRead,
    joinGroup,
    leaveGroup
  } = useSignalR(token)

  // Rejoindre un groupe lors du montage
  useEffect(() => {
    if (isConnected) {
      // Exemple: rejoindre le groupe de la société
      joinGroup('societe_1').catch(console.error)
    }

    return () => {
      if (isConnected) {
        leaveGroup('societe_1').catch(console.error)
      }
    }
  }, [isConnected, joinGroup, leaveGroup])

  return (
    <div>
      <div className="connection-status">
        <span className={isConnected ? 'connected' : 'disconnected'}>
          {isConnected ? '🟢 Connecté' : '🔴 Déconnecté'}
        </span>
      </div>

      <div className="notifications">
        <h2>Notifications ({notifications.length})</h2>
        {notifications.length === 0 ? (
          <p>Aucune notification</p>
        ) : (
          notifications.map(notification => (
            <div
              key={notification.id}
              className={`notification ${notification.isRead ? 'read' : 'unread'}`}
            >
              <h4>{notification.title}</h4>
              <p>{notification.message}</p>
              <div className="notification-meta">
                <span className="type">{notification.type}</span>
                <span className="date">
                  {new Date(notification.dateCreation).toLocaleString()}
                </span>
                {notification.expediteur && (
                  <span className="sender">
                    De: {notification.expediteur.nomComplet}
                  </span>
                )}
              </div>
              {!notification.isRead && (
                <button onClick={() => markNotificationAsRead(notification.id)}>
                  Marquer comme lu
                </button>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  )
}

export default NotificationComponent

