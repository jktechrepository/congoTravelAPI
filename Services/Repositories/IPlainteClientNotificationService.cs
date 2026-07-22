using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le service de notifications des plaintes clients
    /// </summary>
    public interface IPlainteClientNotificationService
    {
        /// <summary>
        /// Notifie l'équipe d'intervention lors de la création d'une plainte
        /// </summary>
        Task NotifierEquipeInterventionAsync(PlainteClient plainte);
    }
}

