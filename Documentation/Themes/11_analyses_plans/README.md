# Theme 11 - Analyses et Plans

## A lire d'abord

- `ANALYSE_COMPLETE_CODE_EXISTANT.md`
- `ANALYSE_EXPERT_SYSTEME_KENERGIE.md`

## Ensuite

- Les fichiers `ANALYSE_*`, `ANALISE_*`, `PLAN_*`, `RECAPITULATIF_*`, `FIX_*`, `QUESTIONS_*` presents dans ce dossier.
- `ANALYSE_V1_BILLETTERIE_3_MODES_INVENTAIRE.md` pour le cadrage evenementiel (SeatNumbered, ClassQuota, GlobalQuota) — affinage V1 + **convention de nommage préfixe `Evenement*`** (section 12).
- Scripts SQL associes : `Scripts/production_evenement_ticketing_v1.sql` et fichiers `evenement_*` dans `Scripts/`.

## Prochaine etape

- **Phase 1 — socle technique** : modeles EF `Evenement*`, DbContext, permissions, hosted service expiration HOLD.
- Puis **Phase 2 — Mode C** (`GlobalQuota`) : hold → confirm CASH → tickets → availability.

## Objectif

Conserver l'historique des analyses, plans d'action et hypotheses produit/technique en un seul endroit.
