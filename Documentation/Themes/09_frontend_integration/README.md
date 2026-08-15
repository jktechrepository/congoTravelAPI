# Theme 09 - Frontend et Integration

## A lire d'abord

- **`DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md`** — point d'entrée unique (Vue.js + Flutter, 3 personas)
- Fiches modules : `MODULE_01` à `MODULE_09` (voir index dans le document maître)
- **`MODULE_03_RESERVATION_BILLET.md`** — réservation transport + billet embarquement
- **`MODULE_BILLET_AVION_A4.md`** — billet d'avion A4 (preview HTML + PDF, compagnies aériennes uniquement)
- **`MODULE_05_EVENEMENT_BILLETTERIE.md`** — billetterie événement (catalogue, `with-paiement` / électronique, Vue guichet + Flutter voyageur / gate)
- **`MODULE_10_SITE_TOURISTIQUE.md`** — billetterie site touristique (`/api/sites-touristiques/*`, Vue guichet + Flutter client/gate ; voir aussi workflow [`DOCUMENTATION_WORKFLOW_SITE_TOURISTIQUE_V1.md`](../05_transport_sync/DOCUMENTATION_WORKFLOW_SITE_TOURISTIQUE_V1.md))
- **`MODULE_11_RESTAURANT.md`** — réservation restaurant (`/api/restaurants/*`, Vue admin + Flutter client/gate tickets ; voir aussi workflow [`DOCUMENTATION_WORKFLOW_RESTAURANT_V1.md`](../05_transport_sync/DOCUMENTATION_WORKFLOW_RESTAURANT_V1.md))
- **`CHANGELOG_2026-08-15_RESTAURANT_ET_SITE_TOURISTIQUE.md`** — changements du 15 août 2026 (tickets resto, photos, localisation/horaires ST) pour Vue + Flutter
- Tickets restaurant (référence API) : [`DOCUMENTATION_API_TICKETS_RESTAURANT_V1.md`](../05_transport_sync/DOCUMENTATION_API_TICKETS_RESTAURANT_V1.md)
- **`INTEGRATION_FLUTTER_FLEXPAY.md`** — paiement Mobile Money / carte **transport** (modèle unifié + verifier + billets)

## Ensuite

- `DOCUMENTATION_BACKEND_CONTRACT_FRONTENDS.md` — contrats payload détaillés
- `DOCUMENTATION_INTEGRATION_FRONTENDS_VUE_FLUTTER.md` — archive détaillée historique
- `INTEGRATION_VUEJS.md` — dashboards Vue
- [`DOCUMENTATION_API_ENDPOINTS_COMPLETE.md`](../01_demarrage/DOCUMENTATION_API_ENDPOINTS_COMPLETE.md) — catalogue routes
- [`docs/INDEX_MODULES_FRONTEND.md`](../../../docs/INDEX_MODULES_FRONTEND.md)

## Objectif

Offrir un parcours unique aux equipes frontend pour integrer proprement les endpoints backend.
