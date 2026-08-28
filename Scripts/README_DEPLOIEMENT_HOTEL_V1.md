# Déploiement Hôtel V1 — Phases 1 à 5 (+ 7a–7e)

Prérequis : schéma CongoTravel existant avec `Societes`, `Sites`, `ConfigSocietes`, `Roles` et `Permissions`.

1. Exécuter `production_hotel_v1.sql` (catalogue hôtel, room-types, photos, `DureeHoldHotelMinutes`).
2. Exécuter `production_hotel_phase2_allotments.sql` (table `HotelNightAllotments`).
3. Exécuter `production_hotel_phase3_reservations.sql` (réservations, lignes, paiements CASH).
4. Exécuter `production_hotel_phase4_flexpay.sql` (commandes Plan A + paiements FlexPay).
5. Exécuter `production_hotel_hold_expiration_procedure_only.sql` (`sp_ExpireHotelHolds`).
6. Exécuter `assign_hotel_permissions_admin_gerant.sql`.
7. **Phase 7a** : exécuter `production_hotel_phase7a_planification.sql` (templates + lignes + logs + FK allotments).
8. **Phase 7b** : exécuter `production_hotel_phase7b_global_quota.sql` (`HotelNights`, planif Global, InventoryMode).
9. **Phase 7c** : exécuter `production_hotel_phase7c_rooms.sql` (`HotelRooms`, `HotelRoomAssignments`).
10. **Phase 7d** : exécuter `production_hotel_phase7d_checkin.sql` (`CheckedInAtUtc`, `CheckedOutAtUtc`).
11. **Phase 7e** : exécuter `production_hotel_phase7e_extras.sql` (`HotelExtras`, `HotelReservationExtras`).
12. Configurer `FlexPay:HotelEnabled=true` et `FlexPay:HotelCallbackRelativePath=/api/hotels/flexpay/callback`, puis redémarrer l’API.

Les scripts sont idempotents.

**Phase 1** : établissements, types de chambres, photos, publication.  
**Phase 2** : allotments nuit × type (Draft/publish), batch plage dates, `GET /api/hotels/availability`.
**Phase 3** : hold ClassQuota multi-nuit, acompte CASH, confirmation, annulation, expiration et lectures réservations.
**Phase 4** : FlexPay Plan A (aucune réservation avant succès), callback/verifier/abandon, SignalR avec `domain: hotel` et expiration des commandes.
**Phase 5** : lectures Client tenantées (dont `GET /api/hotels/reservations/client/{idClient}`), dashboard société/super-admin/widget et permission `Hotel.Dashboard.Read`.
**Phase 7a** : planifications templates (`/api/hotels/planifications`) + `POST {id}/generer` → allotments Draft (option publish). Permissions : `Hotel.Etablissement.Read` / `.Write` (aucun nouveau code).
**Phase 7b** : GlobalQuota exclusif — `HotelNight` + `/api/hotels/nights`, factory strategies, planif Global, availability/achat sans `roomTypeId`.
**Phase 7c** : catalogue `HotelRoom` + `/api/hotels/rooms` + `assign-rooms` post-confirm (pas SeatNumbered inventaire).
**Phase 7d** : check-in / check-out réception — `POST|PUT …/reservations/{id}/check-in` et `…/check-out` (timestamps, pas nouveau statut).
**Phase 7e** : catalogue extras `/api/hotels/extras` + lignes réception `…/extras` post-confirm (`PerStay` / `PerNight`, `montantExtras` informatif).

Hors périmètre proche : tickets QR, SeatNumbered-at-booking, hybride Class+Global, paiement FlexPay extras.
