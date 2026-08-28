using System.Globalization;
using System.Text;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Reservation;

namespace CongoTravel.Helpers.Transport
{
    /// <summary>
    /// Règles de compatibilité aller-retour Transport V1.
    /// </summary>
    public static class AllerRetourVoyageCompatibilityHelper
    {
        public static void EnsureCompatible(Voyage voyageAller, Voyage voyageRetour, Destination? destAller, Destination? destRetour)
        {
            if (voyageAller.Id == voyageRetour.Id)
                throw new InvalidOperationException("Les voyages aller et retour doivent être distincts.");

            if (voyageAller.IdSociete != voyageRetour.IdSociete)
                throw new InvalidOperationException(
                    $"Les voyages aller et retour doivent appartenir à la même société ({voyageAller.IdSociete} ≠ {voyageRetour.IdSociete}).");

            if (destAller == null || destRetour == null)
                throw new InvalidOperationException("Destination introuvable pour l'un des voyages aller/retour.");

            var allerDepart = NormalizeCity(destAller.VilleDepart);
            var allerArrivee = NormalizeCity(destAller.VilleArrivee);
            var retourDepart = NormalizeCity(destRetour.VilleDepart);
            var retourArrivee = NormalizeCity(destRetour.VilleArrivee);

            if (allerArrivee != retourDepart || allerDepart != retourArrivee)
            {
                throw new InvalidOperationException(
                    "Incompatibilité géographique aller-retour : " +
                    $"aller {destAller.VilleDepart}→{destAller.VilleArrivee}, " +
                    $"retour {destRetour.VilleDepart}→{destRetour.VilleArrivee}.");
            }

            var instantAller = CombineDepart(voyageAller);
            var instantRetour = CombineDepart(voyageRetour);
            if (instantRetour < instantAller)
            {
                throw new InvalidOperationException(
                    "Le départ du voyage retour doit être postérieur ou égal au départ aller.");
            }
        }

        public static void EnsureSamePassengers(IReadOnlyList<ReservationPassengerInputDto> passagers, int nombreDePlace)
        {
            if (passagers == null || passagers.Count == 0)
                throw new InvalidOperationException("Les passagers sont requis pour un aller-retour.");

            if (passagers.Count != nombreDePlace)
                throw new InvalidOperationException(
                    "Le nombre d'entrées dans Passagers doit être égal à NombreDePlace.");

            foreach (var p in passagers)
            {
                if (string.IsNullOrWhiteSpace(p.NomComplet))
                    throw new InvalidOperationException("Chaque passager doit avoir un nom complet.");
                if (p.IdCategorieSiege <= 0)
                    throw new InvalidOperationException("Chaque passager doit avoir une catégorie de siège valide.");
            }
        }

        /// <summary>Duplique la liste passagers pour le second leg (même identité).</summary>
        public static List<ReservationPassengerInputDto> ClonePassagers(IReadOnlyList<ReservationPassengerInputDto> source)
        {
            return source.Select(p => new ReservationPassengerInputDto
            {
                IdClient = p.IdClient,
                IdCategorieSiege = p.IdCategorieSiege,
                NomComplet = p.NomComplet,
                Telephone = p.Telephone,
                Email = p.Email,
                DocumentType = p.DocumentType,
                DocumentNumero = p.DocumentNumero,
                Genre = p.Genre
            }).ToList();
        }

        public static DateTime CombineDepart(Voyage voyage)
        {
            var date = voyage.DateDepart.Date;
            return date.Add(voyage.HeureDepart);
        }

        private static string NormalizeCity(string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return string.Empty;

            return city.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormC);
        }
    }
}
