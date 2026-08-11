# Déploiement Site Touristique billetterie V1

## Prérequis

- Tables `Societes`, `Sites`, `ConfigSocietes` déjà présentes
- Module Evenement **non requis** (vertical autonome)

## Ordre recommandé

1. `production_site_touristique_ticketing_v1.sql` — tables + `DureeHoldSiteTouristiqueMinutes`
2. `production_site_touristique_planification_v1.sql` — templates planification + FK `IdSiteTouristiquePlanification` sur journées
3. `production_site_touristique_hold_expiration_procedure_only.sql` — procédure `sp_ExpireSiteTouristiqueHolds`
4. (Optionnel) `production_site_touristique_hold_expiration_job.sql` — event scheduler MariaDB
5. **`assign_site_touristique_permissions_admin_gerant.sql`** — permissions + grants (**obligatoire** sur DB existante)

Sans l’étape 5, un compte **Admin** ou **Gerant** déjà créé obtient **403** sur  
`POST /api/sites-touristiques/lieux` (`SiteTouristique.Lieu.Write`).  
Sans les grants Client / Caissier du même script : **403** sur vente ou gate.

Le hosted service .NET appelle aussi `CALL sp_ExpireSiteTouristiqueHolds()` périodiquement.

## Permissions / 403 Admin, Gerant, Caissier, Client

| Symptôme | Cause | Correctif |
|----------|--------|-----------|
| `403` sur création lieu / journée / publish | Rôle `Admin` ou `Gerant` sans grants `SiteTouristique.*` Write | Exécuter `assign_site_touristique_permissions_admin_gerant.sql` |
| `403` sur vente CASH / FlexPay (Client ou Caissier) | Manque `Hold.Create` + `Reservation.Confirm` | Même script assign ST |
| `403` sur gate ticket check/use (Caissier) | Manque `Ticket.Check` / `Ticket.Use` | Même script assign ST (bloc Caissier) |
| Toujours 403 après script | `UserRoles` inactif ou rôle mal orthographié (`Gerant`, `Caissier`, `Client`) | Vérifier avec le diagnostic ci-dessous |

Diagnostic (ST + Restaurant) : `diagnostic_permissions_site_touristique_restaurant.sql`

Le seeder runtime (`PermissionSeeder`) assigne aussi ces permissions au démarrage de l’API, mais **sur une DB déjà peuplée** exécuter le script SQL reste la voie sûre et idempotente. **Pas besoin de regenerer le JWT** après grant.

## Routes API

Préfixe : `/api/sites-touristiques/`

| Ressource | Chemin |
|-----------|--------|
| Lieux | `/lieux` |
| Journées | `/journees` |
| Planifications | `/planifications` (`POST {id}/generer`) |
| Classes | `/classes` |
| Réservations | `/reservations` (`with-paiement`, `with-paiement-electronique`) |
| Tickets | `/tickets` |
| FlexPay | `/flexpay` |
| Dashboard | `/dashboard` |

## Modes inventaire V1

- **GlobalQuota** (Mode C)
- **ClassQuota** (Mode B)

Pas de SeatNumbered.

## Notes

- Produit = `SiteTouristique` (entité `SiteTouristiqueLieu`) ; `Site` reste le guichet marchand
- Entrée ticket : jour calendaire UTC = `DateVisite`
- Hold : `ConfigSocietes.DureeHoldSiteTouristiqueMinutes` (défaut 15, clamp 1–120)
