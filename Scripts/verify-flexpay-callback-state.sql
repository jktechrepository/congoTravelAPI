-- Vérification manuelle après test FlexPay (initiation + callback)
-- Remplacer @OrderNumber par la valeur retournée par reservation_with_paiement_electronique

SET @OrderNumber = 'FP-TEST-001';

-- 1) Transaction FlexPay
SELECT IdTransaction, OrderNumber, StatutPaiement, StatusFlexPay, IdReservation, IdPaiement, NombreCallbacks
FROM TransactionsFlexPay
WHERE OrderNumber = @OrderNumber;

-- 2) Commande en attente (doit être ABSENTE après callback succès)
SELECT *
FROM CommandesReservationEnAttente
WHERE OrderNumberFlexPay = @OrderNumber;

-- 3) Holds sièges (doit être VIDE après callback succès ou échec)
SELECT h.*
FROM SiegeHoldsEnAttente h
JOIN CommandesReservationEnAttente c ON c.IdCommandeReservationEnAttente = h.IdCommandeReservationEnAttente
WHERE c.OrderNumberFlexPay = @OrderNumber;

-- 4) Paiement (IdReservation renseigné + Statut=1 après succès)
SELECT p.IdPaiement, p.IdReservation, p.Statut, p.StatutPaiementMetier, p.ReferenceTransaction, p.MontantPaye
FROM Paiements p
WHERE p.ReferenceTransaction = @OrderNumber;

-- 5) Audit callbacks
SELECT IdCallback, OrderNumber, Code, TraiteAvecSucces, DateReception, MessageErreur
FROM CallbacksFlexPay
WHERE OrderNumber = @OrderNumber
ORDER BY DateReception DESC;

-- 6) Réservation créée (via paiement)
SELECT r.*
FROM Reservations r
JOIN Paiements p ON p.IdReservation = r.IdReservation
WHERE p.ReferenceTransaction = @OrderNumber;
