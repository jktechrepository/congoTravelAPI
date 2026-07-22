using CongoTravel.Models;
using CongoTravel.Models.DTOs.Mapping;
using Xunit;

namespace CongoTravel.Tests
{
    public class BilletResponseDtoPricingTests
    {
        [Fact]
        public void ResolvePrixVoyage_uses_tarif_when_siege_category_matches()
        {
            var voyage = new Voyage
            {
                Id = 1,
                Prix = 5000,
                VoyageTarifsCategorieSiege = new List<VoyageTarifCategorieSiege>
                {
                    new() { IdCategorieSiege = 2, Prix = 7500 },
                    new() { IdCategorieSiege = 3, Prix = 12000 }
                }
            };
            var billet = new Billet
            {
                Reservation = new Reservation { Voyage = voyage },
                Siege = new Siege { IdCategorieSiege = 3 }
            };

            Assert.Equal(12000, BilletResponseDtoPricing.ResolvePrixVoyage(billet));
        }

        [Fact]
        public void ResolvePrixVoyage_falls_back_to_voyage_prix_when_no_tarif_row()
        {
            var voyage = new Voyage
            {
                Id = 1,
                Prix = 5000,
                VoyageTarifsCategorieSiege = new List<VoyageTarifCategorieSiege>
                {
                    new() { IdCategorieSiege = 2, Prix = 7500 }
                }
            };
            var billet = new Billet
            {
                Reservation = new Reservation { Voyage = voyage },
                Siege = new Siege { IdCategorieSiege = 99 }
            };

            Assert.Equal(5000, BilletResponseDtoPricing.ResolvePrixVoyage(billet));
        }

        [Fact]
        public void ResolvePrixVoyage_falls_back_when_no_siege()
        {
            var voyage = new Voyage { Id = 1, Prix = 3333 };
            var billet = new Billet
            {
                Reservation = new Reservation { Voyage = voyage },
                Siege = null
            };

            Assert.Equal(3333, BilletResponseDtoPricing.ResolvePrixVoyage(billet));
        }
    }
}
