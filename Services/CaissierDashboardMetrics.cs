using CongoTravel.Models;

namespace CongoTravel.Services
{
    /// <summary>
    /// Calculs métier pour le dashboard caissier (testables sans DbContext).
    /// </summary>
    public static class CaissierDashboardMetrics
    {
        /// <summary>
        /// Nombre de personnes transportées liées aux paiements du jour :
        /// somme des <see cref="ReservationPassenger"/> actifs par réservation payée,
        /// ou <see cref="Reservation.NombreDePlace"/> (min. 1) pour les réservations sans lignes passagers (legacy).
        /// </summary>
        public static int CountPassagersFromPaidReservations(
            IReadOnlyList<int> reservationIdsPaidToday,
            IReadOnlyList<ReservationPassenger> passengersForThoseReservations,
            IReadOnlyList<Reservation> reservationsWithoutPassengerRows)
        {
            if (reservationIdsPaidToday.Count == 0)
                return 0;

            var paidSet = reservationIdsPaidToday.Distinct().ToHashSet();
            var passengersByReservation = passengersForThoseReservations
                .Where(p => p.Statut && paidSet.Contains(p.IdReservation))
                .GroupBy(p => p.IdReservation)
                .ToDictionary(g => g.Key, g => g.Count());

            var total = passengersByReservation.Values.Sum();

            foreach (var reservation in reservationsWithoutPassengerRows)
            {
                if (!paidSet.Contains(reservation.IdReservation))
                    continue;
                if (passengersByReservation.ContainsKey(reservation.IdReservation))
                    continue;

                total += reservation.NombreDePlace > 0 ? reservation.NombreDePlace : 1;
            }

            return total;
        }
    }
}
