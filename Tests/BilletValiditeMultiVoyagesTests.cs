using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class BilletValiditeMultiVoyagesTests
    {
        private static CongoTravelDbContext BuildDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new CongoTravelDbContext(options);
        }

        [Fact]
        public async Task CheckBillet_returns_expired_when_validity_passed()
        {
            await using var db = BuildDbContext(nameof(CheckBillet_returns_expired_when_validity_passed));
            var voyage = new Voyage
            {
                Id = 100,
                IdSociete = 1,
                IdVehicule = 10,
                IdDestination = 20,
                DateDepart = DateTime.Today.AddDays(-10),
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 100,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 100
            };
            var reservation = new Reservation
            {
                IdReservation = 200,
                IdVoyage = 100,
                IdClient = 300,
                IdUtilisateur = 400,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            };
            var billet = new Billet
            {
                IdBillet = 500,
                IdSociete = 1,
                IdReservation = 200,
                QrCode = "QR-EXP",
                DateGeneration = DateTime.UtcNow.AddDays(-10),
                IsUsed = false,
                DateValiditeDebut = DateTime.Today.AddDays(-9),
                DateValiditeFin = DateTime.Today.AddDays(-1)
            };

            db.Voyages.Add(voyage);
            db.Reservations.Add(reservation);
            db.Billets.Add(billet);
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.CheckBilletByQrCodeAsync("QR-EXP");

            Assert.False(result.EmbarquementAutorise);
            Assert.Equal("BilletExpire", result.Statut);
            Assert.Equal(0m, result.KiloBagageOffert);
            Assert.Equal(voyage.DateDepart.Date, result.DateDepartVoyage);
            Assert.Equal(voyage.HeureDepart, result.HeureDepartVoyage);
        }

        [Fact]
        public async Task CheckBillet_midnight_validity_end_allows_scan_same_calendar_day()
        {
            await using var db = BuildDbContext(nameof(CheckBillet_midnight_validity_end_allows_scan_same_calendar_day));
            var depart = DateTime.Today;
            var voyage = new Voyage
            {
                Id = 101,
                IdSociete = 1,
                IdVehicule = 10,
                IdDestination = 20,
                DateDepart = depart,
                HeureDepart = TimeSpan.FromHours(12),
                Prix = 100,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 100,
                Statut = true
            };
            var reservation = new Reservation
            {
                IdReservation = 201,
                IdVoyage = 101,
                IdClient = 300,
                IdUtilisateur = 400,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            };
            var billet = new Billet
            {
                IdBillet = 501,
                IdSociete = 1,
                IdReservation = 201,
                QrCode = "QR-MIDNIGHT",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = depart.Date,
                DateValiditeFin = depart.Date.AddDays(1)
            };

            db.Voyages.Add(voyage);
            db.Reservations.Add(reservation);
            db.Billets.Add(billet);
            db.ConfigSocietes.Add(ConfigSocieteDefaults.CreateForSociete(1));
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.CheckBilletByQrCodeAsync("QR-MIDNIGHT");

            Assert.NotEqual("BilletExpire", result.Statut);
            Assert.Equal(depart.Date, result.DateDepartVoyage);
            Assert.Equal(TimeSpan.FromHours(12), result.HeureDepartVoyage);
        }

        [Fact]
        public async Task CheckBillet_returns_passenger_identity_not_buyer_in_nomClient_fields()
        {
            await using var db = BuildDbContext(nameof(CheckBillet_returns_passenger_identity_not_buyer_in_nomClient_fields));
            var depart = DateTime.Today;
            var societe = new Societe
            {
                IdSociete = 1,
                Nom = "Congo Travel",
                Logo = "https://cdn.example/logo-check.png",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var client = new Client
            {
                IdClient = 300,
                NomClient = "Acheteur Dupont",
                Telephone = "+243111",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            };
            var voyage = new Voyage
            {
                Id = 102,
                IdSociete = 1,
                IdVehicule = 10,
                IdDestination = 20,
                DateDepart = depart,
                HeureDepart = TimeSpan.FromHours(12),
                Prix = 100,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 100,
                Statut = true
            };
            var reservation = new Reservation
            {
                IdReservation = 202,
                IdVoyage = 102,
                IdClient = 300,
                IdUtilisateur = 400,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            };
            var passenger = new ReservationPassenger
            {
                IdReservationPassenger = 600,
                IdReservation = 202,
                NomComplet = "Passager Réel",
                Telephone = "+243999",
                IdSociete = 1,
                Statut = true
            };
            var billet = new Billet
            {
                IdBillet = 502,
                IdSociete = 1,
                IdReservation = 202,
                IdReservationPassenger = 600,
                QrCode = "QR-PASSENGER",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = depart.Date,
                DateValiditeFin = depart.Date.AddDays(1)
            };

            db.Societes.Add(societe);
            db.Clients.Add(client);
            db.Voyages.Add(voyage);
            db.Reservations.Add(reservation);
            db.ReservationPassengers.Add(passenger);
            db.Billets.Add(billet);
            db.ConfigSocietes.Add(new ConfigSociete
            {
                IdSociete = 1,
                PoidsBagageParKiloOffert = 20m,
                DateCreation = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.CheckBilletByQrCodeAsync("QR-PASSENGER");

            Assert.Equal("Passager Réel", result.NomClient);
            Assert.Equal("+243999", result.TelephoneClient);
            Assert.Equal(20m, result.KiloBagageOffert);
            Assert.Equal("https://cdn.example/logo-check.png", result.LogoSociete);
            Assert.NotEqual(client.NomClient, result.NomClient);
            Assert.NotEqual(client.Telephone, result.TelephoneClient);
        }

        [Fact]
        public async Task CheckBillet_returns_null_logo_when_societe_logo_missing()
        {
            await using var db = BuildDbContext(nameof(CheckBillet_returns_null_logo_when_societe_logo_missing));
            var depart = DateTime.Today;
            db.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Congo Travel",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            db.Voyages.Add(new Voyage
            {
                Id = 104,
                IdSociete = 1,
                IdVehicule = 10,
                IdDestination = 20,
                DateDepart = depart,
                HeureDepart = TimeSpan.FromHours(12),
                Prix = 100,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 100,
                Statut = true
            });

            db.Reservations.Add(new Reservation
            {
                IdReservation = 204,
                IdVoyage = 104,
                IdClient = 300,
                IdUtilisateur = 400,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });

            db.Billets.Add(new Billet
            {
                IdBillet = 503,
                IdSociete = 1,
                IdReservation = 204,
                QrCode = "QR-NO-LOGO",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = depart.Date,
                DateValiditeFin = depart.Date.AddDays(1)
            });

            db.ConfigSocietes.Add(ConfigSocieteDefaults.CreateForSociete(1));
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.CheckBilletByQrCodeAsync("QR-NO-LOGO");

            Assert.Equal(0m, result.KiloBagageOffert);
            Assert.Null(result.LogoSociete);
        }

        [Fact]
        public async Task GetByQrCode_apply_compat_returns_passenger_identity_not_buyer()
        {
            await using var db = BuildDbContext(nameof(GetByQrCode_apply_compat_returns_passenger_identity_not_buyer));
            var depart = DateTime.Today;
            var client = new Client
            {
                IdClient = 301,
                NomClient = "Acheteur Dupont",
                Telephone = "+243111",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            };
            var voyage = new Voyage
            {
                Id = 103,
                IdSociete = 1,
                IdVehicule = 10,
                IdDestination = 20,
                DateDepart = depart,
                HeureDepart = TimeSpan.FromHours(12),
                Prix = 100,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 100,
                Statut = true
            };
            var reservation = new Reservation
            {
                IdReservation = 203,
                IdVoyage = 103,
                IdClient = 301,
                IdUtilisateur = 400,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1,
                Client = client
            };
            var passenger = new ReservationPassenger
            {
                IdReservationPassenger = 601,
                IdReservation = 203,
                NomComplet = "Passager Réel",
                Telephone = "+243999",
                IdSociete = 1,
                Statut = true
            };
            var billet = new Billet
            {
                IdBillet = 503,
                IdSociete = 1,
                IdReservation = 203,
                IdReservationPassenger = 601,
                QrCode = "QR-PASSENGER-QRCODE",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = depart.Date,
                DateValiditeFin = depart.Date.AddDays(1)
            };

            db.Clients.Add(client);
            db.Voyages.Add(voyage);
            db.Reservations.Add(reservation);
            db.ReservationPassengers.Add(passenger);
            db.Billets.Add(billet);
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var loaded = (await service.GetByQrCodeAsync("QR-PASSENGER-QRCODE")).ToList();
            Assert.Single(loaded);

            var dto = new BilletResponseDto
            {
                NomClient = client.NomClient,
                TelephoneClient = client.Telephone,
                QrCode = loaded[0].QrCode
            };
            BilletPassengerIdentityCompat.ApplyPassengerIdentityToClientFields(dto, loaded[0]);

            Assert.Equal("Passager Réel", dto.NomClient);
            Assert.Equal("+243999", dto.TelephoneClient);
            Assert.NotEqual(client.NomClient, dto.NomClient);
            Assert.NotEqual(client.Telephone, dto.TelephoneClient);
        }

        [Fact]
        public async Task ReaffecterBillet_requires_delta_confirmation_then_succeeds()
        {
            await using var db = BuildDbContext(nameof(ReaffecterBillet_requires_delta_confirmation_then_succeeds));

            db.Vehicules.AddRange(
                new Vehicule { IdVehicule = 10, AliasVehicule = "BUS-A", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true },
                new Vehicule { IdVehicule = 11, AliasVehicule = "BUS-B", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            db.CategorieSieges.Add(new CategorieSiege { IdCategorieSiege = 1, IdSociete = 1, CodeCategorieSiege = "ECO", Libelle = "Eco", Statut = true });
            db.Sieges.AddRange(
                new Siege { IdSiege = 1000, IdVehicule = 10, IdSociete = 1, IdCategorieSiege = 1, CodeSiege = "A1", NumeroOrdre = 1, EstActif = true },
                new Siege { IdSiege = 1001, IdVehicule = 11, IdSociete = 1, IdCategorieSiege = 1, CodeSiege = "B1", NumeroOrdre = 1, EstActif = true });
            db.Destinations.Add(new Destination { IdDestination = 20, IdSociete = 1, VilleDepart = "Goma", VilleArrivee = "Bukavu", Statut = true });
            db.Voyages.AddRange(
                new Voyage
                {
                    Id = 100,
                    IdSociete = 1,
                    IdVehicule = 10,
                    IdDestination = 20,
                    DateDepart = DateTime.Today.AddDays(1),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100
                },
                new Voyage
                {
                    Id = 101,
                    IdSociete = 1,
                    IdVehicule = 11,
                    IdDestination = 20,
                    DateDepart = DateTime.Today,
                    HeureDepart = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(2)),
                    Prix = 150,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 150
                });

            db.Reservations.Add(new Reservation
            {
                IdReservation = 200,
                IdVoyage = 100,
                IdClient = 300,
                IdUtilisateur = 400,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });
            db.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservationPassenger = 600,
                IdReservation = 200,
                IdSociete = 1,
                NomComplet = "Passager Test",
                Statut = true
            });
            db.VoyageSeatAllocations.Add(new VoyageSeatAllocation
            {
                IdVoyage = 100,
                IdSiege = 1000,
                IdReservationPassenger = 600,
                Statut = "CONFIRME"
            });
            db.Billets.Add(new Billet
            {
                IdBillet = 500,
                IdSociete = 1,
                IdReservation = 200,
                IdReservationPassenger = 600,
                IdSiege = 1000,
                CodeSiege = "A1",
                QrCode = "QR-REALLOC",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = DateTime.Today,
                DateValiditeFin = DateTime.Today.AddDays(7)
            });

            await ConfigSocieteTestHelper.SeedAsync(db, 1, c => c.DureeValiditeBilletJours = 7);
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);

            var conflict = await service.ReaffecterBilletAsync(
                idSociete: 1,
                idBillet: 500,
                idVoyageCible: 101,
                idUtilisateurEnregistrement: 999,
                confirmerPaiementDifferentiel: false,
                methodePaiement: "FlexPay",
                referenceTransaction: "TX-REF-1");

            Assert.False(conflict.Success);
            Assert.Equal(409, conflict.StatusCode);
            Assert.True(conflict.PaiementDifferentielRequis);
            Assert.Equal(50m, conflict.DifferentielTarifaire);

            var success = await service.ReaffecterBilletAsync(
                idSociete: 1,
                idBillet: 500,
                idVoyageCible: 101,
                idUtilisateurEnregistrement: 999,
                confirmerPaiementDifferentiel: true,
                methodePaiement: "FlexPay",
                referenceTransaction: "TX-REF-1");

            Assert.True(success.Success);
            Assert.Equal(50m, success.DifferentielTarifaire);

            var reservation = await db.Reservations.FirstAsync(r => r.IdReservation == 200);
            Assert.Equal(101, reservation.IdVoyage);

            var paiementDifferentiel = await db.Paiements
                .FirstOrDefaultAsync(p => p.IdReservation == 200 && p.MontantAPaye == 50m);
            Assert.NotNull(paiementDifferentiel);
            Assert.Equal("FlexPay", paiementDifferentiel!.MethodePaiement);
        }

        [Fact]
        public async Task ReaffecterBillet_refuses_when_departure_missed_even_with_penalite_override()
        {
            await using var db = BuildDbContext(nameof(ReaffecterBillet_refuses_when_departure_missed_even_with_penalite_override));

            db.Vehicules.AddRange(
                new Vehicule { IdVehicule = 20, AliasVehicule = "BUS-C", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true },
                new Vehicule { IdVehicule = 21, AliasVehicule = "BUS-D", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            db.CategorieSieges.Add(new CategorieSiege { IdCategorieSiege = 2, IdSociete = 1, CodeCategorieSiege = "VIP", Libelle = "Vip", Statut = true });
            db.Sieges.AddRange(
                new Siege { IdSiege = 2000, IdVehicule = 20, IdSociete = 1, IdCategorieSiege = 2, CodeSiege = "C1", NumeroOrdre = 1, EstActif = true },
                new Siege { IdSiege = 2001, IdVehicule = 21, IdSociete = 1, IdCategorieSiege = 2, CodeSiege = "D1", NumeroOrdre = 1, EstActif = true });
            db.Destinations.Add(new Destination { IdDestination = 30, IdSociete = 1, VilleDepart = "Bukavu", VilleArrivee = "Uvira", Statut = true });
            db.Voyages.AddRange(
                new Voyage
                {
                    Id = 200,
                    IdSociete = 1,
                    IdVehicule = 20,
                    IdDestination = 30,
                    DateDepart = DateTime.Today.AddDays(-1),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100
                },
                new Voyage
                {
                    Id = 201,
                    IdSociete = 1,
                    IdVehicule = 21,
                    IdDestination = 30,
                    DateDepart = DateTime.Today.AddDays(1),
                    HeureDepart = TimeSpan.FromHours(10),
                    Prix = 120,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 120,
                    Statut = true
                });
            db.Reservations.Add(new Reservation
            {
                IdReservation = 300,
                IdVoyage = 200,
                IdClient = 301,
                IdUtilisateur = 401,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });
            db.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservationPassenger = 700,
                IdReservation = 300,
                IdSociete = 1,
                NomComplet = "Passager Penalite",
                Statut = true
            });
            db.VoyageSeatAllocations.Add(new VoyageSeatAllocation
            {
                IdVoyage = 200,
                IdSiege = 2000,
                IdReservationPassenger = 700,
                Statut = "CONFIRME"
            });
            db.Billets.Add(new Billet
            {
                IdBillet = 800,
                IdSociete = 1,
                IdReservation = 300,
                IdReservationPassenger = 700,
                IdSiege = 2000,
                CodeSiege = "C1",
                QrCode = "QR-PENALITE",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = DateTime.Today.AddDays(-1),
                DateValiditeFin = DateTime.Today.AddDays(7),
                PenaliteOverride = 25m
            });

            await ConfigSocieteTestHelper.SeedAsync(db, 1, c => c.DureeValiditeBilletJours = 7);
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.ReaffecterBilletAsync(
                idSociete: 1,
                idBillet: 800,
                idVoyageCible: 201,
                idUtilisateurEnregistrement: 999,
                confirmerPaiementDifferentiel: true,
                methodePaiement: "FlexPay",
                referenceTransaction: "TX-PENALITE-1");

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("fenêtre limite", result.Message, StringComparison.OrdinalIgnoreCase);
            var paiement = await db.Paiements.FirstOrDefaultAsync(p => p.IdReservation == 300 && p.ReferenceTransaction == "TX-PENALITE-1");
            Assert.Null(paiement);
        }

        [Fact]
        public async Task ReaffecterBillet_refuses_when_deadline_is_passed()
        {
            await using var db = BuildDbContext(nameof(ReaffecterBillet_refuses_when_deadline_is_passed));

            db.Vehicules.AddRange(
                new Vehicule { IdVehicule = 30, AliasVehicule = "BUS-E", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true },
                new Vehicule { IdVehicule = 31, AliasVehicule = "BUS-F", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            db.CategorieSieges.Add(new CategorieSiege { IdCategorieSiege = 3, IdSociete = 1, CodeCategorieSiege = "ECO2", Libelle = "Eco2", Statut = true });
            db.Sieges.AddRange(
                new Siege { IdSiege = 3000, IdVehicule = 30, IdSociete = 1, IdCategorieSiege = 3, CodeSiege = "E1", NumeroOrdre = 1, EstActif = true },
                new Siege { IdSiege = 3001, IdVehicule = 31, IdSociete = 1, IdCategorieSiege = 3, CodeSiege = "F1", NumeroOrdre = 1, EstActif = true });
            db.Destinations.Add(new Destination { IdDestination = 40, IdSociete = 1, VilleDepart = "Goma", VilleArrivee = "Rutshuru", Statut = true });
            db.Voyages.AddRange(
                new Voyage
                {
                    Id = 300,
                    IdSociete = 1,
                    IdVehicule = 30,
                    IdDestination = 40,
                    DateDepart = DateTime.Today,
                    HeureDepart = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(1)),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100
                },
                new Voyage
                {
                    Id = 301,
                    IdSociete = 1,
                    IdVehicule = 31,
                    IdDestination = 40,
                    DateDepart = DateTime.Today,
                    HeureDepart = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(2)),
                    Prix = 110,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 110
                });
            db.Reservations.Add(new Reservation
            {
                IdReservation = 400,
                IdVoyage = 300,
                IdClient = 302,
                IdUtilisateur = 402,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });
            db.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservationPassenger = 900,
                IdReservation = 400,
                IdSociete = 1,
                NomComplet = "Passager Deadline",
                Statut = true
            });
            db.VoyageSeatAllocations.Add(new VoyageSeatAllocation
            {
                IdVoyage = 300,
                IdSiege = 3000,
                IdReservationPassenger = 900,
                Statut = "CONFIRME"
            });
            db.Billets.Add(new Billet
            {
                IdBillet = 901,
                IdSociete = 1,
                IdReservation = 400,
                IdReservationPassenger = 900,
                IdSiege = 3000,
                CodeSiege = "E1",
                QrCode = "QR-DEADLINE",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = DateTime.Today,
                DateValiditeFin = DateTime.Today.AddDays(7)
            });

            await ConfigSocieteTestHelper.SeedAsync(db, 1, c =>
            {
                c.DureeValiditeBilletJours = 7;
                c.HeuresLimiteReaffectation = 2;
                c.PenaliteReaffectationPourcentage = 20m;
            });
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.ReaffecterBilletAsync(
                idSociete: 1,
                idBillet: 901,
                idVoyageCible: 301,
                idUtilisateurEnregistrement: 999,
                confirmerPaiementDifferentiel: true);

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("fenêtre limite", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, result.HeuresLimiteReaffectation);
            Assert.NotNull(result.DeadlineReaffectation);
        }

        [Fact]
        public async Task ReaffecterBillet_allows_until_departure_when_limit_is_zero()
        {
            await using var db = BuildDbContext(nameof(ReaffecterBillet_allows_until_departure_when_limit_is_zero));

            db.Vehicules.AddRange(
                new Vehicule { IdVehicule = 40, AliasVehicule = "BUS-G", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true },
                new Vehicule { IdVehicule = 41, AliasVehicule = "BUS-H", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            db.CategorieSieges.Add(new CategorieSiege { IdCategorieSiege = 4, IdSociete = 1, CodeCategorieSiege = "ECO3", Libelle = "Eco3", Statut = true });
            db.Sieges.AddRange(
                new Siege { IdSiege = 4000, IdVehicule = 40, IdSociete = 1, IdCategorieSiege = 4, CodeSiege = "G1", NumeroOrdre = 1, EstActif = true },
                new Siege { IdSiege = 4001, IdVehicule = 41, IdSociete = 1, IdCategorieSiege = 4, CodeSiege = "H1", NumeroOrdre = 1, EstActif = true });
            db.Destinations.Add(new Destination { IdDestination = 50, IdSociete = 1, VilleDepart = "Bukavu", VilleArrivee = "Goma", Statut = true });
            db.Voyages.AddRange(
                new Voyage
                {
                    Id = 400,
                    IdSociete = 1,
                    IdVehicule = 40,
                    IdDestination = 50,
                    DateDepart = DateTime.Today,
                    HeureDepart = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(2)),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100
                },
                new Voyage
                {
                    Id = 401,
                    IdSociete = 1,
                    IdVehicule = 41,
                    IdDestination = 50,
                    DateDepart = DateTime.Today,
                    HeureDepart = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(3)),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100
                });
            db.Reservations.Add(new Reservation
            {
                IdReservation = 500,
                IdVoyage = 400,
                IdClient = 303,
                IdUtilisateur = 403,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });
            db.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservationPassenger = 1000,
                IdReservation = 500,
                IdSociete = 1,
                NomComplet = "Passager Zero",
                Statut = true
            });
            db.VoyageSeatAllocations.Add(new VoyageSeatAllocation
            {
                IdVoyage = 400,
                IdSiege = 4000,
                IdReservationPassenger = 1000,
                Statut = "CONFIRME"
            });
            db.Billets.Add(new Billet
            {
                IdBillet = 1001,
                IdSociete = 1,
                IdReservation = 500,
                IdReservationPassenger = 1000,
                IdSiege = 4000,
                CodeSiege = "G1",
                QrCode = "QR-ZERO",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = DateTime.Today,
                DateValiditeFin = DateTime.Today.AddDays(7)
            });

            await ConfigSocieteTestHelper.SeedAsync(db, 1, c =>
            {
                c.DureeValiditeBilletJours = 7;
                c.HeuresLimiteReaffectation = 0;
            });
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.ReaffecterBilletAsync(
                idSociete: 1,
                idBillet: 1001,
                idVoyageCible: 401,
                idUtilisateurEnregistrement: 999,
                confirmerPaiementDifferentiel: true);

            Assert.True(result.Success);
            Assert.Equal(0, result.HeuresLimiteReaffectation);
        }

        [Fact]
        public async Task ReaffecterBillet_refuses_when_target_voyage_departure_passed()
        {
            await using var db = BuildDbContext(nameof(ReaffecterBillet_refuses_when_target_voyage_departure_passed));

            db.Vehicules.AddRange(
                new Vehicule { IdVehicule = 50, AliasVehicule = "BUS-I", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true },
                new Vehicule { IdVehicule = 51, AliasVehicule = "BUS-J", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            db.CategorieSieges.Add(new CategorieSiege { IdCategorieSiege = 5, IdSociete = 1, CodeCategorieSiege = "ECO5", Libelle = "Eco5", Statut = true });
            db.Sieges.AddRange(
                new Siege { IdSiege = 5000, IdVehicule = 50, IdSociete = 1, IdCategorieSiege = 5, CodeSiege = "I1", NumeroOrdre = 1, EstActif = true },
                new Siege { IdSiege = 5001, IdVehicule = 51, IdSociete = 1, IdCategorieSiege = 5, CodeSiege = "J1", NumeroOrdre = 1, EstActif = true });
            db.Destinations.Add(new Destination { IdDestination = 60, IdSociete = 1, VilleDepart = "Goma", VilleArrivee = "Karisimbi", Statut = true });
            db.Voyages.AddRange(
                new Voyage
                {
                    Id = 500,
                    IdSociete = 1,
                    IdVehicule = 50,
                    IdDestination = 60,
                    DateDepart = DateTime.Today.AddDays(1),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                },
                new Voyage
                {
                    Id = 501,
                    IdSociete = 1,
                    IdVehicule = 51,
                    IdDestination = 60,
                    DateDepart = DateTime.Today,
                    HeureDepart = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(-2)),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                });
            db.Reservations.Add(new Reservation
            {
                IdReservation = 600,
                IdVoyage = 500,
                IdClient = 304,
                IdUtilisateur = 404,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });
            db.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservationPassenger = 1100,
                IdReservation = 600,
                IdSociete = 1,
                NomComplet = "Passager Cible Parti",
                Statut = true
            });
            db.VoyageSeatAllocations.Add(new VoyageSeatAllocation
            {
                IdVoyage = 500,
                IdSiege = 5000,
                IdReservationPassenger = 1100,
                Statut = "CONFIRME"
            });
            db.Billets.Add(new Billet
            {
                IdBillet = 1101,
                IdSociete = 1,
                IdReservation = 600,
                IdReservationPassenger = 1100,
                IdSiege = 5000,
                CodeSiege = "I1",
                QrCode = "QR-TARGET-PAST",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = DateTime.Today,
                DateValiditeFin = DateTime.Today.AddDays(7)
            });

            await ConfigSocieteTestHelper.SeedAsync(db, 1);
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.ReaffecterBilletAsync(1, 1101, 501, 999, confirmerPaiementDifferentiel: true);

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("déjà départé", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReaffecterBillet_allows_future_voyage_outside_boarding_window()
        {
            await using var db = BuildDbContext(nameof(ReaffecterBillet_allows_future_voyage_outside_boarding_window));

            db.Vehicules.AddRange(
                new Vehicule { IdVehicule = 60, AliasVehicule = "BUS-K", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true },
                new Vehicule { IdVehicule = 61, AliasVehicule = "BUS-L", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            db.CategorieSieges.Add(new CategorieSiege { IdCategorieSiege = 6, IdSociete = 1, CodeCategorieSiege = "ECO6", Libelle = "Eco6", Statut = true });
            db.Sieges.AddRange(
                new Siege { IdSiege = 6000, IdVehicule = 60, IdSociete = 1, IdCategorieSiege = 6, CodeSiege = "K1", NumeroOrdre = 1, EstActif = true },
                new Siege { IdSiege = 6001, IdVehicule = 61, IdSociete = 1, IdCategorieSiege = 6, CodeSiege = "L1", NumeroOrdre = 1, EstActif = true });
            db.Destinations.Add(new Destination { IdDestination = 70, IdSociete = 1, VilleDepart = "Goma", VilleArrivee = "Butembo", Statut = true });
            db.Voyages.AddRange(
                new Voyage
                {
                    Id = 600,
                    IdSociete = 1,
                    IdVehicule = 60,
                    IdDestination = 70,
                    DateDepart = DateTime.Today,
                    HeureDepart = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(2)),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                },
                new Voyage
                {
                    Id = 601,
                    IdSociete = 1,
                    IdVehicule = 61,
                    IdDestination = 70,
                    DateDepart = DateTime.Today.AddDays(5),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                });
            db.Reservations.Add(new Reservation
            {
                IdReservation = 700,
                IdVoyage = 600,
                IdClient = 305,
                IdUtilisateur = 405,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });
            db.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservationPassenger = 1200,
                IdReservation = 700,
                IdSociete = 1,
                NomComplet = "Passager Futur",
                Statut = true
            });
            db.VoyageSeatAllocations.Add(new VoyageSeatAllocation
            {
                IdVoyage = 600,
                IdSiege = 6000,
                IdReservationPassenger = 1200,
                Statut = "CONFIRME"
            });
            db.Billets.Add(new Billet
            {
                IdBillet = 1201,
                IdSociete = 1,
                IdReservation = 700,
                IdReservationPassenger = 1200,
                IdSiege = 6000,
                CodeSiege = "K1",
                QrCode = "QR-FUTURE",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = DateTime.Today,
                DateValiditeFin = DateTime.Today.AddDays(14)
            });

            await ConfigSocieteTestHelper.SeedAsync(db, 1, c => c.HeuresLimiteReaffectation = 0);
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.ReaffecterBilletAsync(1, 1201, 601, 999, confirmerPaiementDifferentiel: true);

            Assert.True(result.Success);
            var reservation = await db.Reservations.FirstAsync(r => r.IdReservation == 700);
            Assert.Equal(601, reservation.IdVoyage);
        }

        [Fact]
        public async Task ReaffecterBillet_refuses_when_all_seats_in_category_taken()
        {
            await using var db = BuildDbContext(nameof(ReaffecterBillet_refuses_when_all_seats_in_category_taken));

            db.Vehicules.AddRange(
                new Vehicule { IdVehicule = 70, AliasVehicule = "BUS-M", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true },
                new Vehicule { IdVehicule = 71, AliasVehicule = "BUS-N", NombreSiege = 2, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            db.CategorieSieges.Add(new CategorieSiege { IdCategorieSiege = 7, IdSociete = 1, CodeCategorieSiege = "ECO7", Libelle = "Eco7", Statut = true });
            db.Sieges.AddRange(
                new Siege { IdSiege = 7000, IdVehicule = 70, IdSociete = 1, IdCategorieSiege = 7, CodeSiege = "M1", NumeroOrdre = 1, EstActif = true },
                new Siege { IdSiege = 7001, IdVehicule = 71, IdSociete = 1, IdCategorieSiege = 7, CodeSiege = "N1", NumeroOrdre = 1, EstActif = true });
            db.Destinations.Add(new Destination { IdDestination = 80, IdSociete = 1, VilleDepart = "Goma", VilleArrivee = "Beni", Statut = true });
            db.Voyages.AddRange(
                new Voyage
                {
                    Id = 700,
                    IdSociete = 1,
                    IdVehicule = 70,
                    IdDestination = 80,
                    DateDepart = DateTime.Today.AddDays(1),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                },
                new Voyage
                {
                    Id = 701,
                    IdSociete = 1,
                    IdVehicule = 71,
                    IdDestination = 80,
                    DateDepart = DateTime.Today.AddDays(2),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                });
            db.Reservations.AddRange(
                new Reservation
                {
                    IdReservation = 800,
                    IdVoyage = 700,
                    IdClient = 306,
                    IdUtilisateur = 406,
                    IdSociete = 1,
                    DateReservation = DateTime.UtcNow,
                    Statut = true,
                    StatutReservation = "CONFIRMEE",
                    NombreDePlace = 1
                },
                new Reservation
                {
                    IdReservation = 801,
                    IdVoyage = 701,
                    IdClient = 307,
                    IdUtilisateur = 407,
                    IdSociete = 1,
                    DateReservation = DateTime.UtcNow,
                    Statut = true,
                    StatutReservation = "CONFIRMEE",
                    NombreDePlace = 1
                });
            db.ReservationPassengers.AddRange(
                new ReservationPassenger
                {
                    IdReservationPassenger = 1300,
                    IdReservation = 800,
                    IdSociete = 1,
                    NomComplet = "Passager Source",
                    Statut = true
                },
                new ReservationPassenger
                {
                    IdReservationPassenger = 1301,
                    IdReservation = 801,
                    IdSociete = 1,
                    NomComplet = "Passager Cible Occupe",
                    Statut = true
                });
            db.VoyageSeatAllocations.AddRange(
                new VoyageSeatAllocation
                {
                    IdVoyage = 700,
                    IdSiege = 7000,
                    IdReservationPassenger = 1300,
                    Statut = "CONFIRME"
                },
                new VoyageSeatAllocation
                {
                    IdVoyage = 701,
                    IdSiege = 7001,
                    IdReservationPassenger = 1301,
                    Statut = "CONFIRME"
                });
            db.Billets.Add(new Billet
            {
                IdBillet = 1302,
                IdSociete = 1,
                IdReservation = 800,
                IdReservationPassenger = 1300,
                IdSiege = 7000,
                CodeSiege = "M1",
                QrCode = "QR-FULL",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = DateTime.Today,
                DateValiditeFin = DateTime.Today.AddDays(7)
            });

            await ConfigSocieteTestHelper.SeedAsync(db, 1);
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.ReaffecterBilletAsync(1, 1302, 701, 999, confirmerPaiementDifferentiel: true);

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("siège disponible", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReaffecterBillet_refuses_when_seat_on_hold_flexpay()
        {
            await using var db = BuildDbContext(nameof(ReaffecterBillet_refuses_when_seat_on_hold_flexpay));

            db.Vehicules.AddRange(
                new Vehicule { IdVehicule = 80, AliasVehicule = "BUS-O", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true },
                new Vehicule { IdVehicule = 81, AliasVehicule = "BUS-P", NombreSiege = 2, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            db.CategorieSieges.Add(new CategorieSiege { IdCategorieSiege = 8, IdSociete = 1, CodeCategorieSiege = "ECO8", Libelle = "Eco8", Statut = true });
            db.Sieges.AddRange(
                new Siege { IdSiege = 8000, IdVehicule = 80, IdSociete = 1, IdCategorieSiege = 8, CodeSiege = "O1", NumeroOrdre = 1, EstActif = true },
                new Siege { IdSiege = 8001, IdVehicule = 81, IdSociete = 1, IdCategorieSiege = 8, CodeSiege = "P1", NumeroOrdre = 1, EstActif = true });
            db.Destinations.Add(new Destination { IdDestination = 90, IdSociete = 1, VilleDepart = "Goma", VilleArrivee = "Masisi", Statut = true });
            db.Voyages.AddRange(
                new Voyage
                {
                    Id = 800,
                    IdSociete = 1,
                    IdVehicule = 80,
                    IdDestination = 90,
                    DateDepart = DateTime.Today.AddDays(1),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                },
                new Voyage
                {
                    Id = 801,
                    IdSociete = 1,
                    IdVehicule = 81,
                    IdDestination = 90,
                    DateDepart = DateTime.Today.AddDays(2),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                });
            db.Reservations.Add(new Reservation
            {
                IdReservation = 900,
                IdVoyage = 800,
                IdClient = 308,
                IdUtilisateur = 408,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });
            db.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservationPassenger = 1400,
                IdReservation = 900,
                IdSociete = 1,
                NomComplet = "Passager Hold",
                Statut = true
            });
            db.VoyageSeatAllocations.Add(new VoyageSeatAllocation
            {
                IdVoyage = 800,
                IdSiege = 8000,
                IdReservationPassenger = 1400,
                Statut = "CONFIRME"
            });
            db.SiegeHoldsEnAttente.Add(new SiegeHoldEnAttente
            {
                IdVoyage = 801,
                IdSiege = 8001,
                IdCommandeReservationEnAttente = Guid.NewGuid(),
                ExpireAt = DateTime.UtcNow.AddMinutes(10),
                DateCreation = DateTime.UtcNow
            });
            db.Billets.Add(new Billet
            {
                IdBillet = 1401,
                IdSociete = 1,
                IdReservation = 900,
                IdReservationPassenger = 1400,
                IdSiege = 8000,
                CodeSiege = "O1",
                QrCode = "QR-HOLD",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = DateTime.Today,
                DateValiditeFin = DateTime.Today.AddDays(7)
            });

            await ConfigSocieteTestHelper.SeedAsync(db, 1);
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.ReaffecterBilletAsync(1, 1401, 801, 999, confirmerPaiementDifferentiel: true);

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("siège disponible", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReaffecterBillet_refuses_legacy_billet_without_passenger()
        {
            await using var db = BuildDbContext(nameof(ReaffecterBillet_refuses_legacy_billet_without_passenger));

            db.Vehicules.AddRange(
                new Vehicule { IdVehicule = 90, AliasVehicule = "BUS-Q", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true },
                new Vehicule { IdVehicule = 91, AliasVehicule = "BUS-R", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            db.Destinations.Add(new Destination { IdDestination = 100, IdSociete = 1, VilleDepart = "Goma", VilleArrivee = "Rutshuru", Statut = true });
            db.Voyages.AddRange(
                new Voyage
                {
                    Id = 900,
                    IdSociete = 1,
                    IdVehicule = 90,
                    IdDestination = 100,
                    DateDepart = DateTime.Today.AddDays(1),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                },
                new Voyage
                {
                    Id = 901,
                    IdSociete = 1,
                    IdVehicule = 91,
                    IdDestination = 100,
                    DateDepart = DateTime.Today.AddDays(2),
                    HeureDepart = TimeSpan.FromHours(8),
                    Prix = 100,
                    CodeDevisePrix = "CDF",
                    CodeDevisePrincipale = "CDF",
                    TauxVersDevisePrincipale = 1m,
                    PrixDevisePrincipale = 100,
                    Statut = true
                });
            db.Reservations.Add(new Reservation
            {
                IdReservation = 1000,
                IdVoyage = 900,
                IdClient = 309,
                IdUtilisateur = 409,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });
            db.Billets.Add(new Billet
            {
                IdBillet = 1500,
                IdSociete = 1,
                IdReservation = 1000,
                IdReservationPassenger = null,
                QrCode = "QR-LEGACY",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false,
                DateValiditeDebut = DateTime.Today,
                DateValiditeFin = DateTime.Today.AddDays(7)
            });

            await ConfigSocieteTestHelper.SeedAsync(db, 1);
            await db.SaveChangesAsync();

            var service = BilletServiceTestHelper.Create(db);
            var result = await service.ReaffecterBilletAsync(1, 1500, 901, 999, confirmerPaiementDifferentiel: true);

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("passager", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
