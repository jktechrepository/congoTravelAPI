using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Helpers pour faciliter l'audit dans les controllers
    /// </summary>
    public static class AuditHelpers
    {
        /// <summary>
        /// Extrait l'ID de l'utilisateur actuel depuis le JWT token
        /// </summary>
        public static int GetCurrentUserId(this ControllerBase controller)
        {
            var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? controller.User.FindFirst("IdUtilisateur");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return 0; // Utilisateur non identifié
        }

        /// <summary>
        /// Extrait le nom complet de l'utilisateur actuel depuis le JWT token
        /// </summary>
        public static string GetCurrentUserName(this ControllerBase controller)
        {
            var nameClaim = controller.User.FindFirst(ClaimTypes.Name)
                         ?? controller.User.FindFirst("NomUtilisateur");

            return nameClaim?.Value ?? "Utilisateur Inconnu";
        }

        /// <summary>
        /// Extrait le rôle de l'utilisateur actuel depuis le JWT token
        /// </summary>
        public static string? GetCurrentUserRole(this ControllerBase controller)
        {
            var roleClaim = controller.User.FindFirst(ClaimTypes.Role)
                         ?? controller.User.FindFirst("Role");

            return roleClaim?.Value;
        }

        /// <summary>
        /// Extrait l'ID de l'école de l'utilisateur actuel depuis le JWT token
        /// </summary>
        public static int? GetCurrentUserSchoolId(this ControllerBase controller)
        {
            // Essayer plusieurs noms de claims possibles
            var schoolClaim = controller.User.FindFirst("IdSociete")
                           ?? controller.User.FindFirst("idSociete");

            if (schoolClaim != null && !string.IsNullOrWhiteSpace(schoolClaim.Value))
            {
                if (int.TryParse(schoolClaim.Value, out int idSociete) && idSociete > 0)
                {
                    return idSociete;
                }
            }

            return null;
        }

        /// <summary>
        /// Extrait l'adresse IP du client
        /// </summary>
        public static string? GetClientIpAddress(this ControllerBase controller)
        {
            try
            {
                // Vérifier si derrière un proxy (ex: Nginx, Apache)
                var forwardedFor = controller.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // Prendre la première IP si plusieurs (client, proxy1, proxy2)
                    return forwardedFor.Split(',')[0].Trim();
                }

                // Vérifier X-Real-IP
                var realIp = controller.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    return realIp;
                }

                // Sinon, prendre l'IP de connexion directe
                return controller.HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extrait le User-Agent (navigateur ou app mobile)
        /// </summary>
        public static string? GetUserAgent(this ControllerBase controller)
        {
            try
            {
                return controller.Request.Headers["User-Agent"].FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extrait toutes les informations d'audit en une seule fois
        /// </summary>
        public static AuditContext GetAuditContext(this ControllerBase controller)
        {
            return new AuditContext
            {
                UserId = controller.GetCurrentUserId(),
                UserName = controller.GetCurrentUserName(),
                UserRole = controller.GetCurrentUserRole(),
                IdSociete = controller.GetCurrentUserSchoolId(),
                IpAddress = controller.GetClientIpAddress(),
                UserAgent = controller.GetUserAgent()
            };
        }
    }

    /// <summary>
    /// Contexte d'audit complet
    /// </summary>
    public class AuditContext
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserRole { get; set; }
        public int? IdSociete { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}

