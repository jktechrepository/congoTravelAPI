using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class ClientService : IClientRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISmsNotificationService _smsService;
        private readonly IUtilisateurRepository _utilisateurRepository;
        private readonly ILogger<ClientService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;

        public ClientService(
            CongoTravelDbContext context,
            IEmailService emailService,
            ISmsNotificationService smsService,
            IUtilisateurRepository utilisateurRepository,
            ILogger<ClientService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _smsService = smsService;
            _utilisateurRepository = utilisateurRepository;
            _logger = logger;
            _configuration = configuration;
            
            // Récupérer la configuration du frontend
            _baseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "https://k-energie.kansaconsulting.com";
        }

        public async Task<IEnumerable<Client>> GetAllAsync()
        {
            // Les fonctionnalités de catégorie client ne sont plus disponibles après la refactorisation
            return await _context.Clients
                .Where(c => c.Statut == true)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetByCategorieAsync(int idCategorie)
        {
            // Les fonctionnalités de catégorie client ne sont plus disponibles après la refactorisation
            return new List<Client>();
        }

        public async Task<IEnumerable<Client>> GetBySocieteAsync(int idSociete)
        {
            var query = BuildClientPagedBaseQuery();
            query = ApplySocieteReservationScope(query, idSociete);
            return await query
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        // Les fonctionnalités de TypeDeCourant ne sont plus disponibles après la refactorisation
        public async Task<IEnumerable<Client>> GetByTypeDeCourantAsync(int idTypeDeCourant)
        {
            // Méthode obsolète - retourne une liste vide
            return new List<Client>();
        }

        public async Task<IEnumerable<Client>> GetBySocieteAndSearchAsync(int idSociete, string searchTerm, bool includeInactive = false)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetBySocieteAsync(idSociete);
            }

            var term = searchTerm.Trim().ToLower();
            
            // LOG DE DÉBOGAGE : Afficher le terme recherché
            _logger.LogInformation("🔍 Recherche clients - Société: {SocieteId}, Terme: '{SearchTerm}', TermeLower: '{Term}', IncludeInactive: {IncludeInactive}", 
                idSociete, searchTerm, term, includeInactive);

            var query = BuildClientPagedBaseQuery();
            query = ApplySocieteReservationScope(query, idSociete);
            query = query.Where(c =>
                (includeInactive || c.IsActif == true) &&
                (c.NomClient.ToLower().Contains(term) ||
                 (c.AdresseClient != null && c.AdresseClient.ToLower().Contains(term)) ||
                 (c.Telephone != null && c.Telephone.ToLower().Contains(term)) ||
                 (c.EmailClient != null && c.EmailClient.ToLower().Contains(term)) ||
                 (c.GenreClient != null && c.GenreClient.ToLower().Contains(term))));

            var clients = await query
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();

            // LOG DE DÉBOGAGE : Afficher les résultats
            _logger.LogInformation("📊 Résultats recherche - Trouvé: {Count} clients", clients.Count);
            
            // Les fonctionnalités de CodeCons ne sont plus disponibles après la refactorisation

            return clients;
        }

        public async Task<PagedResult<Client>> GetBySocietePagedAsync(int idSociete, ClientPagedSearchRequestDto request)
        {
            request ??= new ClientPagedSearchRequestDto();
            NormalizeClientPagedRequest(request);

            // 🔍 LOG DE DÉBOGAGE : Vérifier les paramètres reçus (sans IdTypeDeCourant)
            _logger.LogInformation("🔍 GetBySocietePagedAsync - SocieteId: {SocieteId}, IncludeInactive: {IncludeInactive}, IsActif: {IsActif}, SearchTerm: '{SearchTerm}', Page: {Page}, PageSize: {PageSize}",
                idSociete, request.IncludeInactive, request.IsActif, GetEffectiveSearchTerm(request), request.PageNumber, request.PageSize);

            var query = BuildClientPagedBaseQuery();
            query = ApplySocieteReservationScope(query, idSociete);
            query = ApplyClientActifFilters(query, request, logPrefix: "GetBySocietePagedAsync");
            query = ApplyClientSearchTermFilter(query, request);
            query = ApplyClientSort(query, request);

            var total = await query.CountAsync();

            _logger.LogInformation("📊 GetBySocietePagedAsync - Total clients trouvés: {Total}", total);

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var actifsCount = data.Count(c => c.IsActif == true);
            var inactifsCount = data.Count(c => c.IsActif == false);

            _logger.LogInformation("📈 GetBySocietePagedAsync - Actifs: {Actifs}, Inactifs: {Inactifs}, Page: {Page}, Total: {Total}",
                actifsCount, inactifsCount, request.PageNumber, total);

            return new PagedResult<Client>(data, total, request.PageNumber, request.PageSize);
        }

        public async Task<Client> GetByIdAsync(int id)
        {
            // Les fonctionnalités de clients usages ne sont plus disponibles après la refactorisation
            return await _context.Clients
                .Where(c => c.Statut == true)
                .FirstOrDefaultAsync(c => c.IdClient == id);
        }

        public async Task<IEnumerable<Client>> GetByNomAsync(string nom)
        {
            // Les fonctionnalités de clients usages et axe ne sont plus disponibles après la refactorisation
            return await _context.Clients
                .Where(c => c.Statut == true && c.NomClient.Contains(nom))
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Client>> GetByIsActifAsync(bool IsActif)
        {
            // Les fonctionnalités de clients usages et axe ne sont plus disponibles après la refactorisation
            return await _context.Clients
                .Where(c => c.Statut == true && c.IsActif == IsActif)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }
        // Les fonctionnalités de CodeCons ne sont plus disponibles après la refactorisation
        public async Task<Client?> GetByCodeConsAsync(string codeCons)
        {
            // Méthode obsolète - retourne null
            return null;
        }

        public async Task<Client> CreateAsync(Client client)
        {
            client.DateCreation = DateTime.Now;
            client.AdresseClient = NormalizeAdresseClient(client.AdresseClient);
            if (!client.Statut)
            {
                client.Statut = true;
            }

            // Les fonctionnalités de CodeCons et IdAxe ne sont plus disponibles après la refactorisation

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            // Note: Les usages sont maintenant gérés via ClientUsage, pas via IdCategorieClient
            // Les usages doivent être ajoutés séparément après la création du client

            // ✨ NOUVEAU : Créer automatiquement un compte utilisateur pour le client
            try
            {
                _logger.LogInformation("🔍 Début de la création automatique du compte utilisateur pour le client {ClientId} (Email: {Email})", 
                    client.IdClient, client.EmailClient);
                
                var result = await CreateDefaultClientUserAsync(client);
                if (result == null)
                {
                    _logger.LogWarning("⚠️ CreateDefaultClientUserAsync a retourné null pour le client {ClientId}", client.IdClient);
                }
                else
                {
                    _logger.LogInformation("✅ Compte utilisateur créé/mis à jour pour le client {ClientId} (IdUtilisateur: {UserId})", 
                        client.IdClient, result.IdUtilisateur);
                }
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire échouer la création du client
                _logger.LogError(ex, "❌ ERREUR lors de la création automatique du compte utilisateur pour le client {ClientId}: {ErrorMessage}", 
                    client.IdClient, ex.Message);
            }

            return client;
        }

        /// <summary>
        /// Crée un client avec ses usages dans une transaction atomique
        /// Utilise la stratégie d'exécution pour gérer les transactions de manière compatible avec MySqlRetryingExecutionStrategy
        /// </summary>
        /// <param name="client">Informations du client</param>
        /// <param name="usages">Liste des usages avec leur libellé et nombre de bâtiments</param>
        /// <returns>Le client créé avec ses usages</returns>
        /// <exception cref="InvalidOperationException">Si un usage n'est pas trouvé ou si une erreur survient</exception>
        public async Task<Client> CreateWithUsagesAsync(Client client, List<(string LibelleUsage, int nombreBatiment)> usages)
        {
            // Utiliser la stratégie d'exécution pour gérer les opérations de manière compatible
            // EF Core gère automatiquement les transactions pour SaveChanges()
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                // 1. Préparer le client
                client.DateCreation = DateTime.Now;
                if (!client.Statut)
                {
                    client.Statut = true;
                }

                // Les fonctionnalités de CodeCons et IdAxe ne sont plus disponibles après la refactorisation

                // 2. Valider et récupérer les usages AVANT d'ajouter le client
                var validatedUsages = new List<(int IdUsage, int nombreBatiment)>();
                if (usages != null && usages.Count > 0)
                {
                    foreach (var usageInfo in usages)
                    {
                        if (string.IsNullOrWhiteSpace(usageInfo.LibelleUsage))
                        {
                            throw new InvalidOperationException($"Le LibelleUsage ne peut pas être vide.");
                        }

                        // Les fonctionnalités d'usage ne sont plus disponibles après la refactorisation
                        throw new InvalidOperationException($"Les fonctionnalités d'usage ne sont plus disponibles après la refactorisation.");

                        }
                }

                // 3. Ajouter le client au contexte
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();

                try
                {
                    _logger.LogInformation(" Début de la création automatique du compte utilisateur pour le client {ClientId} (Email: {Email})", 
                        client.IdClient, client.EmailClient);
                    
                    var result = await CreateDefaultClientUserAsync(client);
                    if (result == null)
                    {
                        _logger.LogWarning("⚠️ CreateDefaultClientUserAsync a retourné null pour le client {ClientId}", client.IdClient);
                    }
                    else
                    {
                        _logger.LogInformation("✅ Compte utilisateur créé/mis à jour pour le client {ClientId} (IdUtilisateur: {UserId})", 
                            client.IdClient, result.IdUtilisateur);
                    }
                }
                catch (Exception ex)
                {
                    // Log l'erreur mais ne pas faire échouer la création du client
                    _logger.LogError(ex, "❌ ERREUR lors de la création automatique du compte utilisateur pour le client {ClientId}: {ErrorMessage}", 
                        client.IdClient, ex.Message);
                }

                // 7. Recharger le client pour le retourner (sans les usages)
                var createdClient = await _context.Clients
                    .FirstOrDefaultAsync(c => c.IdClient == client.IdClient) ?? client;

                _logger.LogInformation("✅ Client avec usages créé avec succès: {IdClient}", client.IdClient);
                return createdClient;
            });
        }

        public async Task<Client> UpdateAsync(Client client)
        {
            var existing = await _context.Clients.FindAsync(client.IdClient);
            if (existing == null)
                return null;

            client.AdresseClient = NormalizeAdresseClient(client.AdresseClient);

            // Sauvegarder les anciennes valeurs pour la synchronisation (sans IdTypeDeCourant)
            var oldNomClient = existing.NomClient;
            var oldTelephone = existing.Telephone;
            var oldEmailClient = existing.EmailClient;
            var oldGenreClient = existing.GenreClient;
            var oldAdresseClient = existing.AdresseClient;

            // Les fonctionnalités de IdTypeDeCourant ne sont plus disponibles après la refactorisation

            _context.Entry(existing).CurrentValues.SetValues(client);
            await _context.SaveChangesAsync();

            // Les fonctionnalités de IdTypeDeCourant ne sont plus disponibles après la refactorisation

            // Note: Les usages sont maintenant gérés via ClientUsage, pas via IdCategorieClient
            // Les usages doivent être gérés séparément via les méthodes AddUsageToClientAsync/RemoveUsageFromClientAsync

            // ✨ SYNCHRONISATION: Mettre à jour les Utilisateurs liés si les champs pertinents ont changé
            var champsModifies = 
                oldNomClient != client.NomClient ||
                oldTelephone != client.Telephone ||
                oldEmailClient != client.EmailClient ||
                oldGenreClient != client.GenreClient ||
                oldAdresseClient != client.AdresseClient;

            if (champsModifies)
            {
                var utilisateursLies = await _context.Utilisateurs
                    .Where(u => u.IdClient == client.IdClient)
                    .ToListAsync();

                foreach (var utilisateur in utilisateursLies)
                {
                    // Synchroniser uniquement les champs qui ont changé
                    if (oldNomClient != client.NomClient && !string.IsNullOrWhiteSpace(client.NomClient))
                    {
                        utilisateur.NomComplet = client.NomClient;
                    }
                    if (oldTelephone != client.Telephone)
                    {
                        // Vérifier l'unicité du téléphone avant de synchroniser
                        if (!string.IsNullOrWhiteSpace(client.Telephone))
                        {
                            var telephoneDejaUtilise = await _context.Utilisateurs
                                .AnyAsync(u => u.Telephone == client.Telephone && u.IdUtilisateur != utilisateur.IdUtilisateur);
                            
                            if (!telephoneDejaUtilise)
                            {
                                utilisateur.Telephone = client.Telephone;
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "⚠️ Téléphone '{Telephone}' non synchronisé pour l'utilisateur {UserId} car déjà utilisé par un autre utilisateur",
                                    client.Telephone, utilisateur.IdUtilisateur);
                            }
                        }
                        else
                        {
                            // Si le téléphone devient null/vide, on peut le synchroniser
                            utilisateur.Telephone = client.Telephone;
                        }
                    }
                    if (oldEmailClient != client.EmailClient)
                    {
                        // Vérifier l'unicité de l'email avant de synchroniser
                        var emailDejaUtilise = await _context.Utilisateurs
                            .AnyAsync(u => u.Email == client.EmailClient && u.IdUtilisateur != utilisateur.IdUtilisateur);
                        
                        if (!emailDejaUtilise && !string.IsNullOrWhiteSpace(client.EmailClient))
                        {
                            utilisateur.Email = client.EmailClient;
                        }
                        else if (emailDejaUtilise)
                        {
                            _logger.LogWarning(
                                "⚠️ Email '{Email}' non synchronisé pour l'utilisateur {UserId} car déjà utilisé par un autre utilisateur",
                                client.EmailClient, utilisateur.IdUtilisateur);
                        }
                    }
                    if (oldGenreClient != client.GenreClient)
                    {
                        utilisateur.Genre = client.GenreClient;
                    }
                    if (oldAdresseClient != client.AdresseClient)
                    {
                        utilisateur.AdresseResidence = client.AdresseClient;
                    }
                }

                if (utilisateursLies.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(
                        "✅ Synchronisation Client → Utilisateurs: {Count} utilisateur(s) mis à jour pour le client {ClientId}",
                        utilisateursLies.Count, client.IdClient);
                }
            }

            return existing;
        }

        /// <summary>
        /// Met à jour un client avec ses usages dans une transaction
        /// </summary>
        public async Task<Client> UpdateWithUsagesAsync(int idClient, Client client, List<(string LibelleUsage, int nombreBatiment, bool Statut)>? usages)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                // 1. Récupérer le client existant
                var existing = await _context.Clients
                    .FirstOrDefaultAsync(c => c.IdClient == idClient);
                
                if (existing == null)
                    return null;

                // 2. Sauvegarder les anciennes valeurs pour la synchronisation (sans IdTypeDeCourant)
                var oldNomClient = existing.NomClient;
                var oldTelephone = existing.Telephone;
                var oldEmailClient = existing.EmailClient;
                var oldGenreClient = existing.GenreClient;
                var oldAdresseClient = existing.AdresseClient;

                // 3. Mettre à jour les champs du client (seulement ceux fournis, sans champs obsolètes)
                if (client.NomClient != null) existing.NomClient = client.NomClient;
                if (client.AdresseClient != null)
                    existing.AdresseClient = NormalizeAdresseClient(client.AdresseClient);
                if (client.Telephone != null) existing.Telephone = client.Telephone;
                if (client.EmailClient != null) existing.EmailClient = client.EmailClient;
                if (client.GenreClient != null) existing.GenreClient = client.GenreClient;
                // Les fonctionnalités de CodeCons, IdAxe et IdTypeDeCourant ne sont plus disponibles après la refactorisation
                // Statut et IsActif sont des bool, pas bool?, donc on les met à jour directement
                existing.Statut = client.Statut;
                existing.IsActif = client.IsActif;

                // 4. Mettre à jour les usages si fournis
                if (usages != null && usages.Count > 0)
                {
                    // Les fonctionnalités d'usage ne sont plus disponibles après la refactorisation
                }

                // 5. Sauvegarder les modifications
                await _context.SaveChangesAsync();

                // 6. Synchroniser avec les Utilisateurs liés si les champs pertinents ont changé
                var champsModifies = 
                    oldNomClient != existing.NomClient ||
                    oldTelephone != existing.Telephone ||
                    oldEmailClient != existing.EmailClient ||
                    oldGenreClient != existing.GenreClient ||
                    oldAdresseClient != existing.AdresseClient;

                if (champsModifies)
                {
                    var utilisateursLies = await _context.Utilisateurs
                        .Where(u => u.IdClient == idClient)
                        .ToListAsync();

                    foreach (var utilisateur in utilisateursLies)
                    {
                        if (oldNomClient != existing.NomClient && !string.IsNullOrWhiteSpace(existing.NomClient))
                            utilisateur.NomComplet = existing.NomClient;
                        if (oldTelephone != existing.Telephone)
                        {
                            if (!string.IsNullOrWhiteSpace(existing.Telephone))
                            {
                                var telephoneDejaUtilise = await _context.Utilisateurs
                                    .AnyAsync(u => u.Telephone == existing.Telephone && u.IdUtilisateur != utilisateur.IdUtilisateur);
                                
                                if (!telephoneDejaUtilise)
                                    utilisateur.Telephone = existing.Telephone;
                            }
                            else
                                utilisateur.Telephone = existing.Telephone;
                        }
                        if (oldEmailClient != existing.EmailClient)
                        {
                            var emailDejaUtilise = await _context.Utilisateurs
                                .AnyAsync(u => u.Email == existing.EmailClient && u.IdUtilisateur != utilisateur.IdUtilisateur);
                            
                            if (!emailDejaUtilise && !string.IsNullOrWhiteSpace(existing.EmailClient))
                                utilisateur.Email = existing.EmailClient;
                        }
                        if (oldGenreClient != existing.GenreClient)
                            utilisateur.Genre = existing.GenreClient;
                        if (oldAdresseClient != existing.AdresseClient)
                            utilisateur.AdresseResidence = existing.AdresseClient;
                    }

                    if (utilisateursLies.Any())
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation(
                            "✅ Synchronisation Client → Utilisateurs: {Count} utilisateur(s) mis à jour pour le client {ClientId}",
                            utilisateursLies.Count, idClient);
                    }
                }

                // 7. Recharger le client avec ses relations (sans les usages)
                var updatedClient = await _context.Clients
                    .FirstOrDefaultAsync(c => c.IdClient == existing.IdClient);

                _logger.LogInformation(" Client mis à jour avec succès: {IdClient}", idClient);
                return updatedClient ?? existing;
            });
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return false;

            // ✨ NOUVEAU : Soft delete au lieu de hard delete
            client.Statut = false;
            client.IsActif = false;
            client.IsDeleted = true; // ✅ Ajout du soft delete pour sync
            client.UpdatedAt = DateTime.UtcNow; // ✅ Ajout de UpdatedAt pour delta sync
            await _context.SaveChangesAsync();

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Client {IdClient} désactivé (soft delete)", id);
            
            return true;
        }
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Clients.AnyAsync(c => c.IdClient == id);
        }
        public async Task<bool> ToggleStatutAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return false;

            client.Statut = !client.Statut;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleIsActifAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return false;

            client.IsActif = !client.IsActif;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetStatutAsync(int id, bool statut)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return false;

            client.Statut = statut;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<Client>> GetPagedAsync(ClientPagedSearchRequestDto request)
        {
            request ??= new ClientPagedSearchRequestDto();
            NormalizeClientPagedRequest(request);

            var query = BuildClientPagedBaseQuery();
            query = ApplyClientActifFilters(query, request, logPrefix: "GetPagedAsync");
            query = ApplyClientSearchTermFilter(query, request);
            query = ApplyClientSort(query, request);

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Client>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Clients non supprimés (soft delete) et au statut actif — base commune pagination globale / société.
        /// </summary>
        private IQueryable<Client> BuildClientPagedBaseQuery()
        {
            return _context.Clients
                .Where(c => c.Statut == true && (!c.IsDeleted.HasValue || !c.IsDeleted.Value));
        }

        /// <summary>
        /// Clients ayant au moins une réservation non supprimée dans la société (aligné dashboard gérant).
        /// </summary>
        private IQueryable<Client> ApplySocieteReservationScope(IQueryable<Client> query, int idSociete)
        {
            return query.Where(c => _context.Reservations.Any(r =>
                r.IdSociete == idSociete && r.Statut && r.IdClient == c.IdClient));
        }

        private static void NormalizeClientPagedRequest(ClientPagedSearchRequestDto request)
        {
            if (request.PageNumber < 1)
                request.PageNumber = 1;

            if (string.IsNullOrWhiteSpace(request.SortBy))
            {
                request.SortBy = "DateCreation";
                request.SortDescending = true;
            }
        }

        /// <summary>
        /// Le DTO dérive de <see cref="PagedRequest"/> avec un SearchTerm masquant ; le binder peut remplir l'un ou l'autre.
        /// </summary>
        private static string? GetEffectiveSearchTerm(ClientPagedSearchRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                return request.SearchTerm;
            return ((PagedRequest)request).SearchTerm;
        }

        private IQueryable<Client> ApplyClientActifFilters(IQueryable<Client> query, ClientPagedSearchRequestDto request, string logPrefix)
        {
            if (request.HasIsActifFilter)
            {
                _logger.LogInformation("🔍 {Prefix} - Filtre IsActif explicite appliqué: {Value}", logPrefix, request.ActifFilterValue);
                return query.Where(c => c.IsActif == request.ActifFilterValue);
            }

            if (request.IncludeInactive)
            {
                _logger.LogInformation("🔍 {Prefix} - Filtre IncludeInactive (tous IsActif)", logPrefix);
                return query;
            }

            _logger.LogInformation("🔍 {Prefix} - Filtre par défaut (IsActif == true)", logPrefix);
            return query.Where(c => c.IsActif == true);
        }

        private static IQueryable<Client> ApplyClientSearchTermFilter(IQueryable<Client> query, ClientPagedSearchRequestDto request)
        {
            var raw = GetEffectiveSearchTerm(request);
            if (string.IsNullOrWhiteSpace(raw))
                return query;

            var term = raw.Trim().ToLower();
            return query.Where(c =>
                c.NomClient.ToLower().Contains(term) ||
                (c.AdresseClient != null && c.AdresseClient.ToLower().Contains(term)) ||
                (c.Telephone != null && c.Telephone.ToLower().Contains(term)) ||
                (c.EmailClient != null && c.EmailClient.ToLower().Contains(term)) ||
                (c.GenreClient != null && c.GenreClient.ToLower().Contains(term)));
        }

        private static IQueryable<Client> ApplyClientSort(IQueryable<Client> query, ClientPagedSearchRequestDto request)
        {
            return request.SortBy switch
            {
                "NomClient" => request.SortDescending
                    ? query.OrderByDescending(c => c.NomClient).ThenByDescending(c => c.IdClient)
                    : query.OrderBy(c => c.NomClient).ThenByDescending(c => c.IdClient),
                "DateCreation" => request.SortDescending
                    ? query.OrderByDescending(c => c.DateCreation).ThenByDescending(c => c.IdClient)
                    : query.OrderBy(c => c.DateCreation).ThenByDescending(c => c.IdClient),
                "IdClient" => request.SortDescending
                    ? query.OrderByDescending(c => c.IdClient)
                    : query.OrderBy(c => c.IdClient),
                _ => query.OrderByDescending(c => c.DateCreation).ThenByDescending(c => c.IdClient)
            };
        }

        /// <summary>

        /// <summary>
        /// Crée automatiquement un utilisateur Client par défaut lors de la création d'un nouveau client
        /// ✨ RBAC: Attribution automatique du rôle "Client"
        /// </summary>
        private async Task<UtilisateurInfo?> CreateDefaultClientUserAsync(Client client)
        {
            try
            {
                _logger.LogInformation("🔍 CreateDefaultClientUserAsync appelé pour client {ClientId} (Email: {Email}, Nom: {Nom})", 
                    client.IdClient, client.EmailClient, client.NomClient);
                
                // Récupérer le rôle Client
                var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Client");
                if (clientRole == null)
                {
                    _logger.LogError("❌ Rôle 'Client' non trouvé. Les rôles n'ont peut-être pas été initialisés.");
                    throw new InvalidOperationException(
                        $"Le rôle 'Client' n'existe pas. " +
                        $"Assurez-vous que les rôles ont été initialisés via PermissionSeeder."
                    );
                }

                _logger.LogInformation("✅ Rôle Client trouvé: {Role} (ID: {RoleId})", clientRole.Nom, clientRole.IdRole);

                // Récupérer la société par défaut (ou la première disponible)
                var societe = await _context.Societes.FirstOrDefaultAsync();
                if (societe == null)
                {
                    _logger.LogError("❌ Aucune société trouvée. Impossible de créer un utilisateur client.");
                    return null;
                }

                _logger.LogInformation("✅ Société trouvée: {SocieteNom} (ID: {SocieteId})", societe.Nom, societe.IdSociete);

                // ✨ Utiliser l'email du client s'il est fourni, sinon générer un email unique
                // Évite les erreurs de contrainte unique sur email vide
                string email;
                if (string.IsNullOrWhiteSpace(client.EmailClient))
                {
                    // Les fonctionnalités de CodeCons ne sont plus disponibles après la refactorisation
                    // Générer un email unique basé sur IdClient
                    email = $"client_{client.IdClient}_{Guid.NewGuid():N}@congotravel.local";
                    _logger.LogInformation("✅ Email généré automatiquement pour le client {ClientId}: {Email}", client.IdClient, email);
                }
                else
                {
                    email = client.EmailClient.Trim();
                }
                
                string telephone = client.Telephone ?? "";
                
                // ═══════════════════════════════════════════════════════════════════
                // ✅ MULTI-RÔLES : Vérifier si un utilisateur existe déjà par email/téléphone
                // ═══════════════════════════════════════════════════════════════════
                
                Utilisateur? existingUser = null;
                
                // 1. Vérifier si un utilisateur existe déjà pour ce client (par IdClient)
                existingUser = await _context.Utilisateurs
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.IdClient == client.IdClient);
                
                // 2. Si pas trouvé, chercher par email ou téléphone (pour le multi-rôles)
                if (existingUser == null && (!string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(telephone)))
                {
                    existingUser = await _context.Utilisateurs
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => 
                            (!string.IsNullOrWhiteSpace(email) && u.Email == email) ||
                            (!string.IsNullOrWhiteSpace(telephone) && u.Telephone == telephone)
                        );
                }
                
                // 3. Si utilisateur existe, ajouter le rôle Client (multi-rôles)
                if (existingUser != null)
                {
                    _logger.LogInformation("✅ Utilisateur existant trouvé pour le client '{NomClient}' (ID: {UserId}, Email: {Email})", 
                        client.NomClient, existingUser.IdUtilisateur, existingUser.Email);
                    
                    // Recharger les UserRoles
                    await _context.Entry(existingUser)
                        .Collection(u => u.UserRoles)
                        .Query()
                        .Include(ur => ur.Role)
                        .LoadAsync();
                    
                    // Vérifier si l'utilisateur a déjà le rôle Client
                    var hasClientRole = existingUser.UserRoles
                        .Any(ur => ur.Role.Nom == "Client" && ur.Statut == true);
                    
                    if (!hasClientRole)
                    {
                        // Ajouter le rôle Client
                        var newUserRole = new UserRole
                        {
                            IdUtilisateur = existingUser.IdUtilisateur,
                            IdRole = clientRole.IdRole,
                            IsPrimary = false, // Ne pas remplacer le rôle principal existant
                            Statut = true,
                            DateAttribution = DateTime.Now
                        };
                        
                        _context.UserRoles.Add(newUserRole);
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("✅ Rôle 'Client' ajouté avec succès à l'utilisateur {UserId}", 
                            existingUser.IdUtilisateur);
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ L'utilisateur {UserId} a déjà le rôle 'Client'", 
                            existingUser.IdUtilisateur);
                    }
                    
                    // Mettre à jour IdClient si nécessaire
                    if (existingUser.IdClient != client.IdClient)
                    {
                        existingUser.IdClient = client.IdClient;
                        await _context.SaveChangesAsync();
                    }
                    
                    // Retourner les infos de l'utilisateur existant
                    var primaryRole = existingUser.UserRoles
                        .Where(ur => ur.Statut == true && ur.IsPrimary)
                        .Select(ur => ur.Role.Nom)
                        .FirstOrDefault()
                        ?? existingUser.UserRoles
                            .Where(ur => ur.Statut == true)
                            .OrderBy(ur => ur.Role.Niveau ?? 999)
                            .Select(ur => ur.Role.Nom)
                            .FirstOrDefault()
                        ?? "Client";
                    
                    return new UtilisateurInfo
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdAgent = existingUser.IdAgent,
                        Email = existingUser.Email ?? email,
                        DefaultUsername = existingUser.DefaultUsername ?? "",
                        Telephone = existingUser.Telephone ?? telephone,
                        MotDePasseParDefaut = "", // Ne jamais révéler le mot de passe d'un compte existant
                        NomComplet = existingUser.NomComplet ?? client.NomClient,
                        Role = primaryRole
                    };
                }
                
                // Construire le nom complet
                string nomComplet = client.NomClient;
                if (string.IsNullOrWhiteSpace(nomComplet))
                {
                    nomComplet = "Client";
                    _logger.LogWarning("⚠️ Le nom du client est NULL, utilisation de la valeur par défaut 'Client'");
                }
                
                // Les fonctionnalités de CodeCons ne sont plus disponibles après la refactorisation
                // Générer un DefaultUsername basé sur IdClient
                string defaultUsername = $"client_{client.IdClient}_{Guid.NewGuid():N}";
                _logger.LogInformation("✅ Utilisation du DefaultUsername généré: {DefaultUsername}", defaultUsername);
                // Les fonctionnalités de CodeCons ne sont plus disponibles après la refactorisation
                
                // Mot de passe par défaut
                string motDePasseParDefaut = "123456";
                
                // Créer l'utilisateur Client
                var clientUser = new Utilisateur
                {
                    IdClient = client.IdClient,
                    ReferenceUtilisateur = Guid.NewGuid(),
                    NomComplet = nomComplet,
                    Email = email,
                    DefaultUsername = defaultUsername,
                    Telephone = telephone,
                    Genre = client.GenreClient,
                    AdresseResidence = client.AdresseClient,
                    MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut),
                    Statut = true,
                    DateCreation = DateTime.Now,
                    IsConnecte = false,
                    DoitChangerMotDePasse = true,
                    IdSociete = societe.IdSociete
                };

                _logger.LogInformation("🔍 Création de l'utilisateur avec les valeurs: NomComplet={NomComplet}, Email={Email}, IdSociete={SocieteId}, IdClient={ClientId}", 
                    clientUser.NomComplet, clientUser.Email, clientUser.IdSociete, clientUser.IdClient);

                // ✨ Vérifier l'unicité de l'email avant insertion
                // Si l'email existe déjà, générer un email unique avec suffixe
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var emailExists = await _context.Utilisateurs
                        .AnyAsync(u => u.Email == email && u.Statut == true);
                    
                    if (emailExists)
                    {
                        // Générer un email unique en ajoutant un suffixe
                        var baseEmail = email;
                        int suffix = 1;
                        string uniqueEmail;
                        
                        do
                        {
                            // Extraire le nom d'utilisateur et le domaine
                            var atIndex = baseEmail.LastIndexOf('@');
                            if (atIndex > 0)
                            {
                                var username = baseEmail.Substring(0, atIndex);
                                var domain = baseEmail.Substring(atIndex);
                                uniqueEmail = $"{username}_{suffix}{domain}";
                            }
                            else
                            {
                                uniqueEmail = $"{baseEmail}_{suffix}";
                            }
                            
                            var exists = await _context.Utilisateurs
                                .AnyAsync(u => u.Email == uniqueEmail && u.Statut == true);
                            
                            if (!exists)
                                break;
                            
                            suffix++;
                        } while (suffix < 10000); // Limite de sécurité
                        
                        email = uniqueEmail;
                        clientUser.Email = email;
                        _logger.LogInformation("⚠️ Email en conflit, utilisation de l'email unique: {Email}", email);
                    }
                }

                // Validation avant ajout
                try
                {
                    _context.Utilisateurs.Add(clientUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Utilisateur sauvegardé avec succès. IdUtilisateur={UserId}", clientUser.IdUtilisateur);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "❌ ERREUR lors de la sauvegarde de l'utilisateur: {ErrorMessage}", saveEx.Message);
                    throw;
                }
                
                // Créer le UserRole pour le système multi-rôles
                UserRole userRole;
                try
                {
                    userRole = new UserRole
                    {
                        IdUtilisateur = clientUser.IdUtilisateur,
                        IdRole = clientRole.IdRole,
                        IsPrimary = true, // Premier rôle = principal
                        Statut = true,
                        DateAttribution = DateTime.Now
                    };
                    
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ UserRole sauvegardé avec succès. IdUserRole={UserRoleId}", userRole.IdUserRole);
                }
                catch (Exception roleEx)
                {
                    _logger.LogError(roleEx, "❌ ERREUR lors de la sauvegarde du UserRole: {ErrorMessage}", roleEx.Message);
                    throw;
                }
                
                _logger.LogInformation("✅ Utilisateur Client créé avec UserRole (ID: {UserId}, Role: {RoleName})", 
                    clientUser.IdUtilisateur, clientRole.Nom);
                
                // Envoyer l'email de bienvenue (si email fourni)
                if (!string.IsNullOrWhiteSpace(email))
                {
                    string nomSociete = societe.Nom ?? "CongoTravel";
                    
                    // Envoi asynchrone (ne bloque pas si échec)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendWelcomeEmailAsync(
                                email,
                                nomComplet,
                                defaultUsername,
                                telephone,
                                motDePasseParDefaut,
                                clientRole.Nom,
                                nomSociete,
                                client.GenreClient ?? "Masculin"
                            );
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, "⚠️ Échec de l'envoi de l'email à {Email}: {ErrorMessage}", 
                                email, emailEx.Message);
                        }
                    });
                }
                
                // Envoyer le SMS de bienvenue (si téléphone fourni)
                if (!string.IsNullOrWhiteSpace(telephone))
                {
                    string nomSociete = societe.Nom ?? "K-Energie";
                    
                    // Créer le message SMS de bienvenue
                    string messageSms = CreateWelcomeSmsMessage(
                        nomComplet,
                        defaultUsername,
                        motDePasseParDefaut,
                        nomSociete
                    );
                    
                    // Envoi asynchrone (ne bloque pas si échec)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var smsLog = await _smsService.EnvoyerSmsAsync(
                                telephone,
                                messageSms,
                                "BIENVENUE_CLIENT"
                            );
                            
                            if (smsLog != null && (smsLog.Statut == "SENT" || smsLog.Statut == "DELIVERED"))
                            {
                                _logger.LogInformation("✅ SMS de bienvenue envoyé avec succès à {Telephone}", telephone);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ Échec de l'envoi du SMS à {Telephone}: {Statut}", 
                                    telephone, smsLog?.Statut ?? "UNKNOWN");
                            }
                        }
                        catch (Exception smsEx)
                        {
                            _logger.LogWarning(smsEx, "⚠️ Échec de l'envoi du SMS à {Telephone}: {ErrorMessage}", 
                                telephone, smsEx.Message);
                        }
                    });
                }
                
                return new UtilisateurInfo
                {
                    IdUtilisateur = clientUser.IdUtilisateur,
                    IdAgent = null,
                    Email = email,
                    DefaultUsername = defaultUsername,
                    Telephone = telephone,
                    MotDePasseParDefaut = motDePasseParDefaut,
                    NomComplet = nomComplet,
                    Role = clientRole.Nom
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERREUR lors de la création de l'utilisateur client: {ErrorMessage}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Crée le message SMS de bienvenue pour un nouveau client
        /// Format: {nomSociete}: Bienvenue ! Votre compte a été créé. Connectez-vous sur {url}. Vos identifiants ont été envoyés sur votre mail.
        /// Système adaptatif si le message dépasse 160 caractères
        /// </summary>
        private string CreateWelcomeSmsMessage(
            string nomComplet,
            string defaultUsername,
            string motDePasseParDefaut,
            string nomSociete)
        {
            // Format demandé: {nomSociete}: Bienvenue ! Votre compte a été créé. Connectez-vous sur {url}. Vos identifiants ont été envoyés sur votre mail.
            var message = $"{nomSociete}: Bienvenue ! Votre compte a été créé. Connectez-vous sur {_baseUrl}. Vos identifiants ont été envoyés sur votre mail.";
            
            // Si le message dépasse 160 caractères (nom de société trop long), utiliser une version plus courte
            if (message.Length > 160)
            {
                // Version courte sans "Connectez-vous sur"
                message = $"{nomSociete}: Bienvenue ! Votre compte a été créé. {_baseUrl}. Vos identifiants ont été envoyés sur votre mail.";
                
                // Si toujours trop long, version ultra-courte
                if (message.Length > 160)
                {
                    message = $"{nomSociete}: Bienvenue ! Compte créé. Identifiants: email envoyé. {_baseUrl}";
                }
            }
            
            return message;
        }

        /// <summary>
        /// Rechercher des clients par terme
        /// </summary>
        public async Task<IEnumerable<Client>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            return await _context.Clients
                .Where(c => c.Statut == true && (
                    c.NomClient.Contains(searchTerm) ||
                    (c.AdresseClient != null && c.AdresseClient.Contains(searchTerm)) ||
                    (c.EmailClient != null && c.EmailClient.Contains(searchTerm)) ||
                    (c.Telephone != null && c.Telephone.Contains(searchTerm)) ||
                    (c.Ville != null && c.Ville.Contains(searchTerm)) ||
                    (c.Province != null && c.Province.Contains(searchTerm))))
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Vérifier si un email existe déjà
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var query = _context.Clients.Where(c => c.EmailClient == email);
            
            if (excludeId.HasValue)
                query = query.Where(c => c.IdClient != excludeId.Value);

            return await query.AnyAsync();
        }

        /// <summary>
        /// Récupérer un client par son email
        /// </summary>
        public async Task<Client?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _context.Clients
                .FirstOrDefaultAsync(c => c.EmailClient == email);
        }

        /// <summary>
        /// Récupérer le nombre total de clients
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Clients
                .Where(c => c.Statut == true)
                .CountAsync();
        }

        // Les fonctionnalités d'usage ne sont plus disponibles après la refactorisation
        // Méthodes obsolètes supprimées pour éviter les erreurs de compilation

        private static string? NormalizeAdresseClient(string? adresse) =>
            string.IsNullOrWhiteSpace(adresse) ? null : adresse.Trim();
    }
}

