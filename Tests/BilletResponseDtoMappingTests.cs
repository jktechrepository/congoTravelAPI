using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Mapping;
using Xunit;

namespace CongoTravel.Tests
{
    public class BilletResponseDtoMappingTests
    {
        private static IMapper CreateMapper() =>
            new MapperConfiguration(
                cfg => cfg.AddProfile<VehiculeMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();

        [Fact]
        public void Billet_response_includes_logo_societe_when_loaded()
        {
            var mapper = CreateMapper();
            var billet = new Billet
            {
                IdBillet = 12,
                IdSociete = 7,
                QrCode = "QR-BILLET-001",
                DateGeneration = DateTime.UtcNow,
                Societe = new Societe
                {
                    IdSociete = 7,
                    Nom = "Congo Travel",
                    Logo = "https://cdn.example/logo-billet.png",
                    Statut = true
                }
            };

            var dto = mapper.Map<BilletResponseDto>(billet);

            Assert.Equal(7, dto.IdSociete);
            Assert.Equal("https://cdn.example/logo-billet.png", dto.LogoSociete);
        }

        [Fact]
        public void Billet_response_logo_societe_null_when_societe_not_loaded()
        {
            var mapper = CreateMapper();
            var billet = new Billet
            {
                IdBillet = 13,
                IdSociete = 8,
                QrCode = "QR-BILLET-002",
                DateGeneration = DateTime.UtcNow
            };

            var dto = mapper.Map<BilletResponseDto>(billet);

            Assert.Equal(8, dto.IdSociete);
            Assert.Null(dto.LogoSociete);
        }
    }
}
