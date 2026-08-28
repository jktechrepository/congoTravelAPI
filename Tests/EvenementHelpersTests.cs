using CongoTravel.Helpers.Evenement;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementHelpersTests
    {
        [Fact]
        public void NormalizeToUtc_specifies_kind_for_unspecified()
        {
            var value = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Unspecified);
            var utc = EvenementDateTimeUtcHelper.NormalizeToUtc(value);

            Assert.Equal(DateTimeKind.Utc, utc.Kind);
            Assert.Equal(18, utc.Hour);
        }

        [Fact]
        public void GenerateReservationReferenceCandidate_respects_max_length_and_prefix()
        {
            var reference = EvenementReferenceGenerator.GenerateReservationReferenceCandidate(
                42,
                new DateTime(2026, 7, 3, 12, 30, 0, DateTimeKind.Utc));

            Assert.StartsWith("EVT-RES-0042-", reference);
            Assert.True(reference.Length <= 64);
        }

        [Fact]
        public void GeneratePaymentReferenceCandidate_uses_distinct_prefix()
        {
            var reference = EvenementReferenceGenerator.GeneratePaymentReferenceCandidate(7);

            Assert.StartsWith("EVT-PAY-0007-", reference);
            Assert.True(reference.Length <= 100);
        }

        [Fact]
        public void GenerateTicketCodeCandidate_uses_evt_tkt_prefix()
        {
            var code = EvenementTicketCodeGenerator.GenerateTicketCodeCandidate(
                5,
                new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc));

            Assert.StartsWith("EVT-TKT-005-20260703080000-", code);
            Assert.True(code.Length <= 100);
            Assert.True(EvenementTicketCodeGenerator.IsValidTicketCodeFormat(code));
        }

        [Fact]
        public void NormalizeTicketCode_trims_and_truncates()
        {
            var longCode = new string('X', 150);
            var normalized = EvenementTicketCodeGenerator.NormalizeTicketCode($"  {longCode}  ");

            Assert.NotNull(normalized);
            Assert.Equal(100, normalized!.Length);
        }

        [Fact]
        public void ResolveHoldMinutes_uses_config_or_default()
        {
            Assert.Equal(15, EvenementHoldDurationHelper.ResolveHoldMinutes(null));
            Assert.Equal(30, EvenementHoldDurationHelper.ResolveHoldMinutes(new ConfigSociete { DureeHoldEvenementMinutes = 30 }));
            Assert.Equal(15, EvenementHoldDurationHelper.ResolveHoldMinutes(new ConfigSociete { DureeHoldEvenementMinutes = 0 }));
            Assert.Equal(120, EvenementHoldDurationHelper.ResolveHoldMinutes(new ConfigSociete { DureeHoldEvenementMinutes = 999 }));
        }

        [Fact]
        public void ComputeExpiresAtUtc_adds_hold_minutes()
        {
            var now = new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc);
            var expires = EvenementHoldDurationHelper.ComputeExpiresAtUtc(now, 20);

            Assert.Equal(now.AddMinutes(20), expires);
        }

        [Fact]
        public void NormalizeKey_trims_and_caps_length()
        {
            Assert.Null(EvenementIdempotencyHelper.NormalizeKey("   "));
            Assert.Equal("abc", EvenementIdempotencyHelper.NormalizeKey("  abc  "));
            Assert.Equal(120, EvenementIdempotencyHelper.NormalizeKey(new string('k', 200))!.Length);
        }

        [Fact]
        public void ResolveEffectiveSocieteId_uses_jwt_for_non_super_admin()
        {
            var currentUser = new FakeCurrentUserService { SocieteId = 9, IsSuperAdmin = false };

            Assert.Equal(9, EvenementTenancyGuard.ResolveEffectiveSocieteId(currentUser));
        }

        [Fact]
        public void ResolveEffectiveSocieteId_null_requested_falls_back_to_jwt()
        {
            var currentUser = new FakeCurrentUserService { SocieteId = 9, IsSuperAdmin = false };

            Assert.Equal(9, EvenementTenancyGuard.ResolveEffectiveSocieteId(currentUser, requestedIdSociete: null));
        }

        [Fact]
        public void ResolveEffectiveSocieteId_same_tenant_request_accepted()
        {
            var currentUser = new FakeCurrentUserService { SocieteId = 9, IsSuperAdmin = false };

            Assert.Equal(9, EvenementTenancyGuard.ResolveEffectiveSocieteId(currentUser, requestedIdSociete: 9));
        }

        [Fact]
        public void ResolveEffectiveSocieteId_rejects_cross_tenant_request()
        {
            var currentUser = new FakeCurrentUserService { SocieteId = 9, IsSuperAdmin = false };

            Assert.Throws<UnauthorizedAccessException>(() =>
                EvenementTenancyGuard.ResolveEffectiveSocieteId(currentUser, requestedIdSociete: 10));
        }

        [Fact]
        public void ResolveEffectiveSocieteId_allows_super_admin_override()
        {
            var currentUser = new FakeCurrentUserService { SocieteId = 1, IsSuperAdmin = true };

            Assert.Equal(42, EvenementTenancyGuard.ResolveEffectiveSocieteId(currentUser, requestedIdSociete: 42));
        }

        [Fact]
        public void ResolveEffectiveSocieteId_super_admin_without_request_uses_jwt()
        {
            var currentUser = new FakeCurrentUserService { SocieteId = 1, IsSuperAdmin = true };

            Assert.Equal(1, EvenementTenancyGuard.ResolveEffectiveSocieteId(currentUser, requestedIdSociete: null));
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public int UserId => 1;
            public int GetUserId() => UserId;
            public string UserRole => "Admin";
            public string PrimaryRole => UserRole;
            public string GetUserRole() => UserRole;
            public int SocieteId { get; init; }
            public int GetSocieteId() => SocieteId;
            public string? SocieteNom => null;
            public string? GetSocieteNom() => null;
            public string? GetUserName() => "test";
            public int? TuteurId => null;
            public int? AgentId => null;
            public int? SiteId => null;
            public int? ClientId => null;
            public int? EleveId => null;
            public string? Email => null;
            public string? UserName => "test";
            public bool IsAuthenticated => true;
            public bool IsSuperAdmin { get; init; }
            public bool IsAdmin => true;
            public bool IsStaff => true;
            public bool HasFinanceAccess => false;
            public bool HasPedagogieAccess => false;
        }
    }
}
