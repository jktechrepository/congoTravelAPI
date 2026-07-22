using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour envoyer des notifications push via Firebase Cloud Messaging
    /// </summary>
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly IUserDeviceRepository _userDeviceRepository;
        private readonly ILogger<FirebaseNotificationService> _logger;
        private static bool _firebaseInitialized;
        private static readonly object Lock = new();

        public FirebaseNotificationService(
            IUserDeviceRepository userDeviceRepository,
            ILogger<FirebaseNotificationService> logger)
        {
            _userDeviceRepository = userDeviceRepository;
            _logger = logger;
        }

        /// <summary>
        /// Initialise Firebase Admin SDK avec les credentials (appelé une seule fois au démarrage)
        /// </summary>
        public static void InitializeFirebase(string credentialsPath)
        {
            if (_firebaseInitialized)
                return;

            lock (Lock)
            {
                if (_firebaseInitialized || FirebaseApp.DefaultInstance != null)
                    return;

                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialsPath)
                });

                _firebaseInitialized = true;
            }
        }

        public async Task<bool> EnvoyerNotificationAUtilisateurAsync(
            int idUtilisateur,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    _logger.LogError(
                        "Firebase n'est pas initialisé. Impossible d'envoyer la notification à l'utilisateur {IdUtilisateur}",
                        idUtilisateur);
                    return false;
                }

                var tokens = await _userDeviceRepository.GetActiveTokensByUtilisateurIdAsync(idUtilisateur);

                if (tokens == null || !tokens.Any())
                {
                    _logger.LogWarning("Aucun token FCM actif pour l'utilisateur {IdUtilisateur}", idUtilisateur);
                    return false;
                }

                var tokenList = tokens.ToList();
                var response = await SendMulticastAsync(tokenList, titre, corps, donnees);

                _logger.LogInformation(
                    "Notification utilisateur {IdUtilisateur}: {Success}/{Total} succès",
                    idUtilisateur, response.SuccessCount, tokenList.Count);

                if (response.FailureCount > 0)
                    await DesactiverTokensInvalidesAsync(response, tokenList);

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi notification utilisateur {IdUtilisateur}", idUtilisateur);
                return false;
            }
        }

        public async Task<int> EnvoyerNotificationParRoleAsync(
            int idRole,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                var tokens = (await _userDeviceRepository.GetActiveTokensByRoleAsync(idRole)).ToList();
                if (tokens.Count == 0)
                {
                    _logger.LogWarning("Aucun token FCM actif pour le rôle {IdRole}", idRole);
                    return 0;
                }

                var response = await SendMulticastAsync(tokens, titre, corps, donnees);
                await DesactiverTokensInvalidesAsync(response, tokens);
                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi notification rôle {IdRole}", idRole);
                return 0;
            }
        }

        public async Task<int> EnvoyerNotificationParSocieteAsync(
            int idSociete,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                var tokens = (await _userDeviceRepository.GetActiveTokensBySocieteAsync(idSociete)).ToList();
                if (tokens.Count == 0)
                {
                    _logger.LogWarning("Aucun token FCM actif pour la société {IdSociete}", idSociete);
                    return 0;
                }

                var response = await SendMulticastAsync(tokens, titre, corps, donnees);
                await DesactiverTokensInvalidesAsync(response, tokens);
                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi notification société {IdSociete}", idSociete);
                return 0;
            }
        }

        public async Task<int> EnvoyerNotificationParClasseAsync(
            int idClasse,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                var tokens = (await _userDeviceRepository.GetActiveTokensByClasseAsync(idClasse)).ToList();
                if (tokens.Count == 0)
                {
                    _logger.LogWarning("Aucun token FCM actif pour la classe {IdClasse}", idClasse);
                    return 0;
                }

                var response = await SendMulticastAsync(tokens, titre, corps, donnees);
                await DesactiverTokensInvalidesAsync(response, tokens);
                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi notification classe {IdClasse}", idClasse);
                return 0;
            }
        }

        public async Task<bool> EnvoyerNotificationATokenAsync(
            string fcmToken,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                var message = new FirebaseAdmin.Messaging.Message
                {
                    Token = fcmToken,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = titre,
                        Body = corps
                    },
                    Data = donnees ?? new Dictionary<string, string>()
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return !string.IsNullOrEmpty(response);
            }
            catch (FirebaseMessagingException fmEx)
            {
                _logger.LogError(fmEx, "Erreur Firebase envoi token {FcmToken}", fcmToken);

                if (fmEx.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                    fmEx.MessagingErrorCode == MessagingErrorCode.Unregistered)
                {
                    await _userDeviceRepository.DeleteByFcmTokenAsync(fcmToken);
                    _logger.LogInformation("Token FCM invalide supprimé: {FcmToken}", fcmToken);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi notification token {FcmToken}", fcmToken);
                return false;
            }
        }

        public async Task<bool> EnvoyerNotificationAvanceeAsync(
            string fcmToken,
            string titre,
            string corps,
            string? imageUrl = null,
            string? clickAction = null,
            Dictionary<string, string>? donnees = null,
            string? sound = null,
            string? badge = null)
        {
            try
            {
                var message = new FirebaseAdmin.Messaging.Message
                {
                    Token = fcmToken,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = titre,
                        Body = corps,
                        ImageUrl = imageUrl
                    },
                    Data = donnees ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = sound ?? "default",
                            ClickAction = clickAction
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = sound ?? "default",
                            Badge = string.IsNullOrEmpty(badge) ? null : int.Parse(badge)
                        }
                    },
                    Webpush = new WebpushConfig
                    {
                        Notification = new WebpushNotification
                        {
                            Title = titre,
                            Body = corps,
                            Icon = imageUrl
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return !string.IsNullOrEmpty(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi notification avancée");
                return false;
            }
        }

        private static async Task<BatchResponse> SendMulticastAsync(
            List<string> tokens,
            string titre,
            string corps,
            Dictionary<string, string>? donnees)
        {
            var message = new MulticastMessage
            {
                Tokens = tokens,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = titre,
                    Body = corps
                },
                Data = donnees ?? new Dictionary<string, string>()
            };

            return await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
        }

        private async Task DesactiverTokensInvalidesAsync(BatchResponse response, List<string> tokens)
        {
            for (var i = 0; i < response.Responses.Count; i++)
            {
                var sendResponse = response.Responses[i];
                if (sendResponse.IsSuccess)
                    continue;

                if (sendResponse.Exception is FirebaseMessagingException fmEx &&
                    (fmEx.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                     fmEx.MessagingErrorCode == MessagingErrorCode.Unregistered))
                {
                    await _userDeviceRepository.DeleteByFcmTokenAsync(tokens[i]);
                    _logger.LogInformation("Token FCM invalide supprimé: {Token}", tokens[i]);
                }
            }
        }
    }
}
