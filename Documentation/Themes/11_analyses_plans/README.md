# Theme 11 - Analyses et Plans

## A lire d'abord

- `ANALYSE_COMPLETE_CODE_EXISTANT.md`
- `ANALYSE_EXPERT_SYSTEME_KENERGIE.md`
- `ADR_MICROSERVICES_PAR_DOMAINE.md` — avis architecture : monolithe modulaire vs microservices (Transport / Événement / Site / Restaurant / Hôtel)

## Ensuite

- Les fichiers `ANALYSE_*`, `ANALISE_*`, `PLAN_*`, `ADR_*`, `RECAPITULATIF_*`, `FIX_*`, `QUESTIONS_*` presents dans ce dossier.
- `ANALYSE_V1_BILLETTERIE_3_MODES_INVENTAIRE.md` pour le cadrage evenementiel (SeatNumbered, ClassQuota, GlobalQuota) — affinage V1 + **convention de nommage préfixe `Evenement*`** (section 12).
- `ANALYSE_V1_SITE_TOURISTIQUE.md` pour la **Partie 3** (lieu + journée, GlobalQuota puis ClassQuota, préfixe `SiteTouristique*`, anti-collision avec `Site` guichet).
- `ANALYSE_V1_RESTAURANT.md` pour la **Partie 4** (établissement + créneau, couverts GlobalQuota puis zones, acompte CASH/FlexPay, préfixe `Restaurant*`).
- `ANALYSE_V1_HOTEL.md` pour la **Partie 5** (hôtel + types de chambres + allotment nuit, ClassQuota multi-nuit, acompte CASH/FlexPay, préfixe `Hotel*`).
- Scripts SQL associes : … Hôtel : … `Scripts/production_hotel_phase7c_rooms.sql`, `Scripts/production_hotel_phase7d_checkin.sql`, `Scripts/production_hotel_phase7e_extras.sql`, puis `Scripts/assign_hotel_permissions_admin_gerant.sql`.

## Prochaine etape

- **Hôtel Partie 5** : Phases 1–6 + **7a**–**7e** livrées (extras réception).
- Restaurant **Phase 6 planif multi-plages livrée** (`/api/restaurants/planifications`) ; reste Phase 6+ : tables numérotées, check-in.
- En parallèle : déployer SQL Site Touristique / Restaurant + front `MODULE_10` / `MODULE_11` si pas encore fait.

## Objectif

Conserver l'historique des analyses, plans d'action et hypotheses produit/technique en un seul endroit.
