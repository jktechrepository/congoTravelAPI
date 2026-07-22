using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Data
{
    public partial class CongoTravelDbContext
    {
        private static void ConfigureEvenementEntities(ModelBuilder modelBuilder)
        {
            ConfigureEvenementSession(modelBuilder);
            ConfigureEvenementClasse(modelBuilder);
            ConfigureEvenementSessionSection(modelBuilder);
            ConfigureEvenementSessionGlobalQuota(modelBuilder);
            ConfigureEvenementSessionClassQuota(modelBuilder);
            ConfigureEvenementReservation(modelBuilder);
            ConfigureEvenementSessionSeat(modelBuilder);
            ConfigureEvenementReservationLine(modelBuilder);
            ConfigureEvenementTicket(modelBuilder);
            ConfigureEvenementPayment(modelBuilder);
        }

        private static void ConfigureEvenementSession(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementSession>(entity =>
            {
                entity.Property(e => e.CodeSession).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(255);
                entity.Property(e => e.InventoryMode)
                    .HasConversion<string>()
                    .HasColumnType("enum('SeatNumbered','ClassQuota','GlobalQuota')");
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('Draft','Published','Closed','Cancelled')")
                    .HasDefaultValue(EvenementSessionStatus.Draft);

                entity.HasCheckConstraint(
                    "CK_EvenementSessions_StartEnd",
                    "`EndAtUtc` IS NULL OR `EndAtUtc` >= `StartAtUtc`");

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.GlobalQuota)
                    .WithOne(q => q.Session)
                    .HasForeignKey<EvenementSessionGlobalQuota>(q => q.IdEvenementSession)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.IdSociete, e.CodeSession })
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementSessions_Societe_CodeSession_UQ");

                entity.HasIndex(e => new { e.IdSociete, e.StartAtUtc })
                    .HasDatabaseName("IX_EvenementSessions_IdSociete_StartAtUtc");
            });
        }

        private static void ConfigureEvenementClasse(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementClasse>(entity =>
            {
                entity.Property(e => e.CodeClasse).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Statut).HasDefaultValue(true);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdSociete, e.CodeClasse })
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementClasses_Societe_CodeClasse_UQ");

                entity.HasIndex(e => e.IdSociete)
                    .HasDatabaseName("IX_EvenementClasses_IdSociete");
            });
        }

        private static void ConfigureEvenementSessionSection(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementSessionSection>(entity =>
            {
                entity.Property(e => e.CodeSection).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(120);

                entity.HasOne(e => e.Session)
                    .WithMany(s => s.Sections)
                    .HasForeignKey(e => e.IdEvenementSession)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.IdEvenementSession, e.CodeSection })
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementSessionSections_Session_CodeSection_UQ");

                entity.HasIndex(e => e.IdEvenementSession)
                    .HasDatabaseName("IX_EvenementSessionSections_IdEvenementSession");
            });
        }

        private static void ConfigureEvenementSessionGlobalQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementSessionGlobalQuota>(entity =>
            {
                entity.Property(e => e.QuantiteHold).HasDefaultValue(0);
                entity.Property(e => e.QuantiteVendue).HasDefaultValue(0);
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");
                ConfigureEvenementCodeDevise(entity.Property(e => e.CodeDevise));

                entity.HasCheckConstraint("CK_EvenementSessionGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
                entity.HasCheckConstraint(
                    "CK_EvenementSessionGlobalQuotas_StockPositive",
                    "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                entity.HasCheckConstraint(
                    "CK_EvenementSessionGlobalQuotas_StockMax",
                    "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
            });
        }

        private static void ConfigureEvenementSessionClassQuota(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementSessionClassQuota>(entity =>
            {
                entity.Property(e => e.QuantiteHold).HasDefaultValue(0);
                entity.Property(e => e.QuantiteVendue).HasDefaultValue(0);
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");
                ConfigureEvenementCodeDevise(entity.Property(e => e.CodeDevise));

                entity.HasOne(e => e.Session)
                    .WithMany(s => s.ClassQuotas)
                    .HasForeignKey(e => e.IdEvenementSession)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Classe)
                    .WithMany(c => c.SessionClassQuotas)
                    .HasForeignKey(e => e.IdEvenementClasse)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdEvenementSession, e.IdEvenementClasse })
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementSessionClassQuotas_Session_Classe_UQ");

                entity.HasIndex(e => e.IdEvenementSession)
                    .HasDatabaseName("IX_EvenementSessionClassQuotas_IdEvenementSession");

                entity.HasCheckConstraint("CK_EvenementSessionClassQuotas_Capacite", "`CapaciteTotale` >= 0");
                entity.HasCheckConstraint(
                    "CK_EvenementSessionClassQuotas_StockPositive",
                    "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                entity.HasCheckConstraint(
                    "CK_EvenementSessionClassQuotas_StockMax",
                    "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
            });
        }

        private static void ConfigureEvenementReservation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementReservation>(entity =>
            {
                entity.Property(e => e.ReferenceReservation).IsRequired().HasMaxLength(64);
                entity.Property(e => e.CustomerRef).HasMaxLength(100);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('HOLD','CONFIRMED','CANCELLED','EXPIRED')");
                entity.Property(e => e.MontantSousTotal).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
                ConfigureEvenementCodeDevise(entity.Property(e => e.CodeDevise));
                entity.Property(e => e.IdempotencyKey).HasMaxLength(120);

                entity.HasOne(e => e.Societe)
                    .WithMany()
                    .HasForeignKey(e => e.IdSociete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Session)
                    .WithMany(s => s.Reservations)
                    .HasForeignKey(e => e.IdEvenementSession)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdSociete, e.ReferenceReservation })
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementReservations_Societe_Reference_UQ");

                entity.HasIndex(e => new { e.IdSociete, e.IdempotencyKey })
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementReservations_Societe_Idempotency_UQ");

                entity.HasIndex(e => new { e.Status, e.ExpiresAtUtc })
                    .HasDatabaseName("IX_EvenementReservations_Status_ExpiresAtUtc");

                entity.HasIndex(e => new { e.IdEvenementSession, e.Status })
                    .HasDatabaseName("IX_EvenementReservations_Session_Status");
            });
        }

        private static void ConfigureEvenementSessionSeat(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementSessionSeat>(entity =>
            {
                entity.Property(e => e.SeatCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SeatStatus)
                    .HasConversion<string>()
                    .HasColumnType("enum('Available','Held','Sold','Blocked')")
                    .HasDefaultValue(EvenementSessionSeatStatus.Available);
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");
                ConfigureEvenementCodeDevise(entity.Property(e => e.CodeDevise));

                entity.HasOne(e => e.Session)
                    .WithMany(s => s.Seats)
                    .HasForeignKey(e => e.IdEvenementSession)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Section)
                    .WithMany(s => s.Seats)
                    .HasForeignKey(e => e.IdEvenementSessionSection)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Classe)
                    .WithMany(c => c.SessionSeats)
                    .HasForeignKey(e => e.IdEvenementClasse)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ReservationCourante)
                    .WithMany(r => r.SeatsEnCours)
                    .HasForeignKey(e => e.IdEvenementReservationCourante)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.IdEvenementSession, e.SeatCode })
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementSessionSeats_Session_SeatCode_UQ");

                entity.HasIndex(e => new { e.IdEvenementSession, e.SeatStatus })
                    .HasDatabaseName("IX_EvenementSessionSeats_Session_SeatStatus");

                entity.HasIndex(e => e.HoldExpireAtUtc)
                    .HasDatabaseName("IX_EvenementSessionSeats_HoldExpireAtUtc");
            });
        }

        private static void ConfigureEvenementReservationLine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementReservationLine>(entity =>
            {
                entity.Property(e => e.LineType)
                    .HasConversion<string>()
                    .HasColumnType("enum('Seat','ClassQuota','GlobalQuota')");
                entity.Property(e => e.PrixUnitaire).HasColumnType("decimal(18,2)");
                ConfigureEvenementCodeDevise(entity.Property(e => e.CodeDevise));

                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.Lines)
                    .HasForeignKey(e => e.IdEvenementReservation)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SessionSeat)
                    .WithMany(s => s.ReservationLines)
                    .HasForeignKey(e => e.IdEvenementSessionSeat)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SessionClassQuota)
                    .WithMany(q => q.ReservationLines)
                    .HasForeignKey(e => e.IdEvenementSessionClassQuota)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.IdEvenementReservation)
                    .HasDatabaseName("IX_EvenementReservationLines_IdEvenementReservation");

                entity.HasIndex(e => new { e.IdEvenementReservation, e.IdEvenementSessionSeat })
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementReservationLines_Reservation_Seat_UQ");

                entity.HasCheckConstraint(
                    "CK_EvenementReservationLines_Quantite",
                    "`Quantite` > 0");
            });
        }

        private static void ConfigureEvenementTicket(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementTicket>(entity =>
            {
                entity.Property(e => e.TicketCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('ISSUED','USED','VOID')")
                    .HasDefaultValue(EvenementTicketStatus.ISSUED);

                entity.HasOne(e => e.ReservationLine)
                    .WithMany(l => l.Tickets)
                    .HasForeignKey(e => e.IdEvenementReservationLine)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.TicketCode)
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementTickets_TicketCode_UQ");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_EvenementTickets_Status");
            });
        }

        private static void ConfigureEvenementPayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvenementPayment>(entity =>
            {
                entity.Property(e => e.ReferencePaiement).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(40);
                entity.Property(e => e.ProviderTxRef).HasMaxLength(120);
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnType("enum('PENDING','SUCCEEDED','FAILED','REFUNDED')");
                entity.Property(e => e.Montant).HasColumnType("decimal(18,2)");
                ConfigureEvenementCodeDevise(entity.Property(e => e.CodeDevise));
                entity.Property(e => e.MontantTarif).HasColumnType("decimal(18,2)");
                ConfigureEvenementCodeDevise(entity.Property(e => e.CodeDeviseTarif));
                entity.Property(e => e.TauxVersDevisePaiement)
                    .HasColumnType("decimal(18,8)")
                    .HasDefaultValue(1m);
                entity.Property(e => e.IdempotencyKey).HasMaxLength(120);

                entity.HasOne(e => e.Reservation)
                    .WithMany(r => r.Payments)
                    .HasForeignKey(e => e.IdEvenementReservation)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ReferencePaiement)
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementPayments_ReferencePaiement_UQ");

                entity.HasIndex(e => e.IdempotencyKey)
                    .IsUnique()
                    .HasDatabaseName("IX_EvenementPayments_Idempotency_UQ");

                entity.HasIndex(e => new { e.IdEvenementReservation, e.Status })
                    .HasDatabaseName("IX_EvenementPayments_Reservation_Status");
            });
        }

        private static void ConfigureEvenementCodeDevise(PropertyBuilder<string> property)
        {
            property.IsRequired().IsFixedLength().HasMaxLength(3).HasDefaultValue("CDF");
        }
    }
}
