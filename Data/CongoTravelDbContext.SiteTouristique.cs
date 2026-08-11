using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Data
{
    public partial class CongoTravelDbContext
    {
        private static void ConfigureSiteTouristiqueEntities(ModelBuilder modelBuilder)
        {
            ConfigureSiteTouristiqueLieu(modelBuilder);
            ConfigureSiteTouristiqueClasse(modelBuilder);
            ConfigureSiteTouristiqueJournee(modelBuilder);
            ConfigureSiteTouristiqueGlobalQuota(modelBuilder);
            ConfigureSiteTouristiqueClassQuota(modelBuilder);
            ConfigureSiteTouristiqueReservation(modelBuilder);
            ConfigureSiteTouristiqueReservationLine(modelBuilder);
            ConfigureSiteTouristiqueTicket(modelBuilder);
            ConfigureSiteTouristiquePayment(modelBuilder);
            ConfigureSiteTouristiquePlanification(modelBuilder);
            ConfigureSiteTouristiquePlanifGlobalQuota(modelBuilder);
            ConfigureSiteTouristiquePlanifClassQuota(modelBuilder);
            ConfigureSiteTouristiquePlanifGenerationLog(modelBuilder);
        }

        private static void ConfigureSiteTouristiqueLieu(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiqueLieu>(entity =>
            {
                entity.ToTable("SiteTouristiques");
                entity.Property(e => e.CodeLieu).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('Draft','Published','Closed','Cancelled')")
                    .HasDefaultValue(SiteTouristiqueStatus.Draft);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Site)
                    .WithMany()
                    .HasForeignKey(e => e.IdSite)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdSociete, e.CodeLieu })
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiques_Societe_CodeLieu_UQ");

                entity.HasIndex(e => e.IdSite)
                    .HasDatabaseName("IX_SiteTouristiques_IdSite");
            });
        }

        private static void ConfigureSiteTouristiqueClasse(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiqueClasse>(entity =>
            {
                entity.ToTable("SiteTouristiqueClasses");
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Actif).HasDefaultValue(true);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdSociete, e.Code })
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiqueClasses_Societe_Code_UQ");

                entity.HasIndex(e => e.IdSociete)
                    .HasDatabaseName("IX_SiteTouristiqueClasses_IdSociete");
            });
        }

        private static void ConfigureSiteTouristiqueJournee(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiqueJournee>(entity =>
            {
                entity.ToTable("SiteTouristiqueJournees");
                entity.Property(e => e.DateVisite).HasColumnType("date");
                entity.Property(e => e.InventoryMode)
                    .HasConversion<string>()
                    .HasColumnType("enum('ClassQuota','GlobalQuota')");
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('Draft','Published','Closed','Cancelled')")
                    .HasDefaultValue(SiteTouristiqueStatus.Draft);
                ConfigureSiteTouristiqueCodeDevise(entity.Property(e => e.CodeDevise));

                entity.HasCheckConstraint(
                    "CK_SiteTouristiqueJournees_SalesWindow",
                    "`SalesCloseAtUtc` IS NULL OR `SalesOpenAtUtc` IS NULL OR `SalesCloseAtUtc` >= `SalesOpenAtUtc`");

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Lieu)
                    .WithMany(l => l.Journees)
                    .HasForeignKey(e => e.IdSiteTouristique)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Planification)
                    .WithMany(p => p.JourneesGenerees)
                    .HasForeignKey(e => e.IdSiteTouristiquePlanification)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.GlobalQuota)
                    .WithOne(q => q.Journee)
                    .HasForeignKey<SiteTouristiqueGlobalQuota>(q => q.IdSiteTouristiqueJournee)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.IdSiteTouristique, e.DateVisite })
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiqueJournees_Lieu_DateVisite_UQ");

                entity.HasIndex(e => new { e.IdSociete, e.DateVisite })
                    .HasDatabaseName("IX_SiteTouristiqueJournees_IdSociete_DateVisite");

                entity.HasIndex(e => e.IdSiteTouristique)
                    .HasDatabaseName("IX_SiteTouristiqueJournees_IdSiteTouristique");

                entity.HasIndex(e => e.IdSiteTouristiquePlanification)
                    .HasDatabaseName("IX_SiteTouristiqueJournees_IdSiteTouristiquePlanification");
            });
        }

        private static void ConfigureSiteTouristiqueGlobalQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiqueGlobalQuota>(entity =>
            {
                entity.ToTable("SiteTouristiqueGlobalQuotas");
                entity.Property(e => e.QuantiteHold).HasDefaultValue(0);
                entity.Property(e => e.QuantiteVendue).HasDefaultValue(0);
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");

                entity.HasCheckConstraint("CK_SiteTouristiqueGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
                entity.HasCheckConstraint(
                    "CK_SiteTouristiqueGlobalQuotas_StockPositive",
                    "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                entity.HasCheckConstraint(
                    "CK_SiteTouristiqueGlobalQuotas_StockMax",
                    "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
            });
        }

        private static void ConfigureSiteTouristiqueClassQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiqueClassQuota>(entity =>
            {
                entity.ToTable("SiteTouristiqueClassQuotas");
                entity.Property(e => e.QuantiteHold).HasDefaultValue(0);
                entity.Property(e => e.QuantiteVendue).HasDefaultValue(0);
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Journee)
                    .WithMany(j => j.ClassQuotas)
                    .HasForeignKey(e => e.IdSiteTouristiqueJournee)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Classe)
                    .WithMany(c => c.ClassQuotas)
                    .HasForeignKey(e => e.IdSiteTouristiqueClasse)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdSiteTouristiqueJournee, e.IdSiteTouristiqueClasse })
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiqueClassQuotas_Journee_Classe_UQ");

                entity.HasIndex(e => e.IdSiteTouristiqueJournee)
                    .HasDatabaseName("IX_SiteTouristiqueClassQuotas_IdSiteTouristiqueJournee");

                entity.HasCheckConstraint("CK_SiteTouristiqueClassQuotas_Capacite", "`CapaciteTotale` >= 0");
                entity.HasCheckConstraint(
                    "CK_SiteTouristiqueClassQuotas_StockPositive",
                    "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                entity.HasCheckConstraint(
                    "CK_SiteTouristiqueClassQuotas_StockMax",
                    "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
            });
        }

        private static void ConfigureSiteTouristiqueReservation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiqueReservation>(entity =>
            {
                entity.ToTable("SiteTouristiqueReservations");
                entity.Property(e => e.ReferenceReservation).IsRequired().HasMaxLength(64);
                entity.Property(e => e.CustomerRef).HasMaxLength(100);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('HOLD','CONFIRMED','CANCELLED','EXPIRED')");
                entity.Property(e => e.MontantSousTotal).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
                ConfigureSiteTouristiqueCodeDevise(entity.Property(e => e.CodeDevise));
                entity.Property(e => e.IdempotencyKey).HasMaxLength(120);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Site)
                    .WithMany()
                    .HasForeignKey(e => e.IdSite)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Journee)
                    .WithMany(j => j.Reservations)
                    .HasForeignKey(e => e.IdSiteTouristiqueJournee)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdSociete, e.ReferenceReservation })
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiqueReservations_Societe_Reference_UQ");

                entity.HasIndex(e => new { e.IdSociete, e.IdempotencyKey })
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiqueReservations_Societe_Idempotency_UQ");

                entity.HasIndex(e => new { e.Status, e.ExpiresAtUtc })
                    .HasDatabaseName("IX_SiteTouristiqueReservations_Status_ExpiresAtUtc");

                entity.HasIndex(e => new { e.IdSiteTouristiqueJournee, e.Status })
                    .HasDatabaseName("IX_SiteTouristiqueReservations_Journee_Status");

                entity.HasIndex(e => e.IdSite)
                    .HasDatabaseName("IX_SiteTouristiqueReservations_IdSite");

                entity.HasIndex(e => e.IdUtilisateur)
                    .HasDatabaseName("IX_SiteTouristiqueReservations_IdUtilisateur");
            });
        }

        private static void ConfigureSiteTouristiqueReservationLine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiqueReservationLine>(entity =>
            {
                entity.ToTable("SiteTouristiqueReservationLines");
                entity.Property(e => e.LineType)
                    .HasConversion<string>()
                    .HasColumnType("enum('ClassQuota','GlobalQuota')");
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");
                ConfigureSiteTouristiqueCodeDevise(entity.Property(e => e.CodeDevise));

                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.Lines)
                    .HasForeignKey(e => e.IdSiteTouristiqueReservation)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ClassQuota)
                    .WithMany(q => q.ReservationLines)
                    .HasForeignKey(e => e.IdSiteTouristiqueClassQuota)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.IdSiteTouristiqueReservation)
                    .HasDatabaseName("IX_SiteTouristiqueReservationLines_IdReservation");

                entity.HasCheckConstraint(
                    "CK_SiteTouristiqueReservationLines_Quantite",
                    "`Quantite` > 0");
            });
        }

        private static void ConfigureSiteTouristiqueTicket(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiqueTicket>(entity =>
            {
                entity.ToTable("SiteTouristiqueTickets");
                entity.Property(e => e.TicketCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('ISSUED','USED','VOID')")
                    .HasDefaultValue(SiteTouristiqueTicketStatus.ISSUED);

                entity.HasOne(e => e.ReservationLine)
                    .WithMany(l => l.Tickets)
                    .HasForeignKey(e => e.IdSiteTouristiqueReservationLine)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.TicketCode)
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiqueTickets_TicketCode_UQ");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_SiteTouristiqueTickets_Status");
            });
        }

        private static void ConfigureSiteTouristiquePayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiquePayment>(entity =>
            {
                entity.ToTable("SiteTouristiquePayments");
                entity.Property(e => e.ReferencePaiement).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(40);
                entity.Property(e => e.ProviderTxRef).HasMaxLength(120);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('PENDING','SUCCEEDED','FAILED','REFUNDED')");
                entity.Property(e => e.Montant).HasColumnType("decimal(18,2)");
                ConfigureSiteTouristiqueCodeDevise(entity.Property(e => e.CodeDevise));
                entity.Property(e => e.MontantTarif).HasColumnType("decimal(18,2)");
                ConfigureSiteTouristiqueCodeDevise(entity.Property(e => e.CodeDeviseTarif));
                entity.Property(e => e.TauxVersDevisePaiement)
                    .HasColumnType("decimal(18,8)")
                    .HasDefaultValue(1m);
                entity.Property(e => e.IdempotencyKey).HasMaxLength(120);

                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.Payments)
                    .HasForeignKey(e => e.IdSiteTouristiqueReservation)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Site)
                    .WithMany()
                    .HasForeignKey(e => e.IdSite)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ReferencePaiement)
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiquePayments_ReferencePaiement_UQ");

                entity.HasIndex(e => e.IdempotencyKey)
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiquePayments_Idempotency_UQ");

                entity.HasIndex(e => new { e.IdSiteTouristiqueReservation, e.Status })
                    .HasDatabaseName("IX_SiteTouristiquePayments_Reservation_Status");

                entity.HasIndex(e => e.IdSite)
                    .HasDatabaseName("IX_SiteTouristiquePayments_IdSite");
            });
        }

        private static void ConfigureSiteTouristiquePlanification(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiquePlanification>(entity =>
            {
                entity.ToTable("SiteTouristiquePlanifications");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                ConfigureSiteTouristiqueCodeDevise(entity.Property(e => e.CodeDevise));
                entity.Property(e => e.InventoryMode)
                    .HasConversion<string>()
                    .HasColumnType("enum('ClassQuota','GlobalQuota')");
                entity.Property(e => e.JoursSemaine)
                    .HasConversion(
                        v => PlanificationVoyageJsonConverters.SerializeJoursSemaine(v),
                        v => PlanificationVoyageJsonConverters.DeserializeJoursSemaine(v),
                        PlanificationVoyageJsonConverters.JoursSemaineComparer)
                    .HasColumnType("longtext");
                entity.Property(e => e.Statut).HasDefaultValue(true);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Lieu)
                    .WithMany(l => l.Planifications)
                    .HasForeignKey(e => e.IdSiteTouristique)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.GlobalQuota)
                    .WithOne(q => q.Planification)
                    .HasForeignKey<SiteTouristiquePlanifGlobalQuota>(q => q.IdSiteTouristiquePlanification)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdSociete)
                    .HasDatabaseName("IX_SiteTouristiquePlanifications_IdSociete");

                entity.HasIndex(e => e.IdSiteTouristique)
                    .HasDatabaseName("IX_SiteTouristiquePlanifications_IdSiteTouristique");
            });
        }

        private static void ConfigureSiteTouristiquePlanifGlobalQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiquePlanifGlobalQuota>(entity =>
            {
                entity.ToTable("SiteTouristiquePlanifGlobalQuotas");
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");
                entity.HasCheckConstraint("CK_SiteTouristiquePlanifGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
            });
        }

        private static void ConfigureSiteTouristiquePlanifClassQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiquePlanifClassQuota>(entity =>
            {
                entity.ToTable("SiteTouristiquePlanifClassQuotas");
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Planification)
                    .WithMany(p => p.ClassQuotas)
                    .HasForeignKey(e => e.IdSiteTouristiquePlanification)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Classe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSiteTouristiqueClasse)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdSiteTouristiquePlanification, e.IdSiteTouristiqueClasse })
                    .IsUnique()
                    .HasDatabaseName("IX_SiteTouristiquePlanifClassQuotas_Planif_Classe_UQ");

                entity.HasIndex(e => e.IdSiteTouristiquePlanification)
                    .HasDatabaseName("IX_SiteTouristiquePlanifClassQuotas_IdPlanification");

                entity.HasCheckConstraint("CK_SiteTouristiquePlanifClassQuotas_Capacite", "`CapaciteTotale` >= 0");
            });
        }

        private static void ConfigureSiteTouristiquePlanifGenerationLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SiteTouristiquePlanifGenerationLog>(entity =>
            {
                entity.ToTable("SiteTouristiquePlanifGenerationLogs");
                entity.Property(e => e.DetailsJson).IsRequired().HasColumnType("longtext");

                entity.HasOne(e => e.Planification)
                    .WithMany(p => p.GenerationLogs)
                    .HasForeignKey(e => e.IdSiteTouristiquePlanification)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdSiteTouristiquePlanification)
                    .HasDatabaseName("IX_SiteTouristiquePlanifGenerationLogs_IdPlanification");
            });
        }

        private static void ConfigureSiteTouristiqueCodeDevise(PropertyBuilder<string> property)
        {
            property.IsRequired().IsFixedLength().HasMaxLength(3).HasDefaultValue("CDF");
        }
    }
}
