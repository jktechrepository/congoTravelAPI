using CongoTravel.Models.DTOs.VoyageTarification;

namespace CongoTravel.Models.DTOs
{
    public class VoyageResponseDto
    {
        public int Id { get; set; }
        public DateTime DateDepart { get; set; }
        public TimeSpan HeureDepart { get; set; }
        public int Prix { get; set; }
        public string CodeDevisePrix { get; set; } = "CDF";
        public string CodeDevisePrincipale { get; set; } = "CDF";
        public decimal TauxVersDevisePrincipale { get; set; }
        public decimal PrixDevisePrincipale { get; set; }
        public int IdVehicule { get; set; }
        /// <summary>Miroir legacy : première étape du trajet (<c>Ordre</c> minimal).</summary>
        public int IdDestination { get; set; }
        public int IdSociete { get; set; }
        public int? IdSite { get; set; }
        public bool Statut { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }

        /// <summary>Étapes ordonnées si chargées (ex. GET par id).</summary>
        public List<VoyageEtapeReadDto>? EtapesDestinations { get; set; }

        /// <summary>Tarifs par catégorie de siège pour ce voyage.</summary>
        public List<VoyageTarifCategorieSiegeResponseItemDto>? Tarifs { get; set; }

        /// <summary>
        /// Sièges encore libres sur ce voyage, regroupés par catégorie (compteur uniquement).
        /// Renseigné sur toutes les réponses <see cref="VoyageResponseDto"/> (listes, détail, création, mise à jour).
        /// </summary>
        public List<VoyageCategorieSiegeDisponiblesSummaryDto> RepartitionCategorieSiegesDisponible { get; set; } = new();

        /// <summary>Photos du véhicule affecté à ce voyage (actives, triées par <c>ordre</c>).</summary>
        public List<PhotoVehiculeDto> PhotosVehicules { get; set; } = new();

        // Navigation properties optionnelles
        public string? AliasVehicule { get; set; }
        public string? LibelleTypeVehicule { get; set; }
        public string? NomSociete { get; set; }
        public string? NomSite { get; set; }
        public string? VilleDepart { get; set; }
        public string? VilleArrivee { get; set; }

        /// <summary>Snapshot ConfigSociete — supplément par place pour paiement électronique (0 = aucun).</summary>
        public decimal MontAddPaieElectronique { get; set; }

        /// <summary>Devise du supplément électronique ; null = devise du voyage.</summary>
        public string? CodeDeviseMontAddPaieElectronique { get; set; }
    }
}
