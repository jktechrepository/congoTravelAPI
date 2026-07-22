/*
 * ========================================================================
 * EXEMPLE D'ENDPOINTS POUR GESTION DU STATUT (SOFT DELETE)
 * ========================================================================
 * 
 * À AJOUTER dans chaque contrôleur qui gère une entité avec le champ Statut
 * 
 * Liste des contrôleurs à modifier :
 * 1. SocieteController
 * 2. DirectionController  
 * 3. ClasseController
 * 4. SectionController
 * 5. OptionController
 * 6. AnneeScolaireController
 * 7. RoleController
 * 8. VacationController
 * 9. CoursController
 * 10. NoteController
 * 11. EvaluationController
 * 12. HoraireController (si existe)
 * 13. MessageController
 * 14. GroupeMessageController
 * 15. DocumentController
 * 16. RessourcePedagogiqueController
 * 17. InscriptionController
 * 18. FraisController (déjà avec Statut, ajouter endpoints si manquants)
 * 
 * ========================================================================
 */

/*
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    // ========================================================================
    // EXEMPLE 1 : DÉSACTIVER (SOFT DELETE)
    // ========================================================================
    
    /// <summary>
    /// Désactive une entité (soft delete) en mettant Statut à false
    /// </summary>
    /// <param name="id">ID de l'entité</param>
    /// <returns>NoContent si succès, NotFound si entité introuvable</returns>
    [HttpPut("{id}/desactiver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Desactiver(int id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            
            if (entity == null)
            {
                return NotFound(new { message = $"Entité avec ID {id} non trouvée" });
            }
            
            // Désactiver l'entité
            entity.Statut = false;
            await _repository.UpdateAsync(entity);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erreur lors de la désactivation", error = ex.Message });
        }
    }

    // ========================================================================
    // EXEMPLE 2 : ACTIVER (RESTAURER)
    // ========================================================================
    
    /// <summary>
    /// Active/Réactive une entité en mettant Statut à true
    /// </summary>
    /// <param name="id">ID de l'entité</param>
    /// <returns>NoContent si succès, NotFound si entité introuvable</returns>
    [HttpPut("{id}/activer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Activer(int id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            
            if (entity == null)
            {
                return NotFound(new { message = $"Entité avec ID {id} non trouvée" });
            }
            
            // Activer l'entité
            entity.Statut = true;
            await _repository.UpdateAsync(entity);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erreur lors de l'activation", error = ex.Message });
        }
    }

    // ========================================================================
    // EXEMPLE 3 : BASCULER STATUT (TOGGLE)
    // ========================================================================
    
    /// <summary>
    /// Bascule le statut d'une entité (true → false ou false → true)
    /// </summary>
    /// <param name="id">ID de l'entité</param>
    /// <returns>OK avec nouveau statut si succès, NotFound si entité introuvable</returns>
    [HttpPut("{id}/basculer-statut")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BasculerStatut(int id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            
            if (entity == null)
            {
                return NotFound(new { message = $"Entité avec ID {id} non trouvée" });
            }
            
            // Basculer le statut
            entity.Statut = !entity.Statut;
            await _repository.UpdateAsync(entity);
            
            return Ok(new 
            { 
                message = "Statut basculé avec succès",
                id = id,
                nouveauStatut = entity.Statut
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erreur lors du basculement du statut", error = ex.Message });
        }
    }

    // ========================================================================
    // EXEMPLE 4 : MODIFICATION DE GetAll POUR FILTRER PAR STATUT
    // ========================================================================
    
    /// <summary>
    /// Récupère toutes les entités (par défaut, seulement celles actives)
    /// </summary>
    /// <param name="includeInactive">Si true, inclut les entités inactives</param>
    /// <returns>Liste des entités</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Entity>>> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            IEnumerable<Entity> entities;
            
            if (includeInactive)
            {
                // Récupérer TOUTES les entités (actives + inactives)
                entities = await _repository.GetAllIncludingInactiveAsync();
            }
            else
            {
                // Récupérer UNIQUEMENT les entités actives (comportement par défaut)
                entities = await _repository.GetAllAsync();
            }
            
            return Ok(entities);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erreur lors de la récupération", error = ex.Message });
        }
    }

    // ========================================================================
    // EXEMPLE 5 : OBTENIR UNIQUEMENT LES ENTITÉS INACTIVES
    // ========================================================================
    
    /// <summary>
    /// Récupère uniquement les entités désactivées
    /// </summary>
    /// <returns>Liste des entités inactives</returns>
    [HttpGet("inactifs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Entity>>> GetInactifs()
    {
        try
        {
            var entities = await _repository.GetInactiveAsync();
            return Ok(entities);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erreur lors de la récupération des inactifs", error = ex.Message });
        }
    }

    // ========================================================================
    // EXEMPLE 6 : SUPPRIMER DÉFINITIVEMENT (HARD DELETE) - OPTIONNEL
    // ========================================================================
    
    /// <summary>
    /// ⚠️ SUPPRESSION DÉFINITIVE - Ne peut pas être annulée !
    /// À utiliser avec EXTRÊME PRÉCAUTION
    /// </summary>
    /// <param name="id">ID de l'entité</param>
    /// <returns>NoContent si succès, NotFound si entité introuvable</returns>
    [HttpDelete("{id}/supprimer-definitivement")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SupprimerDefinitivement(int id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            
            if (entity == null)
            {
                return NotFound(new { message = $"Entité avec ID {id} non trouvée" });
            }
            
            // ⚠️ Suppression PHYSIQUE de la base de données
            await _repository.DeleteAsync(id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erreur lors de la suppression définitive", error = ex.Message });
        }
    }
}
*/

