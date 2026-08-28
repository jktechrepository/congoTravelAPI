using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Data
{
    public partial class CongoTravelDbContext
    {
        private static void ConfigureHotelEntities(ModelBuilder modelBuilder)
        {
            ConfigureHotel(modelBuilder);
            ConfigureHotelPhoto(modelBuilder);
            ConfigureHotelRoomType(modelBuilder);
            ConfigureHotelRoom(modelBuilder);
            ConfigureHotelRoomAssignment(modelBuilder);
            ConfigureHotelExtra(modelBuilder);
            ConfigureHotelReservationExtra(modelBuilder);
            ConfigureHotelNightAllotment(modelBuilder);
            ConfigureHotelNight(modelBuilder);
            ConfigureHotelPlanification(modelBuilder);
            ConfigureHotelPlanificationLigne(modelBuilder);
            ConfigureHotelPlanifGlobalQuota(modelBuilder);
            ConfigureHotelPlanifGenerationLog(modelBuilder);
            ConfigureHotelReservation(modelBuilder);
            ConfigureHotelReservationLine(modelBuilder);
            ConfigureHotelPayment(modelBuilder);
            ConfigureHotelCommandeEnAttente(modelBuilder);
        }

        private static void ConfigureHotel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Hotel>(entity =>
            {
                entity.ToTable("Hotels");
                entity.Property(e => e.CodeHotel).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Adresse).HasMaxLength(500);
                entity.Property(e => e.AcomptePourcentDefaut).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
                entity.Property(e => e.Status).HasConversion<string>()
                    .HasColumnType("enum('Draft','Published','Closed','Cancelled')")
                    .HasDefaultValue(HotelStatus.Draft);
                entity.HasOne(e => e.Societe).WithMany().HasForeignKey(e => e.IdSociete).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.IdSite).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.IdSociete, e.CodeHotel }).IsUnique()
                    .HasDatabaseName("IX_Hotels_Societe_CodeHotel_UQ");
                entity.HasIndex(e => e.IdSite).HasDatabaseName("IX_Hotels_IdSite");
            });
        }

        private static void ConfigureHotelPhoto(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelPhoto>(entity =>
            {
                entity.ToTable("HotelPhotos");
                entity.Property(e => e.PhotoData).IsRequired(false).HasColumnType("mediumblob");
                entity.Property(e => e.StorageKey).HasMaxLength(500);
                entity.Property(e => e.Statut).HasDefaultValue(true);
                entity.Property(e => e.OriginalFileName).HasMaxLength(100);
                entity.Property(e => e.TypeMIME).HasMaxLength(50);
                entity.HasOne(e => e.Hotel).WithMany(h => h.Photos).HasForeignKey(e => e.IdHotel)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.IdHotel, e.Ordre }).IsUnique()
                    .HasDatabaseName("IX_HotelPhotos_Hotel_Ordre_UQ");
                entity.HasIndex(e => e.IdHotel).HasDatabaseName("IX_HotelPhotos_IdHotel");
            });
        }

        private static void ConfigureHotelRoomType(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelRoomType>(entity =>
            {
                entity.ToTable("HotelRoomTypes");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.PrixNuitReference).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CodeDevise).IsFixedLength().HasMaxLength(3);
                entity.Property(e => e.Status).HasConversion<string>()
                    .HasColumnType("enum('Draft','Published','Closed','Cancelled')")
                    .HasDefaultValue(HotelStatus.Draft);
                entity.HasOne(e => e.Societe).WithMany().HasForeignKey(e => e.IdSociete).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Hotel).WithMany(h => h.RoomTypes).HasForeignKey(e => e.IdHotel)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.IdHotel, e.Code }).IsUnique()
                    .HasDatabaseName("IX_HotelRoomTypes_Hotel_Code_UQ");
                entity.HasIndex(e => e.IdSociete).HasDatabaseName("IX_HotelRoomTypes_IdSociete");
                entity.HasIndex(e => e.IdHotel).HasDatabaseName("IX_HotelRoomTypes_IdHotel");
            });
        }

        private static void ConfigureHotelRoom(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelRoom>(entity =>
            {
                entity.ToTable("HotelRooms");
                entity.Property(e => e.Numero).IsRequired().HasMaxLength(32);
                entity.Property(e => e.Etage).HasMaxLength(32);
                entity.Property(e => e.Libelle).HasMaxLength(120);
                entity.Property(e => e.IsActif).HasDefaultValue(true);
                entity.HasOne(e => e.Societe).WithMany().HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Hotel).WithMany(h => h.Rooms).HasForeignKey(e => e.IdHotel)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.RoomType).WithMany(t => t.Rooms).HasForeignKey(e => e.IdHotelRoomType)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.IdHotel, e.Numero }).IsUnique()
                    .HasDatabaseName("IX_HotelRooms_Hotel_Numero_UQ");
                entity.HasIndex(e => e.IdSociete).HasDatabaseName("IX_HotelRooms_IdSociete");
                entity.HasIndex(e => e.IdHotelRoomType).HasDatabaseName("IX_HotelRooms_IdHotelRoomType");
            });
        }

        private static void ConfigureHotelRoomAssignment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelRoomAssignment>(entity =>
            {
                entity.ToTable("HotelRoomAssignments");
                entity.HasOne(e => e.Reservation).WithMany(r => r.RoomAssignments)
                    .HasForeignKey(e => e.IdHotelReservation)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ReservationLine).WithMany(l => l.RoomAssignments)
                    .HasForeignKey(e => e.IdHotelReservationLine)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Room).WithMany(r => r.Assignments)
                    .HasForeignKey(e => e.IdHotelRoom)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.IdHotelRoom, e.IdHotelReservation }).IsUnique()
                    .HasDatabaseName("IX_HotelRoomAssignments_Room_Reservation_UQ");
                entity.HasIndex(e => e.IdHotelRoom).HasDatabaseName("IX_HotelRoomAssignments_IdHotelRoom");
                entity.HasIndex(e => e.IdHotelReservation).HasDatabaseName("IX_HotelRoomAssignments_IdReservation");
                entity.HasIndex(e => e.IdHotelReservationLine).HasDatabaseName("IX_HotelRoomAssignments_IdLine");
            });
        }

        private static void ConfigureHotelExtra(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelExtra>(entity =>
            {
                entity.ToTable("HotelExtras");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(120);
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CodeDevise).IsFixedLength().HasMaxLength(3).HasDefaultValue("CDF");
                entity.Property(e => e.PricingUnit).HasConversion<string>()
                    .HasColumnType("enum('PerStay','PerNight')")
                    .HasDefaultValue(HotelExtraPricingUnit.PerStay);
                entity.Property(e => e.IsActif).HasDefaultValue(true);
                entity.HasOne(e => e.Societe).WithMany().HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Hotel).WithMany(h => h.Extras).HasForeignKey(e => e.IdHotel)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.IdHotel, e.Code }).IsUnique()
                    .HasDatabaseName("IX_HotelExtras_Hotel_Code_UQ");
                entity.HasIndex(e => e.IdSociete).HasDatabaseName("IX_HotelExtras_IdSociete");
            });
        }

        private static void ConfigureHotelReservationExtra(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelReservationExtra>(entity =>
            {
                entity.ToTable("HotelReservationExtras");
                entity.Property(e => e.PrixUnitaireSnapshot).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontantLigne).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CodeDevise).IsFixedLength().HasMaxLength(3);
                entity.HasCheckConstraint("CK_HotelReservationExtras_Quantity", "`Quantity` > 0");
                entity.HasOne(e => e.Reservation).WithMany(r => r.ReservationExtras)
                    .HasForeignKey(e => e.IdHotelReservation)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Extra).WithMany(x => x.ReservationExtras)
                    .HasForeignKey(e => e.IdHotelExtra)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.IdHotelReservation, e.IdHotelExtra }).IsUnique()
                    .HasDatabaseName("IX_HotelReservationExtras_Reservation_Extra_UQ");
                entity.HasIndex(e => e.IdHotelExtra).HasDatabaseName("IX_HotelReservationExtras_IdHotelExtra");
            });
        }

        private static void ConfigureHotelNightAllotment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelNightAllotment>(entity =>
            {
                entity.ToTable("HotelNightAllotments");
                entity.Property(e => e.NightDate).HasColumnType("date");
                entity.Property(e => e.QuantiteHold).HasDefaultValue(0);
                entity.Property(e => e.QuantiteVendue).HasDefaultValue(0);
                entity.Property(e => e.PrixNuit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CodeDevise).IsFixedLength().HasMaxLength(3).HasDefaultValue("CDF");
                entity.Property(e => e.Status).HasConversion<string>()
                    .HasColumnType("enum('Draft','Published','Closed','Cancelled')")
                    .HasDefaultValue(HotelStatus.Draft);

                entity.HasCheckConstraint("CK_HotelNightAllotments_Capacite", "`CapaciteTotale` >= 0");
                entity.HasCheckConstraint(
                    "CK_HotelNightAllotments_StockPositive",
                    "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                entity.HasCheckConstraint(
                    "CK_HotelNightAllotments_StockMax",
                    "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");

                entity.HasOne(e => e.Societe).WithMany().HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Hotel).WithMany(h => h.NightAllotments).HasForeignKey(e => e.IdHotel)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.RoomType).WithMany(r => r.NightAllotments)
                    .HasForeignKey(e => e.IdHotelRoomType)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Planification).WithMany(p => p.AllotmentsGeneres)
                    .HasForeignKey(e => e.IdHotelPlanification)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.IdHotel, e.IdHotelRoomType, e.NightDate }).IsUnique()
                    .HasDatabaseName("IX_HotelNightAllotments_Hotel_RoomType_Night_UQ");
                entity.HasIndex(e => e.IdSociete).HasDatabaseName("IX_HotelNightAllotments_IdSociete");
                entity.HasIndex(e => new { e.IdHotel, e.NightDate })
                    .HasDatabaseName("IX_HotelNightAllotments_IdHotel_NightDate");
                entity.HasIndex(e => e.IdHotelRoomType).HasDatabaseName("IX_HotelNightAllotments_IdHotelRoomType");
                entity.HasIndex(e => e.IdHotelPlanification)
                    .HasDatabaseName("IX_HotelNightAllotments_IdHotelPlanification");
            });
        }

        private static void ConfigureHotelNight(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelNight>(entity =>
            {
                entity.ToTable("HotelNights");
                entity.Property(e => e.NightDate).HasColumnType("date");
                entity.Property(e => e.QuantiteHold).HasDefaultValue(0);
                entity.Property(e => e.QuantiteVendue).HasDefaultValue(0);
                entity.Property(e => e.PrixNuit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CodeDevise).IsFixedLength().HasMaxLength(3).HasDefaultValue("CDF");
                entity.Property(e => e.Status).HasConversion<string>()
                    .HasColumnType("enum('Draft','Published','Closed','Cancelled')")
                    .HasDefaultValue(HotelStatus.Draft);

                entity.HasCheckConstraint("CK_HotelNights_Capacite", "`CapaciteTotale` >= 0");
                entity.HasCheckConstraint(
                    "CK_HotelNights_StockPositive",
                    "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                entity.HasCheckConstraint(
                    "CK_HotelNights_StockMax",
                    "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");

                entity.HasOne(e => e.Societe).WithMany().HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Hotel).WithMany(h => h.Nights).HasForeignKey(e => e.IdHotel)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Planification).WithMany(p => p.NightsGenerees)
                    .HasForeignKey(e => e.IdHotelPlanification)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.IdHotel, e.NightDate }).IsUnique()
                    .HasDatabaseName("IX_HotelNights_Hotel_Night_UQ");
                entity.HasIndex(e => e.IdSociete).HasDatabaseName("IX_HotelNights_IdSociete");
                entity.HasIndex(e => e.IdHotelPlanification)
                    .HasDatabaseName("IX_HotelNights_IdHotelPlanification");
            });
        }

        private static void ConfigureHotelPlanification(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelPlanification>(entity =>
            {
                entity.ToTable("HotelPlanifications");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CodeDevise).IsFixedLength().HasMaxLength(3);
                entity.Property(e => e.InventoryMode).HasConversion<string>()
                    .HasColumnType("enum('ClassQuota','GlobalQuota')")
                    .HasDefaultValue(HotelInventoryMode.ClassQuota);
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

                entity.HasOne(e => e.Hotel)
                    .WithMany(h => h.Planifications)
                    .HasForeignKey(e => e.IdHotel)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.GlobalQuota)
                    .WithOne(q => q.Planification)
                    .HasForeignKey<HotelPlanifGlobalQuota>(q => q.IdHotelPlanification)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdSociete)
                    .HasDatabaseName("IX_HotelPlanifications_IdSociete");
                entity.HasIndex(e => e.IdHotel)
                    .HasDatabaseName("IX_HotelPlanifications_IdHotel");
            });
        }

        private static void ConfigureHotelPlanifGlobalQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelPlanifGlobalQuota>(entity =>
            {
                entity.ToTable("HotelPlanifGlobalQuotas");
                entity.Property(e => e.PrixNuit).HasColumnType("decimal(18,2)");
                entity.HasCheckConstraint("CK_HotelPlanifGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
            });
        }

        private static void ConfigureHotelPlanificationLigne(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelPlanificationLigne>(entity =>
            {
                entity.ToTable("HotelPlanificationLignes");
                entity.Property(e => e.PrixNuit).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Planification)
                    .WithMany(p => p.Lignes)
                    .HasForeignKey(e => e.IdHotelPlanification)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RoomType)
                    .WithMany()
                    .HasForeignKey(e => e.IdHotelRoomType)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdHotelPlanification, e.IdHotelRoomType })
                    .IsUnique()
                    .HasDatabaseName("IX_HotelPlanificationLignes_Planif_RoomType_UQ");
                entity.HasIndex(e => e.IdHotelPlanification)
                    .HasDatabaseName("IX_HotelPlanificationLignes_IdPlanification");

                entity.HasCheckConstraint("CK_HotelPlanificationLignes_Capacite", "`CapaciteTotale` >= 0");
            });
        }

        private static void ConfigureHotelPlanifGenerationLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelPlanifGenerationLog>(entity =>
            {
                entity.ToTable("HotelPlanifGenerationLogs");
                entity.Property(e => e.DetailsJson).IsRequired().HasColumnType("longtext");

                entity.HasOne(e => e.Planification)
                    .WithMany(p => p.GenerationLogs)
                    .HasForeignKey(e => e.IdHotelPlanification)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdHotelPlanification)
                    .HasDatabaseName("IX_HotelPlanifGenerationLogs_IdPlanification");
            });
        }

        private static void ConfigureHotelReservation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelReservation>(entity =>
            {
                entity.ToTable("HotelReservations");
                entity.Property(e => e.CheckInDate).HasColumnType("date");
                entity.Property(e => e.CheckOutDate).HasColumnType("date");
                entity.Property(e => e.Status).HasConversion<string>()
                    .HasColumnType("enum('HOLD','CONFIRMED','CANCELLED','EXPIRED')");
                entity.Property(e => e.InventoryMode).HasConversion<string>()
                    .HasColumnType("enum('ClassQuota','GlobalQuota')")
                    .HasDefaultValue(HotelInventoryMode.ClassQuota);
                entity.Property(e => e.MontantSejour).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontantSousTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CodeDevise).IsFixedLength().HasMaxLength(3);
                entity.HasOne(e => e.Societe).WithMany().HasForeignKey(e => e.IdSociete).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Hotel).WithMany(h => h.Reservations).HasForeignKey(e => e.IdHotel).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.IdSite).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.IdClient).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.IdSociete, e.ReferenceReservation }).IsUnique();
                entity.HasIndex(e => new { e.IdSociete, e.IdempotencyKey }).IsUnique();
                entity.HasIndex(e => new { e.Status, e.ExpiresAtUtc });
            });
        }

        private static void ConfigureHotelReservationLine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelReservationLine>(entity =>
            {
                entity.ToTable("HotelReservationLines");
                entity.Property(e => e.LineType).HasConversion<string>()
                    .HasColumnType("enum('ClassQuota','GlobalQuota')")
                    .HasDefaultValue(HotelReservationLineType.ClassQuota);
                entity.HasCheckConstraint("CK_HotelReservationLines_Quantity", "`Quantity` > 0");
                entity.HasOne(e => e.Reservation).WithMany(r => r.Lines)
                    .HasForeignKey(e => e.IdHotelReservation).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.RoomType).WithMany(r => r.ReservationLines)
                    .HasForeignKey(e => e.IdHotelRoomType)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Night).WithMany()
                    .HasForeignKey(e => e.IdHotelNight)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.IdHotelReservation);
                entity.HasIndex(e => e.IdHotelRoomType);
                entity.HasIndex(e => e.IdHotelNight);
            });
        }

        private static void ConfigureHotelPayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelPayment>(entity =>
            {
                entity.ToTable("HotelPayments");
                entity.Property(e => e.Status).HasConversion<string>()
                    .HasColumnType("enum('PENDING','SUCCEEDED','FAILED','REFUNDED')");
                entity.Property(e => e.CodeDevise).IsFixedLength().HasMaxLength(3);
                entity.Property(e => e.CodeDeviseTarif).IsFixedLength().HasMaxLength(3);
                entity.HasOne(e => e.Reservation).WithMany(r => r.Payments)
                    .HasForeignKey(e => e.IdHotelReservation).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CommandeEnAttente).WithMany()
                    .HasForeignKey(e => e.IdHotelCommandeEnAttente).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.IdSite).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ReferencePaiement).IsUnique();
                entity.HasIndex(e => e.IdempotencyKey).IsUnique();
                entity.HasIndex(e => new { e.IdHotelReservation, e.Status });
                entity.HasIndex(e => e.IdHotelCommandeEnAttente);
            });
        }

        private static void ConfigureHotelCommandeEnAttente(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HotelCommandeEnAttente>(entity =>
            {
                entity.ToTable("HotelCommandesEnAttente");
                entity.Property(e => e.MontantTarif).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MontantFlexPay).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TauxVersDevisePaiement).HasColumnType("decimal(18,8)");
                entity.Property(e => e.PayloadMetierJson).IsRequired();
                entity.HasOne(e => e.Hotel).WithMany().HasForeignKey(e => e.IdHotel).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.IdSite).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.PaiementEnAttente).WithMany().HasForeignKey(e => e.IdPaiementEnAttente).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.DateExpiration);
                entity.HasIndex(e => e.OrderNumberFlexPay);
                entity.HasIndex(e => e.IdempotencyKey).IsUnique();
                entity.HasIndex(e => new { e.IdSociete, e.IdHotel });
            });
        }
    }
}
