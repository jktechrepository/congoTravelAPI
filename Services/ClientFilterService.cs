using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Communication;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service de filtrage des clients selon des critères de ciblage
    /// </summary>
    public class ClientFilterService : IClientFilterService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<ClientFilterService> _logger;

        public ClientFilterService(
            CongoTravelDbContext context,
            ILogger<ClientFilterService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Client>> GetClientsByCriteriaAsync(CriteresCiblageDto? criteres)
        {
            // ✅ TOUJOURS filtrer par Statut = true (clients actifs uniquement)
            var query = _context.Clients
                .Where(c => c.Statut == true)
                .AsQueryable();

            // Si aucun critère, retourner tous les clients actifs
            if (criteres == null)
            {
                return await query.ToListAsync();
            }

            // Si une liste spécifique d'IDs clients est fournie, utiliser uniquement celle-ci
            if (criteres.ListeIdClients != null && criteres.ListeIdClients.Length > 0)
            {
                query = query.Where(c => criteres.ListeIdClients.Contains(c.IdClient));
                return await query.ToListAsync();
            }

            // Filtrer par catégories clients (via les usages -> catégories)


            // Filtrer par IsActif (si spécifié)
            if (criteres.ClientsActifs.HasValue)
            {
                if (criteres.ClientsActifs.Value)
                {
                    query = query.Where(c => c.IsActif == true);
                }
                else
                {
                    query = query.Where(c => c.IsActif == false);
                }
            }

          

            // ✨ NOUVEAU : Filtrer par nombre de factures en arriérés
            if (criteres.NombreFacturesArrieresMin.HasValue || criteres.NombreFacturesArrieresMax.HasValue)
            {
                // Récupérer d'abord les IDs des clients qui correspondent aux autres critères
                var clientsIdsFiltres = await query.Select(c => c.IdClient).ToListAsync();

                if (clientsIdsFiltres.Any())
                {
                    // Définir les bornes min et max
                    var minArrieres = criteres.NombreFacturesArrieresMin ?? 0;
                    var maxArrieres = criteres.NombreFacturesArrieresMax ?? int.MaxValue;

                    // Filtrer les clients selon min/max
                    var clientsIdsValides = new List<int>();

                   

                    // Si aucun client ne correspond, retourner une liste vide
                    if (!clientsIdsValides.Any())
                    {
                        _logger.LogInformation(
                            "✅ Filtrage clients: 0 client(s) trouvé(s) avec les critères de factures en arriérés (min: {Min}, max: {Max})",
                            minArrieres, maxArrieres);
                        return new List<Client>();
                    }

                    // Filtrer la query avec les IDs valides
                    query = query.Where(c => clientsIdsValides.Contains(c.IdClient));
                }
                else
                {
                    // Aucun client ne correspond aux autres critères, retourner liste vide
                    _logger.LogInformation(
                        "✅ Filtrage clients: 0 client(s) trouvé(s) (aucun client ne correspond aux critères de base)");
                    return new List<Client>();
                }
            }

            var clients = await query
                .ToListAsync();

            _logger.LogInformation(
                "✅ Filtrage clients: {Count} client(s) trouvé(s) avec les critères spécifiés",
                clients.Count);

            return clients;
        }
    }
}

