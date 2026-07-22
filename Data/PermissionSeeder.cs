using CongoTravel.Models;
using CongoTravel.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Data
{
    /// <summary>
    /// Initialise les permissions par défaut du système RBAC
    /// </summary>
    public static class PermissionSeeder
    {
        /// <summary>
        /// Crée toutes les permissions par défaut et les assigne aux rôles appropriés
        /// </summary>
        public static async Task SeedPermissionsAsync(CongoTravelDbContext context)
        {
            // Vérifier si des permissions existent déjà
            var permissionsExist = await context.Permissions.AnyAsync();
            
            Console.WriteLine("🔨 Initialisation des permissions par défaut...");

            // 1. Créer tous les rôles nécessaires d'abord
            await CreateMissingRolesAsync(context);

            // 2. Ajouter les permissions (nouvelles permissions seront ajoutées même si certaines existent déjà)
            var defaultPermissions = GetDefaultPermissions();
            var existingPermissionNames = await context.Permissions.Select(p => p.Nom).ToListAsync();
            
            var newPermissions = defaultPermissions
                .Where(p => !existingPermissionNames.Contains(p.Nom))
                .ToList();
            
            if (newPermissions.Any())
            {
                await context.Permissions.AddRangeAsync(newPermissions);
                await context.SaveChangesAsync();
                Console.WriteLine($" {newPermissions.Count} nouvelles permissions créées");
            }
            else
            {
                Console.WriteLine(" Toutes les permissions existent déjà");
            }

            // 3. TOUJOURS assigner les permissions aux rôles
            // AssignPermissionsToRolesAsync vérifie déjà les assignations existantes
            // et n'ajoute que les permissions manquantes
            await AssignPermissionsToRolesAsync(context);
            Console.WriteLine(" Vérification et assignation des permissions aux rôles terminée");
        }

        /// <summary>
        /// Crée tous les rôles manquants dans le système
        /// </summary>
        private static async Task CreateMissingRolesAsync(CongoTravelDbContext context)
        {
            // ⚠️ IMPORTANT: Cette liste doit couvrir tous les rôles référencés dans AssignPermissionsToRolesAsync
            var roleNames = new[]
            {
                "Super-Admin",
                "Admin",
                "Gerant",
                "Financier",
                "Caissier",
                "Client"
            };
            var existingRoles = await context.Roles.Select(r => r.Nom).ToListAsync();

            foreach (var roleName in roleNames)
            {
                if (!existingRoles.Contains(roleName))
                {
                    context.Roles.Add(new Role
                    {
                        Nom = roleName,
                        DateCreation = DateTime.UtcNow,
                        Statut = true
                    });
                    Console.WriteLine($" Rôle '{roleName}' créé");
                }
                else
                {
                    Console.WriteLine($" Rôle '{roleName}' existe déjà");
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Retourne la liste de toutes les permissions par défaut (80+ permissions)
        /// </summary>
        private static List<Permission> GetDefaultPermissions()
        {
            return new List<Permission>
            {
                // ═══════════════════════════════════════════════════════════════════
                // AGENT - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Agent.Create",  Categorie = "Agent", Action = "Create",  Description = "Créer un agent",       Statut = true },
                new Permission { Nom = "Agent.Read",    Categorie = "Agent", Action = "Read",    Description = "Voir un agent",        Statut = true },
                new Permission { Nom = "Agent.ReadAll", Categorie = "Agent", Action = "ReadAll", Description = "Voir tous les agents", Statut = true },
                new Permission { Nom = "Agent.Update",  Categorie = "Agent", Action = "Update",  Description = "Modifier un agent",    Statut = true },
                new Permission { Nom = "Agent.Delete",  Categorie = "Agent", Action = "Delete",  Description = "Supprimer un agent",   Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // AUDITLOG - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "AuditLog.Create",  Categorie = "AuditLog", Action = "Create",  Description = "Créer un AuditLog",       Statut = true },
                new Permission { Nom = "AuditLog.Read",    Categorie = "AuditLog", Action = "Read",    Description = "Voir un AuditLog",        Statut = true },
                new Permission { Nom = "AuditLog.ReadAll", Categorie = "AuditLog", Action = "ReadAll", Description = "Voir tous les AuditLog", Statut = true },
                new Permission { Nom = "AuditLog.Update",  Categorie = "AuditLog", Action = "Update",  Description = "Modifier un AuditLog",    Statut = true },
                new Permission { Nom = "AuditLog.Delete",  Categorie = "AuditLog", Action = "Delete",  Description = "Supprimer un AuditLog",   Statut = true },

                // ================================
                // BILLET - 5 permissions
                // ================================
                new Permission { Nom = "Billet.Create", Categorie = "Billet", Action = "Create", Description = "Créer un billet", Statut = true },
                new Permission { Nom = "Billet.Read", Categorie = "Billet", Action = "Read", Description = "Voir un billet", Statut = true },
                new Permission { Nom = "Billet.ReadAll", Categorie = "Billet", Action = "ReadAll", Description = "Voir tous les billets", Statut = true },
                new Permission { Nom = "Billet.Update", Categorie = "Billet", Action = "Update", Description = "Modifier un billet", Statut = true },
                new Permission { Nom = "Billet.Delete", Categorie = "Billet", Action = "Delete", Description = "Supprimer un billet", Statut = true },

                // ================================
                // BilletEmbarquement - 5 permissions
                // ================================
                new Permission { Nom = "BilletEmbarquement.Create", Categorie = "BilletEmbarquement",  Action = "Create", Description = "Créer un BilletEmbarquement", Statut = true },
                new Permission { Nom = "BilletEmbarquement.Read", Categorie = "BilletEmbarquement",    Action = "Read", Description = "Voir un BilletEmbarquement", Statut = true },
                new Permission { Nom = "BilletEmbarquement.ReadAll", Categorie = "BilletEmbarquement", Action = "ReadAll", Description = "Voir tous les BilletEmbarquement", Statut = true },
                new Permission { Nom = "BilletEmbarquement.Update", Categorie = "BilletEmbarquement",  Action = "Update", Description = "Modifier un BilletEmbarquement", Statut = true },
                new Permission { Nom = "BilletEmbarquement.Delete", Categorie = "BilletEmbarquement",  Action = "Delete", Description = "Supprimer un BilletEmbarquement", Statut = true },

                
                // ================================
                // CATEGORIE SIEGE
                // ================================
                new Permission { Nom = "CategorieSiege.Create", Categorie = "CategorieSiege", Action = "Create", Description = "Créer une catégorie de siège", Statut = true },
                new Permission { Nom = "CategorieSiege.Read", Categorie = "CategorieSiege", Action = "Read", Description = "Voir une catégorie de siège", Statut = true },
                new Permission { Nom = "CategorieSiege.ReadAll", Categorie = "CategorieSiege", Action = "ReadAll", Description = "Voir toutes les catégories de sièges", Statut = true },
                new Permission { Nom = "CategorieSiege.Update", Categorie = "CategorieSiege", Action = "Update", Description = "Modifier une catégorie de siège", Statut = true },
                new Permission { Nom = "CategorieSiege.Delete", Categorie = "CategorieSiege", Action = "Delete", Description = "Supprimer une catégorie de siège", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // CLIENT - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Client.Create",  Categorie = "Client", Action = "Create",  Description = "Créer un client",       Statut = true },
                new Permission { Nom = "Client.Read",    Categorie = "Client", Action = "Read",    Description = "Voir un client",        Statut = true },
                new Permission { Nom = "Client.ReadAll", Categorie = "Client", Action = "ReadAll", Description = "Voir tous les clients", Statut = true },
                new Permission { Nom = "Client.Update",  Categorie = "Client", Action = "Update",  Description = "Modifier un client",    Statut = true },
                new Permission { Nom = "Client.Delete",  Categorie = "Client", Action = "Delete",  Description = "Supprimer un client",   Statut = true },

                
                // ═══════════════════════════════════════════════════════════════════
                // COMMUNICATION CAMPAIGN - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "CommunicationCampaign.Create", Categorie = "CommunicationCampaign", Action = "Create", Description = "Créer une campagne de communication", Statut = true },
                new Permission { Nom = "CommunicationCampaign.Read", Categorie = "CommunicationCampaign", Action = "Read", Description = "Voir une campagne de communication", Statut = true },
                new Permission { Nom = "CommunicationCampaign.ReadAll", Categorie = "CommunicationCampaign", Action = "ReadAll", Description = "Voir toutes les campagnes de communication", Statut = true },
                new Permission { Nom = "CommunicationCampaign.Update", Categorie = "CommunicationCampaign", Action = "Update", Description = "Modifier une campagne de communication", Statut = true },
                new Permission { Nom = "CommunicationCampaign.Delete", Categorie = "CommunicationCampaign", Action = "Delete", Description = "Supprimer une campagne de communication", Statut = true },

                // ================================
                // DESTINATION - 5 permissions
                // ================================
                new Permission { Nom = "Destination.Create", Categorie = "Destination", Action = "Create", Description = "Créer une destination", Statut = true },
                new Permission { Nom = "Destination.Read", Categorie = "Destination", Action = "Read", Description = "Voir une destination", Statut = true },
                new Permission { Nom = "Destination.ReadAll", Categorie = "Destination", Action = "ReadAll", Description = "Voir toutes les destinations", Statut = true },
                new Permission { Nom = "Destination.Update", Categorie = "Destination", Action = "Update", Description = "Modifier une destination", Statut = true },
                new Permission { Nom = "Destination.Delete", Categorie = "Destination", Action = "Delete", Description = "Supprimer une destination", Statut = true },

                // ================================
                // DEVISE - multi-devise
                // ================================
                new Permission { Nom = "Devise.Create", Categorie = "Devise", Action = "Create", Description = "Créer une devise", Statut = true },
                new Permission { Nom = "Devise.Read", Categorie = "Devise", Action = "Read", Description = "Voir une devise", Statut = true },
                new Permission { Nom = "Devise.ReadAll", Categorie = "Devise", Action = "ReadAll", Description = "Voir toutes les devises", Statut = true },
                new Permission { Nom = "Devise.Update", Categorie = "Devise", Action = "Update", Description = "Modifier une devise", Statut = true },
                new Permission { Nom = "Devise.Delete", Categorie = "Devise", Action = "Delete", Description = "Supprimer une devise", Statut = true },

                // ================================
                // NOTIFICATION PREFERENCE
                // ================================
                new Permission { Nom = "NotificationPreference.Create", Categorie = "NotificationPreference", Action = "Create", Description = "Créer une préférence de notification", Statut = true },
                new Permission { Nom = "NotificationPreference.Read", Categorie = "NotificationPreference", Action = "Read", Description = "Voir une préférence de notification", Statut = true },
                new Permission { Nom = "NotificationPreference.ReadAll", Categorie = "NotificationPreference", Action = "ReadAll", Description = "Voir les préférences de notification", Statut = true },
                new Permission { Nom = "NotificationPreference.Update", Categorie = "NotificationPreference", Action = "Update", Description = "Modifier une préférence de notification", Statut = true },
                new Permission { Nom = "NotificationPreference.Delete", Categorie = "NotificationPreference", Action = "Delete", Description = "Supprimer une préférence de notification", Statut = true },

                // ================================
                // NOTIFICATION 
                // ================================
                new Permission { Nom = "Notification.Create", Categorie = "Notification", Action = "Create", Description = "Créer une préférence de Notification", Statut = true },
                new Permission { Nom = "Notification.Read", Categorie = "Notification", Action = "Read", Description = "Voir une préférence de Notification", Statut = true },
                new Permission { Nom = "Notification.ReadAll", Categorie = "Notification", Action = "ReadAll", Description = "Voir les préférences de Notification", Statut = true },
                new Permission { Nom = "Notification.Update", Categorie = "Notification", Action = "Update", Description = "Modifier une préférence de Notification", Statut = true },
                new Permission { Nom = "Notification.Delete", Categorie = "Notification", Action = "Delete", Description = "Supprimer une préférence de Notification", Statut = true },

                // ================================
                // PAIEMENT - 5 permissions
                // ================================
                new Permission { Nom = "Paiement.Create", Categorie = "Paiement", Action = "Create", Description = "Créer un paiement", Statut = true },
                new Permission { Nom = "Paiement.Read", Categorie = "Paiement", Action = "Read", Description = "Voir un paiement", Statut = true },
                new Permission { Nom = "Paiement.ReadAll", Categorie = "Paiement", Action = "ReadAll", Description = "Voir tous les paiements", Statut = true },
                new Permission { Nom = "Paiement.Update", Categorie = "Paiement", Action = "Update", Description = "Modifier un paiement", Statut = true },
                new Permission { Nom = "Paiement.Delete", Categorie = "Paiement", Action = "Delete", Description = "Supprimer un paiement", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // PERMISSION - 7 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Permission.Create", Categorie = "Permission", Action = "Create", Description = "Créer une permission", Statut = true },
                new Permission { Nom = "Permission.Read", Categorie = "Permission", Action = "Read", Description = "Voir une permission", Statut = true },
                new Permission { Nom = "Permission.ReadAll", Categorie = "Permission", Action = "ReadAll", Description = "Voir toutes les permissions", Statut = true },
                new Permission { Nom = "Permission.Update", Categorie = "Permission", Action = "Update", Description = "Modifier une permission", Statut = true },
                new Permission { Nom = "Permission.Delete", Categorie = "Permission", Action = "Delete", Description = "Supprimer une permission", Statut = true },
                new Permission { Nom = "Permission.Assign", Categorie = "Permission", Action = "Assign", Description = "Assigner une permission à un rôle", Statut = true },
                new Permission { Nom = "Permission.Revoke", Categorie = "Permission", Action = "Revoke", Description = "Retirer une permission d'un rôle", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // UTILISATEUR - 6 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Utilisateur.Create",         Categorie = "Utilisateur", Action = "Create",         Description = "Créer un utilisateur",                     Statut = true },
                new Permission { Nom = "Utilisateur.Read",           Categorie = "Utilisateur", Action = "Read",           Description = "Voir un utilisateur",                      Statut = true },
                new Permission { Nom = "Utilisateur.ReadAll",        Categorie = "Utilisateur", Action = "ReadAll",        Description = "Voir tous les utilisateurs",               Statut = true },
                new Permission { Nom = "Utilisateur.Update",         Categorie = "Utilisateur", Action = "Update",         Description = "Modifier un utilisateur",                  Statut = true },
                new Permission { Nom = "Utilisateur.Delete",         Categorie = "Utilisateur", Action = "Delete",         Description = "Supprimer un utilisateur",                 Statut = true },
                new Permission { Nom = "Utilisateur.ChangePassword", Categorie = "Utilisateur", Action = "ChangePassword", Description = "Changer le mot de passe d'un utilisateur", Statut = true },
                new Permission { Nom = "Utilisateur.DeactivateSelf", Categorie = "Utilisateur", Action = "DeactivateSelf", Description = "Désactiver son propre compte utilisateur", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
               
          
                // ═══════════════════════════════════════════════════════════════════
                // RÔLE - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Role.Create", Categorie = "Role", Action = "Create", Description = "Créer un rôle", Statut = true },
                new Permission { Nom = "Role.Read", Categorie = "Role", Action = "Read", Description = "Voir un rôle", Statut = true },
                new Permission { Nom = "Role.ReadAll", Categorie = "Role", Action = "ReadAll", Description = "Voir tous les rôles", Statut = true },
                new Permission { Nom = "Role.Update", Categorie = "Role", Action = "Update", Description = "Modifier un rôle", Statut = true },
                new Permission { Nom = "Role.Delete", Categorie = "Role", Action = "Delete", Description = "Supprimer un rôle", Statut = true },

              
                // ═══════════════════════════════════════════════════════════════════
                // PLAINTE CLIENT - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "PlainteClient.Create", Categorie = "PlainteClient", Action = "Create", Description = "Créer une plainte client", Statut = true },
                new Permission { Nom = "PlainteClient.Read", Categorie = "PlainteClient", Action = "Read", Description = "Voir une plainte client", Statut = true },
                new Permission { Nom = "PlainteClient.ReadAll", Categorie = "PlainteClient", Action = "ReadAll", Description = "Voir toutes les plaintes clients", Statut = true },
                new Permission { Nom = "PlainteClient.Update", Categorie = "PlainteClient", Action = "Update", Description = "Modifier une plainte client", Statut = true },
                new Permission { Nom = "PlainteClient.Delete", Categorie = "PlainteClient", Action = "Delete", Description = "Supprimer une plainte client", Statut = true },

                // ================================
                // REMBOURSEMENT
                // ================================
                new Permission { Nom = "Remboursement.Create", Categorie = "Remboursement", Action = "Create", Description = "Créer un remboursement", Statut = true },
                new Permission { Nom = "Remboursement.Read", Categorie = "Remboursement", Action = "Read", Description = "Voir un remboursement", Statut = true },
                new Permission { Nom = "Remboursement.ReadAll", Categorie = "Remboursement", Action = "ReadAll", Description = "Voir tous les remboursements", Statut = true },
                new Permission { Nom = "Remboursement.Update", Categorie = "Remboursement", Action = "Update", Description = "Modifier un remboursement", Statut = true },
                new Permission { Nom = "Remboursement.Delete", Categorie = "Remboursement", Action = "Delete", Description = "Supprimer un remboursement", Statut = true },

                // ================================
                // REVERSEMENT SITE (FlexPay PayOut)
                // ================================
                new Permission { Nom = "ReversementSite.Create", Categorie = "ReversementSite", Action = "Create", Description = "Initier un reversement vers un site", Statut = true },
                new Permission { Nom = "ReversementSite.Read", Categorie = "ReversementSite", Action = "Read", Description = "Voir un reversement site", Statut = true },
                new Permission { Nom = "ReversementSite.ReadAll", Categorie = "ReversementSite", Action = "ReadAll", Description = "Voir tous les reversements site", Statut = true },

                // ================================
                // RESERVATION - 5 permissions
                // ================================
                new Permission { Nom = "Reservation.Create", Categorie = "Reservation", Action = "Create", Description = "Créer une réservation", Statut = true },
                new Permission { Nom = "Reservation.Read", Categorie = "Reservation", Action = "Read", Description = "Voir une réservation", Statut = true },
                new Permission { Nom = "Reservation.ReadAll", Categorie = "Reservation", Action = "ReadAll", Description = "Voir toutes les réservations", Statut = true },
                new Permission { Nom = "Reservation.Update", Categorie = "Reservation", Action = "Update", Description = "Modifier une réservation", Statut = true },
                new Permission { Nom = "Reservation.Delete", Categorie = "Reservation", Action = "Delete", Description = "Supprimer une réservation", Statut = true },

                // ================================
                // RESERVATIONPASSAGERS - 5 permissions
                // ================================
                new Permission { Nom = "ReservationPassenger.Create", Categorie = "ReservationPassenger", Action = "Create", Description = "Créer une ReservationPassenger", Statut = true },
                new Permission { Nom = "ReservationPassenger.Read", Categorie = "ReservationPassenger", Action = "Read", Description = "Voir une ReservationPassenger", Statut = true },
                new Permission { Nom = "ReservationPassenger.ReadAll", Categorie = "ReservationPassenger", Action = "ReadAll", Description = "Voir toutes les ReservationPassenger", Statut = true },
                new Permission { Nom = "ReservationPassenger.Update", Categorie = "ReservationPassenger", Action = "Update", Description = "Modifier une ReservationPassenger", Statut = true },
                new Permission { Nom = "ReservationPassenger.Delete", Categorie = "ReservationPassenger", Action = "Delete", Description = "Supprimer une ReservationPassenger", Statut = true },

                // ================================
                // Role - 5 permissions
                // ================================
                new Permission { Nom = "Role.Create", Categorie = "Role", Action = "Create", Description = "Créer un Role", Statut = true },
                new Permission { Nom = "Role.Read", Categorie = "Role", Action = "Read", Description = "Voir un Role", Statut = true },
                new Permission { Nom = "Role.ReadAll", Categorie = "Role", Action = "ReadAll", Description = "Voir tous les Role", Statut = true },
                new Permission { Nom = "Role.Update", Categorie = "Role", Action = "Update", Description = "Modifier un Role", Statut = true },
                new Permission { Nom = "Role.Delete", Categorie = "Role", Action = "Delete", Description = "Supprimer un Role", Statut = true },

                // ================================
                // RolePermission - 5 permissions
                // ================================
                new Permission { Nom = "RolePermission.Create", Categorie = "RolePermission", Action = "Create", Description = "Créer un RolePermission", Statut = true },
                new Permission { Nom = "RolePermission.Read", Categorie = "RolePermission", Action = "Read", Description = "Voir un RolePermission", Statut = true },
                new Permission { Nom = "RolePermission.ReadAll", Categorie = "RolePermission", Action = "ReadAll", Description = "Voir tous les RolePermission", Statut = true },
                new Permission { Nom = "RolePermission.Update", Categorie = "RolePermission", Action = "Update", Description = "Modifier un RolePermission", Statut = true },
                new Permission { Nom = "RolePermission.Delete", Categorie = "RolePermission", Action = "Delete", Description = "Supprimer un RolePermission", Statut = true },

                // ================================
                // SIEGE - (Place)
                // ================================
                new Permission { Nom = "Siege.Create", Categorie = "Siege", Action = "Create", Description = "Créer un Siege", Statut = true },
                new Permission { Nom = "Siege.Read", Categorie = "Siege", Action = "Read", Description = "Voir un Siege", Statut = true },
                new Permission { Nom = "Siege.ReadAll", Categorie = "Siege", Action = "ReadAll", Description = "Voir les Siege", Statut = true },
                new Permission { Nom = "Siege.Update", Categorie = "Siege", Action = "Update", Description = "Modifier un Siege", Statut = true },
                new Permission { Nom = "Siege.Delete", Categorie = "Siege", Action = "Delete", Description = "Supprimer un Siege", Statut = true },

                // ================================
                // SITE - sites opérationnels par société
                // ================================
                new Permission { Nom = "Site.Create", Categorie = "Site", Action = "Create", Description = "Créer un site", Statut = true },
                new Permission { Nom = "Site.Read", Categorie = "Site", Action = "Read", Description = "Voir un site", Statut = true },
                new Permission { Nom = "Site.ReadAll", Categorie = "Site", Action = "ReadAll", Description = "Voir les sites", Statut = true },
                new Permission { Nom = "Site.Update", Categorie = "Site", Action = "Update", Description = "Modifier un site", Statut = true },
                new Permission { Nom = "Site.Delete", Categorie = "Site", Action = "Delete", Description = "Supprimer un site", Statut = true },


                // ═══════════════════════════════════════════════════════════════════
                // Societe - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Societe.Create",  Categorie = "Societe", Action = "Create",  Description = "Créer une Societe", Statut = true },
                new Permission { Nom = "Societe.Read",    Categorie = "Societe", Action = "Read",    Description = "Voir les informations d'une Societe", Statut = true },
                new Permission { Nom = "Societe.ReadAll", Categorie = "Societe", Action = "ReadAll", Description = "Voir toutes les Societes", Statut = true },
                new Permission { Nom = "Societe.Update",  Categorie = "Societe", Action = "Update",  Description = "Modifier une Societe", Statut = true },
                new Permission { Nom = "Societe.Delete",  Categorie = "Societe", Action = "Delete",  Description = "Supprimer une Societe", Statut = true },

                new Permission { Nom = "ConfigSociete.Read", Categorie = "ConfigSociete", Action = "Read", Description = "Lire la configuration métier d'une société", Statut = true },
                new Permission { Nom = "ConfigSociete.Update", Categorie = "ConfigSociete", Action = "Update", Description = "Modifier la configuration métier d'une société", Statut = true },

                
                // ================================
                // TAUX DE CHANGE
                // ================================
                new Permission { Nom = "TauxChange.Create", Categorie = "TauxChange", Action = "Create", Description = "Créer un taux de change", Statut = true },
                new Permission { Nom = "TauxChange.Read", Categorie = "TauxChange", Action = "Read", Description = "Voir un taux de change", Statut = true },
                new Permission { Nom = "TauxChange.ReadAll", Categorie = "TauxChange", Action = "ReadAll", Description = "Voir les taux de change", Statut = true },
                
                // ================================
                // TYPE VEHICULE - 5 permissions
                // ================================
                new Permission { Nom = "TypeVehicule.Create", Categorie = "TypeVehicule", Action = "Create", Description = "Créer un type de véhicule", Statut = true },
                new Permission { Nom = "TypeVehicule.Read", Categorie = "TypeVehicule", Action = "Read", Description = "Voir un type de véhicule", Statut = true },
                new Permission { Nom = "TypeVehicule.ReadAll", Categorie = "TypeVehicule", Action = "ReadAll", Description = "Voir tous les types de véhicule", Statut = true },
                new Permission { Nom = "TypeVehicule.Update", Categorie = "TypeVehicule", Action = "Update", Description = "Modifier un type de véhicule", Statut = true },
                new Permission { Nom = "TypeVehicule.Delete", Categorie = "TypeVehicule", Action = "Delete", Description = "Supprimer un type de véhicule", Statut = true },

                // ================================
                // UserPermissions - 5 permissions
                // ================================
                new Permission { Nom = "UserPermission.Create", Categorie = "UserPermission", Action = "Create", Description = "Créer un type de UserPermission", Statut = true },
                new Permission { Nom = "UserPermission.Read", Categorie = "UserPermission", Action = "Read", Description = "Voir un type de UserPermission", Statut = true },
                new Permission { Nom = "UserPermission.ReadAll", Categorie = "UserPermission", Action = "ReadAll", Description = "Voir tous les types de UserPermission", Statut = true },
                new Permission { Nom = "UserPermission.Update", Categorie = "UserPermission", Action = "Update", Description = "Modifier un type de UserPermission", Statut = true },
                new Permission { Nom = "UserPermission.Delete", Categorie = "UserPermission", Action = "Delete", Description = "Supprimer un type de UserPermission", Statut = true },

                // ================================
                // UserRole - 5 permissions
                // ================================
                new Permission { Nom = "UserRole.Create", Categorie = "UserRole", Action = "Create", Description = "Créer un   UserRole", Statut = true },
                new Permission { Nom = "UserRole.Read", Categorie = "UserRole", Action = "Read", Description = "Voir un  UserRole", Statut = true },
                new Permission { Nom = "UserRole.ReadAll", Categorie = "UserRole", Action = "ReadAll", Description = "Voir tous les   UserRole", Statut = true },
                new Permission { Nom = "UserRole.Update", Categorie = "UserRole", Action = "Update", Description = "Modifier un   UserRole", Statut = true },
                new Permission { Nom = "UserRole.Delete", Categorie = "UserRole", Action = "Delete", Description = "Supprimer un  UserRole", Statut = true },

                // ================================
                // Utilisateur - 5 permissions
                // ================================
                new Permission { Nom = "Utilisateur.Create", Categorie = "Utilisateur", Action = "Create", Description = "Créer un  Utilisateur", Statut = true },
                new Permission { Nom = "Utilisateur.Read", Categorie = "Utilisateur", Action = "Read", Description = "Voir un  Utilisateur", Statut = true },
                new Permission { Nom = "Utilisateur.ReadAll", Categorie = "Utilisateur", Action = "ReadAll", Description = "Voir tous les  Utilisateur", Statut = true },
                new Permission { Nom = "Utilisateur.Update", Categorie = "Utilisateur", Action = "Update", Description = "Modifier un  Utilisateur", Statut = true },
                new Permission { Nom = "Utilisateur.Delete", Categorie = "Utilisateur", Action = "Delete", Description = "Supprimer un  Utilisateur", Statut = true },
                new Permission { Nom = "Utilisateur.DeactivateSelf", Categorie = "Utilisateur", Action = "DeactivateSelf", Description = "Désactiver son propre compte utilisateur", Statut = true },


                // ================================
                // VEHICULE - 5 permissions
                // ================================
                new Permission { Nom = "Vehicule.Create", Categorie = "Vehicule", Action = "Create", Description = "Créer un véhicule", Statut = true },
                new Permission { Nom = "Vehicule.Read", Categorie = "Vehicule", Action = "Read", Description = "Voir un véhicule", Statut = true },
                new Permission { Nom = "Vehicule.ReadAll", Categorie = "Vehicule", Action = "ReadAll", Description = "Voir tous les véhicules", Statut = true },
                new Permission { Nom = "Vehicule.Update", Categorie = "Vehicule", Action = "Update", Description = "Modifier un véhicule", Statut = true },
                new Permission { Nom = "Vehicule.Delete", Categorie = "Vehicule", Action = "Delete", Description = "Supprimer un véhicule", Statut = true },

                // ================================
                // VOYAGE - 5 permissions
                // ================================
                new Permission { Nom = "Voyage.Create", Categorie = "Voyage", Action = "Create", Description = "Créer un voyage", Statut = true },
                new Permission { Nom = "Voyage.Read", Categorie = "Voyage", Action = "Read", Description = "Voir un voyage", Statut = true },
                new Permission { Nom = "Voyage.ReadAll", Categorie = "Voyage", Action = "ReadAll", Description = "Voir tous les voyages", Statut = true },
                new Permission { Nom = "Voyage.Update", Categorie = "Voyage", Action = "Update", Description = "Modifier un voyage", Statut = true },
                new Permission { Nom = "Voyage.Delete", Categorie = "Voyage", Action = "Delete", Description = "Supprimer un voyage", Statut = true },

                new Permission { Nom = "FeuilleDeRoute.Generer", Categorie = "FeuilleDeRoute", Action = "Generer", Description = "Générer une feuille de route", Statut = true },
                new Permission { Nom = "FeuilleDeRoute.Read", Categorie = "FeuilleDeRoute", Action = "Read", Description = "Consulter les feuilles de route", Statut = true },

           
                // ================================
                // VoyageDestination - 5 permissions
                // ================================
                new Permission { Nom = "VoyageDestination.Create", Categorie = "VoyageDestination", Action = "Create", Description = "Créer un VoyageDestination", Statut = true },
                new Permission { Nom = "VoyageDestination.Read", Categorie = "VoyageDestination", Action = "Read", Description = "Voir un VoyageDestination", Statut = true },
                new Permission { Nom = "VoyageDestination.ReadAll", Categorie = "VoyageDestination", Action = "ReadAll", Description = "Voir tous les VoyageDestination", Statut = true },
                new Permission { Nom = "VoyageDestination.Update", Categorie = "VoyageDestination", Action = "Update", Description = "Modifier un VoyageDestination", Statut = true },
                new Permission { Nom = "VoyageDestination.Delete", Categorie = "VoyageDestination", Action = "Delete", Description = "Supprimer un VoyageDestination", Statut = true },

          
                // ================================
                // VoyageSeatAllocation - 5 permissions
                // ================================
                new Permission { Nom = "VoyageSeatAllocation.Create", Categorie = "VoyageSeatAllocation", Action = "Create", Description = "Créer un VoyageSeatAllocation", Statut = true },
                new Permission { Nom = "VoyageSeatAllocation.Read", Categorie = "VoyageSeatAllocation", Action = "Read", Description = "Voir un VoyageSeatAllocation", Statut = true },
                new Permission { Nom = "VoyageSeatAllocation.ReadAll", Categorie = "VoyageSeatAllocation", Action = "ReadAll", Description = "Voir tous les VoyageSeatAllocation", Statut = true },
                new Permission { Nom = "VoyageSeatAllocation.Update", Categorie = "VoyageSeatAllocation", Action = "Update", Description = "Modifier un VoyageSeatAllocation", Statut = true },
                new Permission { Nom = "VoyageSeatAllocation.Delete", Categorie = "VoyageSeatAllocation", Action = "Delete", Description = "Supprimer un VoyageSeatAllocation", Statut = true },


                // ================================
                // VoyageTarifCategorieSiege - 5 permissions
                // ================================
                new Permission { Nom = "VoyageTarifCategorieSiege.Create", Categorie = "VoyageTarifCategorieSiege", Action = "Create", Description = "Créer un VoyageTarifCategorieSiege", Statut = true },
                new Permission { Nom = "VoyageTarifCategorieSiege.Read", Categorie = "VoyageTarifCategorieSiege", Action = "Read", Description = "Voir un VoyageTarifCategorieSiege", Statut = true },
                new Permission { Nom = "VoyageTarifCategorieSiege.ReadAll", Categorie = "VoyageTarifCategorieSiege", Action = "ReadAll", Description = "Voir tous les VoyageTarifCategorieSiege", Statut = true },
                new Permission { Nom = "VoyageTarifCategorieSiege.Update", Categorie = "VoyageTarifCategorieSiege", Action = "Update", Description = "Modifier un VoyageTarifCategorieSiege", Statut = true },
                new Permission { Nom = "VoyageTarifCategorieSiege.Delete", Categorie = "VoyageTarifCategorieSiege", Action = "Delete", Description = "Supprimer un VoyageTarifCategorieSiege", Statut = true },


             
                // ================================
                // METRICS / AUDIT / REPORTING / DASHBOARDS (lecture)
                // ================================
                new Permission { Nom = "Metrics.ReadAll", Categorie = "Metrics", Action = "ReadAll", Description = "Voir les métriques", Statut = true },
                new Permission { Nom = "Audit.ReadAll", Categorie = "Audit", Action = "ReadAll", Description = "Voir les audits", Statut = true },
                new Permission { Nom = "Audit.DetectSuspicious", Categorie = "Audit", Action = "DetectSuspicious", Description = "Détecter les activités suspectes", Statut = true },
                new Permission { Nom = "Dashboard.ReadAll", Categorie = "Dashboard", Action = "ReadAll", Description = "Voir le dashboard", Statut = true },
                new Permission { Nom = "ClientDashboard.ReadAll", Categorie = "ClientDashboard", Action = "ReadAll", Description = "Voir le dashboard client", Statut = true },
                new Permission { Nom = "FinanceReporting.ReadAll", Categorie = "FinanceReporting", Action = "ReadAll", Description = "Voir les rapports financiers", Statut = true },
                new Permission { Nom = "Statistiques.ReadAll", Categorie = "Statistiques", Action = "ReadAll", Description = "Voir les statistiques", Statut = true },

                // ================================
                // SYNC (exécution)
                // ================================
                new Permission { Nom = "Sync.Execute", Categorie = "Sync", Action = "Execute", Description = "Exécuter la synchronisation", Statut = true },

                // ================================
                // EVENEMENT (billetterie événementielle V1)
                // ================================
                new Permission { Nom = "Evenement.Session.Read", Categorie = "Evenement", Action = "Session.Read", Description = "Lister / consulter sessions et disponibilités événement", Statut = true },
                new Permission { Nom = "Evenement.Session.Write", Categorie = "Evenement", Action = "Session.Write", Description = "Créer, publier ou fermer une session événement", Statut = true },
                new Permission { Nom = "Evenement.Hold.Create", Categorie = "Evenement", Action = "Hold.Create", Description = "Créer un hold réservation événement", Statut = true },
                new Permission { Nom = "Evenement.Reservation.Confirm", Categorie = "Evenement", Action = "Reservation.Confirm", Description = "Confirmer paiement ou annuler une réservation événement", Statut = true },
                new Permission { Nom = "Evenement.Ticket.Check", Categorie = "Evenement", Action = "Ticket.Check", Description = "Vérifier un ticket événement (contrôle entrée)", Statut = true },
                new Permission { Nom = "Evenement.Ticket.Use", Categorie = "Evenement", Action = "Ticket.Use", Description = "Marquer un ticket événement comme utilisé", Statut = true },
                new Permission { Nom = "Evenement.Dashboard.Read", Categorie = "Evenement", Action = "Dashboard.Read", Description = "Consulter le dashboard billetterie événement", Statut = true },
            };
        }

        /// <summary>
        /// Assigne les permissions aux rôles appropriés
        /// </summary>
        private static async Task AssignPermissionsToRolesAsync(CongoTravelDbContext context)
        {
            // Récupérer les rôles existants
            var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Super-Admin");
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Admin");
            var gerantRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Gerant");
            var financierRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Financier"); // ✨ Changé de Comptable à Financier
            var caissierRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Caissier");
            var clientRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Client");
         
            if (superAdminRole == null)
            {
                Console.WriteLine(" Rôles non trouvés. Les permissions seront créées mais non assignées.");
                Console.WriteLine(" Vous devrez assigner manuellement les permissions aux rôles.");
                return;
            }

            // Récupérer toutes les permissions
            var allPermissions = await context.Permissions.ToListAsync();

            // ═══════════════════════════════════════════════════════════════════
            //  SUPER-ADMIN : TOUTES LES PERMISSIONS (Root User - Aucune restriction)
            // ═══════════════════════════════════════════════════════════════════
            if (superAdminRole != null)
            {
                // Vérifier les permissions déjà assignées
                var existingSuperAdminPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == superAdminRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var permissionsToAdd = allPermissions
                    .Where(p => !existingSuperAdminPermissions.Contains(p.IdPermission))
                    .ToList();
                
                foreach (var permission in permissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = superAdminRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (permissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {permissionsToAdd.Count} permissions assignées à Super-Admin (Root - Aucune restriction)");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Super-Admin ({existingSuperAdminPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            //  ADMIN : Gestion complète de sa societe (sauf création/suppression de la societe)
            // ═══════════════════════════════════════════════════════════════════
            if (adminRole != null)
            {
                var adminPermissions = allPermissions.Where(p =>
                    // Écoles : Lecture et modification uniquement (pas création/suppression)
                    (p.Categorie == "Societe" && (p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Update")) ||
                    // Gestion complète de son école
                    p.Categorie == "Utilisateur" ||
                    p.Categorie == "Agent" ||
                    p.Categorie == "Client" ||
                    p.Categorie == "PlainteClient" ||
                    p.Categorie == "CommunicationCampaign" ||
                    // NOUVELLES ENTITÉS DE TRANSPORT - Gestion complète pour l'Admin
                    p.Categorie == "Voyage" ||
                    p.Categorie == "FeuilleDeRoute" ||
                    p.Categorie == "TypeVehicule" ||
                    p.Categorie == "Reservation" ||
                    p.Categorie == "Paiement" ||
                    p.Categorie == "Destination" ||
                    p.Categorie == "Vehicule" ||
                    p.Categorie == "Billet" ||
                    p.Categorie == "Site" ||
                    p.Categorie == "ConfigSociete" ||
                    // Modules additionnels
                    p.Categorie == "Devise" ||
                    p.Categorie == "TauxChange" ||
                    p.Categorie == "Remboursement" ||
                    p.Categorie == "ReversementSite" ||
                    p.Categorie == "CategorieSiege" ||
                    p.Categorie == "NotificationPreference" ||
                    p.Categorie == "Metrics" ||
                    p.Categorie == "Audit" ||
                    p.Categorie == "Dashboard" ||
                    p.Categorie == "FinanceReporting" ||
                    p.Categorie == "Statistiques" ||
                    p.Categorie == "Sync" ||
                    p.Categorie == "Evenement"
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingAdminPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == adminRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var adminPermissionsToAdd = adminPermissions
                    .Where(p => !existingAdminPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in adminPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = adminRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (adminPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {adminPermissionsToAdd.Count} nouvelles permissions assignées à Admin");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Admin ({existingAdminPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            //  GERANT : Mêmes permissions que Admin - Gestion complète de sa société
            // Peut créer des utilisateurs sauf Admin et Super-Admin (vérifié au niveau métier)
            // ═══════════════════════════════════════════════════════════════════
            if (gerantRole != null)
            {
                // Prendre toutes les permissions de Admin + nouvelles entités de transport
                var gerantPermissions = allPermissions.Where(p =>
                    // Societe : Lecture et modification uniquement (pas création/suppression)
                    (p.Categorie == "Societe" && (p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Update")) ||
                    // Gestion complète de son societe
                    p.Categorie == "Utilisateur" ||
                    p.Categorie == "Agent" ||
                    p.Categorie == "Client" ||
                    // Factures : Gestion complète
                    p.Categorie == "PlainteClient" ||
                    p.Categorie == "CommunicationCampaign" ||
                    // NOUVELLES ENTITÉS DE TRANSPORT - Gestion complète pour le Gerant
                    p.Categorie == "Voyage" ||
                    p.Categorie == "FeuilleDeRoute" ||
                    p.Categorie == "TypeVehicule" ||
                    p.Categorie == "Reservation" ||
                    // Paiements : Gestion complète
                    p.Categorie == "Paiement" ||
                    p.Categorie == "Destination" ||
                    p.Categorie == "Vehicule" ||
                    p.Categorie == "Billet" ||
                    p.Categorie == "Site" ||
                    p.Categorie == "ConfigSociete" ||
                    // Modules additionnels
                    p.Categorie == "Devise" ||
                    p.Categorie == "TauxChange" ||
                    p.Categorie == "Remboursement" ||
                    p.Categorie == "ReversementSite" ||
                    p.Categorie == "CategorieSiege" ||
                    p.Categorie == "NotificationPreference" ||
                    p.Categorie == "Metrics" ||
                    p.Categorie == "Audit" ||
                    p.Categorie == "Dashboard" ||
                    p.Categorie == "FinanceReporting" ||
                    p.Categorie == "Statistiques" ||
                    p.Categorie == "Sync" ||
                    p.Categorie == "Evenement"
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingGerantPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == gerantRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var gerantPermissionsToAdd = gerantPermissions
                    .Where(p => !existingGerantPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in gerantPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = gerantRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (gerantPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {gerantPermissionsToAdd.Count} nouvelles permissions assignées à Gerant");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Gerant ({existingGerantPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            //  CAISSIER : Gestion des paiements et transactions
            // ═══════════════════════════════════════════════════════════════════
            if (caissierRole != null)
            {
                var caissierPermissions = allPermissions.Where(p =>
                    // Factures : Création et lecture uniquement (PAS modification ni suppression)
                    (p.Categorie == "Facture" && p.Action != "Update" && p.Action != "Delete") ||
                    // Clients : Lecture seule (pour vérifier les factures)
                    (p.Categorie == "Client" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Sites : lecture seule (point de vente)
                    (p.Categorie == "Site" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Devises : lecture seule
                    (p.Categorie == "Devise" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Taux : lecture seule
                    (p.Categorie == "TauxChange" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Remboursements : création/lecture (caisse)
                    (p.Categorie == "Remboursement" && (p.Action == "Create" || p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Feuille de route : génération et consultation (embarquement)
                    p.Categorie == "FeuilleDeRoute" ||
                    // Événementiel : caisse / point de vente (pas gestion sessions)
                    p.Nom == "Evenement.Session.Read" ||
                    p.Nom == "Evenement.Hold.Create" ||
                    p.Nom == "Evenement.Reservation.Confirm" ||
                    p.Nom == "Evenement.Ticket.Check" ||
                    p.Nom == "Evenement.Ticket.Use"
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingCaissierPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == caissierRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var caissierPermissionsToAdd = caissierPermissions
                    .Where(p => !existingCaissierPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in caissierPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = caissierRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (caissierPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {caissierPermissionsToAdd.Count} nouvelles permissions assignées à Caissier");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Caissier ({existingCaissierPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            //  FINANCIER : Gestion financière (Paiements)
            // Peut créer et lire les paiements, mais PAS modifier ni supprimer
            // ═══════════════════════════════════════════════════════════════════
            if (financierRole != null)
            {
                var financierPermissions = allPermissions.Where(p =>
                    // Factures : Création et lecture uniquement (PAS modification ni suppression)
                    (p.Categorie == "Facture" && p.Action != "Update" && p.Action != "Delete") ||
                    // Clients : Lecture seule (pour vérifier les factures)
                    (p.Categorie == "Client" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Catégorie Clients : Lecture seule
                    (p.Categorie == "CategorieClient" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // NOUVELLES ENTITÉS DE TRANSPORT - Accès limité pour le Financier
                    // Voyages : Lecture seule (pour vérifier les paiements associés)
                    (p.Categorie == "Voyage" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Feuille de route : consultation seule
                    (p.Categorie == "FeuilleDeRoute" && p.Action == "Read") ||
                    // TypeVehicule : Lecture seule
                    (p.Categorie == "TypeVehicule" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Reservations : Lecture seule (pour vérifier les paiements)
                    (p.Categorie == "Reservation" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Paiements : Gestion complète (création, lecture, modification, suppression)
                    p.Categorie == "Paiement" ||
                    // Destinations : Lecture seule
                    (p.Categorie == "Destination" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Vehicule : Lecture seule (pour vérifier les opérations)
                    (p.Categorie == "Vehicule" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Billets : Lecture seule (pour vérifier les paiements associés)
                    (p.Categorie == "Billet" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Sites : lecture seule
                    (p.Categorie == "Site" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Devises : lecture seule
                    (p.Categorie == "Devise" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Taux : lecture seule
                    (p.Categorie == "TauxChange" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Remboursements : gestion complète
                    p.Categorie == "Remboursement" ||
                    // Reversements site : initiation et consultation
                    (p.Categorie == "ReversementSite" && p.Action != "ReadAll") ||
                    // Reporting : lecture
                    (p.Categorie == "Dashboard" && p.Action == "ReadAll") ||
                    (p.Categorie == "FinanceReporting" && p.Action == "ReadAll") ||
                    (p.Categorie == "Statistiques" && p.Action == "ReadAll") ||
                    // Événementiel : consultation et confirmation paiement
                    p.Nom == "Evenement.Session.Read" ||
                    p.Nom == "Evenement.Reservation.Confirm" ||
                    p.Nom == "Evenement.Ticket.Check" ||
                    p.Nom == "Evenement.Dashboard.Read"
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingFinancierPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == financierRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var financierPermissionsToAdd = financierPermissions
                    .Where(p => !existingFinancierPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in financierPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = financierRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (financierPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {financierPermissionsToAdd.Count} nouvelles permissions assignées à Financier");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Financier ({existingFinancierPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            // 🔵 CLIENT : CongoTravel transport (profil, réservations, billets)
            // ═══════════════════════════════════════════════════════════════════
            if (clientRole != null)
            {
                var clientPermissions = allPermissions.Where(p =>
                    (p.Categorie == "Client" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    (p.Categorie == "PlainteClient" && (p.Action == "Create" || p.Action == "Read" || p.Action == "ReadAll")) ||
                    (p.Categorie == "ClientDashboard" && p.Action == "ReadAll") ||
                    (p.Categorie == "Reservation" && (p.Action == "Create" || p.Action == "Read" || p.Action == "ReadAll")) ||
                    (p.Categorie == "Paiement" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    (p.Categorie == "Billet" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    (p.Categorie == "Voyage" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    (p.Categorie == "Destination" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Événementiel : réservation en ligne (pas contrôle entrée ni admin session)
                    p.Nom == "Evenement.Session.Read" ||
                    p.Nom == "Evenement.Hold.Create"
                ).ToList();

                var legacyClientPermissionIds = allPermissions
                    .Where(p => p.Categorie == "Facture" || p.Categorie == "CategorieClient")
                    .Select(p => p.IdPermission)
                    .ToHashSet();

                var legacyToRemove = await context.RolePermissions
                    .Where(rp => rp.IdRole == clientRole.IdRole && legacyClientPermissionIds.Contains(rp.IdPermission))
                    .ToListAsync();
                if (legacyToRemove.Count > 0)
                {
                    context.RolePermissions.RemoveRange(legacyToRemove);
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {legacyToRemove.Count} permission(s) legacy retirée(s) du rôle Client");
                }

                // Vérifier les permissions déjà assignées
                var existingClientPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == clientRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var permissionsToAdd = clientPermissions
                    .Where(p => !existingClientPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in permissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = clientRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (permissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {permissionsToAdd.Count} permissions assignées à Client");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Client ({existingClientPermissions.Count} permissions)");
                }
            }

            // Utilisateur.DeactivateSelf — tous les rôles (auto-désactivation compte)
            var deactivateSelfPermission = await context.Permissions
                .FirstOrDefaultAsync(p => p.Nom == "Utilisateur.DeactivateSelf");
            if (deactivateSelfPermission != null)
            {
                var allRoles = await context.Roles.Where(r => r.Statut == true).ToListAsync();
                foreach (var role in allRoles)
                {
                    var alreadyAssigned = await context.RolePermissions.AnyAsync(rp =>
                        rp.IdRole == role.IdRole && rp.IdPermission == deactivateSelfPermission.IdPermission);
                    if (!alreadyAssigned)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            IdRole = role.IdRole,
                            IdPermission = deactivateSelfPermission.IdPermission,
                            DateAttribution = DateTime.UtcNow
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}

