using CongoTravel.Services.Repositories;
using System.Net;
using System.Net.Mail;

namespace CongoTravelAPI.Services
{
    /// <summary>
    /// Service d'envoi d'emails via SMTP (LWS / mail.rusa-travel.com)
    /// </summary>
    public class EmailService : IEmailService
    {
        private const string DefaultPlatformName = "CongoTravel";
        private const string DefaultFrontendBaseUrl = "https://congotravel.kansaconsulting.com";

        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderPassword;
        private readonly string _senderName;
        private readonly string _replyToEmail;
        private readonly string _frontendBaseUrl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "mail.rusa-travel.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            _senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
            _senderPassword = _configuration["EmailSettings:Password"] ?? "";
            _senderName = _configuration["EmailSettings:SenderName"] ?? DefaultPlatformName;
            _replyToEmail = _configuration["EmailSettings:ReplyToEmail"] ?? _senderEmail;
            _frontendBaseUrl = (_configuration["FrontendSettings:BaseUrl"] ?? DefaultFrontendBaseUrl).TrimEnd('/');
        }

        /// <summary>
        /// Envoie un email de bienvenue avec les identifiants de connexion
        /// </summary>
        public async Task<bool> SendWelcomeEmailAsync(
            string email,
            string nomComplet,
            string defaultUsername,
            string telephone,
            string motDePasseParDefaut,
            string role,
            string nomSociete,
            string genre = "Masculin",
            string fonction = null,
            string matricule = null,
            string nomEnfant = null,
            string classeEnfant = null,
            string matriculeEnfant = null)
        {
            try
            {
                // Vérifier que l'email est fourni
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("⚠️ Email non fourni. Envoi d'email ignoré.");
                    return false;
                }

                _logger.LogInformation($"📧 Préparation de l'email de bienvenue pour {email}...");

                // Créer le sujet
                string subject = $"Bienvenue sur CongoTravel - Vos identifiants de connexion";

                // Créer le contenu HTML
                string htmlBody = CreateWelcomeEmailTemplate(
                    nomComplet, 
                    email, 
                    defaultUsername, 
                    telephone, 
                    motDePasseParDefaut, 
                    role, 
                    nomSociete,
                    genre,
                    fonction,
                    matricule,
                    nomEnfant,         // ✨ Passer le nom de l'enfant
                    classeEnfant,      // ✨ Passer la classe de l'enfant
                    matriculeEnfant);  // ✨ Passer le matricule de l'enfant

                // Vérifier que le HTML n'est pas vide
                if (string.IsNullOrWhiteSpace(htmlBody))
                {
                    _logger.LogError("❌ Le template HTML est vide !");
                    return false;
                }

                _logger.LogInformation("✅ Template HTML créé avec succès (Longueur: {Length} caractères)", htmlBody.Length);

                // Créer le contenu texte brut (fallback)
                string plainTextBody = CreatePlainTextWelcomeEmail(
                    nomComplet, 
                    email, 
                    defaultUsername, 
                    telephone, 
                    motDePasseParDefaut, 
                    role, 
                    nomSociete,
                    genre,
                    fonction,
                    matricule,
                    nomEnfant,         // ✨ Passer le nom de l'enfant
                    classeEnfant,      // ✨ Passer la classe de l'enfant
                    matriculeEnfant);  // ✨ Passer le matricule de l'enfant

                // Envoyer l'email
                bool success = await SendEmailAsync(email, nomComplet, subject, plainTextBody, htmlBody);

                if (success)
                {
                    _logger.LogInformation($"✅ Email de bienvenue envoyé avec succès à {email}");
                }
                else
                {
                    _logger.LogWarning($"⚠️ Échec de l'envoi de l'email à {email}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de l'envoi de l'email de bienvenue à {email}");
                return false;
            }
        }

