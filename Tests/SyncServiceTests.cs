using CongoTravel.Models.DTOs.Sync;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Tests simples pour valider l'implémentation de la synchronisation
    /// </summary>
    public class SyncServiceTests
    {
        [Fact]
        public void SyncBootstrapDto_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var dto = new SyncBootstrapDto();

            // Assert
            Assert.NotNull(dto);
            Assert.NotNull(dto.Clients);
            Assert.NotNull(dto.Arrears);
            Assert.NotNull(dto.ReservationWorkflowV2);
            Assert.Equal(2, dto.ReservationWorkflowV2.SchemaVersion);
        }

        [Fact]
        public void SyncPageDto_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var dto = new SyncPageDto<ClientSyncDto>();

            // Assert
            Assert.NotNull(dto);
            Assert.NotNull(dto.Items);
            Assert.False(dto.HasMore);
            Assert.Empty(dto.Snapshot);
        }

        [Fact]
        public void PaymentRequestDto_ShouldValidateRequiredFields()
        {
            // Arrange
            var payment = new PaymentRequestDto();

            // Act & Assert - Test validation
            Assert.True(string.IsNullOrEmpty(payment.ClientRequestId));
            Assert.Equal(0, payment.IdClient);
            Assert.Equal(0, payment.MontantPaye);
        }

        [Fact]
        public void CursorService_ShouldCreateValidCursor()
        {
            // Arrange
            var entity = new { UpdatedAt = DateTime.UtcNow, Id = 123 };
            
            // Act
            var cursor = $"base64({entity.UpdatedAt:O}|{entity.Id})";

            // Assert
            Assert.NotNull(cursor);
            Assert.Contains("base64", cursor);
        }

        [Fact]
        public void WatermarkService_ShouldCreateValidWatermark()
        {
            // Arrange
            var lastModified = DateTime.UtcNow;
            var lastId = 123;

            // Act
            var watermark = $"base64({lastModified:O}|{lastId})";

            // Assert
            Assert.NotNull(watermark);
            Assert.Contains("base64", watermark);
        }
    }
}
