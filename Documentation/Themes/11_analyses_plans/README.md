# Theme 11 - Analyses et Plans

## A lire d'abord

- `ANALYSE_COMPLETE_CODE_EXISTANT.md`
- `ANALYSE_EXPERT_SYSTEME_KENERGIE.md`

## Ensuite

- Les fichiers `ANALYSE_*`, `ANALISE_*`, `PLAN_*`, `RECAPITULATIF_*`, `FIX_*`, `QUESTIONS_*` presents dans ce dossier.
- `ANALYSE_V1_BILLETTERIE_3_MODES_INVENTAIRE.md` pour le cadrage evenementiel (SeatNumbered, ClassQuota, GlobalQuota) — affinage V1 + **convention de nommage préfixe `Evenement*`** (section 12).
- `ANALYSE_V1_SITE_TOURISTIQUE.md` pour la **Partie 3** (lieu + journée, GlobalQuota puis ClassQuota, préfixe `SiteTouristique*`, anti-collision avec `Site` guichet).
- `ANALYSE_V1_RESTAURANT.md` pour la **Partie 4** (établissement + créneau, couverts GlobalQuota puis zones, acompte CASH/FlexPay, préfixe `Restaurant*`).
- Scripts SQL associes : `Scripts/production_evenement_ticketing_v1.sql`, `Scripts/production_site_touristique_ticketing_v1.sql`, `Scripts/production_restaurant_v1.sql`, `Scripts/production_restaurant_phase2_reservations.sql`, `Scripts/production_restaurant_phase4_zones.sql`, `Scripts/production_restaurant_hold_expiration_procedure_only.sql` (+ `Scripts/assign_restaurant_permissions_admin_gerant.sql`).

## Prochaine etape

- Restaurant **Phase 6 planif multi-plages livrée** (`/api/restaurants/planifications`) ; reste Phase 6+ : tables numérotées, check-in.
- En parallèle : déployer SQL Site Touristique / Restaurant + front `MODULE_10` / `MODULE_11` si pas encore fait.

## Objectif

Conserver l'historique des analyses, plans d'action et hypotheses produit/technique en un seul endroit.