// ========================================================================
// EXEMPLE DE MODIFICATIONS DANS LES REPOSITORIES/SERVICES
// ========================================================================

/*
 * À ajouter dans chaque Repository/Service :
 */

/*
// Interface
public interface IEntityRepository
{
    // Existant
    Task<IEnumerable<Entity>> GetAllAsync();
    Task<Entity> GetByIdAsync(int id);
    Task<Entity> CreateAsync(Entity entity);
    Task<Entity> UpdateAsync(Entity entity);
    Task<bool> DeleteAsync(int id);
    
    // ✨ NOUVELLES MÉTHODES À AJOUTER
    Task<IEnumerable<Entity>> GetAllIncludingInactiveAsync();
    Task<IEnumerable<Entity>> GetInactiveAsync();
}

// Implémentation dans le Service
public class EntityService : IEntityRepository
{
    private readonly CongoTravelDbContext _context;
    
    public EntityService(CongoTravelDbContext context)
    {
        _context = context;
    }
    
    // Méthode GetAllAsync MODIFIÉE - Filtre par défaut sur Statut = true
    public async Task<IEnumerable<Entity>> GetAllAsync()
    {
        return await _context.Entities
            .Where(e => e.Statut == true)  // ✨ FILTRE AJOUTÉ
            .ToListAsync();
    }
    
    // ✨ NOUVELLE MÉTHODE - Récupère TOUT (actifs + inactifs)
    public async Task<IEnumerable<Entity>> GetAllIncludingInactiveAsync()
    {
        return await _context.Entities.ToListAsync();
    }
    
    // ✨ NOUVELLE MÉTHODE - Récupère uniquement les inactifs
    public async Task<IEnumerable<Entity>> GetInactiveAsync()
    {
        return await _context.Entities
            .Where(e => e.Statut == false)
            .ToListAsync();
    }
    
    // Les autres méthodes restent inchangées...
}
*/

// ========================================================================
// EXEMPLES D'UTILISATION DES ENDPOINTS
// ========================================================================

/*
 * 1. DÉSACTIVER UNE ÉCOLE :
 * PUT /api/Societe/5/desactiver
 * 
 * 2. ACTIVER UNE CLASSE :
 * PUT /api/Classe/12/activer
 * 
 * 3. BASCULER LE STATUT D'UN COURS :
 * PUT /api/Cours/8/basculer-statut
 * 
 * 4. RÉCUPÉRER TOUTES LES ÉCOLES (actives seulement) :
 * GET /api/Societe
 * 
 * 5. RÉCUPÉRER TOUTES LES ÉCOLES (actives + inactives) :
 * GET /api/Societe?includeInactive=true
 * 
 * 6. RÉCUPÉRER UNIQUEMENT LES ÉCOLES INACTIVES :
 * GET /api/Societe/inactifs
 * 
 * 7. SUPPRIMER DÉFINITIVEMENT (À UTILISER AVEC PRÉCAUTION) :
 * DELETE /api/Societe/5/supprimer-definitivement
 */

