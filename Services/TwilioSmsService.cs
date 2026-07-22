using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CongoTravel.Services
{
    /// <summary>
    /// 📱 Service d'envoi de SMS via Twilio
    /// Implémente toutes les fonctionnalités de notification SMS
    /// </summary>
    public class TwilioSmsService : ISmsNotificationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TwilioSmsService> _logger;
        
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _twilioPhoneNumber;
        private readonly string _twilioSenderId;
        private readonly double _prixParSms;
        private readonly bool _smsEnabled;

        public TwilioSmsService(
            CongoTravelDbContext context,
            IConfiguration configuration,
            ILogger<TwilioSmsService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

            // ✅ Récupérer la configuration Twilio depuis appsettings.json
            _accountSid = _configuration["Twilio:AccountSid"] ?? "";
            _authToken = _configuration["Twilio:AuthToken"] ?? "";
            _twilioPhoneNumber = _configuration["Twilio:PhoneNumber"] ?? "";
            _twilioSenderId = _configuration["Twilio:SenderId"] ?? "";
            _prixParSms = double.Parse(_configuration["Twilio:PrixParSms"] ?? "0.0467", CultureInfo.InvariantCulture);
            _smsEnabled = bool.Parse(_configuration["Twilio:Enabled"] ?? "true");

            // ✅ Initialiser Twilio Client si activé
            if (_smsEnabled && !string.IsNullOrEmpty(_accountSid) && !string.IsNullOrEmpty(_authToken))
            {
                TwilioClient.Init(_accountSid, _authToken);
                _logger.LogInformation("✅ Twilio SMS Service initialisé avec succès");
            }
            else
            {
                _logger.LogWarning("⚠️  Twilio SMS Service DÉSACTIVÉ ou mal configuré");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 📤 ENVOI DE SMS
        // ═══════════════════════════════════════════════════════════════

        public async Task<SmsLog?> EnvoyerSmsAsync(string numeroTelephone, string message, string? typeNotification = null)
        {
            try
            {
                // ⚠️ Vérifier si SMS est activé
                if (!_smsEnabled)
                {
                    _logger.LogWarning("SMS désactivé dans la configuration");
                    return null;
                }

                // ✅ IMPORTANT : Normaliser d'abord, puis valider
                // En production, beaucoup de numéros sont saisis sous forme "081..." ou "+243 81 ..."
                // Si on valide avant normalisation, le SMS est silencieusement ignoré.
                string numeroFormate = FormaterNumeroTelephone(numeroTelephone);

                // ✅ Valider le numéro normalisé (E.164)
                if (!ValiderNumeroTelephone(numeroFormate))
                {
                    _logger.LogWarning(
                        "⚠️ Numéro de téléphone invalide : original='{Original}' → formaté='{Formate}'",
                        numeroTelephone ?? "NULL",
                        numeroFormate ?? "NULL"
                    );
                    return null;
                }

                // ✅ Créer le log SMS (statut initial : PENDING)
                var smsLog = new SmsLog
                {
                    NumeroDestinataire = numeroFormate,
                    Message = message,
                    TypeNotification = typeNotification,
                    Statut = "PENDING",
                    CoutUsd = _prixParSms,
                    CoutFc = _prixParSms * 2500, // Taux approximatif USD → FC
                    NombreSegments = CalculerNombreSegments(message),
                    NumeroExpediteur = _twilioSenderId,
                    DateEnvoi = DateTime.Now
                };

                // ✅ Envoyer le SMS via Twilio
                try
                {
                    // ✅ Supporter 2 modes Twilio :
                    // - MessagingServiceSid (commence par "MG...")
                    // - From (numéro E.164) via PhoneNumber
                    if (string.IsNullOrWhiteSpace(_twilioSenderId) && string.IsNullOrWhiteSpace(_twilioPhoneNumber))
                    {
                        smsLog.Statut = "FAILED";
                        smsLog.MessageErreur = "Twilio:SenderId et Twilio:PhoneNumber non configurés";
                        smsLog.DateEchec = DateTime.Now;
                        _logger.LogError("❌ Configuration Twilio incomplète pour envoi SMS (SenderId/PhoneNumber)");
                        
                        // ✅ FIX: Vérifier que le contexte n'est pas disposé
                        try
                        {
                            if (_context.Database.CanConnect())
                            {
                                _context.SmsLogs.Add(smsLog);
                                await _context.SaveChangesAsync();
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            _logger.LogWarning("⚠️ Contexte DB disposé, log SMS d'erreur non sauvegardé");
                        }
                        return smsLog;
                    }

                    // Twilio SDK: CreateMessageOptions (pas un type imbriqué dans MessageResource)
                    var options = new CreateMessageOptions(
                        to: new PhoneNumber(numeroFormate)
                    )
                    {
                        Body = message
                    };

                    // Si SenderId est un MessagingServiceSid (MG...), l'utiliser correctement.
                    if (!string.IsNullOrWhiteSpace(_twilioSenderId) &&
                        _twilioSenderId.StartsWith("MG", StringComparison.OrdinalIgnoreCase))
                    {
                        options.MessagingServiceSid = _twilioSenderId;
                    }
                    else if (!string.IsNullOrWhiteSpace(_twilioSenderId))
                    {
                        // Sinon, considérer SenderId comme un numéro expéditeur E.164
                        options.From = new PhoneNumber(_twilioSenderId);
                    }
                    else
                    {
                        // Fallback : utiliser Twilio:PhoneNumber si SenderId vide
                        options.From = new PhoneNumber(_twilioPhoneNumber);
                    }

                    var messageResource = await MessageResource.CreateAsync(options);

                    // ✅ Mettre à jour avec les infos Twilio
                    smsLog.MessageSid = messageResource.Sid;
                    smsLog.Statut = messageResource.Status.ToString().ToUpper();
                    smsLog.NombreSegments = int.Parse(messageResource.NumSegments ?? "1");

                    _logger.LogInformation($"✅ SMS envoyé avec succès : {messageResource.Sid} → {numeroFormate}");
                }
                catch (Exception ex)
                {
                    // ❌ Échec d'envoi
                    smsLog.Statut = "FAILED";
                    smsLog.MessageErreur = ex.Message;
                    smsLog.DateEchec = DateTime.Now;

                    _logger.LogError(ex, $"❌ Échec d'envoi SMS vers {numeroFormate}: {ex.Message}");
                }

                // ✅ Sauvegarder dans la base de données
                try
                {
                    // ✅ FIX: Vérifier que le contexte n'est pas disposé avant de sauvegarder
                    if (_context.Database.CanConnect())
                    {
                        _context.SmsLogs.Add(smsLog);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Contexte DB disposé, log SMS non sauvegardé (mais SMS envoyé)");
                    }
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogWarning($"⚠️ Contexte DB disposé, log SMS non sauvegardé pour {numeroTelephone} (mais SMS envoyé)");
                    // Détacher l'entité du contexte pour éviter les problèmes suivants
                    _context.Entry(smsLog).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                catch (Exception saveEx)
                {
                    _logger.LogWarning(saveEx, $"⚠️ Erreur lors de la sauvegarde du log SMS, mais SMS envoyé avec succès");
                    // Détacher l'entité du contexte pour éviter les problèmes suivants
                    _context.Entry(smsLog).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }

                return smsLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur globale lors de l'envoi SMS : {ex.Message}");
                return null;
            }
        }

        public async Task<SmsLog?> EnvoyerSmsAUtilisateurAsync(int idUtilisateur, string message, string? typeNotification = null)
        {
            try
            {
                // ✅ Récupérer l'utilisateur
                var utilisateur = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == idUtilisateur);

                if (utilisateur == null)
                {
                    _logger.LogWarning($"Utilisateur {idUtilisateur} introuvable");
                    return null;
                }

                // ✅ Vérifier qu'il a un numéro de téléphone
                if (string.IsNullOrWhiteSpace(utilisateur.Telephone))
                {
                    _logger.LogWarning($"Utilisateur {idUtilisateur} ({utilisateur.NomComplet ?? "Utilisateur"}) n'a pas de numéro de téléphone");
                    return null;
                }

                // ✅ Envoyer le SMS
                var smsLog = await EnvoyerSmsAsync(utilisateur.Telephone, message, typeNotification);

                // ✅ Lier le SMS à l'utilisateur
                if (smsLog != null)
                {
                    try
                    {
                        // Si l'entité est détachée (sauvegarde précédente échouée), la réattacher
                        if (_context.Entry(smsLog).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            _context.SmsLogs.Attach(smsLog);
                        }
                        
                        smsLog.IdUtilisateur = idUtilisateur;
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"⚠️ Erreur lors de la mise à jour du log SMS pour utilisateur {idUtilisateur}, mais SMS envoyé avec succès");
                        // Détacher pour éviter les problèmes suivants
                        _context.Entry(smsLog).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }

                return smsLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi SMS à l'utilisateur {idUtilisateur}");
                return null;
            }
        }

        public async Task<List<SmsLog>> EnvoyerSmsEnMasseAsync(List<string> numerosDestination, string message, string? typeNotification = null)
        {
            var resultats = new List<SmsLog>();

            foreach (var numero in numerosDestination)
            {
                var smsLog = await EnvoyerSmsAsync(numero, message, typeNotification);
                if (smsLog != null)
                {
                    resultats.Add(smsLog);
                }

                // ⏱️ Petit délai pour éviter le rate limiting Twilio (1 SMS/seconde recommandé)
                await Task.Delay(1000);
            }

            _logger.LogInformation($"📨 Envoi en masse terminé : {resultats.Count}/{numerosDestination.Count} SMS envoyés");

            return resultats;
        }

        public async Task<List<SmsLog>> EnvoyerSmsParRoleAsync(string role, string message, string? typeNotification = null)
        {
            // ✅ Récupérer tous les utilisateurs avec ce rôle et un numéro de téléphone
            var utilisateurs = await _context.Utilisateurs
                .Include(u => u.Role)
                .Where(u => u.Role != null && u.Role.Nom == role && u.Telephone != null && u.Telephone != "")
                .ToListAsync();

            _logger.LogInformation($"📨 Envoi SMS aux {utilisateurs.Count} utilisateurs avec rôle {role}");

            var resultats = new List<SmsLog>();

            foreach (var utilisateur in utilisateurs)
            {
                var smsLog = await EnvoyerSmsAUtilisateurAsync(utilisateur.IdUtilisateur, message, typeNotification);
                if (smsLog != null)
                {
                    resultats.Add(smsLog);
                }

                await Task.Delay(1000); // Rate limiting
            }

            return resultats;
        }

        public async Task<List<SmsLog>> EnvoyerSmsParSocieteAsync(int idSociete, string message, string? typeNotification = null)
        {
            // ⚠️ NOTE: Le modèle Tuteur a été supprimé
            // Pour l'instant, on récupère tous les utilisateurs actifs de la société avec numéro
            var utilisateurs = await _context.Utilisateurs
                .Where(u => u.Statut == true &&
                           u.IdSociete == idSociete &&
                           u.Telephone != null &&
                           u.Telephone != "")
                .ToListAsync();

            _logger.LogInformation($"📨 Envoi SMS aux {utilisateurs.Count} utilisateurs de la société {idSociete}");

            var resultats = new List<SmsLog>();

            foreach (var utilisateur in utilisateurs)
            {
                var smsLog = await EnvoyerSmsAUtilisateurAsync(utilisateur.IdUtilisateur, message, typeNotification);
                if (smsLog != null)
                {
                    resultats.Add(smsLog);
                }

                await Task.Delay(1000); // Rate limiting
            }

            return resultats;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔍 VÉRIFICATION ET TRACKING
        // ═══════════════════════════════════════════════════════════════

        public async Task<SmsLog?> VerifierStatutSmsAsync(string messageSid)
        {
            try
            {
                // ✅ Récupérer le SMS dans la base
                var smsLog = await _context.SmsLogs
                    .FirstOrDefaultAsync(s => s.MessageSid == messageSid);

                if (smsLog == null)
                {
                    _logger.LogWarning($"SMS {messageSid} introuvable dans la base");
                    return null;
                }

                // ✅ Interroger Twilio pour le statut actuel
                var messageResource = await MessageResource.FetchAsync(messageSid);

                // ✅ Mettre à jour le statut
                smsLog.Statut = messageResource.Status.ToString().ToUpper();

                if (messageResource.Status == MessageResource.StatusEnum.Delivered)
                {
                    smsLog.DateLivraison = DateTime.Now;
                }
                else if (messageResource.Status == MessageResource.StatusEnum.Failed ||
                         messageResource.Status == MessageResource.StatusEnum.Undelivered)
                {
                    smsLog.DateEchec = DateTime.Now;
                    smsLog.CodeErreur = messageResource.ErrorCode;
                    smsLog.MessageErreur = messageResource.ErrorMessage;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Statut SMS {messageSid} mis à jour : {smsLog.Statut}");

                return smsLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la vérification du statut SMS {messageSid}");
                return null;
            }
        }

        public async Task<int> MettreAJourStatutsSmsEnAttenteAsync()
        {
            try
            {
                // ✅ Récupérer tous les SMS en attente (envoyés il y a moins de 24h)
                var smsEnAttente = await _context.SmsLogs
                    .Where(s => (s.Statut == "PENDING" || s.Statut == "SENT" || s.Statut == "QUEUED") &&
                               s.MessageSid != null &&
                               s.DateEnvoi > DateTime.Now.AddHours(-24))
                    .ToListAsync();

                int misAJour = 0;

                foreach (var sms in smsEnAttente)
                {
                    if (sms.MessageSid != null)
                    {
                        var updated = await VerifierStatutSmsAsync(sms.MessageSid);
                        if (updated != null)
                        {
                            misAJour++;
                        }

                        await Task.Delay(500); // Rate limiting
                    }
                }

                _logger.LogInformation($"✅ {misAJour}/{smsEnAttente.Count} SMS mis à jour");

                return misAJour;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des statuts SMS");
                return 0;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 📊 HISTORIQUE ET RAPPORTS
        // ═══════════════════════════════════════════════════════════════

        public async Task<PagedResult<SmsLog>> GetHistoriqueSmsAsync(
            PagedRequest request,
            string? statut = null,
            string? typeNotification = null,
            int? idUtilisateur = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null)
        {
            var query = _context.SmsLogs
                .Include(s => s.Utilisateur)
                .AsQueryable();

            // ✅ Filtres
            if (!string.IsNullOrWhiteSpace(statut))
            {
                query = query.Where(s => s.Statut == statut.ToUpper());
            }

            if (!string.IsNullOrWhiteSpace(typeNotification))
            {
                query = query.Where(s => s.TypeNotification == typeNotification);
            }

            if (idUtilisateur.HasValue)
            {
                query = query.Where(s => s.IdUtilisateur == idUtilisateur.Value);
            }

            if (dateDebut.HasValue)
            {
                query = query.Where(s => s.DateEnvoi >= dateDebut.Value);
            }

            if (dateFin.HasValue)
            {
                query = query.Where(s => s.DateEnvoi <= dateFin.Value);
            }

            // ✅ Recherche
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                query = query.Where(s =>
                    (s.NumeroDestinataire != null && s.NumeroDestinataire.ToLower().Contains(searchLower)) ||
                    (s.Message != null && s.Message.ToLower().Contains(searchLower)) ||
                    (s.MessageSid != null && s.MessageSid.ToLower().Contains(searchLower))
                );
            }

            // ✅ Tri par défaut : plus récents en premier
            query = query.OrderByDescending(s => s.DateEnvoi);

            // ✅ Pagination
            var totalRecords = await query.CountAsync();
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<SmsLog>(data, request.PageNumber, request.PageSize, totalRecords);
        }

        public async Task<object> GetRapportCoutsSmsAsync(DateTime dateDebut, DateTime dateFin)
        {
            var sms = await _context.SmsLogs
                .Where(s => s.DateEnvoi >= dateDebut && s.DateEnvoi <= dateFin)
                .ToListAsync();

            var rapport = new
            {
                Periode = new { DateDebut = dateDebut, DateFin = dateFin },
                TotalSms = sms.Count,
                SmsEnvoyes = sms.Count(s => s.Statut == "SENT" || s.Statut == "DELIVERED"),
                SmsDelivres = sms.Count(s => s.Statut == "DELIVERED"),
                SmsEchoues = sms.Count(s => s.Statut == "FAILED" || s.Statut == "UNDELIVERED"),
                CoutTotalUsd = sms.Sum(s => s.CoutUsd),
                CoutTotalFc = sms.Sum(s => s.CoutFc),
                RepartitionParType = sms
                    .GroupBy(s => s.TypeNotification ?? "NON_SPECIFIE")
                    .Select(g => new
                    {
                        Type = g.Key,
                        Nombre = g.Count(),
                        CoutUsd = g.Sum(s => s.CoutUsd),
                        CoutFc = g.Sum(s => s.CoutFc)
                    })
                    .OrderByDescending(x => x.Nombre)
                    .ToList()
            };

            return rapport;
        }

        public async Task<object> GetStatistiquesSmsAsync()
        {
            var totalSms = await _context.SmsLogs.CountAsync();
            var totalCoutUsd = await _context.SmsLogs.SumAsync(s => s.CoutUsd);
            var totalCoutFc = await _context.SmsLogs.SumAsync(s => s.CoutFc);

            var stats = new
            {
                TotalSmsEnvoyes = totalSms,
                SmsAujourdhui = await _context.SmsLogs.CountAsync(s => s.DateEnvoi.Date == DateTime.Today),
                SmsCeMois = await _context.SmsLogs.CountAsync(s => s.DateEnvoi.Month == DateTime.Now.Month && s.DateEnvoi.Year == DateTime.Now.Year),
                SmsDelivres = await _context.SmsLogs.CountAsync(s => s.Statut == "DELIVERED"),
                SmsEchoues = await _context.SmsLogs.CountAsync(s => s.Statut == "FAILED" || s.Statut == "UNDELIVERED"),
                TauxLivraison = totalSms > 0 ? (await _context.SmsLogs.CountAsync(s => s.Statut == "DELIVERED") * 100.0 / totalSms) : 0,
                CoutTotalUsd = totalCoutUsd,
                CoutTotalFc = totalCoutFc,
                CoutMoyenParSms = totalSms > 0 ? totalCoutUsd / totalSms : 0
            };

            return stats;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔧 UTILITAIRES
        // ═══════════════════════════════════════════════════════════════

        public bool ValiderNumeroTelephone(string numeroTelephone)
        {
            if (string.IsNullOrWhiteSpace(numeroTelephone))
                return false;

            // Format international : +243XXXXXXXXX (RDC) ou tout autre pays
            var regex = new Regex(@"^\+?[1-9]\d{1,14}$");
            return regex.IsMatch(numeroTelephone);
        }

        public string FormaterNumeroTelephone(string numeroTelephone)
        {
            // Supprimer espaces et caractères spéciaux
            var cleaned = Regex.Replace(numeroTelephone, @"[^\d+]", "");

            // Si commence déjà par +, retourner tel quel
            if (cleaned.StartsWith("+"))
                return cleaned;

            // Si commence par 0, remplacer par +243 (RDC)
            if (cleaned.StartsWith("0"))
                return "+243" + cleaned.Substring(1);

            // Si commence par 243, ajouter +
            if (cleaned.StartsWith("243"))
                return "+" + cleaned;

            // Sinon, ajouter +243 par défaut (RDC)
            return "+243" + cleaned;
        }

        public int CalculerNombreSegments(string message)
        {
            if (string.IsNullOrEmpty(message))
                return 0;

            // SMS standard : 160 caractères par segment
            // SMS avec caractères spéciaux (Unicode) : 70 caractères par segment
            bool contientUnicode = message.Any(c => c > 127);
            int tailleSegment = contientUnicode ? 70 : 160;

            return (int)Math.Ceiling((double)message.Length / tailleSegment);
        }
    }
}

