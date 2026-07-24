using CongoTravel.Configuration;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementFlexPayContractTests
    {
        [Fact]
        public void FlexPayOptions_IsEventEnabled_falls_back_to_global_enabled()
        {
            var opts = new FlexPayOptions { Enabled = true, EventEnabled = null };
            Assert.True(opts.IsEventEnabled);

            opts.EventEnabled = false;
            Assert.False(opts.IsEventEnabled);
        }

        [Fact]
        public void ResolveEvenementCallbackUrl_uses_event_path_on_public_host()
        {
            var url = FlexPayUrlHelper.ResolveEvenementCallbackUrl(
                httpContext: null,
                callbackBaseUrl: "https://api.prod.com/api/FlexPay/callback",
                eventCallbackRelativePath: EvenementFlexPayConstants.CallbackRoute,
                forceProductionCallbackInDev: true);

            Assert.Equal("https://api.prod.com/api/events/flexpay/callback", url);
        }

        [Fact]
        public void ResolveEvenementCallbackUrl_uses_custom_relative_path()
        {
            var url = FlexPayUrlHelper.ResolveEvenementCallbackUrl(
                httpContext: null,
                callbackBaseUrl: "https://api.prod.com",
                eventCallbackRelativePath: "/custom/events/callback",
                forceProductionCallbackInDev: true);

            Assert.Equal("https://api.prod.com/custom/events/callback", url);
        }

        [Fact]
        public void EvenementFlexPayReferenceHelper_builds_bounded_references()
        {
            var merchantRef = EvenementFlexPayReferenceHelper.BuildMerchantReference(12345);
            var pendingOrder = EvenementFlexPayReferenceHelper.BuildPendingOrderNumber(12345);

            Assert.True(merchantRef.Length <= 20);
            Assert.StartsWith("EVT", merchantRef);
            Assert.True(pendingOrder.Length <= 100);
            Assert.StartsWith("PENDING-EVT-12345-", pendingOrder);
        }

        [Fact]
        public void EvenementFlexPayConstants_match_planned_routes()
        {
            Assert.Equal("/api/events/flexpay/callback", EvenementFlexPayConstants.CallbackRoute);
            Assert.Equal("/api/events/flexpay/verifier", EvenementFlexPayConstants.VerifierRoutePrefix);
            Assert.Equal("FLEXPAY", EvenementFlexPayConstants.Provider);
        }
    }
}
