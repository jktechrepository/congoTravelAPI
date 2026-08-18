using CongoTravel.Models;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.SiteTouristique;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace CongoTravel.Data
{
    public partial class CongoTravelDbContext : DbContext
    {
        public CongoTravelDbContext(DbContextOptions<CongoTravelDbContext> options)
            : base(options)
        {
        }

        // DbSets pour les modèles conservés
        public DbSet<Societe> Societes { get; set; }
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }
        public DbSet<SmsLog> SmsLogs { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<CommunicationCampaign> CommunicationCampaigns { get; set; }
        public DbSet<PlainteClient> PlainteClients { get; set; }
        // Les fonctionnalités de crash ne sont plus disponibles après la refactorisation
        public DbSet<Destination> Destinations { get; set; }
        public DbSet<Vehicule> Vehicules { get; set; }
        public DbSet<PhotoVehicule> PhotoVehicules { get; set; }
        public DbSet<TypeVehicule> TypeVehicules { get; set; }
        public DbSet<Voyage> Voyages { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Billet> Billets { get; set; }
        public DbSet<BilletEmbarquement> BilletEmbarquements { get; set; }
        public DbSet<FeuilleDeRoute> FeuilleDeRoutes { get; set; }
        public DbSet<FeuilleDeRoutePassager> FeuilleDeRoutePassagers { get; set; }
        public DbSet<Paiement> Paiements { get; set; }
        public DbSet<DeviseMonetaire> DevisesMonetaires { get; set; }
        public DbSet<TauxChange> TauxChanges { get; set; }
        public DbSet<Remboursement> Remboursements { get; set; }
        public DbSet<Siege> Sieges { get; set; }
        public DbSet<VoyageDestination> VoyageDestinations { get; set; }
        public DbSet<ReservationPassenger> ReservationPassengers { get; set; }
        public DbSet<VoyageSeatAllocation> VoyageSeatAllocations { get; set; }
        public DbSet<CategorieSiege> CategorieSieges { get; set; }
        public DbSet<VoyageTarifCategorieSiege> VoyageTarifsCategorieSiege { get; set; }
        public DbSet<PlanificationVoyage> PlanificationsVoyage { get; set; }
        public DbSet<PlanificationVoyageEtape> PlanificationVoyageEtapes { get; set; }
        public DbSet<PlanificationVoyageTarif> PlanificationVoyageTarifs { get; set; }
        public DbSet<PlanificationGenerationLog> PlanificationGenerationLogs { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<SiegeHoldEnAttente> SiegeHoldsEnAttente { get; set; }
        public DbSet<CommandeReservationEnAttente> CommandesReservationEnAttente { get; set; }
        public DbSet<InfoPaiementSociete> InfoPaiementsSociete { get; set; }
        public DbSet<ConfigSociete> ConfigSocietes { get; set; }
        public DbSet<TransactionFlexPay> TransactionsFlexPay { get; set; }
        public DbSet<CallbackFlexPay> CallbacksFlexPay { get; set; }
        public DbSet<ReversementSite> ReversementsSite { get; set; }

        public DbSet<EvenementSession> EvenementSessions { get; set; }
        public DbSet<EvenementClasse> EvenementClasses { get; set; }
        public DbSet<EvenementSessionSection> EvenementSessionSections { get; set; }
        public DbSet<EvenementSessionGlobalQuota> EvenementSessionGlobalQuotas { get; set; }
        public DbSet<EvenementSessionClassQuota> EvenementSessionClassQuotas { get; set; }
        public DbSet<EvenementSessionSeat> EvenementSessionSeats { get; set; }
        public DbSet<EvenementReservation> EvenementReservations { get; set; }
        public DbSet<EvenementReservationLine> EvenementReservationLines { get; set; }
        public DbSet<EvenementTicket> EvenementTickets { get; set; }
        public DbSet<EvenementPayment> EvenementPayments { get; set; }
        public DbSet<EvenementSessionPhoto> EvenementSessionPhotos { get; set; }

        public DbSet<SiteTouristiqueLieu> SiteTouristiques { get; set; }
        public DbSet<SiteTouristiqueLieuPhoto> SiteTouristiqueLieuPhotos { get; set; }
        public DbSet<SiteTouristiqueClasse> SiteTouristiqueClasses { get; set; }
        public DbSet<SiteTouristiqueJournee> SiteTouristiqueJournees { get; set; }
        public DbSet<SiteTouristiqueGlobalQuota> SiteTouristiqueGlobalQuotas { get; set; }
        public DbSet<SiteTouristiqueClassQuota> SiteTouristiqueClassQuotas { get; set; }
        public DbSet<SiteTouristiqueReservation> SiteTouristiqueReservations { get; set; }
        public DbSet<SiteTouristiqueReservationLine> SiteTouristiqueReservationLines { get; set; }
        public DbSet<SiteTouristiqueTicket> SiteTouristiqueTickets { get; set; }
        public DbSet<SiteTouristiquePayment> SiteTouristiquePayments { get; set; }
        public DbSet<SiteTouristiquePlanification> SiteTouristiquePlanifications { get; set; }
        public DbSet<SiteTouristiquePlanifGlobalQuota> SiteTouristiquePlanifGlobalQuotas { get; set; }
        public DbSet<SiteTouristiquePlanifClassQuota> SiteTouristiquePlanifClassQuotas { get; set; }
        public DbSet<SiteTouristiquePlanifGenerationLog> SiteTouristiquePlanifGenerationLogs { get; set; }

        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<RestaurantPhoto> RestaurantPhotos { get; set; }
        public DbSet<RestaurantZone> RestaurantZones { get; set; }
        public DbSet<RestaurantCreneau> RestaurantCreneaux { get; set; }
        public DbSet<RestaurantCreneauGlobalQuota> RestaurantCreneauGlobalQuotas { get; set; }
        public DbSet<RestaurantCreneauZoneQuota> RestaurantCreneauZoneQuotas { get; set; }
        public DbSet<RestaurantReservation> RestaurantReservations { get; set; }
        public DbSet<RestaurantReservationLine> RestaurantReservationLines { get; set; }
        public DbSet<RestaurantTicket> RestaurantTickets { get; set; }
        public DbSet<RestaurantPayment> RestaurantPayments { get; set; }
        public DbSet<RestaurantPlanification> RestaurantPlanifications { get; set; }
        public DbSet<RestaurantPlanificationPlage> RestaurantPlanificationPlages { get; set; }
        public DbSet<RestaurantPlanifPlageGlobalQuota> RestaurantPlanifPlageGlobalQuotas { get; set; }
        public DbSet<RestaurantPlanifPlageZoneQuota> RestaurantPlanifPlageZoneQuotas { get; set; }
        public DbSet<RestaurantPlanifGenerationLog> RestaurantPlanifGenerationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration Utilisateur
            modelBuilder.Entity<Utilisateur>()
                .HasOne(u => u.Societe)
                .WithMany(e => e.Utilisateurs)
                .HasForeignKey(u => u.IdSociete)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Utilisateur>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Utilisateurs)
                .HasForeignKey(u => u.IdRole)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Utilisateur>()
                .Property(u => u.IdRole)
                .IsRequired(false);

            modelBuilder.Entity<Utilisateur>()
                .HasOne(u => u.Agent)
                .WithMany(a => a.Utilisateurs)
                .HasForeignKey(u => u.IdAgent)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Utilisateur>()
                .HasOne(u => u.Client)
                .WithMany(c => c.Utilisateurs)
                .HasForeignKey(u => u.IdClient)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Utilisateur>()
                .HasOne(u => u.Site)
                .WithMany()
                .HasForeignKey(u => u.IdSite)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Utilisateur>()
                .HasIndex(u => u.IdSite)
                .HasDatabaseName("IX_Utilisateurs_IdSite");

            // Index unique sur l'email
            modelBuilder.Entity<Utilisateur>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Utilisateurs_Email_Unique");

            modelBuilder.Entity<Utilisateur>()
                .Property(u => u.AuthProvider)
                .HasMaxLength(32);

            modelBuilder.Entity<Utilisateur>()
                .Property(u => u.ExternalSubjectId)
                .HasMaxLength(128);

            modelBuilder.Entity<Utilisateur>()
                .HasIndex(u => new { u.AuthProvider, u.ExternalSubjectId })
                .IsUnique()
                .HasDatabaseName("IX_Utilisateurs_AuthProvider_ExternalSubjectId");

            // Configuration Agent
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.Societe)
                .WithMany(e => e.Agents)
                .HasForeignKey(a => a.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Agent>()
                .HasOne(a => a.Site)
                .WithMany()
                .HasForeignKey(a => a.IdSite)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Agent>()
                .HasIndex(a => a.IdSite)
                .HasDatabaseName("IX_Agents_IdSite");

            modelBuilder.Entity<Agent>(entity =>
            {
                entity.Property(e => e.Matricule).IsRequired(false);
                entity.Property(e => e.NomComplet).IsRequired(false);
                entity.Property(e => e.Genre).IsRequired(false);
                entity.Property(e => e.TelephoneAgent).IsRequired(false);
                entity.Property(e => e.EmailAgent).IsRequired(false);
                entity.Property(e => e.Statut).IsRequired(false);
                entity.Property(e => e.EtatCivil).IsRequired(false);
                entity.Property(e => e.SerialNumber).IsRequired(false);
                entity.Property(e => e.Fonction).IsRequired(false);
                entity.Property(e => e.RoleAgent).IsRequired(false);
                entity.Property(e => e.PhotoUrl).IsRequired(false);
                entity.Property(e => e.IdSociete).IsRequired(true);
                entity.Property(e => e.IdSite).IsRequired(false);
                entity.Property(e => e.AdresseResidence).IsRequired(false);
                // Note: Les champs d'adresse structurés (Province, Ville, etc.) ont été supprimés
                // Agent utilise maintenant uniquement AdresseResidence
            });

            // Index unique sur le matricule Agent
            modelBuilder.Entity<Agent>()
                .HasIndex(a => a.Matricule)
                .IsUnique()
                .HasDatabaseName("IX_Agents_Matricule_Unique");

            // Index unique sur l'email Agent
            modelBuilder.Entity<Agent>()
                .HasIndex(a => a.EmailAgent)
                .IsUnique()
                .HasDatabaseName("IX_Agents_Email_Unique");

            // Index unique sur le SerialNumber Agent
            modelBuilder.Entity<Agent>()
                .HasIndex(a => a.SerialNumber)
                .IsUnique()
                .HasDatabaseName("IX_Agents_SerialNumber_Unique");

            // Configuration UserRole (Multi-rôles)
            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.IdUtilisateur, ur.IdRole })
                .IsUnique()
                .HasDatabaseName("IX_UserRole_Utilisateur_Role_Unique");

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Utilisateur)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.IdUtilisateur)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany()
                .HasForeignKey(ur => ur.IdRole)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => ur.IdUtilisateur)
                .HasDatabaseName("IX_UserRole_IdUtilisateur");

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => ur.IdRole)
                .HasDatabaseName("IX_UserRole_IdRole");

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.IdUtilisateur, ur.Statut })
                .HasDatabaseName("IX_UserRole_Utilisateur_Statut");

            // Configuration AuditLog
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.TableName, a.RecordId })
                .HasDatabaseName("IX_AuditLog_Table_Record");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserId)
                .HasDatabaseName("IX_AuditLog_UserId");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.DateAction)
                .HasDatabaseName("IX_AuditLog_DateAction");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.IdSociete)
                .HasDatabaseName("IX_AuditLog_IdSociete");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Action)
                .HasDatabaseName("IX_AuditLog_Action");

            // Configuration Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Expediteur)
                .WithMany(u => u.NotificationsEnvoyees)
                .HasForeignKey(n => n.IdExpediteur)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Destinataire)
                .WithMany(u => u.NotificationsRecues)
                .HasForeignKey(n => n.IdDestinataire)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Societe)
                .WithMany(e => e.Notifications)
                .HasForeignKey(n => n.IdSociete)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Agent)
                .WithMany()
                .HasForeignKey(n => n.IdAgent)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Configuration PasswordResetToken
            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(t => t.Utilisateur)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(t => t.IdUtilisateur)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration EmailVerificationToken
            modelBuilder.Entity<EmailVerificationToken>()
                .HasIndex(t => new { t.IdUtilisateur, t.DateUtilisation });

            modelBuilder.Entity<EmailVerificationToken>()
                .HasOne(t => t.Utilisateur)
                .WithMany(u => u.EmailVerificationTokens)
                .HasForeignKey(t => t.IdUtilisateur)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration Role
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Nom)
                .IsUnique();

            // Configuration RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions) // ✅ Spécifier la navigation property
                .HasForeignKey(rp => rp.IdRole)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions) // ✅ Spécifier la navigation property
                .HasForeignKey(rp => rp.IdPermission)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration UserPermission
            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Utilisateur)
                .WithMany()
                .HasForeignKey(up => up.IdUtilisateur)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany()
                .HasForeignKey(up => up.IdPermission)
                .OnDelete(DeleteBehavior.Cascade);

            
            // Configuration Client
            modelBuilder.Entity<Client>()
                .Property(c => c.Statut)
                .HasDefaultValue(true);

            // Index unique sur EmailClient pour éviter les doublons
            modelBuilder.Entity<Client>()
                .HasIndex(c => c.EmailClient)
                .IsUnique()
                .HasDatabaseName("IX_Clients_EmailClient_Unique")
                .HasFilter("EmailClient IS NOT NULL");

            // Index unique sur Telephone pour éviter les doublons
            modelBuilder.Entity<Client>()
                .HasIndex(c => c.Telephone)
                .IsUnique()
                .HasDatabaseName("IX_Clients_Telephone_Unique")
                .HasFilter("Telephone IS NOT NULL");

           

            
            // ═══════════════════════════════════════════════════════════════
            // ✨ NOUVEAUX INDEX POUR LA SYNCHRONISATION
            // ═══════════════════════════════════════════════════════════════════

            // Index pour cursor pagination sur Clients (via relation indirecte)
            modelBuilder.Entity<Client>()
                .HasIndex(c => new { c.UpdatedAt, c.IdClient })
                .HasDatabaseName("IX_Clients_Sync");

            
            // Configuration CommunicationCampaign
            modelBuilder.Entity<CommunicationCampaign>()
                .HasOne(c => c.Societe)
                .WithMany()
                .HasForeignKey(c => c.IdSociete)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CommunicationCampaign>()
                .HasOne(c => c.UtilisateurCreateur)
                .WithMany()
                .HasForeignKey(c => c.IdUtilisateurCreateur)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuration PlainteClient
            modelBuilder.Entity<PlainteClient>()
                .HasOne(p => p.Client)
                .WithMany()
                .HasForeignKey(p => p.IdClient)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlainteClient>()
                .HasOne(p => p.AgentAssigné)
                .WithMany()
                .HasForeignKey(p => p.IdAgentAssigné)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PlainteClient>()
                .HasOne(p => p.UtilisateurCreateur)
                .WithMany()
                .HasForeignKey(p => p.IdUtilisateurCreateur)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuration Statut pour PlainteClient avec valeur par défaut
            modelBuilder.Entity<PlainteClient>()
                .Property(p => p.Statut)
                .HasDefaultValue(true);

            // Les fonctionnalités de ClientCrashed et ArriereeCrashed ne sont plus disponibles après la refactorisation

            // Configuration Destination
            modelBuilder.Entity<Destination>()
                .HasOne(d => d.Societe)
                .WithMany(s => s.Destinations)
                .HasForeignKey(d => d.IdSociete)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Destination>(entity =>
            {
                entity.Property(e => e.HeureDepart).IsRequired(false);
                entity.Property(e => e.JourDepart)
                    .HasColumnName("jourDepart")
                    .IsRequired(false)
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Destination>()
                .HasIndex(d => new { d.VilleDepart, d.VilleArrivee })
                .HasDatabaseName("IX_Destinations_Villes");

            modelBuilder.Entity<Destination>()
                .HasIndex(d => d.IdSociete)
                .HasDatabaseName("IX_Destinations_IdSociete");

            modelBuilder.Entity<Destination>()
                .HasIndex(d => new { d.IdSociete, d.VilleDepart, d.VilleArrivee })
                .IsUnique()
                .HasDatabaseName("IX_Destinations_Societe_Villes_Unique");

            // Configuration CategorieSiege (référentiel par société — phase tarification sièges)
            modelBuilder.Entity<CategorieSiege>()
                .HasOne(c => c.Societe)
                .WithMany(s => s.CategorieSieges)
                .HasForeignKey(c => c.IdSociete)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CategorieSiege>(entity =>
            {
                entity.Property(e => e.CodeCategorieSiege).IsRequired().HasMaxLength(40);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Statut).IsRequired();
            });

            modelBuilder.Entity<CategorieSiege>()
                .HasIndex(c => c.IdSociete)
                .HasDatabaseName("IX_CategorieSieges_IdSociete");

            modelBuilder.Entity<CategorieSiege>()
                .HasIndex(c => new { c.IdSociete, c.CodeCategorieSiege })
                .IsUnique()
                .HasDatabaseName("IX_CategorieSieges_Societe_Code_Unique");

            // Configuration Site (sites opérationnels par société)
            modelBuilder.Entity<Site>()
                .HasOne(a => a.Societe)
                .WithMany(s => s.Sites)
                .HasForeignKey(a => a.IdSociete)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Site>(entity =>
            {
                entity.Property(e => e.CodeSite).IsRequired().HasMaxLength(40);
                entity.Property(e => e.NomSite).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Statut).IsRequired();
                entity.Property(e => e.IsSitePrincipal).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.NumeroMobileMoney).HasMaxLength(30);
            });

            modelBuilder.Entity<Site>()
                .HasIndex(a => new { a.IdSociete, a.IsSitePrincipal })
                .HasDatabaseName("IX_Sites_IdSociete_IsSitePrincipal");

            modelBuilder.Entity<Site>()
                .HasIndex(a => new { a.IdSociete, a.CodeSite })
                .IsUnique()
                .HasDatabaseName("IX_Sites_Societe_CodeSite_Unique");

            modelBuilder.Entity<Site>()
                .HasIndex(a => a.IdSociete)
                .HasDatabaseName("IX_Sites_IdSociete");

            modelBuilder.Entity<Site>()
                .HasIndex(a => a.Statut)
                .HasDatabaseName("IX_Sites_Statut");

            modelBuilder.Entity<Site>()
                .HasIndex(a => a.Ville)
                .HasDatabaseName("IX_Sites_Ville");

            // Configuration Vehicule
            modelBuilder.Entity<Vehicule>()
                .HasOne(b => b.Societe)
                .WithMany(s => s.Vehicules)
                .HasForeignKey(b => b.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicule>()
                .HasOne(b => b.TypeVehicule)
                .WithMany(t => t.Vehicules)
                .HasForeignKey(b => b.IdTypeVehicule)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicule>(entity =>
            {
                entity.Property(e => e.Marques).IsRequired(false);
                entity.Property(e => e.AliasVehicule).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Statut).IsRequired(false);
                entity.Property(e => e.IdSociete).IsRequired(true);
                entity.Property(e => e.IdTypeVehicule).IsRequired(true);
            });

            modelBuilder.Entity<Vehicule>()
                .HasIndex(b => new { b.IdSociete, b.AliasVehicule })
                .IsUnique()
                .HasDatabaseName("IX_Vehicules_Societe_AliasVehicule_Unique");

            modelBuilder.Entity<Vehicule>()
                .HasIndex(b => b.IdSociete)
                .HasDatabaseName("IX_Vehicules_IdSociete");

            modelBuilder.Entity<Vehicule>()
                .HasIndex(b => b.IdTypeVehicule)
                .HasDatabaseName("IX_Vehicules_IdTypeVehicule");

            // Configuration PhotoVehicule (max 3 photos par véhicule via Ordre 1..3)
            modelBuilder.Entity<PhotoVehicule>()
                .HasOne(p => p.Vehicule)
                .WithMany(v => v.Photos)
                .HasForeignKey(p => p.IdVehicule)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PhotoVehicule>(entity =>
            {
                entity.Property(e => e.PhotoData).IsRequired().HasColumnType("mediumblob");
                entity.Property(e => e.Ordre).IsRequired();
                entity.Property(e => e.Statut).IsRequired();
            });

            modelBuilder.Entity<PhotoVehicule>()
                .HasIndex(p => new { p.IdVehicule, p.Ordre })
                .IsUnique()
                .HasDatabaseName("IX_PhotoVehicules_Vehicule_Ordre_Unique");

            modelBuilder.Entity<PhotoVehicule>()
                .HasIndex(p => p.IdVehicule)
                .HasDatabaseName("IX_PhotoVehicules_IdVehicule");

            // Configuration TypeVehicule
            modelBuilder.Entity<TypeVehicule>()
                .HasOne(t => t.Societe)
                .WithMany(s => s.TypeVehicules)
                .HasForeignKey(t => t.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TypeVehicule>(entity =>
            {
                entity.Property(e => e.Libelle).IsRequired(true).HasMaxLength(20);
                entity.Property(e => e.IdSociete).IsRequired(true);
                entity.Property(e => e.Statut).IsRequired(true);
            });

            modelBuilder.Entity<TypeVehicule>()
                .HasIndex(t => t.IdSociete)
                .HasDatabaseName("IX_TypeVehicules_IdSociete");

            modelBuilder.Entity<TypeVehicule>()
                .HasIndex(t => t.Libelle)
                .HasDatabaseName("IX_TypeVehicules_Libelle");

            modelBuilder.Entity<TypeVehicule>()
                .HasIndex(t => new { t.IdSociete, t.Libelle })
                .IsUnique()
                .HasDatabaseName("IX_TypeVehicules_Societe_Libelle_Unique");

            // Configuration Voyage
            modelBuilder.Entity<Voyage>()
                .HasOne(v => v.Vehicule)
                .WithMany()
                .HasForeignKey(v => v.IdVehicule)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Voyage>()
                .HasOne(v => v.Destination)
                .WithMany()
                .HasForeignKey(v => v.IdDestination)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Voyage>()
                .HasOne(v => v.Societe)
                .WithMany(s => s.Voyages)
                .HasForeignKey(v => v.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Voyage>()
                .HasOne(v => v.Site)
                .WithMany()
                .HasForeignKey(v => v.IdSite)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Voyage>(entity =>
            {
                entity.Property(e => e.Statut).IsRequired(false);
                entity.Property(e => e.IdVehicule).IsRequired(true);
                entity.Property(e => e.IdDestination).IsRequired(true);
                entity.Property(e => e.IdSociete).IsRequired(true);
                entity.Property(e => e.IdSite).IsRequired(false);
                entity.Property(e => e.Prix).IsRequired(true);
                entity.Property(e => e.CodeDevisePrix).IsRequired(true).HasMaxLength(3);
                entity.Property(e => e.CodeDevisePrincipale).IsRequired(true).HasMaxLength(3);
                entity.Property(e => e.TauxVersDevisePrincipale).IsRequired(true).HasColumnType("decimal(18,8)");
                entity.Property(e => e.PrixDevisePrincipale).IsRequired(true).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DateDepart).IsRequired(true);
                entity.Property(e => e.HeureDepart).IsRequired(true);
            });

            modelBuilder.Entity<ConfigSociete>(entity =>
            {
                entity.Property(e => e.PenaliteReaffectationPourcentage).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.DureeValiditeBilletJours).HasDefaultValue(0);
                entity.Property(e => e.HeuresLimiteReaffectation).HasDefaultValue(2);
                entity.Property(e => e.HeuresOuvertureEmbarquementAvantDepart).HasDefaultValue(3);
                entity.Property(e => e.HeuresFermetureEmbarquementApresJourDepart).HasDefaultValue(24);
                entity.Property(e => e.HeuresOuvertureEntreeEvenementAvantDebut).HasDefaultValue(3);
                entity.Property(e => e.HeuresOuvertureEntreeRestaurantAvantDebut).HasDefaultValue(1);
                entity.Property(e => e.DureeHoldFlexPayMinutes).HasDefaultValue(15);
                entity.Property(e => e.DureeHoldEvenementMinutes).HasDefaultValue(15);
                entity.Property(e => e.DureeHoldSiteTouristiqueMinutes).HasDefaultValue(15);
                entity.Property(e => e.DureeHoldRestaurantMinutes).HasDefaultValue(15);
                entity.Property(e => e.ReaffectationActive).HasDefaultValue(true);
                entity.Property(e => e.ReservationIsActif).HasDefaultValue(true);
                entity.Property(e => e.ActiviteTransport).HasDefaultValue(true);
                entity.Property(e => e.ActiviteEvenement).HasDefaultValue(true);
                entity.Property(e => e.ActiviteSiteTouristique).HasDefaultValue(true);
                entity.Property(e => e.ActiviteRestaurant).HasDefaultValue(true);
                entity.Property(e => e.AutoReversementPaiementElectronique).HasDefaultValue(false);
                entity.Property(e => e.PourcentageReversementSite).HasColumnType("decimal(18,2)").HasDefaultValue(100m);
                entity.Property(e => e.FraisPlateforme).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.CodeDeviseFraisPlateforme).HasMaxLength(3);
                entity.Property(e => e.MontAddPaieElectronique).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
                entity.Property(e => e.CodeDeviseMontAddPaieElectronique).HasMaxLength(3);
                entity.Property(e => e.PoidsBagageParKiloOffert).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            });

            modelBuilder.Entity<ConfigSociete>()
                .HasIndex(c => c.IdSociete)
                .IsUnique()
                .HasDatabaseName("IX_ConfigSociete_IdSociete_Unique");

            modelBuilder.Entity<ConfigSociete>()
                .HasOne(c => c.Societe)
                .WithOne()
                .HasForeignKey<ConfigSociete>(c => c.IdSociete)
                .OnDelete(DeleteBehavior.Cascade);

            // Index pour optimiser les requêtes
            modelBuilder.Entity<Voyage>()
                .HasIndex(v => v.IdVehicule)
                .HasDatabaseName("IX_Voyages_IdVehicule");

            modelBuilder.Entity<Voyage>()
                .HasIndex(v => v.IdDestination)
                .HasDatabaseName("IX_Voyages_IdDestination");

            modelBuilder.Entity<Voyage>()
                .HasIndex(v => v.DateDepart)
                .HasDatabaseName("IX_Voyages_DateDepart");

            modelBuilder.Entity<Voyage>()
                .HasIndex(v => v.IdSociete)
                .HasDatabaseName("IX_Voyages_IdSociete");

            modelBuilder.Entity<Voyage>()
                .HasIndex(v => v.IdSite)
                .HasDatabaseName("IX_Voyages_IdSite");

            modelBuilder.Entity<Voyage>()
                .HasIndex(v => new { v.IdSociete, v.CodeDevisePrix, v.DateDepart })
                .HasDatabaseName("IX_Voyages_Societe_DevisePrix_Date");

            modelBuilder.Entity<Remboursement>()
                .HasOne(r => r.Paiement)
                .WithMany()
                .HasForeignKey(r => r.IdPaiement)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Remboursement>(entity =>
            {
                entity.Property(e => e.CodeDeviseRemboursement).IsRequired().HasMaxLength(3);
                entity.Property(e => e.CodeDevisePrincipale).IsRequired().HasMaxLength(3);
                entity.Property(e => e.MontantRembourse).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.TauxVersDevisePrincipale).IsRequired().HasColumnType("decimal(18,8)");
                entity.Property(e => e.MontantRembourseDevisePrincipale).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.Statut).IsRequired();
            });

            modelBuilder.Entity<Remboursement>()
                .HasIndex(r => new { r.IdSociete, r.DateRemboursement })
                .HasDatabaseName("IX_Remboursements_Societe_Date");

            // Configuration Reservation
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Utilisateur)
                .WithMany()
                .HasForeignKey(r => r.IdUtilisateur)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Client)
                .WithMany()
                .HasForeignKey(r => r.IdClient)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Voyage)
                .WithMany()
                .HasForeignKey(r => r.IdVoyage)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Societe)
                .WithMany(s => s.Reservations)
                .HasForeignKey(r => r.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Site)
                .WithMany()
                .HasForeignKey(r => r.IdSite)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.Property(e => e.StatutReservation).IsRequired(true).HasMaxLength(20);
                entity.Property(e => e.Statut).IsRequired(true);
                entity.Property(e => e.IdUtilisateur).IsRequired(true);
                entity.Property(e => e.IdClient).IsRequired(true);
                entity.Property(e => e.IdVoyage).IsRequired(true);
                entity.Property(e => e.IdSociete).IsRequired(true);
                entity.Property(e => e.NombreDePlace).IsRequired(true).HasDefaultValue(1);
                entity.Property(e => e.DateReservation).IsRequired(true);
            });

            // Index pour optimiser les requêtes
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.IdUtilisateur)
                .HasDatabaseName("IX_Reservations_IdUtilisateur");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.IdClient)
                .HasDatabaseName("IX_Reservations_IdClient");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.IdVoyage)
                .HasDatabaseName("IX_Reservations_IdVoyage");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.StatutReservation)
                .HasDatabaseName("IX_Reservations_StatutReservation");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.DateReservation)
                .HasDatabaseName("IX_Reservations_DateReservation");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.IdSociete)
                .HasDatabaseName("IX_Reservations_IdSociete");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.IdSite)
                .HasDatabaseName("IX_Reservations_IdSite");

            // Configuration Siege (référentiel véhicule)
            modelBuilder.Entity<Siege>()
                .HasOne(s => s.Vehicule)
                .WithMany(b => b.Sieges)
                .HasForeignKey(s => s.IdVehicule)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Siege>()
                .HasOne(s => s.Societe)
                .WithMany()
                .HasForeignKey(s => s.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Siege>()
                .HasOne(s => s.CategorieSiege)
                .WithMany(c => c.Sieges)
                .HasForeignKey(s => s.IdCategorieSiege)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Siege>()
                .HasIndex(s => s.IdCategorieSiege)
                .HasDatabaseName("IX_Sieges_IdCategorieSiege");

            modelBuilder.Entity<Siege>()
                .HasIndex(s => new { s.IdVehicule, s.NumeroOrdre })
                .IsUnique()
                .HasDatabaseName("IX_Sieges_Vehicule_NumeroOrdre_Unique");

            modelBuilder.Entity<Siege>()
                .HasIndex(s => new { s.IdVehicule, s.CodeSiege })
                .IsUnique()
                .HasDatabaseName("IX_Sieges_Vehicule_CodeSiege_Unique");

            modelBuilder.Entity<Siege>()
                .HasIndex(s => s.IdSociete)
                .HasDatabaseName("IX_Sieges_IdSociete");

            modelBuilder.Entity<Siege>(entity =>
            {
                entity.Property(e => e.CodeSiege).IsRequired().HasMaxLength(120);
                entity.Property(e => e.NumeroOrdre).IsRequired();
                entity.Property(e => e.EstActif).IsRequired();
                entity.Property(e => e.IdCategorieSiege).IsRequired();
            });

            // Tarifs voyage × catégorie de siège (prix par place)
            modelBuilder.Entity<VoyageTarifCategorieSiege>()
                .HasOne(t => t.Voyage)
                .WithMany(v => v.VoyageTarifsCategorieSiege)
                .HasForeignKey(t => t.IdVoyage)
                .HasPrincipalKey(v => v.Id)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VoyageTarifCategorieSiege>()
                .HasOne(t => t.CategorieSiege)
                .WithMany(c => c.VoyageTarifsCategorieSiege)
                .HasForeignKey(t => t.IdCategorieSiege)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoyageTarifCategorieSiege>()
                .HasOne(t => t.Societe)
                .WithMany()
                .HasForeignKey(t => t.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoyageTarifCategorieSiege>()
                .HasIndex(t => new { t.IdVoyage, t.IdCategorieSiege })
                .IsUnique()
                .HasDatabaseName("IX_VoyageTarifCategorieSieges_Voyage_Categorie_Unique");

            modelBuilder.Entity<VoyageTarifCategorieSiege>()
                .HasIndex(t => t.IdSociete)
                .HasDatabaseName("IX_VoyageTarifCategorieSieges_IdSociete");

            modelBuilder.Entity<VoyageTarifCategorieSiege>(entity =>
            {
                entity.Property(e => e.Prix).IsRequired();
            });

            // Configuration VoyageDestination
            modelBuilder.Entity<VoyageDestination>()
                .HasOne(vd => vd.Voyage)
                .WithMany(v => v.VoyageDestinations)
                .HasForeignKey(vd => vd.IdVoyage)
                .HasPrincipalKey(v => v.Id)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VoyageDestination>()
                .HasOne(vd => vd.Destination)
                .WithMany()
                .HasForeignKey(vd => vd.IdDestination)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoyageDestination>()
                .HasOne(vd => vd.Societe)
                .WithMany()
                .HasForeignKey(vd => vd.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoyageDestination>()
                .HasIndex(vd => new { vd.IdVoyage, vd.Ordre })
                .IsUnique()
                .HasDatabaseName("IX_VoyageDestinations_Voyage_Ordre_Unique");

            modelBuilder.Entity<VoyageDestination>()
                .HasIndex(vd => vd.IdSociete)
                .HasDatabaseName("IX_VoyageDestinations_IdSociete");

            modelBuilder.Entity<Voyage>()
                .HasOne(v => v.PlanificationVoyage)
                .WithMany(p => p.VoyagesGeneres)
                .HasForeignKey(v => v.IdPlanificationVoyage)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Voyage>()
                .HasIndex(v => v.IdPlanificationVoyage)
                .HasDatabaseName("IX_Voyages_IdPlanificationVoyage");

            // Configuration PlanificationVoyage
            modelBuilder.Entity<PlanificationVoyage>(entity =>
            {
                entity.Property(p => p.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(p => p.CodeDevisePrix).IsRequired().HasMaxLength(3);
                entity.Property(p => p.JoursSemaine)
                    .HasConversion(
                        v => PlanificationVoyageJsonConverters.SerializeJoursSemaine(v),
                        v => PlanificationVoyageJsonConverters.DeserializeJoursSemaine(v),
                        PlanificationVoyageJsonConverters.JoursSemaineComparer)
                    .HasColumnType("longtext");
            });

            modelBuilder.Entity<PlanificationVoyage>()
                .HasOne(p => p.Societe)
                .WithMany()
                .HasForeignKey(p => p.IdSociete)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlanificationVoyage>()
                .HasOne(p => p.Site)
                .WithMany()
                .HasForeignKey(p => p.IdSite)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlanificationVoyage>()
                .HasOne(p => p.Vehicule)
                .WithMany()
                .HasForeignKey(p => p.IdVehicule)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlanificationVoyage>()
                .HasIndex(p => p.IdSociete)
                .HasDatabaseName("IX_PlanificationsVoyage_IdSociete");

            modelBuilder.Entity<PlanificationVoyageEtape>()
                .HasOne(e => e.PlanificationVoyage)
                .WithMany(p => p.Etapes)
                .HasForeignKey(e => e.IdPlanificationVoyage)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlanificationVoyageEtape>()
                .HasOne(e => e.Destination)
                .WithMany()
                .HasForeignKey(e => e.IdDestination)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlanificationVoyageEtape>()
                .HasIndex(e => new { e.IdPlanificationVoyage, e.Ordre })
                .IsUnique()
                .HasDatabaseName("IX_PlanificationVoyageEtapes_Planif_Ordre_Unique");

            modelBuilder.Entity<PlanificationVoyageTarif>()
                .HasOne(t => t.PlanificationVoyage)
                .WithMany(p => p.Tarifs)
                .HasForeignKey(t => t.IdPlanificationVoyage)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlanificationVoyageTarif>()
                .HasOne(t => t.CategorieSiege)
                .WithMany()
                .HasForeignKey(t => t.IdCategorieSiege)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlanificationVoyageTarif>()
                .HasIndex(t => new { t.IdPlanificationVoyage, t.IdCategorieSiege })
                .IsUnique()
                .HasDatabaseName("IX_PlanificationVoyageTarifs_Planif_Categorie_Unique");

            modelBuilder.Entity<PlanificationGenerationLog>()
                .HasOne(l => l.PlanificationVoyage)
                .WithMany()
                .HasForeignKey(l => l.IdPlanificationVoyage)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlanificationGenerationLog>()
                .HasIndex(l => l.IdPlanificationVoyage)
                .HasDatabaseName("IX_PlanificationGenerationLogs_IdPlanificationVoyage");

            // Configuration ReservationPassenger
            modelBuilder.Entity<ReservationPassenger>()
                .HasOne(rp => rp.Reservation)
                .WithMany(r => r.Passagers)
                .HasForeignKey(rp => rp.IdReservation)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReservationPassenger>()
                .HasOne(rp => rp.Client)
                .WithMany()
                .HasForeignKey(rp => rp.IdClient)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationPassenger>()
                .HasOne(rp => rp.Societe)
                .WithMany()
                .HasForeignKey(rp => rp.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationPassenger>()
                .HasIndex(rp => rp.IdReservation)
                .HasDatabaseName("IX_ReservationPassengers_IdReservation");

            modelBuilder.Entity<ReservationPassenger>()
                .HasIndex(rp => rp.IdClient)
                .HasDatabaseName("IX_ReservationPassengers_IdClient");

            modelBuilder.Entity<ReservationPassenger>()
                .HasIndex(rp => rp.IdSociete)
                .HasDatabaseName("IX_ReservationPassengers_IdSociete");

            modelBuilder.Entity<ReservationPassenger>(entity =>
            {
                entity.Property(e => e.NomComplet).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Statut).IsRequired();
            });

            // Configuration VoyageSeatAllocation
            modelBuilder.Entity<VoyageSeatAllocation>()
                .HasOne(a => a.Voyage)
                .WithMany()
                .HasForeignKey(a => a.IdVoyage)
                .HasPrincipalKey(v => v.Id)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoyageSeatAllocation>()
                .HasOne(a => a.Siege)
                .WithMany(s => s.VoyageSeatAllocations!)
                .HasForeignKey(a => a.IdSiege)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoyageSeatAllocation>()
                .HasOne(a => a.ReservationPassenger)
                .WithMany(rp => rp.VoyageSeatAllocations!)
                .HasForeignKey(a => a.IdReservationPassenger)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VoyageSeatAllocation>()
                .HasIndex(a => new { a.IdVoyage, a.IdSiege })
                .IsUnique()
                .HasDatabaseName("IX_VoyageSeatAllocations_Voyage_Siege_Unique");

            modelBuilder.Entity<VoyageSeatAllocation>()
                .HasIndex(a => a.IdReservationPassenger)
                .IsUnique()
                .HasDatabaseName("IX_VoyageSeatAllocations_ReservationPassenger_Unique");

            modelBuilder.Entity<VoyageSeatAllocation>()
                .HasIndex(a => a.IdVoyage)
                .HasDatabaseName("IX_VoyageSeatAllocations_IdVoyage");

            modelBuilder.Entity<VoyageSeatAllocation>(entity =>
            {
                entity.Property(e => e.Statut).IsRequired().HasMaxLength(20);
            });

            // Configuration Billet
            modelBuilder.Entity<Billet>()
                .HasOne(b => b.Reservation)
                .WithMany()
                .HasForeignKey(b => b.IdReservation)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Billet>()
                .HasOne(b => b.Societe)
                .WithMany(s => s.Billets)
                .HasForeignKey(b => b.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Billet>()
                .HasOne(b => b.Site)
                .WithMany()
                .HasForeignKey(b => b.IdSite)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Billet>()
                .HasOne(b => b.Client)
                .WithMany()
                .HasForeignKey(b => b.IdClient)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Billet>()
                .HasOne(b => b.ReservationPassenger)
                .WithMany(rp => rp.Billets)
                .HasForeignKey(b => b.IdReservationPassenger)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Billet>()
                .HasOne(b => b.Siege)
                .WithMany()
                .HasForeignKey(b => b.IdSiege)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Billet>()
                .HasIndex(b => b.IdReservationPassenger)
                .HasDatabaseName("IX_Billets_IdReservationPassenger");

            modelBuilder.Entity<Billet>()
                .HasIndex(b => b.IdSiege)
                .HasDatabaseName("IX_Billets_IdSiege");

            modelBuilder.Entity<Billet>(entity =>
            {
                entity.Property(e => e.QrCode).IsRequired(true).HasMaxLength(255);
                entity.Property(e => e.IdReservation).IsRequired(false);
                entity.Property(e => e.IdSociete).IsRequired(true);
                entity.Property(e => e.IdClient).IsRequired(false);
                entity.Property(e => e.DateGeneration).IsRequired(true);
                entity.Property(e => e.IdReservationPassenger).IsRequired(false);
                entity.Property(e => e.IdSiege).IsRequired(false);
                entity.Property(e => e.CodeSiege).IsRequired(false).HasMaxLength(120);
                entity.Property(e => e.IsUsed).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.DateValiditeDebut).IsRequired(false);
                entity.Property(e => e.DateValiditeFin).IsRequired(false);
                entity.Property(e => e.PenaliteOverride).IsRequired(false).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<BilletEmbarquement>()
                .HasOne(e => e.Billet)
                .WithMany()
                .HasForeignKey(e => e.IdBillet)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BilletEmbarquement>()
                .HasOne(e => e.ReservationPassenger)
                .WithMany()
                .HasForeignKey(e => e.IdReservationPassenger)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BilletEmbarquement>()
                .HasOne(e => e.Societe)
                .WithMany()
                .HasForeignKey(e => e.IdSociete)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BilletEmbarquement>()
                .HasOne(e => e.UtilisateurEnregistrement)
                .WithMany()
                .HasForeignKey(e => e.IdUtilisateurEnregistrement)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BilletEmbarquement>()
                .HasIndex(e => e.IdBillet)
                .IsUnique()
                .HasDatabaseName("IX_BilletEmbarquements_IdBillet_Unique");

            modelBuilder.Entity<BilletEmbarquement>()
                .HasIndex(e => e.IdSociete)
                .HasDatabaseName("IX_BilletEmbarquements_IdSociete");

            modelBuilder.Entity<BilletEmbarquement>()
                .HasIndex(e => e.IdReservationPassenger)
                .HasDatabaseName("IX_BilletEmbarquements_IdReservationPassenger");

            modelBuilder.Entity<FeuilleDeRoute>(entity =>
            {
                entity.HasKey(e => e.IdFeuilleDeRoute);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Voyage)
                    .WithMany()
                    .HasForeignKey(e => e.IdVoyage)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UtilisateurGeneration)
                    .WithMany()
                    .HasForeignKey(e => e.IdUtilisateurGeneration)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Passagers)
                    .WithOne(p => p.FeuilleDeRoute)
                    .HasForeignKey(p => p.IdFeuilleDeRoute)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.DateEmbarquement).HasColumnType("date");
                entity.Property(e => e.VoyageCodeDevise).HasMaxLength(3).IsRequired();
                entity.Property(e => e.SocieteNom).HasMaxLength(150);
                entity.Property(e => e.SocieteTelephone).HasMaxLength(50);
                entity.Property(e => e.SocieteEmail).HasMaxLength(256);
                entity.Property(e => e.SocieteAdresse).HasMaxLength(500);
                entity.Property(e => e.DestinationLibelle).HasMaxLength(450);
                entity.Property(e => e.VehiculeImmatriculation).HasMaxLength(20);
                entity.Property(e => e.VehiculeAlias).HasMaxLength(100);
                entity.Property(e => e.SiteNom).HasMaxLength(200);

                entity.HasIndex(e => e.IdSociete)
                    .HasDatabaseName("IX_FeuilleDeRoutes_IdSociete");
                entity.HasIndex(e => e.IdVoyage)
                    .HasDatabaseName("IX_FeuilleDeRoutes_IdVoyage");
                entity.HasIndex(e => new { e.IdSociete, e.DateEmbarquement })
                    .HasDatabaseName("IX_FeuilleDeRoutes_Societe_DateEmbarquement");
            });

            modelBuilder.Entity<FeuilleDeRoutePassager>(entity =>
            {
                entity.HasKey(e => e.IdFeuilleDeRoutePassager);

                entity.Property(e => e.NomComplet).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Telephone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.DocumentType).HasMaxLength(50);
                entity.Property(e => e.DocumentNumero).HasMaxLength(100);
                entity.Property(e => e.CodeSiege).HasMaxLength(120);

                entity.HasIndex(e => e.IdFeuilleDeRoute)
                    .HasDatabaseName("IX_FeuilleDeRoutePassagers_IdFeuilleDeRoute");
            });

            // Configuration Paiement
            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Reservation)
                .WithMany()
                .HasForeignKey(p => p.IdReservation)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Utilisateur)
                .WithMany()
                .HasForeignKey(p => p.IdUtilisateur)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Societe)
                .WithMany()
                .HasForeignKey(p => p.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Site)
                .WithMany()
                .HasForeignKey(p => p.IdSite)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Paiement>(entity =>
            {
                entity.Property(e => e.MontantAPaye).IsRequired(true).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontantPaye).IsRequired(false).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ResteAPaye).IsRequired(false).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CodeDevisePaiement).IsRequired(true).HasMaxLength(3);
                entity.Property(e => e.CodeDevisePrincipale).IsRequired(true).HasMaxLength(3);
                entity.Property(e => e.TauxVersDevisePrincipale).IsRequired(true).HasColumnType("decimal(18,8)");
                entity.Property(e => e.MontantAPayeDevisePrincipale).IsRequired(true).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontantPayeDevisePrincipale).IsRequired(false).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ResteAPayeDevisePrincipale).IsRequired(false).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DatePaiement).IsRequired(true);
                entity.Property(e => e.MethodePaiement).IsRequired(false).HasMaxLength(50);
                entity.Property(e => e.ReferenceTransaction).IsRequired(false).HasMaxLength(100);
                entity.Property(e => e.Statut).IsRequired(true);
                entity.Property(e => e.StatutPaiementMetier).IsRequired(false);
                entity.Property(e => e.DateCreation).IsRequired(true);
                entity.Property(e => e.DateModification).IsRequired(false);
                entity.Property(e => e.IdReservation).IsRequired(false);
                entity.Property(e => e.IdUtilisateur).IsRequired(true);
                entity.Property(e => e.IdSociete).IsRequired(true);
                entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            });

            // Index pour optimiser les requêtes sur les paiements
            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.IdReservation)
                .HasDatabaseName("IX_Paiements_IdReservation");

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.IdUtilisateur)
                .HasDatabaseName("IX_Paiements_IdUtilisateur");

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.IdSociete)
                .HasDatabaseName("IX_Paiements_IdSociete");

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.IdSite)
                .HasDatabaseName("IX_Paiements_IdSite");

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.Statut)
                .HasDatabaseName("IX_Paiements_Statut");

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.DateCreation)
                .HasDatabaseName("IX_Paiements_DateCreation");

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => new { p.IdSociete, p.CodeDevisePaiement, p.DatePaiement })
                .HasDatabaseName("IX_Paiements_Societe_DevisePaiement_DatePaiement");

            modelBuilder.Entity<SiegeHoldEnAttente>(entity =>
            {
                entity.Property(e => e.ExpireAt).IsRequired();
                entity.Property(e => e.DateCreation).IsRequired();
                entity.HasIndex(e => new { e.IdVoyage, e.IdSiege })
                    .IsUnique()
                    .HasDatabaseName("IX_SiegeHoldsEnAttente_Voyage_Siege_Unique");
                entity.HasIndex(e => new { e.IdVoyage, e.ExpireAt })
                    .HasDatabaseName("IX_SiegeHoldsEnAttente_Voyage_ExpireAt");
                entity.HasIndex(e => e.IdCommandeReservationEnAttente)
                    .HasDatabaseName("IX_SiegeHoldsEnAttente_IdCommande");
            });

            modelBuilder.Entity<CommandeReservationEnAttente>(entity =>
            {
                entity.Property(e => e.MethodePaiement).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CodeDeviseVoyage).IsRequired().HasMaxLength(3);
                entity.Property(e => e.CodeDevisePaiement).IsRequired().HasMaxLength(3);
                entity.Property(e => e.PayloadMetierJson).IsRequired();
                entity.HasIndex(e => e.OrderNumberFlexPay)
                    .IsUnique()
                    .HasDatabaseName("IX_CommandesReservationEnAttente_OrderNumber");
                entity.HasIndex(e => new { e.IdSociete, e.DateCreation })
                    .HasDatabaseName("IX_CommandesReservationEnAttente_Societe_Date");
            });

            modelBuilder.Entity<InfoPaiementSociete>(entity =>
            {
                entity.Property(e => e.CodeMarchand).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApiToken).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.IdSite)
                    .IsUnique()
                    .HasDatabaseName("IX_InfoPaiementSociete_IdSite_Unique");
                entity.HasIndex(e => e.IdSociete)
                    .HasDatabaseName("IX_InfoPaiementSociete_IdSociete");
                entity.HasOne(e => e.Site)
                    .WithMany()
                    .HasForeignKey(e => e.IdSite)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TransactionFlexPay>(entity =>
            {
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TypePaiement).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(10);
                entity.HasIndex(e => e.OrderNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_TransactionFlexPay_OrderNumber");
                entity.HasIndex(e => e.Reference)
                    .HasDatabaseName("IX_TransactionFlexPay_Reference");
            });

            modelBuilder.Entity<CallbackFlexPay>(entity =>
            {
                entity.HasIndex(e => e.OrderNumber)
                    .HasDatabaseName("IX_CallbackFlexPay_OrderNumber");
                entity.HasIndex(e => e.DateReception)
                    .HasDatabaseName("IX_CallbackFlexPay_DateReception");
                entity.HasOne(e => e.Transaction)
                    .WithMany()
                    .HasForeignKey(e => e.IdTransaction)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReversementSite>(entity =>
            {
                entity.Property(e => e.NumeroMobileMoney).IsRequired().HasMaxLength(30);
                entity.Property(e => e.CodeDevise).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Origine).IsRequired().HasMaxLength(30).HasDefaultValue("Manuel");
                entity.HasIndex(e => e.OrderNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_ReversementSite_OrderNumber")
                    .HasFilter("[OrderNumber] IS NOT NULL");
                entity.HasIndex(e => e.IdPaiement)
                    .IsUnique()
                    .HasDatabaseName("IX_ReversementSite_IdPaiement")
                    .HasFilter("[IdPaiement] IS NOT NULL");
                entity.HasIndex(e => new { e.IdSociete, e.IdSite, e.DateCreation })
                    .HasDatabaseName("IX_ReversementSite_Societe_Site_Date");
                entity.HasOne<Site>()
                    .WithMany()
                    .HasForeignKey(e => e.IdSite)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Societe>()
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuration devises monétaires
            modelBuilder.Entity<DeviseMonetaire>(entity =>
            {
                entity.Property(e => e.CodeDevise).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Symbole).IsRequired(false).HasMaxLength(10);
                entity.Property(e => e.Statut).IsRequired();
            });

            modelBuilder.Entity<DeviseMonetaire>()
                .HasOne(d => d.Societe)
                .WithMany()
                .HasForeignKey(d => d.IdSociete)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DeviseMonetaire>()
                .HasIndex(e => new { e.IdSociete, e.CodeDevise })
                .IsUnique()
                .HasDatabaseName("IX_DevisesMonetaires_Societe_CodeDevise_Unique");

            modelBuilder.Entity<DeviseMonetaire>()
                .HasIndex(e => e.IdSociete)
                .HasDatabaseName("IX_DevisesMonetaires_IdSociete");

            modelBuilder.Entity<DeviseMonetaire>().HasData(
                new DeviseMonetaire { IdDeviseMonetaire = 1, CodeDevise = "CDF", Libelle = "Franc congolais", Symbole = "FC", Statut = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new DeviseMonetaire { IdDeviseMonetaire = 2, CodeDevise = "USD", Libelle = "Dollar americain", Symbole = "$", Statut = true, DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Configuration taux de change
            modelBuilder.Entity<TauxChange>()
                .HasOne(t => t.Societe)
                .WithMany()
                .HasForeignKey(t => t.IdSociete)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TauxChange>(entity =>
            {
                entity.Property(e => e.CodeDeviseSource).IsRequired().HasMaxLength(3);
                entity.Property(e => e.CodeDeviseCible).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Taux).IsRequired().HasColumnType("decimal(18,8)");
                entity.Property(e => e.Statut).IsRequired();
            });

            modelBuilder.Entity<TauxChange>()
                .HasIndex(t => new { t.IdSociete, t.CodeDeviseSource, t.CodeDeviseCible, t.DateEffet })
                .HasDatabaseName("IX_TauxChanges_Societe_Paire_DateEffet");

            // Index pour optimiser les requêtes
            modelBuilder.Entity<Billet>()
                .HasIndex(b => b.IdReservation)
                .HasDatabaseName("IX_Billets_IdReservation");

            modelBuilder.Entity<Billet>()
                .HasIndex(b => b.QrCode)
                .HasDatabaseName("IX_Billets_QrCode");

            modelBuilder.Entity<Billet>()
                .HasIndex(b => b.DateGeneration)
                .HasDatabaseName("IX_Billets_DateGeneration");

            modelBuilder.Entity<Billet>()
                .HasIndex(b => b.IdSociete)
                .HasDatabaseName("IX_Billets_IdSociete");

            modelBuilder.Entity<Billet>()
                .HasIndex(b => b.IdSite)
                .HasDatabaseName("IX_Billets_IdSite");

            ConfigureEvenementEntities(modelBuilder);
            ConfigureSiteTouristiqueEntities(modelBuilder);
            ConfigureRestaurantEntities(modelBuilder);
        }

        /// <summary>Code site réservé à l'initialisation (unique par société).</summary>
        private const string DefaultInitializedSiteCode = "DEFAUT";

        /// <summary>
        /// Initialise les données par défaut du système (Super-Admin, Société par défaut, etc.)
        /// </summary>
        public async Task InitializeDefaultDataAsync()
        {
            // Utiliser CreateExecutionStrategy() pour gérer les transactions avec MySqlRetryingExecutionStrategy
            var strategy = Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await Database.BeginTransactionAsync())
                {
                    try
                    {
                        var currentDate = DateTime.Now;
                        
                        // 1. Créer ou récupérer les rôles
                        var superAdminRole = await CreateOrGetSuperAdminRoleAsync(currentDate);
                        var adminRole = await CreateOrGetAdminRoleAsync(currentDate);
                        var gerantRole = await CreateOrGetGerantRoleAsync(currentDate);
                        
                        // 2. Créer ou récupérer la société par défaut
                        var defaultSociete = await CreateOrGetDefaultSocieteAsync(currentDate);

                        // 2b. Garantir le site par défaut de cette société (idempotent)
                        var defaultSite = await CreateOrGetDefaultSiteAsync(defaultSociete, currentDate);
                        
                        // 3. Créer l'Agent Manager Général + Utilisateur Super-Admin
                        var superAdminUser = await CreateOrGetSuperAdminWithAgentAsync(superAdminRole, defaultSociete, currentDate);
                        
                        // 4. Créer l'Agent Admin + Utilisateur Admin
                        var adminUser = await CreateOrGetAdminWithAgentAsync(adminRole, defaultSociete, currentDate);
                        
                        // 5. Créer l'Agent Gerant + Utilisateur Gerant (liés au site par défaut si IdSite vide)
                        var gerantUser = await CreateOrGetGerantWithAgentAsync(gerantRole, defaultSociete, defaultSite, currentDate);
                        
                        await transaction.CommitAsync();
                        
                        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
                        Console.WriteLine("║     ✅ INITIALISATION DES DONNÉES PAR DÉFAUT TERMINÉE      ║");
                        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
                        Console.WriteLine($"📋 Rôle Super-Admin: ID {superAdminRole.IdRole}");
                        Console.WriteLine($"📋 Rôle Admin: ID {adminRole.IdRole}");
                        Console.WriteLine($"📋 Rôle Gerant: ID {gerantRole.IdRole}");
                        Console.WriteLine($"🏢 Société par défaut: ID {defaultSociete.IdSociete} - {defaultSociete.Nom}");
                        Console.WriteLine($"📍 Site par défaut: ID {defaultSite.IdSite} - {defaultSite.NomSite} (code {defaultSite.CodeSite})");
                        Console.WriteLine($"👤 Utilisateur Super-Admin: ID {superAdminUser.IdUtilisateur}");
                        Console.WriteLine($"   📧 Email: {superAdminUser.Email}");
                        Console.WriteLine($"   📱 Téléphone: {superAdminUser.Telephone}");
                        Console.WriteLine($"   🔑 Username: {superAdminUser.DefaultUsername}");
                        Console.WriteLine($"   ⚠️  Mot de passe par défaut: Super-Admin");
                        Console.WriteLine($"   🔒 Doit changer le mot de passe: {superAdminUser.DoitChangerMotDePasse}");
                        Console.WriteLine($"👤 Utilisateur Admin: ID {adminUser.IdUtilisateur}");
                        Console.WriteLine($"   📧 Email: {adminUser.Email}");
                        Console.WriteLine($"   📱 Téléphone: {adminUser.Telephone}");
                        Console.WriteLine($"   🔑 Username: {adminUser.DefaultUsername}");
                        Console.WriteLine($"   ⚠️  Mot de passe par défaut: Admin");
                        Console.WriteLine($"   🔒 Doit changer le mot de passe: {adminUser.DoitChangerMotDePasse}");
                        Console.WriteLine($"👤 Utilisateur Gerant: ID {gerantUser.IdUtilisateur}");
                        Console.WriteLine($"   📧 Email: {gerantUser.Email}");
                        Console.WriteLine($"   📱 Téléphone: {gerantUser.Telephone}");
                        Console.WriteLine($"   🔑 Username: {gerantUser.DefaultUsername}");
                        Console.WriteLine($"   ⚠️  Mot de passe par défaut: Gerant");
                        Console.WriteLine($"   🔒 Doit changer le mot de passe: {gerantUser.DoitChangerMotDePasse}");
                        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Erreur lors de l'initialisation: {ex.Message}");
                        throw;
                    }
                }
            });
        }

        private async Task<Role> CreateOrGetSuperAdminRoleAsync(DateTime currentDate)
        {
            var existingRole = await Roles.FirstOrDefaultAsync(r => r.Nom == "Super-Admin");
            
            if (existingRole != null)
            {
                Console.WriteLine($"Rôle Super-Admin existe déjà avec l'ID: {existingRole.IdRole}");
                return existingRole;
            }
            
            var newRole = new Role
            {
                Nom = "Super-Admin",
                DateCreation = currentDate
            };
            
            Roles.Add(newRole);
            await SaveChangesAsync();
            
            Console.WriteLine($"Rôle Super-Admin créé avec l'ID: {newRole.IdRole}");
            return newRole;
        }

        private async Task<Role> CreateOrGetAdminRoleAsync(DateTime currentDate)
        {
            var existingRole = await Roles.FirstOrDefaultAsync(r => r.Nom == "Admin");
            
            if (existingRole != null)
            {
                Console.WriteLine($"Rôle Admin existe déjà avec l'ID: {existingRole.IdRole}");
                return existingRole;
            }
            
            var newRole = new Role
            {
                Nom = "Admin",
                Niveau = 2,
                DateCreation = currentDate
            };
            
            Roles.Add(newRole);
            await SaveChangesAsync();
            
            Console.WriteLine($"Rôle Admin créé avec l'ID: {newRole.IdRole}");
            return newRole;
        }

        private async Task<Role> CreateOrGetGerantRoleAsync(DateTime currentDate)
        {
            var existingRole = await Roles.FirstOrDefaultAsync(r => r.Nom == "Gerant");
            
            if (existingRole != null)
            {
                Console.WriteLine($"Rôle Gerant existe déjà avec l'ID: {existingRole.IdRole}");
                return existingRole;
            }
            
            var newRole = new Role
            {
                Nom = "Gerant",
                Niveau = 3,
                DateCreation = currentDate
            };
            
            Roles.Add(newRole);
            await SaveChangesAsync();
            
            Console.WriteLine($"Rôle Gerant créé avec l'ID: {newRole.IdRole}");
            return newRole;
        }

        private async Task<Societe> CreateOrGetDefaultSocieteAsync(DateTime currentDate)
        {
            var existingSociete = await Societes.FirstOrDefaultAsync(e => e.Nom == "CongoTravel");
            
            if (existingSociete != null)
            {
                Console.WriteLine($"✅ Société par défaut existe déjà avec l'ID: {existingSociete.IdSociete}");
                return existingSociete;
            }
            
            var newSociete = new Societe
            {
                Nom = "CongoTravel",
                Devise = "Excellence et Innovation",
                CodeDevisePrincipale = "CDF",
                Type = "Privée",
                Description = "Société d'excellence offrant des services de qualité énergétique",
                Telephone = "+243999999999",
                EmailContact = "contact@congotravel.cd",
                SiteWeb = "https://www.congotravel.cd",
                NomCompletResponsable = "Administrateur Super Admin", // Nom complet du responsable
                GenreResponsable = "Masculin",
                Statut = true,
                DateCreation = currentDate
            };
            
            Societes.Add(newSociete);
            await SaveChangesAsync();
            
            Console.WriteLine($"✅ Société par défaut créée avec l'ID: {newSociete.IdSociete}");
            Console.WriteLine($"   Nom: {newSociete.Nom}");
            Console.WriteLine($"   Email: {newSociete.EmailContact}");
            Console.WriteLine($"   Téléphone: {newSociete.Telephone}");
            return newSociete;
        }

        /// <summary>
        /// Retrouve ou crée le site d'initialisation pour la société par défaut (aucun doublon : code fixe par société).
        /// </summary>
        private async Task<Site> CreateOrGetDefaultSiteAsync(Societe defaultSociete, DateTime currentDate)
        {
            var existingSite = await Sites.FirstOrDefaultAsync(s =>
                s.IdSociete == defaultSociete.IdSociete &&
                s.CodeSite == DefaultInitializedSiteCode);

            if (existingSite != null)
            {
                Console.WriteLine($"✅ Site par défaut déjà présent — ID {existingSite.IdSite}, nom: {existingSite.NomSite}, code: {existingSite.CodeSite}");
                return existingSite;
            }

            var newSite = new Site
            {
                IdSociete = defaultSociete.IdSociete,
                CodeSite = DefaultInitializedSiteCode,
                NomSite = "Site principal CongoTravel",
                Ville = null,
                Adresse = null,
                Telephone = defaultSociete.Telephone,
                NomResponsableSite = string.IsNullOrWhiteSpace(defaultSociete.NomCompletResponsable)
                    ? "Responsable Site"
                    : defaultSociete.NomCompletResponsable.Trim(),
                Email = string.IsNullOrWhiteSpace(defaultSociete.EmailContact) ? null : defaultSociete.EmailContact.Trim(),
                Genre = string.IsNullOrWhiteSpace(defaultSociete.GenreResponsable) ? "Masculin" : defaultSociete.GenreResponsable.Trim(),
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = currentDate
            };

            Sites.Add(newSite);
            await SaveChangesAsync();

            Console.WriteLine($"✅ Site par défaut créé — ID {newSite.IdSite}, nom: {newSite.NomSite}, code: {newSite.CodeSite}");
            return newSite;
        }

        private async Task<Utilisateur> CreateOrGetSuperAdminWithAgentAsync(Role superAdminRole, Societe defaultSociete, DateTime currentDate)
        {
            var existingUser = await Utilisateurs
                .FirstOrDefaultAsync(u => u.IdRole == superAdminRole.IdRole && u.IdSociete == defaultSociete.IdSociete);
            
            if (existingUser != null)
            {
                Console.WriteLine($"✅ Utilisateur Super-Admin existe déjà avec l'ID: {existingUser.IdUtilisateur}");
                
                // Vérifier si l'association UserRole existe, sinon la créer
                var existingUserRole = await UserRoles
                    .FirstOrDefaultAsync(ur => ur.IdUtilisateur == existingUser.IdUtilisateur && ur.IdRole == superAdminRole.IdRole);
                
                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdRole = superAdminRole.IdRole,
                        IsPrimary = true, // Rôle principal
                        DateAttribution = currentDate,
                        Statut = true // Statut actif
                    };
                    
                    UserRoles.Add(userRole);
                    await SaveChangesAsync();
                    Console.WriteLine($"✅ Association UserRole créée pour l'utilisateur existant {existingUser.IdUtilisateur} avec le rôle Super-Admin");
                }
                
                return existingUser;
            }
            
            // 1. Créer l'Agent Manager Général
            var managerAgent = await CreateOrGetManagerGeneralAgentAsync(defaultSociete, currentDate);
            
            // 2. Générer le hash du mot de passe par défaut
            // Mot de passe par défaut: "Super-Admin" (à changer lors de la première connexion)
            string motDePasseParDefaut = "Super-Admin";
            string motDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut, BCrypt.Net.BCrypt.GenerateSalt(11));
            
            // 3. Créer l'Utilisateur Super-Admin lié à cet Agent
            var newUser = new Utilisateur
            {
                IdAgent = managerAgent.IdAgent,
                ReferenceUtilisateur = Guid.NewGuid(),
                NomComplet = managerAgent.NomComplet,
                Email = "superadmin@congotravel.cd",
                DefaultUsername = "SuperAdmin",
                Telephone = "+243999999999",
                MotDePasseHash = motDePasseHash,
                Genre = managerAgent.Genre,
                DateNaissance = managerAgent.DateNaissance,
                Statut = true,
                IdRole = superAdminRole.IdRole,
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate,
                IsConnecte = false,
                DoitChangerMotDePasse = true // Forcer le changement de mot de passe à la première connexion
            };
            
            Utilisateurs.Add(newUser);
            await SaveChangesAsync();
            
            // 4. Créer l'association UserRole (Multi-rôles)
            var newUserRoleCheck = await UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == newUser.IdUtilisateur && ur.IdRole == superAdminRole.IdRole);
            
            if (newUserRoleCheck == null)
            {
                var userRole = new UserRole
                {
                    IdUtilisateur = newUser.IdUtilisateur,
                    IdRole = superAdminRole.IdRole,
                    IsPrimary = true, // Rôle principal
                    DateAttribution = currentDate,
                    Statut = true // Statut actif
                };
                
                UserRoles.Add(userRole);
                await SaveChangesAsync();
                Console.WriteLine($"✅ Association UserRole créée pour l'utilisateur {newUser.IdUtilisateur} avec le rôle Super-Admin");
            }
            
            Console.WriteLine($"✅ Utilisateur Super-Admin créé avec l'ID: {newUser.IdUtilisateur} (lié à l'Agent {managerAgent.IdAgent})");
            Console.WriteLine($"   Email: {newUser.Email}");
            Console.WriteLine($"   Username: {newUser.DefaultUsername}");
            Console.WriteLine($"   Téléphone: {newUser.Telephone}");
            Console.WriteLine($"   ⚠️  Mot de passe par défaut: {motDePasseParDefaut} (à changer à la première connexion)");
            return newUser;
        }

        private async Task<Agent> CreateOrGetManagerGeneralAgentAsync(Societe defaultSociete, DateTime currentDate)
        {
            var existingManager = await Agents
                .FirstOrDefaultAsync(a => a.IdSociete == defaultSociete.IdSociete && a.Fonction == "Manager Général");
            
            if (existingManager != null)
            {
                Console.WriteLine($"Agent Manager Général existe déjà avec l'ID: {existingManager.IdAgent}");
                return existingManager;
            }
            
            var managerAgent = new Agent
            {
                NomComplet = "Administrateur Super Admin",
                Genre = "Masculin",
                DateNaissance = DateTime.Now.AddYears(-40),
                TelephoneAgent = "+243999999999",
                EmailAgent = "superadmin@congotravel.cd",
                Statut = true,
                EtatCivil = "Marié",
                Fonction = "Manager Général",
                RoleAgent = "Super-Administrateur",
                Matricule = await GenerateUniqueMatriculeAgentAsync(),
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate
            };
            
            Agents.Add(managerAgent);
            await SaveChangesAsync();
            
            Console.WriteLine($"Agent Manager Général créé avec l'ID: {managerAgent.IdAgent} - Matricule: {managerAgent.Matricule}");
            return managerAgent;
        }

        private async Task<string> GenerateUniqueMatriculeAgentAsync()
        {
            string matricule;
            
            do
            {
                string annee = DateTime.Now.Year.ToString().Substring(2);
                string guid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                matricule = $"NAT{annee}-{guid}";
                
            } while (await Agents.AnyAsync(a => a.Matricule == matricule));
            
            return matricule;
        }

        private async Task<Utilisateur> CreateOrGetAdminWithAgentAsync(Role adminRole, Societe defaultSociete, DateTime currentDate)
        {
            var existingUser = await Utilisateurs
                .FirstOrDefaultAsync(u => u.IdRole == adminRole.IdRole && u.IdSociete == defaultSociete.IdSociete && u.Email == "admin@congotravel.cd");
            
            if (existingUser != null)
            {
                Console.WriteLine($"✅ Utilisateur Admin existe déjà avec l'ID: {existingUser.IdUtilisateur}");
                
                // Vérifier si l'association UserRole existe, sinon la créer
                var existingUserRole = await UserRoles
                    .FirstOrDefaultAsync(ur => ur.IdUtilisateur == existingUser.IdUtilisateur && ur.IdRole == adminRole.IdRole);
                
                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdRole = adminRole.IdRole,
                        IsPrimary = true, // Rôle principal
                        DateAttribution = currentDate,
                        Statut = true // Statut actif
                    };
                    
                    UserRoles.Add(userRole);
                    await SaveChangesAsync();
                    Console.WriteLine($"✅ Association UserRole créée pour l'utilisateur existant {existingUser.IdUtilisateur} avec le rôle Admin");
                }
                
                return existingUser;
            }
            
            // 1. Créer l'Agent Admin
            var adminAgent = await CreateOrGetAdminAgentAsync(defaultSociete, currentDate);
            
            // 2. Générer le hash du mot de passe par défaut
            // Mot de passe par défaut: "Admin" (à changer lors de la première connexion)
            string motDePasseParDefaut = "Admin";
            string motDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut, BCrypt.Net.BCrypt.GenerateSalt(11));
            
            // 3. Créer l'Utilisateur Admin lié à cet Agent
            var newUser = new Utilisateur
            {
                IdAgent = adminAgent.IdAgent,
                ReferenceUtilisateur = Guid.NewGuid(),
                NomComplet = adminAgent.NomComplet,
                Email = "admin@congotravel.cd",
                DefaultUsername = "Admin",
                Telephone = "+243888888888",
                MotDePasseHash = motDePasseHash,
                Genre = adminAgent.Genre,
                DateNaissance = adminAgent.DateNaissance,
                Statut = true,
                IdRole = adminRole.IdRole,
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate,
                IsConnecte = false,
                DoitChangerMotDePasse = true // Forcer le changement de mot de passe à la première connexion
            };
            
            Utilisateurs.Add(newUser);
            await SaveChangesAsync();
            
            // 4. Créer l'association UserRole (Multi-rôles)
            var newUserRoleCheck = await UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == newUser.IdUtilisateur && ur.IdRole == adminRole.IdRole);
            
            if (newUserRoleCheck == null)
            {
                var userRole = new UserRole
                {
                    IdUtilisateur = newUser.IdUtilisateur,
                    IdRole = adminRole.IdRole,
                    IsPrimary = true, // Rôle principal
                    DateAttribution = currentDate,
                    Statut = true // Statut actif
                };
                
                UserRoles.Add(userRole);
                await SaveChangesAsync();
                Console.WriteLine($"✅ Association UserRole créée pour l'utilisateur {newUser.IdUtilisateur} avec le rôle Admin");
            }
            
            Console.WriteLine($"✅ Utilisateur Admin créé avec l'ID: {newUser.IdUtilisateur} (lié à l'Agent {adminAgent.IdAgent})");
            Console.WriteLine($"   Email: {newUser.Email}");
            Console.WriteLine($"   Username: {newUser.DefaultUsername}");
            Console.WriteLine($"   Téléphone: {newUser.Telephone}");
            Console.WriteLine($"   ⚠️  Mot de passe par défaut: {motDePasseParDefaut} (à changer à la première connexion)");
            return newUser;
        }

        private async Task<Agent> CreateOrGetAdminAgentAsync(Societe defaultSociete, DateTime currentDate)
        {
            var existingAdmin = await Agents
                .FirstOrDefaultAsync(a => a.IdSociete == defaultSociete.IdSociete && a.Fonction == "Administrateur");
            
            if (existingAdmin != null)
            {
                Console.WriteLine($"Agent Administrateur existe déjà avec l'ID: {existingAdmin.IdAgent}");
                return existingAdmin;
            }
            
            var adminAgent = new Agent
            {
                NomComplet = "Administrateur CongoTravel",
                Genre = "Masculin",
                DateNaissance = DateTime.Now.AddYears(-35),
                TelephoneAgent = "+243888888888",
                EmailAgent = "admin@congotravel.cd",
                Statut = true,
                EtatCivil = "Marié",
                Fonction = "Administrateur",
                RoleAgent = "Admin",
                Matricule = await GenerateUniqueMatriculeAgentAsync(),
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate
            };
            
            Agents.Add(adminAgent);
            await SaveChangesAsync();
            
            Console.WriteLine($"Agent Administrateur créé avec l'ID: {adminAgent.IdAgent} - Matricule: {adminAgent.Matricule}");
            return adminAgent;
        }

        private async Task<Agent> CreateOrGetGerantAgentAsync(Societe defaultSociete, Site defaultSite, DateTime currentDate)
        {
            var existingGerant = await Agents
                .FirstOrDefaultAsync(a => a.IdSociete == defaultSociete.IdSociete && a.Fonction == "Gérant");
            
            if (existingGerant != null)
            {
                Console.WriteLine($"Agent Gérant existe déjà avec l'ID: {existingGerant.IdAgent}");
                if (existingGerant.IdSite == null && defaultSite.IdSociete == defaultSociete.IdSociete)
                {
                    existingGerant.IdSite = defaultSite.IdSite;
                    await SaveChangesAsync();
                    Console.WriteLine($"   ↳ IdSite renseigné avec le site par défaut (ID {defaultSite.IdSite})");
                }
                return existingGerant;
            }
            
            var gerantAgent = new Agent
            {
                NomComplet = "Gérant CongoTravel",
                Genre = "Masculin",
                DateNaissance = DateTime.Now.AddYears(-40),
                TelephoneAgent = "+243777777777",
                EmailAgent = "gerant@congotravel.cd",
                Statut = true,
                EtatCivil = "Marié",
                Fonction = "Gérant",
                RoleAgent = "Gerant",
                Matricule = await GenerateUniqueMatriculeAgentAsync(),
                IdSociete = defaultSociete.IdSociete,
                IdSite = defaultSite.IdSite,
                DateCreation = currentDate
            };
            
            Agents.Add(gerantAgent);
            await SaveChangesAsync();
            
            Console.WriteLine($"Agent Gérant créé avec l'ID: {gerantAgent.IdAgent} - Matricule: {gerantAgent.Matricule}");
            return gerantAgent;
        }

        private async Task<Utilisateur> CreateOrGetGerantWithAgentAsync(Role gerantRole, Societe defaultSociete, Site defaultSite, DateTime currentDate)
        {
            var existingUser = await Utilisateurs
                .FirstOrDefaultAsync(u => u.IdRole == gerantRole.IdRole && u.IdSociete == defaultSociete.IdSociete && u.Email == "gerant@congotravel.cd");
            
            if (existingUser != null)
            {
                Console.WriteLine($"Utilisateur Gerant existe déjà avec l'ID: {existingUser.IdUtilisateur}");
                
                // Vérifier si l'association UserRole existe, sinon la créer
                var existingUserRole = await UserRoles
                    .FirstOrDefaultAsync(ur => ur.IdUtilisateur == existingUser.IdUtilisateur && ur.IdRole == gerantRole.IdRole);
                
                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdRole = gerantRole.IdRole,
                        IsPrimary = true, // Rôle principal
                        DateAttribution = currentDate,
                        Statut = true // Statut actif
                    };
                    
                    UserRoles.Add(userRole);
                    await SaveChangesAsync();
                    Console.WriteLine($"Association UserRole créée pour l'utilisateur existant {existingUser.IdUtilisateur} avec le rôle Gerant");
                }

                // Rattrapage non destructif : lier le site par défaut si encore vide
                var patchNeeded = false;
                if (existingUser.IdSite == null && defaultSite.IdSociete == defaultSociete.IdSociete)
                {
                    existingUser.IdSite = defaultSite.IdSite;
                    patchNeeded = true;
                }

                if (existingUser.IdAgent.HasValue)
                {
                    var linkedAgent = await Agents.FirstOrDefaultAsync(a => a.IdAgent == existingUser.IdAgent.Value);
                    if (linkedAgent != null &&
                        linkedAgent.IdSite == null &&
                        linkedAgent.IdSociete == defaultSociete.IdSociete &&
                        defaultSite.IdSociete == defaultSociete.IdSociete)
                    {
                        linkedAgent.IdSite = defaultSite.IdSite;
                        patchNeeded = true;
                    }
                }

                if (patchNeeded)
                {
                    await SaveChangesAsync();
                    Console.WriteLine($"   ↳ IdSite utilisateur / agent gérant mis à jour avec le site par défaut (ID {defaultSite.IdSite})");
                }
                
                return existingUser;
            }
            
            // 1. Créer l'Agent Gerant
            var gerantAgent = await CreateOrGetGerantAgentAsync(defaultSociete, defaultSite, currentDate);
            
            // 2. Générer le hash du mot de passe par défaut
            // Mot de passe par défaut: "Gerant" (à changer lors de la première connexion)
            string motDePasseParDefaut = "Gerant";
            string motDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut, BCrypt.Net.BCrypt.GenerateSalt(11));
            
            // 3. Créer l'Utilisateur Gerant lié à cet Agent
            var newUser = new Utilisateur
            {
                IdAgent = gerantAgent.IdAgent,
                ReferenceUtilisateur = Guid.NewGuid(),
                NomComplet = gerantAgent.NomComplet,
                Email = "gerant@congotravel.cd",
                DefaultUsername = "Gerant",
                Telephone = "+243777777777",
                MotDePasseHash = motDePasseHash,
                Genre = gerantAgent.Genre,
                DateNaissance = gerantAgent.DateNaissance,
                Statut = true,
                IdRole = gerantRole.IdRole,
                IdSociete = defaultSociete.IdSociete,
                IdSite = defaultSite.IdSite,
                DateCreation = currentDate,
                IsConnecte = false,
                DoitChangerMotDePasse = true // Forcer le changement de mot de passe à la première connexion
            };
            
            Utilisateurs.Add(newUser);
            await SaveChangesAsync();
            
            // 4. Créer l'association UserRole (Multi-rôles)
            var newUserRoleCheck = await UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == newUser.IdUtilisateur && ur.IdRole == gerantRole.IdRole);
            
            if (newUserRoleCheck == null)
            {
                var userRole = new UserRole
                {
                    IdUtilisateur = newUser.IdUtilisateur,
                    IdRole = gerantRole.IdRole,
                    IsPrimary = true, // Rôle principal
                    DateAttribution = currentDate,
                    Statut = true // Statut actif
                };
                
                UserRoles.Add(userRole);
                await SaveChangesAsync();
                Console.WriteLine($"Association UserRole créée pour l'utilisateur {newUser.IdUtilisateur} avec le rôle Gerant");
            }
            
            Console.WriteLine($"Utilisateur Gerant créé avec l'ID: {newUser.IdUtilisateur} (lié à l'Agent {gerantAgent.IdAgent}, site {defaultSite.IdSite})");
            Console.WriteLine($"   Email: {newUser.Email}");
            Console.WriteLine($"   Username: {newUser.DefaultUsername}");
            Console.WriteLine($"   Téléphone: {newUser.Telephone}");
            Console.WriteLine($"   Mot de passe par défaut: {motDePasseParDefaut} (à changer à la première connexion)");
            return newUser;
        }
    }
}
