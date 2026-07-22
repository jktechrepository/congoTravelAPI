using CongoTravel.Services;
using CongoTravel.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CongoTravel
{
    public static class ApplicationExtensions
    {
        /// <summary>
        /// Initialise les données de base de l'application
        /// </summary>
        public static async Task InitializeApplicationDataAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CongoTravelDbContext>();
          //  var typeDeCourantDataService = scope.ServiceProvider.GetRequiredService<TypeDeCourantDataService>();

            try
            {
                // Initialiser les types de courant par défaut
               // await typeDeCourantDataService.InitializeDefaultTypesAsync();
                
                // Valider et réparer les données si nécessaire
              //  await typeDeCourantDataService.ValidateAndRepairDataAsync();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Erreur lors de l'initialisation des données de l'application");
                throw;
            }
        }
    }
}
