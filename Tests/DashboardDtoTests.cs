using Xunit;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Tests
{
    public class DashboardDtoTests
    {
        [Fact]
        public void DashboardDto_PeutEtreInstancie()
        {
            var dashboard = new DashboardDto
            {
                CodeDevisePrincipale = "CDF",
                TotalAgents = 10,
                TotalClientsActifs = 100,
                TransportStatistiques = new DashboardTransportStatistiquesDto
                {
                    VoyagesActifs = 5,
                    ReservationsConfirmeesMois = 20
                },
                CollecteMois = new CollecteMoisDto { Montant = 50000 }
            };

            Assert.NotNull(dashboard);
            Assert.Equal("CDF", dashboard.CodeDevisePrincipale);
            Assert.Equal(10, dashboard.TotalAgents);
            Assert.Equal(100, dashboard.TotalClientsActifs);
            Assert.Equal(50000, dashboard.CollecteMois.Montant);
            Assert.Equal(5, dashboard.TransportStatistiques.VoyagesActifs);
        }

        [Fact]
        public void TopAgentCollecteurDto_PeutEtreInstancie()
        {
            var agent = new TopAgentCollecteurDto
            {
                IdAgent = 1,
                NomComplet = "Test Agent",
                MontantCollecte = 10000,
                NombrePaiements = 5
            };

            Assert.NotNull(agent);
            Assert.Equal(1, agent.IdAgent);
            Assert.Equal("Test Agent", agent.NomComplet);
            Assert.Equal(10000, agent.MontantCollecte);
            Assert.Equal(5, agent.NombrePaiements);
        }

        [Fact]
        public void DashboardDto_InitialiseAvecValeursParDefaut()
        {
            var dashboard = new DashboardDto();

            Assert.NotNull(dashboard);
            Assert.Equal(0, dashboard.TotalAgents);
            Assert.Equal(0, dashboard.TotalClientsActifs);
            Assert.Equal("CDF", dashboard.CodeDevisePrincipale);
            Assert.NotNull(dashboard.TransportStatistiques);
            Assert.NotNull(dashboard.CollecteMois);
            Assert.NotNull(dashboard.CollecteParOrigineGroupe);
            Assert.NotNull(dashboard.CollecteOrigineGroupeSynthese);
            Assert.NotNull(dashboard.Top5AgentsCollecteurs);
            Assert.Empty(dashboard.Top5AgentsCollecteurs);
            Assert.Empty(dashboard.CollecteParOrigineGroupe);
        }
    }
}
