using CongoTravel.Models.DTOs.Sync;
using Xunit;
using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Tests de validation pour les DTOs de synchronisation
    /// </summary>
    public class SyncValidationTests
    {
        [Fact]
        public void SyncRequestDto_ShouldHaveValidDefaults()
        {
            // Arrange & Act
            var request = new SyncRequestDto();

            // Assert
            Assert.Equal(1000, request.PageSize);
            Assert.Null(request.Cursor);
            Assert.Null(request.Snapshot);
            Assert.Null(request.Since);
        }

        [Fact]
        public void SyncArrearsRequestDto_ShouldHaveValidDefaults()
        {
            // Arrange & Act
            var request = new SyncArrearsRequestDto();

            // Assert
            Assert.Equal(1000, request.PageSize);
            Assert.True(request.OnlyOutstanding); // Valeur par défaut
        }

        [Fact]
        public void PaymentBatchRequestDto_ShouldValidateItems()
        {
            // Arrange
            var request = new PaymentBatchRequestDto();

            // Act
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            // Assert
            Assert.NotNull(request.Items);
            Assert.Empty(request.Items);
        }

        [Fact]
        public void PaymentRequestDto_ShouldValidateRequiredFields()
        {
            // Arrange
            var payment = new PaymentRequestDto
            {
                ClientRequestId = "test-uuid",
                IdClient = 1,
                MontantPaye = 100,
                DatePaiementUtc = DateTime.UtcNow,
                MethodePaiement = "Espèces"
            };

            // Act
            var context = new ValidationContext(payment);
            var isValid = Validator.TryValidateObject(payment, context, null);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void PaymentRequestDto_ShouldFailValidationWithoutRequiredFields()
        {
            // Arrange
            var payment = new PaymentRequestDto(); // Empty

            // Act
            var context = new ValidationContext(payment);
            var isValid = Validator.TryValidateObject(payment, context, null);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void SyncDeletionsRequestDto_ShouldRequireSince()
        {
            // Arrange
            var request = new SyncDeletionsRequestDto();

            // Act
            var context = new ValidationContext(request);
            var isValid = Validator.TryValidateObject(request, context, null);

            // Assert
            Assert.False(isValid); // Since est requis
        }
    }
}
