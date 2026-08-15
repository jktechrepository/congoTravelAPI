# Déploiement Restaurant réservation V1

## Prérequis

- Tables `Societes`, `Sites`, `ConfigSocietes` déjà présentes
- Modules Evenement / Site Touristique **non requis** (vertical autonome)

## Ordre recommandé

1. `production_restaurant_v1.sql` — établissements, créneaux, GlobalQuota, `DureeHoldRestaurantMinutes`
2. `production_restaurant_phase2_reservations.sql` — réservations, lignes, paiements acompte
3. `production_restaurant_hold_expiration_procedure_only.sql` — procédure `sp_ExpireRestaurantHolds`
4. `production_restaurant_phase4_zones.sql` — zones + ZoneQuota (Mode B ClassQuota)
5. **`production_restaurant_planification_v1.sql`** — templates multi-plages + FK optionnelles sur `RestaurantCreneaux`
6. **`assign_restaurant_permissions_admin_gerant.sql`** — permissions + grants (**obligatoire** sur DB existante)
7. (DB déjà en prod / UAT) **`add_restaurant_reservation_id_client.sql`** — colonne `IdClient` sur `RestaurantReservations` (no-op si table absente)

### Schéma EF vs scripts `production_*`

Le schéma Restaurant est désormais aussi dans les migrations EF (`AddSiteTouristiqueAndRestaurantSchema` → inclus dans `deployProduction.sql`). Deux voies possibles :

| Situation | Action |
|-----------|--------|
| DB neuve / from-scratch | `deployProduction.sql` (ou `dotnet ef database update`) crée les tables Restaurant + `IdClient` |
| DB déjà créée via `production_restaurant_*.sql` | **Ne pas** rejouer les `CREATE` EF. Insérer la migration dans `__EFMigrationsHistory` (voir UAT ci-dessous) |
| DB sans tables Restaurant | Appliquer la migration EF **incrémentale** depuis la dernière migration déjà appliquée — **pas** tout le script from-scratch sur une DB peuplée |
| Colonne `IdClient` seule manquante | `add_restaurant_reservation_id_client.sql` (no-op si table absente) |

### Important — UAT / prod

- Si `__EFMigrationsHistory` contient `20260811111200_AddSiteTouristiqueAndRestaurantReservationIdClient` (migration ALTER retirée) : **supprimer** cette ligne.
- Tables déjà là via `production_*` : marquer la migration EF comme appliquée sans exécuter les `CREATE` :

```sql
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260811142803_AddSiteTouristiqueAndRestaurantSchema', '6.0.25');
```

- Pour Événement seul : `add_evenement_reservation_id_client.sql` (indépendant de Restaurant).

Sans l’étape 6, un compte **Admin** ou **Gerant** déjà créé obtient **403** sur  
`POST /api/restaurants/etablissements` (`Restaurant.Etablissement.Write`).  
Sans les grants Client / Caissier du même script : **403** sur vente d’acompte.

Le hosted service .NET appelle aussi `CALL sp_ExpireRestaurantHolds()` périodiquement.

## Permissions / 403 Admin, Gerant, Caissier, Client

| Symptôme | Cause | Correctif |
|----------|--------|-----------|
| `403` sur création établissement / créneau | Rôle `Admin` ou `Gerant` sans grants `Restaurant.*` Write | Exécuter `assign_restaurant_permissions_admin_gerant.sql` |
| `403` sur acompte CASH / FlexPay (Client ou Caissier) | Manque `Hold.Create` + `Reservation.Confirm` | Même script assign Restaurant |
| Toujours 403 après script | `UserRoles` inactif ou rôle mal orthographié (`Gerant`, `Caissier`, `Client`) | Vérifier avec le diagnostic ci-dessous |

Diagnostic : `diagnostic_permissions_site_touristique_restaurant.sql`

Le seeder runtime (`PermissionSeeder`) assigne aussi ces permissions au démarrage de l’API, mais **sur une DB déjà peuplée** exécuter le script SQL reste la voie sûre et idempotente. **Pas besoin de regenerer le JWT** après grant.

## Routes API

Préfixe : `/api/restaurants/`

| Ressource | Chemin |
|-----------|--------|
| Établissements | `/etablissements` |
| Zones | `/zones` |
| Créneaux | `/creneaux` (+ `/availability`) |
| Planifications | `/planifications` (+ `/{id}/generer`) |
| Réservations | `/reservations` (`with-paiement`, `with-paiement-electronique`) |
| FlexPay | `/flexpay` |
| Dashboard | `/dashboard` (+ `/widget`, `/super-admin`) |

## Modes inventaire V1

- **GlobalQuota** (Mode C) — couverts globaux
- **ClassQuota** (Mode B) — quotas par zone

Pas de tables numérotées / gate QR en V1 (Phase 6+).

## Notes

- Produit = `Restaurant` (établissement) ; `Site` reste le guichet marchand FlexPay / caisse
- Vente V1 = **acompte** (CASH / FlexPay), pas l’addition repas
- Hold : `ConfigSocietes.DureeHoldRestaurantMinutes` (défaut 15, clamp 1–120)
- Front : [`MODULE_11_RESTAURANT.md`](../Documentation/Themes/09_frontend_integration/MODULE_11_RESTAURANT.md)
- Workflow : [`DOCUMENTATION_WORKFLOW_RESTAURANT_V1.md`](../Documentation/Themes/05_transport_sync/DOCUMENTATION_WORKFLOW_RESTAURANT_V1.md)