        /// <summary>
        /// Envoie un email de réinitialisation de mot de passe
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string email, string nomComplet, string resetToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("⚠️ Email non fourni. Envoi d'email ignoré.");
                    return false;
                }

                _logger.LogInformation($"📧 Préparation de l'email de réinitialisation pour {email}...");

                string subject = "CongoTravel - Réinitialisation de votre mot de passe";
                string htmlBody = CreatePasswordResetEmailTemplate(nomComplet, resetToken);
                string plainTextBody = $"Bonjour {nomComplet},\n\nVotre code de réinitialisation : {resetToken}\n\nCordialement,\nL'équipe CongoTravel";

                bool success = await SendEmailAsync(email, nomComplet, subject, plainTextBody, htmlBody);

                if (success)
                {
                    _logger.LogInformation($"✅ Email de réinitialisation envoyé avec succès à {email}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de l'envoi de l'email de réinitialisation à {email}");
                return false;
            }
        }

        /// <summary>
        /// Envoie un email de confirmation de changement de mot de passe
        /// </summary>
        public async Task<bool> SendPasswordChangedConfirmationEmailAsync(string email, string nomComplet, DateTime dateChangement, string adresseIP = null)
        {
            try
            {
                // Vérifier que l'email est fourni
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("⚠️ Email non fourni. Envoi d'email de confirmation ignoré.");
                    return false;
                }

                _logger.LogInformation($"📧 Préparation de l'email de confirmation de changement de mot de passe pour {email}...");

                string subject = "Confirmation de changement de mot de passe - CongoTravel";
                string htmlBody = CreatePasswordChangedConfirmationEmailTemplate(nomComplet, dateChangement, adresseIP);
                string plainTextBody = $"Bonjour {nomComplet},\n\nVotre mot de passe a été modifié avec succès le {dateChangement:dd/MM/yyyy à HH:mm}.\n\nSi vous n'avez pas effectué cette modification, contactez immédiatement le support.\n\nCordialement,\nL'équipe CongoTravel";

                bool success = await SendEmailAsync(email, nomComplet, subject, plainTextBody, htmlBody);

                if (success)
                {
                    _logger.LogInformation($"✅ Email de confirmation de changement de mot de passe envoyé avec succès à {email}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de l'envoi de l'email de confirmation à {email}");
                return false;
            }
        }

        /// <summary>
        /// Envoie un email avec le lien de vérification d'adresse
        /// </summary>
        public async Task<bool> SendEmailVerificationLinkAsync(string email, string nomComplet, string verificationUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Email non fourni. Envoi de vérification ignoré.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(verificationUrl))
                {
                    _logger.LogWarning("URL de vérification manquante. Envoi ignoré.");
                    return false;
                }

                _logger.LogInformation("Préparation de l'email de vérification pour {Email}...", email);

                string subject = "CongoTravel - Vérifiez votre adresse email";
                string htmlBody = CreateEmailVerificationLinkTemplate(nomComplet, verificationUrl);
                string plainTextBody =
                    $"Bonjour {nomComplet},\n\n" +
                    "Merci de vous être inscrit sur CongoTravel. Pour confirmer que cette adresse vous appartient, ouvrez le lien suivant :\n\n" +
                    $"{verificationUrl}\n\n" +
                    "Ce lien expire dans 24 heures.\n\n" +
                    "Si vous n'avez pas créé de compte, ignorez cet email.\n\n" +
                    "Cordialement,\nL'équipe CongoTravel";

                bool success = await SendEmailAsync(email, nomComplet, subject, plainTextBody, htmlBody);
                if (success)
                    _logger.LogInformation("Email de vérification envoyé à {Email}", email);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de vérification à {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Méthode générique d'envoi d'email via SMTP
        /// </summary>
        private async Task<bool> SendEmailAsync(
            string toEmail, 
            string toName, 
            string subject, 
            string plainTextBody, 
            string htmlBody)
        {
            try
            {
                // Vérifier que les paramètres SMTP sont configurés
                if (string.IsNullOrWhiteSpace(_senderEmail) || string.IsNullOrWhiteSpace(_senderPassword))
                {
                    _logger.LogError("❌ Configuration SMTP incomplète : Email={Email}, Password={HasPassword}", 
                        _senderEmail, !string.IsNullOrWhiteSpace(_senderPassword));
                    return false;
                }

                _logger.LogInformation("🔍 Tentative d'envoi d'email à {ToEmail} via {SmtpServer}:{Port}", 
                    toEmail, _smtpServer, _smtpPort);

                using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;

                    string cleanPassword = _senderPassword?.Replace(" ", "") ?? "";
                    smtpClient.Credentials = new NetworkCredential(_senderEmail, cleanPassword);
                    smtpClient.Timeout = 30000;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(_senderEmail, _senderName);
                        mailMessage.ReplyToList.Add(new MailAddress(_replyToEmail, _senderName));
                        mailMessage.To.Add(new MailAddress(toEmail, toName));
                        mailMessage.Subject = subject;
                        
                        // Définir le corps HTML comme contenu principal
                        mailMessage.Body = htmlBody;
                        mailMessage.IsBodyHtml = true;
                        
                        // Encodage UTF-8 pour supporter les caractères spéciaux
                        mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
                        mailMessage.SubjectEncoding = System.Text.Encoding.UTF8;

                        _logger.LogInformation("📧 Envoi de l'email en cours... (Body length: {Length} chars)", htmlBody?.Length ?? 0);
                        await smtpClient.SendMailAsync(mailMessage);
                        _logger.LogInformation("✅ Email envoyé avec succès à {ToEmail}", toEmail);
                        return true;
                    }
                }
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx,
                    "Erreur SMTP lors de l'envoi à {ToEmail} via {SmtpServer}:{Port}: {Message}. StatusCode: {StatusCode}. " +
                    "Vérifiez EmailSettings (auth, port 587 STARTTLS, serveur mail.rusa-travel.com ou mail94.lwspanel.com).",
                    toEmail, _smtpServer, _smtpPort, smtpEx.Message, smtpEx.StatusCode);
                
                // Log supplémentaire pour diagnostiquer
                if (smtpEx.InnerException != null)
                {
                    _logger.LogError(smtpEx.InnerException, "❌ Exception interne SMTP: {Message}", smtpEx.InnerException.Message);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur générale lors de l'envoi à {ToEmail}: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Génère la salutation appropriée selon le genre
        /// </summary>
        private string GetSalutation(string genre, string nomComplet)
        {
            // Normaliser le genre
            string genreNormalized = genre?.Trim().ToLower() ?? "masculin";
            
            if (genreNormalized == "feminin" || genreNormalized == "féminin" || genreNormalized == "f")
            {
                return $"Madame {nomComplet}";
            }
            else
            {
                return $"Monsieur {nomComplet}";
            }
        }

        /// <summary>
        /// Crée le template HTML pour l'email de bienvenue (Style AWS)
        /// </summary>
        private string CreateWelcomeEmailTemplate(
            string nomComplet,
            string email,
            string defaultUsername,
            string telephone,
            string motDePasseParDefaut,
            string role,
            string nomSociete,
            string genre = "Masculin",
            string fonction = null,
            string matricule = null,
            string nomEnfant = null,
            string classeEnfant = null,
            string matriculeEnfant = null)
        {
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Bienvenue sur CongoTravel</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
            line-height: 1.6;
        }}
        .email-wrapper {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }}
        .header {{
            background-color: #232f3e;
            padding: 30px 40px;
            text-align: center;
        }}
        .header-logo {{
            color: #ffffff;
            font-size: 28px;
            font-weight: 600;
            letter-spacing: 1px;
            margin: 0;
        }}
        .header-logo .highlight {{
            color: #ff9900;
        }}
        .content {{
            padding: 40px;
            color: #232f3e;
        }}
        .title {{
            font-size: 24px;
            font-weight: 600;
            color: #232f3e;
            margin: 0 0 20px 0;
        }}
        .greeting {{
            font-size: 16px;
            color: #232f3e;
            margin: 0 0 20px 0;
        }}
        .message {{
            font-size: 16px;
            color: #232f3e;
            margin: 0 0 20px 0;
        }}
        .info-section {{
            background-color: #f8f9fa;
            border: 1px solid #e0e0e0;
            border-radius: 4px;
            padding: 25px;
            margin: 25px 0;
        }}
        .info-section-title {{
            font-size: 18px;
            font-weight: 600;
            color: #232f3e;
            margin: 0 0 20px 0;
        }}
        .credential-row {{
            display: flex;
            padding: 12px 0;
            border-bottom: 1px solid #e0e0e0;
        }}
        .credential-row:last-child {{
            border-bottom: none;
        }}
        .credential-label {{
            font-weight: 500;
            color: #666666;
            width: 140px;
            flex-shrink: 0;
        }}
        .credential-value {{
            color: #232f3e;
            font-family: 'Courier New', monospace;
            font-weight: 600;
            flex: 1;
        }}
        .warning-box {{
            background-color: #fff3cd;
            border-left: 4px solid #ff9900;
            padding: 15px 20px;
            margin: 25px 0;
            border-radius: 4px;
        }}
        .warning-box strong {{
            color: #856404;
        }}
        .button {{
            display: inline-block;
            background-color: #ff9900;
            color: #ffffff;
            padding: 14px 32px;
            text-decoration: none;
            border-radius: 4px;
            margin: 25px 0;
            font-weight: 600;
            font-size: 16px;
        }}
        .button:hover {{
            background-color: #e68900;
        }}
        .footer {{
            background-color: #f5f5f5;
            padding: 30px 40px;
            text-align: center;
            color: #666666;
            font-size: 12px;
            border-top: 1px solid #e0e0e0;
        }}
        .footer-text {{
            margin: 5px 0;
            color: #666666;
        }}
        .role-badge {{
            display: inline-block;
            background-color: #232f3e;
            color: #ffffff;
            padding: 6px 16px;
            border-radius: 4px;
            font-size: 14px;
            font-weight: 500;
            margin: 0 5px;
        }}
        .highlight-box {{
            background-color: #f8f9fa;
            border-left: 3px solid #ff9900;
            padding: 15px 20px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .highlight-box-title {{
            font-size: 16px;
            font-weight: 600;
            color: #232f3e;
            margin: 0 0 10px 0;
        }}
        .highlight-box-text {{
            font-size: 14px;
            color: #666666;
            margin: 5px 0;
        }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='header'>
            <h1 class='header-logo'>CongoTravel</h1>
        </div>
        
        <div class='content'>
            <h2 class='title'>Bienvenue sur CongoTravel</h2>
            
            <p class='greeting'>Bonjour <strong>{GetSalutation(genre, nomComplet)}</strong>,</p>
            
            <p class='message'>
                Votre compte a été créé avec succès sur la plateforme CongoTravel. 
                Vous faites maintenant partie de <strong>{nomSociete}</strong> en tant que <span class='role-badge'>{(!string.IsNullOrWhiteSpace(fonction) ? fonction : role)}</span>.
            </p>
            
            {(!string.IsNullOrWhiteSpace(matricule) ? $@"
            <div class='highlight-box'>
                <p class='highlight-box-text'><strong>Matricule :</strong> {matricule}</p>
            </div>" : "")}
            
            {(!string.IsNullOrWhiteSpace(nomEnfant) ? $@"
            <div class='highlight-box'>
                <p class='highlight-box-title'>Votre enfant inscrit</p>
                <p class='highlight-box-text'><strong>Nom complet :</strong> {nomEnfant}</p>
                {(!string.IsNullOrWhiteSpace(classeEnfant) ? $"<p class='highlight-box-text'><strong>Classe :</strong> {classeEnfant}</p>" : "")}
                {(!string.IsNullOrWhiteSpace(matriculeEnfant) ? $"<p class='highlight-box-text'><strong>Matricule :</strong> {matriculeEnfant}</p>" : "")}
            </div>
            
            <div class='highlight-box'>
                <p class='highlight-box-title'>En tant que technicien, vous pourrez :</p>
                <ul style='margin: 10px 0; padding-left: 25px; color: #666666;'>
                    <li style='margin: 5px 0;'>Effectuer la maintenance et le support technique</li>
                    <li style='margin: 5px 0;'>Consulter les notes et bulletins</li>
                    <li style='margin: 5px 0;'>Recevoir les notifications importantes de l'école</li>
                    <li style='margin: 5px 0;'>Communiquer avec les caissiers</li>
                    <li style='margin: 5px 0;'>Gérer les paiements des frais scolaires</li>
                </ul>
            </div>" : "")}
            
            <div class='info-section'>
                <h3 class='info-section-title'>Vos identifiants de connexion</h3>
                <div class='credential-row'>
                    <span class='credential-label'>Email :</span>
                    <span class='credential-value'>{email}</span>
                </div>
                <div class='credential-row'>
                    <span class='credential-label'>Nom d'utilisateur :</span>
                    <span class='credential-value'>{defaultUsername}</span>
                </div>
                <div class='credential-row'>
                    <span class='credential-label'>Téléphone :</span>
                    <span class='credential-value'>{telephone}</span>
                </div>
                <div class='credential-row'>
                    <span class='credential-label'>Mot de passe :</span>
                    <span class='credential-value'>{motDePasseParDefaut}</span>
                </div>
            </div>
            
            <div class='warning-box'>
                <strong>Important :</strong> Pour des raisons de sécurité, vous devrez <strong>obligatoirement changer votre mot de passe</strong> lors de votre première connexion.
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{_frontendBaseUrl}' class='button'>Se connecter maintenant</a>
            </div>
            
            <p style='margin-top: 30px; font-size: 14px; color: #666666;'>
                Vous pouvez vous connecter en utilisant votre <strong>email</strong>, votre <strong>nom d'utilisateur</strong> ou votre <strong>numéro de téléphone</strong>.
            </p>
        </div>
        
        <div class='footer'>
            <p class='footer-text'>Cet email a été envoyé automatiquement par CongoTravel Platform.</p>
            <p class='footer-text'>© 2025 CongoTravel. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Crée la version texte brut de l'email de bienvenue
        /// </summary>
        private string CreatePlainTextWelcomeEmail(
            string nomComplet,
            string email,
            string defaultUsername,
            string telephone,
            string motDePasseParDefaut,
            string role,
            string nomSociete,
            string genre = "Masculin",
            string fonction = null,
            string matricule = null,
            string nomEnfant = null,
            string classeEnfant = null,
            string matriculeEnfant = null)
        {
            return $@"
═══════════════════════════════════════════════════════════
    BIENVENUE SUR CONGOTRAVEL
═══════════════════════════════════════════════════════════

Bonjour {GetSalutation(genre, nomComplet)},

Votre compte a été créé avec succès sur la plateforme CongoTravel.

📧 IDENTIFIANTS DE CONNEXION :
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Email             : {email}
Nom d'utilisateur : {defaultUsername}
Téléphone         : {telephone}
Mot de passe      : {motDePasseParDefaut}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔐 Rôle       : {(!string.IsNullOrWhiteSpace(fonction) ? fonction : role)}
🏫 École      : {nomSociete}
{(!string.IsNullOrWhiteSpace(matricule) ? $"🆔 Matricule  : {matricule}\n" : "")}
{(!string.IsNullOrWhiteSpace(nomEnfant) ? $@"
👶 VOTRE ENFANT INSCRIT :
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Nom complet : {nomEnfant}
{(!string.IsNullOrWhiteSpace(classeEnfant) ? $"Classe      : {classeEnfant}\n" : "")}{(!string.IsNullOrWhiteSpace(matriculeEnfant) ? $"Matricule   : {matriculeEnfant}\n" : "")}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

💡 EN TANT QUE TECHNICIEN, VOUS POURREZ :
• Suivre la scolarité de votre enfant en temps réel
• Consulter les notes et bulletins
• Recevoir les notifications importantes de l'école
• Communiquer avec les caissiers
• Gérer les paiements des frais scolaires

" : "")}
⚠️ IMPORTANT : Pour des raisons de sécurité, vous devrez 
OBLIGATOIREMENT changer votre mot de passe lors de votre 
première connexion.

🔗 Se connecter : {_frontendBaseUrl}

💡 Vous pouvez vous connecter en utilisant votre email, 
votre nom d'utilisateur ou votre numéro de téléphone.

═══════════════════════════════════════════════════════════

Cet email a été envoyé automatiquement.
Merci de ne pas y répondre.

CongoTravel Platform - Votre partenaire éducatif
© 2025 CongoTravel. Tous droits réservés.
";
        }

        /// <summary>
        /// Crée le template HTML pour l'email de réinitialisation de mot de passe (Style AWS)
        /// </summary>
        private string CreatePasswordResetEmailTemplate(string nomComplet, string resetToken)
        {
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Vérifiez votre identité - CongoTravel</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
            line-height: 1.6;
        }}
        .email-wrapper {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }}
        .header {{
            background-color: #232f3e;
            padding: 30px 40px;
            text-align: center;
        }}
        .header-logo {{
            color: #ffffff;
            font-size: 28px;
            font-weight: 600;
            letter-spacing: 1px;
            margin: 0;
        }}
        .header-logo .highlight {{
            color: #ff9900;
        }}
        .content {{
            padding: 40px;
            color: #232f3e;
        }}
        .title {{
            font-size: 24px;
            font-weight: 600;
            color: #232f3e;
            margin: 0 0 20px 0;
        }}
        .greeting {{
            font-size: 16px;
            color: #232f3e;
            margin: 0 0 20px 0;
        }}
        .message {{
            font-size: 16px;
            color: #232f3e;
            margin: 0 0 30px 0;
        }}
        .verification-section {{
            margin: 30px 0;
        }}
        .verification-label {{
            font-size: 16px;
            color: #232f3e;
            margin: 0 0 15px 0;
            font-weight: 500;
        }}
        .verification-code {{
            font-size: 48px;
            font-weight: 700;
            color: #232f3e;
            letter-spacing: 8px;
            text-align: center;
            margin: 20px 0;
            font-family: 'Courier New', monospace;
        }}
        .expiration-notice {{
            font-size: 14px;
            color: #666666;
            text-align: center;
            margin: 15px 0 30px 0;
            font-style: italic;
        }}
        .warning-message {{
            font-size: 14px;
            color: #666666;
            margin: 30px 0 0 0;
            padding-top: 20px;
            border-top: 1px solid #e0e0e0;
        }}
        .footer {{
            background-color: #f5f5f5;
            padding: 30px 40px;
            text-align: center;
            color: #666666;
            font-size: 12px;
        }}
        .footer-text {{
            margin: 5px 0;
            color: #666666;
        }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='header'>
            <h1 class='header-logo'>CongoTravel</h1>
        </div>
        
        <div class='content'>
            <h2 class='title'>Vérifiez votre identité</h2>
            
            <p class='greeting'>Bonjour,</p>
            
            <p class='message'>
                Nous avons identifié une demande de réinitialisation de mot de passe pour l'utilisateur <strong>{nomComplet}</strong> sur votre compte CongoTravel. 
                Si vous avez demandé la réinitialisation de votre mot de passe, veuillez saisir le code ci-dessous pour vérifier votre identité et terminer votre réinitialisation.
            </p>
            
            <div class='verification-section'>
                <p class='verification-label'>Code de vérification</p>
                <div class='verification-code'>{resetToken}</div>
                <p class='expiration-notice'>(Ce code expirera 5 minutes après son envoi.)</p>
            </div>
            
            <p class='warning-message'>
                Si vous n'avez pas demandé la réinitialisation de votre mot de passe, nous vous recommandons de ne pas utiliser ce code et de vérifier la sécurité de votre compte.
            </p>
        </div>
        
        <div class='footer'>
            <p class='footer-text'>Cet email a été envoyé automatiquement par CongoTravel Platform.</p>
            <p class='footer-text'>© 2025 CongoTravel. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Template HTML — lien de vérification d'email
        /// </summary>
        private string CreateEmailVerificationLinkTemplate(string nomComplet, string verificationUrl)
        {
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Vérifiez votre email - CongoTravel</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; background-color: #f5f5f5; margin: 0; padding: 0; line-height: 1.6; }}
        .email-wrapper {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
        .header {{ background-color: #232f3e; padding: 30px 40px; text-align: center; }}
        .header-logo {{ color: #ffffff; font-size: 28px; font-weight: 600; margin: 0; }}
        .content {{ padding: 40px; color: #232f3e; }}
        .title {{ font-size: 24px; font-weight: 600; margin: 0 0 20px 0; }}
        .message {{ font-size: 16px; margin: 0 0 24px 0; }}
        .btn {{ display: inline-block; background-color: #ff9900; color: #232f3e !important; text-decoration: none; font-weight: 600; padding: 14px 28px; border-radius: 4px; }}
        .link-fallback {{ font-size: 13px; color: #666666; word-break: break-all; margin-top: 24px; }}
        .footer {{ background-color: #f5f5f5; padding: 24px 40px; text-align: center; font-size: 12px; color: #666666; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='header'><h1 class='header-logo'>CongoTravel</h1></div>
        <div class='content'>
            <h2 class='title'>Confirmez votre adresse email</h2>
            <p class='message'>Bonjour <strong>{System.Net.WebUtility.HtmlEncode(nomComplet)}</strong>,</p>
            <p class='message'>
                Merci de votre inscription. Cliquez sur le bouton ci-dessous pour confirmer que cette adresse vous appartient.
                Le lien est valable 24 heures.
            </p>
            <p style='text-align:center;margin:32px 0;'>
                <a class='btn' href='{verificationUrl}'>Vérifier mon email</a>
            </p>
            <p class='link-fallback'>
                Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :<br/>
                {verificationUrl}
            </p>
            <p class='message' style='font-size:14px;color:#666666;margin-top:28px;'>
                Si vous n'avez pas créé de compte CongoTravel, ignorez simplement cet email.
            </p>
        </div>
        <div class='footer'>
            <p>Cet email a été envoyé automatiquement par CongoTravel.</p>
            <p>© {DateTime.UtcNow.Year} CongoTravel. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Crée le template HTML pour l'email de confirmation de changement de mot de passe
        /// </summary>
        private string CreatePasswordChangedConfirmationEmailTemplate(string nomComplet, DateTime dateChangement, string adresseIP = null)
        {
            string adresseIPInfo = !string.IsNullOrWhiteSpace(adresseIP) ? $" depuis l'adresse IP : <strong>{adresseIP}</strong>" : "";
            string dateFormatee = dateChangement.ToString("dd/MM/yyyy 'à' HH:mm");
            
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Confirmation de changement de mot de passe</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .content {{
            padding: 30px;
            color: #333;
            line-height: 1.6;
        }}
        .success-box {{
            background-color: #d4edda;
            border: 2px solid #c3e6cb;
            padding: 20px;
            margin: 20px 0;
            border-radius: 5px;
            text-align: center;
        }}
        .success-icon {{
            font-size: 48px;
            color: #28a745;
            margin-bottom: 10px;
        }}
        .info-box {{
            background-color: #f8f9fa;
            border-left: 5px solid #28a745;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .warning-box {{
            background-color: #fff3cd;
            border: 2px solid #ffeaa7;
            padding: 15px;
            margin: 20px 0;
            border-radius: 5px;
            border-left: 5px solid #ffc107;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            color: #777;
            font-size: 14px;
        }}
        .button {{
            display: inline-block;
            background-color: #28a745;
            color: white;
            padding: 12px 24px;
            text-decoration: none;
            border-radius: 5px;
            margin: 15px 0;
            font-weight: bold;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Mot de passe modifié</h1>
        </div>
        
        <div class='content'>
            <p>Bonjour <strong>{nomComplet}</strong>,</p>
            
            <div class='success-box'>
                <div class='success-icon'>🔐</div>
                <h2 style='color: #28a745; margin: 0;'>Changement confirmé !</h2>
            </div>
            
            <p>Nous vous confirmons que votre mot de passe a été <strong>modifié avec succès</strong>.</p>
            
            <div class='info-box'>
                <p><strong>📅 Date et heure :</strong> {dateFormatee}</p>
                <p><strong>🌐 Connexion :</strong> {adresseIPInfo}</p>
            </div>
            
            <div class='warning-box'>
                <h3 style='color: #856404; margin-top: 0;'>⚠️ Important</h3>
                <p>Si vous n'avez pas effectué cette modification de mot de passe :</p>
                <ul>
                    <li>Contactez immédiatement le support technique</li>
                    <li>Vérifiez l'activité de votre compte</li>
                    <li>Considérez changer à nouveau votre mot de passe</li>
                </ul>
            </div>
            
            <p style='text-align: center;'>
                <a href='{_frontendBaseUrl}' class='button'>Se connecter maintenant</a>
            </p>
            
            <p>Pour toute question ou assistance, n'hésitez pas à nous contacter.</p>
        </div>
        
        <div class='footer'>
            <p><strong>CongoTravel Platform</strong></p>
            <p style='font-size: 12px; color: #999;'>© 2025 CongoTravel. Tous droits réservés.</p>
            <p style='font-size: 11px; color: #ccc;'>Cet email a été envoyé automatiquement pour votre sécurité.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Envoie un email générique (implémentation de l'interface)
        /// </summary>
        public async Task<bool> SendGenericEmailAsync(string toEmail, string toName, string subject, string plainTextBody, string htmlBody)
        {
            return await SendEmailAsync(toEmail, toName, subject, plainTextBody, htmlBody);
        }
    }
}
