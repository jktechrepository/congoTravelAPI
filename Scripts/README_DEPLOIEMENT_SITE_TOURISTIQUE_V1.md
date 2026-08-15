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
6. (DB déjà en prod / UAT) **`add_site_touristique_reservation_id_client.sql`** — colonne `IdClient` sur `SiteTouristiqueReservations` (no-op si table absente)

### Schéma EF vs scripts `production_*`

Le schéma ST est désormais aussi dans les migrations EF (`AddSiteTouristiqueAndRestaurantSchema` → inclus dans `deployProduction.sql`). Deux voies possibles :

| Situation | Action |
|-----------|--------|
| DB neuve / from-scratch | `deployProduction.sql` (ou `dotnet ef database update`) crée les tables ST + `IdClient` |
| DB déjà créée via `production_site_touristique_*.sql` | **Ne pas** rejouer les `CREATE` EF. Insérer la migration dans `__EFMigrationsHistory` (voir UAT ci-dessous) |
| DB sans tables ST | Appliquer la migration EF **incrémentale** depuis la dernière migration déjà appliquée — **pas** tout le script from-scratch sur une DB peuplée |
| Colonne `IdClient` seule manquante | `add_site_touristique_reservation_id_client.sql` (no-op si table absente) |

### Important — UAT / prod

- Si `__EFMigrationsHistory` contient `20260811111200_AddSiteTouristiqueAndRestaurantReservationIdClient` (migration ALTER retirée) : **supprimer** cette ligne.
- Tables déjà là via `production_*` : marquer la migration EF comme appliquée sans exécuter les `CREATE` :

```sql
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260811142803_AddSiteTouristiqueAndRestaurantSchema', '6.0.25');
```

- Pour Événement seul : `add_evenement_reservation_id_client.sql` (indépendant de ST).

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
