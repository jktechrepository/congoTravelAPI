using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le service d'exécution des campagnes de communication
    /// </summary>
    public interface ICommunicationDispatchService
    {
        /// <summary>
        /// Exécute une campagne de communication immédiatement
        /// </summary>
        Task<CommunicationCampaignDispatchResult> ExecuteCampaignAsync(int idCampagne);

        /// <summary>
        /// Prévise les clients qui seront ciblés par une campagne
        /// </summary>
        Task<List<Client>> PreviewTargetedClientsAsync(int idCampagne);
    }

    /// <summary>
    /// Résultat de l'exécution d'une campagne
    /// </summary>
    public class CommunicationCampaignDispatchResult
    {
        public int IdCampagne { get; set; }
        public int NombreDestinataires { get; set; }
        public int NombreEnvoyes { get; set; }
        public int NombreSucces { get; set; }
        public int NombreEchecs { get; set; }
        public Dictionary<string, int> StatistiquesParCanal { get; set; } = new();
        public DateTime DateExecution { get; set; }
        public string? MessageErreur { get; set; }
    }
}

