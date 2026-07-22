using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>Le hub SignalR « Dashboard » n’est plus dans le dépôt API — réactiver des tests quand il sera réintroduit.</summary>
    public class DashboardSignalRIntegrationTests
    {
        [Fact(Skip = "DashboardHub absent du projet CongoTravel — test réservé à une future implémentation SignalR.")]
        public void Placeholder_SignalR_dashboard() { }
    }
}
