using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Data
{
    public partial class CongoTravelDbContext
    {
        private static void ConfigureRestaurantEntities(ModelBuilder modelBuilder)
        {
            ConfigureRestaurant(modelBuilder);
            ConfigureRestaurantPhoto(modelBuilder);
            ConfigureRestaurantZone(modelBuilder);
            ConfigureRestaurantCreneau(modelBuilder);
            ConfigureRestaurantCreneauGlobalQuota(modelBuilder);
            ConfigureRestaurantCreneauZoneQuota(modelBuilder);
            ConfigureRestaurantReservation(modelBuilder);
            ConfigureRestaurantReservationLine(modelBuilder);
            ConfigureRestaurantTicket(modelBuilder);
            ConfigureRestaurantCommandeEnAttente(modelBuilder);
            ConfigureRestaurantPayment(modelBuilder);
            ConfigureRestaurantPlanification(modelBuilder);
            ConfigureRestaurantPlanificationPlage(modelBuilder);
            ConfigureRestaurantPlanifPlageGlobalQuota(modelBuilder);
            ConfigureRestaurantPlanifPlageZoneQuota(modelBuilder);
            ConfigureRestaurantPlanifGenerationLog(modelBuilder);
        }

        private static void ConfigureRestaurant(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Restaurant>(entity =>
            {
                entity.ToTable("Restaurants");
                entity.Property(e => e.CodeRestaurant).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Adresse).HasMaxLength(500);
                entity.Property(e => e.AcomptePourcentDefaut)
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(0m);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('Draft','Published','Closed','Cancelled')")
                    .HasDefaultValue(RestaurantStatus.Draft);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Site)
                    .WithMany()
                    .HasForeignKey(e => e.IdSite)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdSociete, e.CodeRestaurant })
                    .IsUnique()
                    .HasDatabaseName("IX_Restaurants_Societe_CodeRestaurant_UQ");

                entity.HasIndex(e => e.IdSite)
                    .HasDatabaseName("IX_Restaurants_IdSite");
            });
        }

        private static void ConfigureRestaurantPhoto(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantPhoto>(entity =>
            {
                entity.ToTable("RestaurantPhotos");
                entity.Property(e => e.PhotoData).IsRequired(false).HasColumnType("mediumblob");
                entity.Property(e => e.StorageKey).HasMaxLength(500);
                entity.Property(e => e.Ordre).IsRequired();
                entity.Property(e => e.Statut).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.OriginalFileName).HasMaxLength(100);
                entity.Property(e => e.TypeMIME).HasMaxLength(50);

                entity.HasOne(e => e.Restaurant)
                    .WithMany(r => r.Photos)
                    .HasForeignKey(e => e.IdRestaurant)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.IdRestaurant, e.Ordre })
                    .IsUnique()
                    .HasDatabaseName("IX_RestaurantPhotos_Restaurant_Ordre_UQ");

                entity.HasIndex(e => e.IdRestaurant)
                    .HasDatabaseName("IX_RestaurantPhotos_IdRestaurant");
            });
        }

        private static void ConfigureRestaurantZone(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantZone>(entity =>
            {
                entity.ToTable("RestaurantZones");
                entity.Property(e => e.Code).HasMaxLength(64);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Actif).HasDefaultValue(true);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Restaurant)
                    .WithMany(r => r.Zones)
                    .HasForeignKey(e => e.IdRestaurant)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdRestaurant, e.Code })
                    .IsUnique()
                    .HasDatabaseName("IX_RestaurantZones_Restaurant_Code_UQ");

                entity.HasIndex(e => e.IdRestaurant)
                    .HasDatabaseName("IX_RestaurantZones_IdRestaurant");

                entity.HasIndex(e => e.IdSociete)
                    .HasDatabaseName("IX_RestaurantZones_IdSociete");
            });
        }

        private static void ConfigureRestaurantCreneau(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantCreneau>(entity =>
            {
                entity.ToTable("RestaurantCreneaux");
                entity.Property(e => e.DateService).HasColumnType("date");
                entity.Property(e => e.InventoryMode)
                    .HasConversion<string>()
                    .HasColumnType("enum('GlobalQuota','ClassQuota')");
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('Draft','Published','Closed','Cancelled')")
                    .HasDefaultValue(RestaurantStatus.Draft);
                entity.Property(e => e.MontantAcompte).HasColumnType("decimal(18,2)");
                ConfigureRestaurantCodeDevise(entity.Property(e => e.CodeDevise));

                entity.HasCheckConstraint(
                    "CK_RestaurantCreneaux_StartEnd",
                    "`EndAtUtc` > `StartAtUtc`");

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Restaurant)
                    .WithMany(r => r.Creneaux)
                    .HasForeignKey(e => e.IdRestaurant)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Planification)
                    .WithMany(p => p.CreneauxGeneres)
                    .HasForeignKey(e => e.IdRestaurantPlanification)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.PlanificationPlage)
                    .WithMany(p => p.CreneauxGeneres)
                    .HasForeignKey(e => e.IdRestaurantPlanificationPlage)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.GlobalQuota)
                    .WithOne(q => q.Creneau)
                    .HasForeignKey<RestaurantCreneauGlobalQuota>(q => q.IdRestaurantCreneau)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.IdRestaurant, e.StartAtUtc })
                    .HasDatabaseName("IX_RestaurantCreneaux_IdRestaurant_StartAtUtc");

                entity.HasIndex(e => new { e.IdSociete, e.DateService })
                    .HasDatabaseName("IX_RestaurantCreneaux_IdSociete_DateService");

                entity.HasIndex(e => e.IdRestaurant)
                    .HasDatabaseName("IX_RestaurantCreneaux_IdRestaurant");

                entity.HasIndex(e => e.IdRestaurantPlanification)
                    .HasDatabaseName("IX_RestaurantCreneaux_IdRestaurantPlanification");

                entity.HasIndex(e => e.IdRestaurantPlanificationPlage)
                    .HasDatabaseName("IX_RestaurantCreneaux_IdRestaurantPlanificationPlage");
            });
        }

        private static void ConfigureRestaurantCreneauGlobalQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantCreneauGlobalQuota>(entity =>
            {
                entity.ToTable("RestaurantCreneauGlobalQuotas");
                entity.Property(e => e.QuantiteHold).HasDefaultValue(0);
                entity.Property(e => e.QuantiteVendue).HasDefaultValue(0);
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");

                entity.HasCheckConstraint("CK_RestaurantCreneauGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
                entity.HasCheckConstraint(
                    "CK_RestaurantCreneauGlobalQuotas_StockPositive",
                    "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                entity.HasCheckConstraint(
                    "CK_RestaurantCreneauGlobalQuotas_StockMax",
                    "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
            });
        }

        private static void ConfigureRestaurantCreneauZoneQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantCreneauZoneQuota>(entity =>
            {
                entity.ToTable("RestaurantCreneauZoneQuotas");
                entity.Property(e => e.QuantiteHold).HasDefaultValue(0);
                entity.Property(e => e.QuantiteVendue).HasDefaultValue(0);
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Creneau)
                    .WithMany(c => c.ZoneQuotas)
                    .HasForeignKey(e => e.IdRestaurantCreneau)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Zone)
                    .WithMany(z => z.ZoneQuotas)
                    .HasForeignKey(e => e.IdRestaurantZone)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdRestaurantCreneau, e.IdRestaurantZone })
                    .IsUnique()
                    .HasDatabaseName("IX_RestaurantCreneauZoneQuotas_Creneau_Zone_UQ");

                entity.HasIndex(e => e.IdRestaurantCreneau)
                    .HasDatabaseName("IX_RestaurantCreneauZoneQuotas_IdRestaurantCreneau");

                entity.HasCheckConstraint("CK_RestaurantCreneauZoneQuotas_Capacite", "`CapaciteTotale` >= 0");
                entity.HasCheckConstraint(
                    "CK_RestaurantCreneauZoneQuotas_StockPositive",
                    "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                entity.HasCheckConstraint(
                    "CK_RestaurantCreneauZoneQuotas_StockMax",
                    "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
            });
        }

        private static void ConfigureRestaurantReservation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantReservation>(entity =>
            {
                entity.ToTable("RestaurantReservations");
                entity.Property(e => e.ReferenceReservation).IsRequired().HasMaxLength(64);
                entity.Property(e => e.CustomerRef).HasMaxLength(100);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('HOLD','CONFIRMED','CANCELLED','EXPIRED')");
                entity.Property(e => e.MontantSousTotal).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
                ConfigureRestaurantCodeDevise(entity.Property(e => e.CodeDevise));
                entity.Property(e => e.IdempotencyKey).HasMaxLength(120);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Site)
                    .WithMany()
                    .HasForeignKey(e => e.IdSite)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Restaurant)
                    .WithMany(r => r.Reservations)
                    .HasForeignKey(e => e.IdRestaurant)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Creneau)
                    .WithMany(c => c.Reservations)
                    .HasForeignKey(e => e.IdRestaurantCreneau)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdSociete, e.ReferenceReservation })
                    .IsUnique()
                    .HasDatabaseName("IX_RestaurantReservations_Societe_Reference_UQ");

                entity.HasIndex(e => new { e.IdSociete, e.IdempotencyKey })
                    .IsUnique()
                    .HasDatabaseName("IX_RestaurantReservations_Societe_Idempotency_UQ");

                entity.HasIndex(e => new { e.Status, e.ExpiresAtUtc })
                    .HasDatabaseName("IX_RestaurantReservations_Status_ExpiresAtUtc");

                entity.HasIndex(e => new { e.IdRestaurantCreneau, e.Status })
                    .HasDatabaseName("IX_RestaurantReservations_Creneau_Status");

                entity.HasIndex(e => e.IdSite)
                    .HasDatabaseName("IX_RestaurantReservations_IdSite");

                entity.HasIndex(e => e.IdUtilisateur)
                    .HasDatabaseName("IX_RestaurantReservations_IdUtilisateur");

                entity.HasIndex(e => e.IdClient)
                    .HasDatabaseName("IX_RestaurantReservations_IdClient");

                entity.HasIndex(e => e.IdRestaurant)
                    .HasDatabaseName("IX_RestaurantReservations_IdRestaurant");

                entity.HasOne(e => e.Client)
                    .WithMany()
                    .HasForeignKey(e => e.IdClient)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureRestaurantReservationLine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantReservationLine>(entity =>
            {
                entity.ToTable("RestaurantReservationLines");
                entity.Property(e => e.LineType)
                    .HasConversion<string>()
                    .HasColumnType("enum('GlobalQuota','ClassQuota')");
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontantLigne).HasColumnType("decimal(18,2)");
                ConfigureRestaurantCodeDevise(entity.Property(e => e.CodeDevise));

                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.Lines)
                    .HasForeignKey(e => e.IdRestaurantReservation)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.GlobalQuota)
                    .WithMany(q => q.ReservationLines)
                    .HasForeignKey(e => e.IdRestaurantCreneauGlobalQuota)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ZoneQuota)
                    .WithMany(q => q.ReservationLines)
                    .HasForeignKey(e => e.IdRestaurantCreneauZoneQuota)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.IdRestaurantReservation)
                    .HasDatabaseName("IX_RestaurantReservationLines_IdReservation");

                entity.HasIndex(e => e.IdRestaurantCreneauZoneQuota)
                    .HasDatabaseName("IX_RestaurantReservationLines_IdZoneQuota");

                entity.HasCheckConstraint(
                    "CK_RestaurantReservationLines_Quantite",
                    "`Quantite` > 0");
            });
        }

        private static void ConfigureRestaurantTicket(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantTicket>(entity =>
            {
                entity.ToTable("RestaurantTickets");
                entity.Property(e => e.TicketCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('ISSUED','USED','VOID')")
                    .HasDefaultValue(RestaurantTicketStatus.ISSUED);

                entity.HasOne(e => e.ReservationLine)
                    .WithMany(l => l.Tickets)
                    .HasForeignKey(e => e.IdRestaurantReservationLine)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.TicketCode)
                    .IsUnique()
                    .HasDatabaseName("IX_RestaurantTickets_TicketCode_UQ");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_RestaurantTickets_Status");
            });
        }

        private static void ConfigureRestaurantPayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantPayment>(entity =>
            {
                entity.ToTable("RestaurantPayments");
                entity.Property(e => e.ReferencePaiement).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(40);
                entity.Property(e => e.ProviderTxRef).HasMaxLength(120);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('PENDING','SUCCEEDED','FAILED','REFUNDED')");
                entity.Property(e => e.Montant).HasColumnType("decimal(18,2)");
                ConfigureRestaurantCodeDevise(entity.Property(e => e.CodeDevise));
                entity.Property(e => e.MontantTarif).HasColumnType("decimal(18,2)");
                ConfigureRestaurantCodeDevise(entity.Property(e => e.CodeDeviseTarif));
                entity.Property(e => e.TauxVersDevisePaiement)
                    .HasColumnType("decimal(18,8)")
                    .HasDefaultValue(1m);
                entity.Property(e => e.IdempotencyKey).HasMaxLength(120);

                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.Payments)
                    .HasForeignKey(e => e.IdRestaurantReservation)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CommandeEnAttente)
                    .WithMany()
                    .HasForeignKey(e => e.IdRestaurantCommandeEnAttente)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Site)
                    .WithMany()
                    .HasForeignKey(e => e.IdSite)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ReferencePaiement)
                    .IsUnique()
                    .HasDatabaseName("IX_RestaurantPayments_ReferencePaiement_UQ");

                entity.HasIndex(e => e.IdempotencyKey)
                    .IsUnique()
                    .HasDatabaseName("IX_RestaurantPayments_Idempotency_UQ");

                entity.HasIndex(e => new { e.IdRestaurantReservation, e.Status })
                    .HasDatabaseName("IX_RestaurantPayments_Reservation_Status");

                entity.HasIndex(e => e.IdRestaurantCommandeEnAttente)
                    .HasDatabaseName("IX_RestaurantPayments_IdRestaurantCommandeEnAttente");

                entity.HasIndex(e => e.IdSite)
                    .HasDatabaseName("IX_RestaurantPayments_IdSite");
            });
        }

        private static void ConfigureRestaurantCommandeEnAttente(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantCommandeEnAttente>(entity =>
            {
                entity.ToTable("RestaurantCommandesEnAttente");
                entity.Property(e => e.MethodePaiement).IsRequired().HasMaxLength(50);
                entity.Property(e => e.MontantTarif).HasColumnType("decimal(18,2)");
                ConfigureRestaurantCodeDevise(entity.Property(e => e.CodeDeviseTarif));
                entity.Property(e => e.MontantFlexPay).HasColumnType("decimal(18,2)");
                ConfigureRestaurantCodeDevise(entity.Property(e => e.CodeDevisePaiement));
                entity.Property(e => e.TauxVersDevisePaiement).HasColumnType("decimal(18,8)").HasDefaultValue(1m);
                entity.Property(e => e.OrderNumberFlexPay).HasMaxLength(120);
                entity.Property(e => e.ReferenceFlexPay).HasMaxLength(120);
                entity.Property(e => e.IdempotencyKey).HasMaxLength(120);
                entity.Property(e => e.PayloadMetierJson).IsRequired();
                entity.HasOne(e => e.Creneau).WithMany().HasForeignKey(e => e.IdRestaurantCreneau).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.IdSite).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.PaiementEnAttente).WithMany().HasForeignKey(e => e.IdPaiementEnAttente).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.DateExpiration).HasDatabaseName("IX_RestaurantCommandesEnAttente_DateExpiration");
                entity.HasIndex(e => e.OrderNumberFlexPay).HasDatabaseName("IX_RestaurantCommandesEnAttente_OrderNumberFlexPay");
                entity.HasIndex(e => e.IdempotencyKey).IsUnique().HasDatabaseName("IX_RestaurantCommandesEnAttente_Idempotency_UQ");
                entity.HasIndex(e => new { e.IdSociete, e.IdRestaurantCreneau }).HasDatabaseName("IX_RestaurantCommandesEnAttente_Societe_Creneau");
            });
        }

        private static void ConfigureRestaurantPlanification(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantPlanification>(entity =>
            {
                entity.ToTable("RestaurantPlanifications");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                ConfigureRestaurantCodeDevise(entity.Property(e => e.CodeDevise));
                entity.Property(e => e.MontantAcompte).HasColumnType("decimal(18,2)");
                entity.Property(e => e.InventoryMode)
                    .HasConversion<string>()
                    .HasColumnType("enum('GlobalQuota','ClassQuota')");
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

                entity.HasOne(e => e.Restaurant)
                    .WithMany(r => r.Planifications)
                    .HasForeignKey(e => e.IdRestaurant)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.IdSociete)
                    .HasDatabaseName("IX_RestaurantPlanifications_IdSociete");

                entity.HasIndex(e => e.IdRestaurant)
                    .HasDatabaseName("IX_RestaurantPlanifications_IdRestaurant");
            });
        }

        private static void ConfigureRestaurantPlanificationPlage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantPlanificationPlage>(entity =>
            {
                entity.ToTable("RestaurantPlanificationPlages");
                entity.Property(e => e.Libelle).HasMaxLength(120);
                entity.Property(e => e.StartTime).HasColumnType("time");
                entity.Property(e => e.EndTime).HasColumnType("time");

                entity.HasCheckConstraint(
                    "CK_RestaurantPlanificationPlages_StartEnd",
                    "`EndTime` > `StartTime`");

                entity.HasOne(e => e.Planification)
                    .WithMany(p => p.Plages)
                    .HasForeignKey(e => e.IdRestaurantPlanification)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.GlobalQuota)
                    .WithOne(q => q.Plage)
                    .HasForeignKey<RestaurantPlanifPlageGlobalQuota>(q => q.IdRestaurantPlanificationPlage)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdRestaurantPlanification)
                    .HasDatabaseName("IX_RestaurantPlanificationPlages_IdPlanification");
            });
        }

        private static void ConfigureRestaurantPlanifPlageGlobalQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantPlanifPlageGlobalQuota>(entity =>
            {
                entity.ToTable("RestaurantPlanifPlageGlobalQuotas");
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");
                entity.HasCheckConstraint("CK_RestaurantPlanifPlageGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
            });
        }

        private static void ConfigureRestaurantPlanifPlageZoneQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantPlanifPlageZoneQuota>(entity =>
            {
                entity.ToTable("RestaurantPlanifPlageZoneQuotas");
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Plage)
                    .WithMany(p => p.ZoneQuotas)
                    .HasForeignKey(e => e.IdRestaurantPlanificationPlage)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Zone)
                    .WithMany()
                    .HasForeignKey(e => e.IdRestaurantZone)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdRestaurantPlanificationPlage, e.IdRestaurantZone })
                    .IsUnique()
                    .HasDatabaseName("IX_RestaurantPlanifPlageZoneQuotas_Plage_Zone_UQ");

                entity.HasIndex(e => e.IdRestaurantPlanificationPlage)
                    .HasDatabaseName("IX_RestaurantPlanifPlageZoneQuotas_IdPlage");

                entity.HasCheckConstraint("CK_RestaurantPlanifPlageZoneQuotas_Capacite", "`CapaciteTotale` >= 0");
            });
        }

        private static void ConfigureRestaurantPlanifGenerationLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantPlanifGenerationLog>(entity =>
            {
                entity.ToTable("RestaurantPlanifGenerationLogs");
                entity.Property(e => e.DetailsJson).IsRequired().HasColumnType("longtext");
                entity.Property(e => e.NombrePublies).HasDefaultValue(0);

                entity.HasOne(e => e.Planification)
                    .WithMany(p => p.GenerationLogs)
                    .HasForeignKey(e => e.IdRestaurantPlanification)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdRestaurantPlanification)
                    .HasDatabaseName("IX_RestaurantPlanifGenerationLogs_IdPlanification");
            });
        }

        private static void ConfigureRestaurantCodeDevise(PropertyBuilder<string> property)
        {
            property.IsRequired().IsFixedLength().HasMaxLength(3).HasDefaultValue("CDF");
        }
    }
}
