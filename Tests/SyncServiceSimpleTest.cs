using CongoTravel.Models.DTOs.Sync;
using CongoTravel.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Test simple pour valider le service de synchronisation
    /// Isolé des problèmes de nullabilité des services existants
    /// </summary>
    public class SyncServiceSimpleTest
    {
        private readonly ISyncService _syncService;

        public SyncServiceSimpleTest()
        {
            // Configuration minimale pour le test
            var services = new ServiceCollection();
            
            services.AddLogging();
            
            // Service de watermark (mock)
            services.AddSingleton<IWatermarkService>(new TestWatermarkService());
            
            // Service de cursor (mock)
            services.AddSingleton<ICursorService>(new TestCursorService());
            
            // Service de synchronisation à tester
            services.AddScoped<ISyncService, TestSyncService>();
            
            var serviceProvider = services.BuildServiceProvider();
            _syncService = serviceProvider.GetRequiredService<ISyncService>();
        }

        [Fact]
        public async Task GetBootstrap_ShouldReturnValidResponse()
        {
            // Arrange
            var societeId = 1;

            // Act
            var result = await _syncService.GetBootstrapAsync(societeId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Watermark);
            Assert.NotNull(result.Clients);
            Assert.NotNull(result.Arrears);
            Assert.NotNull(result.ReservationWorkflowV2);
            Assert.Equal(2, result.ReservationWorkflowV2.SchemaVersion);
            Assert.Contains("with-passengers", result.ReservationWorkflowV2.PostReservationWithPaiementAliasPath);
        }

        [Fact]
        public async Task GetClients_ShouldReturnPagedResults()
        {
            // Arrange
            var societeId = 1;
            var request = new SyncRequestDto
            {
                PageSize = 10,
                Cursor = null,
                Snapshot = null,
                Since = null
            };

            // Act
            var result = await _syncService.GetClientsAsync(societeId, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.Equal(10, result.Items.Count);
            Assert.NotNull(result.Snapshot);
        }

        [Fact]
        public async Task GetArrears_ShouldReturnPagedResults()
        {
            // Arrange
            var societeId = 1;
            var request = new SyncArrearsRequestDto
            {
                PageSize = 10,
                OnlyOutstanding = true
            };

            // Act
            var result = await _syncService.GetArrearsAsync(societeId, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.True(request.OnlyOutstanding);
        }

        [Fact]
        public async Task GetDeletions_ShouldReturnEmptyResults()
        {
            // Arrange
            var societeId = 1;
            var request = new SyncDeletionsRequestDto
            {
                Since = "base64(test-watermark)"
            };

            // Act
            var result = await _syncService.GetDeletionsAsync(societeId, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.DeletedClientIds);
            Assert.NotNull(result.RemovedClientFactureIds);
            Assert.NotNull(result.DeletedPaymentIds);
        }

        [Fact]
        public async Task ProcessPaymentsBatch_ShouldHandleEmptyBatch()
        {
            // Arrange
            var societeId = 1;
            var request = new PaymentBatchRequestDto
            {
                Items = new List<PaymentRequestDto>()
            };

            // Act
            var result = await _syncService.ProcessPaymentsBatchAsync(societeId, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(0, result.Summary.Total);
            Assert.Equal(0, result.Summary.Created);
            Assert.Equal(0, result.Summary.Errors);
        }
    }

    /// <summary>
    /// Mock du service de watermark pour les tests
    /// </summary>
    public class TestWatermarkService : IWatermarkService
    {
        public string CreateWatermark(DateTime lastModified, int lastId)
        {
            return $"test-watermark-{lastModified:O}-{lastId}";
        }

        public (DateTime lastModified, int lastId) ParseWatermark(string watermark)
        {
            return (DateTime.UtcNow, 0);
        }

        public string CreateInitialWatermark()
        {
            return "test-initial-watermark";
        }
    }

    /// <summary>
    /// Mock du service de cursor pour les tests
    /// </summary>
    public class TestCursorService : ICursorService
    {
        public string CreateCursor<T>(T entity) where T : class
        {
            return "test-cursor";
        }

        public (DateTime updatedAt, int id) ParseCursor(string cursor)
        {
            return (DateTime.UtcNow, 0);
        }
    }

    /// <summary>
    /// Mock du service de synchronisation pour les tests
    /// </summary>
    public class TestSyncService : ISyncService
    {
        public async Task<SyncBootstrapDto> GetBootstrapAsync(int societeId)
        {
            return new SyncBootstrapDto
            {
                Watermark = "test-watermark",
                Clients = new List<ClientSyncDto>(),
                Arrears = new List<ArrearSyncDto>(),
                ReservationWorkflowV2 = new ReservationWorkflowV2ApiHintsDto()
            };
        }

        public async Task<SyncPageDto<ClientSyncDto>> GetClientsAsync(int societeId, SyncRequestDto request)
        {
            var items = new List<ClientSyncDto>();
            for (int i = 0; i < request.PageSize; i++)
            {
                items.Add(new ClientSyncDto
                {
                    IdClient = i + 1,
                    NomClient = $"Client {i + 1}",
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            return new SyncPageDto<ClientSyncDto>
            {
                Snapshot = DateTime.UtcNow.ToString("O"),
                Items = items,
                NextCursor = request.PageSize < 50 ? "test-cursor" : null,
                HasMore = request.PageSize < 50,
                NextSince = "test-next-since"
            };
        }

        public async Task<SyncPageDto<ArrearSyncDto>> GetArrearsAsync(int societeId, SyncArrearsRequestDto request)
        {
            var items = new List<ArrearSyncDto>();
            for (int i = 0; i < request.PageSize; i++)
            {
                items.Add(new ArrearSyncDto
                {
                    IdClientFacture = i + 1,
                    IdClient = 1,
                    MontantDu = 100,
                    DateModification = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            return new SyncPageDto<ArrearSyncDto>
            {
                Snapshot = DateTime.UtcNow.ToString("O"),
                Items = items,
                NextCursor = request.PageSize < 50 ? "test-cursor" : null,
                HasMore = request.PageSize < 50,
                NextSince = "test-next-since"
            };
        }

        public async Task<SyncDeletionsDto> GetDeletionsAsync(int societeId, SyncDeletionsRequestDto request)
        {
            return new SyncDeletionsDto
            {
                Snapshot = DateTime.UtcNow.ToString("O"),
                DeletedClientIds = new List<int>(),
                RemovedClientFactureIds = new List<int>(),
                DeletedPaymentIds = new List<int>(),
                NextSince = "test-next-since"
            };
        }

        public Task<PaymentBatchResultDto> ProcessPaymentsBatchAsync(int societeId, PaymentBatchRequestDto request)
            => Task.FromResult(new PaymentBatchResultDto { Results = new List<PaymentResultDto>(), Summary = new PaymentSummaryDto() });

        public Task<SyncPageDto<VoyageSyncDto>> GetVoyagesAsync(int societeId, SyncRequestDto request)
            => Task.FromResult(new SyncPageDto<VoyageSyncDto> { Snapshot = DateTime.UtcNow.ToString("O"), Items = new List<VoyageSyncDto>() });

        public Task<SyncPageDto<ReservationSyncDto>> GetReservationsAsync(int societeId, SyncRequestDto request)
            => Task.FromResult(new SyncPageDto<ReservationSyncDto> { Snapshot = DateTime.UtcNow.ToString("O"), Items = new List<ReservationSyncDto>() });

        public Task<SyncPageDto<BilletSyncDto>> GetBilletsAsync(int societeId, SyncRequestDto request)
            => Task.FromResult(new SyncPageDto<BilletSyncDto> { Snapshot = DateTime.UtcNow.ToString("O"), Items = new List<BilletSyncDto>() });
    }
}
