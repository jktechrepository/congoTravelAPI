# Index Documentation Thematique

Point d'entree unique de la documentation organisee par themes.

## Structure unifiee

Tous les documents de la racine ont ete reclasses dans:

- `Documentation/Themes/01_demarrage`
- `Documentation/Themes/02_securite_auth`
- `Documentation/Themes/03_utilisateurs_roles_agents`
- `Documentation/Themes/04_clients_referentiels`
- `Documentation/Themes/05_transport_sync`
- `Documentation/Themes/06_facturation_paiement`
- `Documentation/Themes/07_dashboards_reporting`
- `Documentation/Themes/08_notifications_communication`
- `Documentation/Themes/09_frontend_integration`
- `Documentation/Themes/10_tests_exploitation`
- `Documentation/Themes/11_analyses_plans`

## Regles de consultation

- Commencer par `Documentation/Themes/01_demarrage`.
- Documentation legacy Kenergie / ClientFacture : `Documentation/Archive/Kenergie/README.md`.
- Pour frontend et contrats, voir `Documentation/Themes/09_frontend_integration` puis `docs/`.
- Pour deploiement et exploitation, voir `Documentation/Themes/10_tests_exploitation` puis `Scripts/`.

## Regles de classement des nouveaux .md

- Prefixe conseille:
  - `DOCUMENTATION_` pour la reference stable
  - `GUIDE_` pour le mode operatoire
  - `ANALYSE_` ou `PLAN_` pour le cadrage
  - `RECAPITULATIF_` ou `SYNTHESE_` pour les conclusions
- Emplacement:
  - docs produit/backend: `Documentation/Themes/*`
  - docs frontend/tests fonctionnels: `docs/`
  - docs operations/deploiement: `Scripts/`
  - racine reservee a: `README.md`, `INDEX_DOCUMENTATION.md`, `INDEX_DOCUMENTATION_COMPLETE.md`

## Parcours rapide par profil

- Backend (nouveau): `Documentation/Themes/01_demarrage` -> `Documentation/Themes/02_securite_auth` -> `Documentation/Themes/03_utilisateurs_roles_agents`
- **Frontend (Vue.js + Flutter)** : `Documentation/Themes/09_frontend_integration/DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md` -> fiches `MODULE_01` à `MODULE_09` -> `Documentation/Themes/05_transport_sync` (détail transport) -> `docs/`
- Produit/ops: `Documentation/Themes/07_dashboards_reporting` -> `Documentation/Themes/10_tests_exploitation` -> `Scripts/`
