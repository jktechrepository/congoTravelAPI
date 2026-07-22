# 📚 Index de la Documentation Complète - CongoTravelApi

## 📌 Point d'entree thematique

- Voir `Documentation/INDEX_DOCUMENTATION_THEMATIQUE.md` pour la vue unifiee par thematiques.

**Date de création :** 15 janvier 2026  
**Version :** 1.0

---

## 🎯 Documentation Principale

### 📖 [DOCUMENTATION_COMPLETE_BACKEND_WORKFLOW.md](./Documentation/Themes/01_demarrage/DOCUMENTATION_COMPLETE_BACKEND_WORKFLOW.md) ⭐ **COMMENCEZ ICI**

Documentation complète et exhaustive du projet CongoTravelApi, incluant :
- Vue d'ensemble et architecture
- Tous les modules et fonctionnalités
- Liste complète des endpoints API
- Modèles de données
- Services et repositories
- Authentification et sécurité
- Configuration et déploiement
- Guide de développement

---

## 📦 Documentation par Module

### 🔐 Authentification et Utilisateurs

- **Module Utilisateur** : Gestion des utilisateurs, authentification JWT, refresh tokens
- **Module Rôles** : Gestion des rôles et permissions
- **Module Audit** : Traçabilité des actions

**Endpoints :** `/api/Utilisateur`, `/api/Role`, `/api/Permission`, `/api/Audit`

---

### 👥 Gestion des Clients

- **Module Client** : CRUD clients, génération CodeCons, import/export Excel
- **Module ClientUsage** : Gestion des usages clients
- **Module ClientCrashed** : Gestion des erreurs d'import

**Endpoints :** `/api/Client`, `/api/ClientUsage`, `/api/ClientCrashed`

**Documentation détaillée :**
- `docs/API_DOCUMENTATION_CLIENT.md` (si disponible)

---

### 💰 Facturation et Paiements

- **Module Facture** : Création, diffusion, consolidation
- **Module ClientFacture** : Relation Client-Facture, arriérés, consolidation
- **Module Paiement** : Enregistrement, totaux, factures impayées

**Endpoints :** `/api/Facture`, `/api/ClientFacture`, `/api/Paiement`

**Documentation détaillée :**
- `RECAPITULATIF_FINAL_CLIENTFACTURE.md` - Récapitulatif ClientFacture
- `PLAN_ACTION_ADAPTATION_PAIEMENT_CLIENTFACTURE.md` - Adaptation Paiement
- `PLAN_ACTION_ADAPTATION_DIFFUSION_CLIENTFACTURE.md` - Adaptation Diffusion

---

### 📢 Communication

- **Module CommunicationCampaign** : Campagnes de communication ciblées
- **Module ClientFilter** : Filtrage avancé des clients (par arriérés ✨)

**Endpoints :** `/api/CommunicationCampaign`

**Documentation détaillée :**
- `docs/API_DOCUMENTATION_COMMUNICATION.md` - Documentation Communication complète
- `PLAN_TRAVAIL_FILTRAGE_ARRIERES_COMMUNICATION.md` - Filtrage par arriérés

---

### 🔔 Notifications

- **Module Notification** : Gestion des notifications
- **Module NotificationPush** : Notifications push
- **Module NotificationPreference** : Préférences de notification

**Endpoints :** `/api/Notification`, `/api/NotificationPush`, `/api/NotificationPreference`

**Canaux supportés :**
- Push (Firebase Cloud Messaging)
- SMS (Twilio)
- Email (SMTP)
- In-App (SignalR)

---

### 📋 Plaintes et Signalements

- **Module PlainteClient** : Gestion des plaintes clients
- **Module PanneSignalement** : Signalements de pannes

**Endpoints :** `/api/PlainteClient`, `/api/PanneSignalement`

**Documentation détaillée :**
- `docs/API_DOCUMENTATION_PLAINTE_CLIENT.md` - Documentation Plaintes
- `docs/API_DOCUMENTATION_PANNE_SIGNALEMENT.md` - Documentation Pannes

---

### 📊 Dashboard et Statistiques

- **Module Dashboard** : Statistiques globales par société

**Endpoints :** `/api/Dashboard`

**Métriques disponibles :**
- Nombre de clients actifs
- Total des factures
- Total des paiements
- Paiements du mois
- Total général des arriérés ✨

---

### 🏢 Organisation

- **Module Societe** : Gestion des sociétés
- **Module Axe** : Gestion des axes
- **Module Cabine** : Gestion des cabines
- **Module CategorieClient** : Gestion des catégories
- **Module Usage** : Gestion des usages
- **Module Agent** : Gestion des agents

**Endpoints :** `/api/Societe`, `/api/Axe`, `/api/Cabine`, `/api/CategorieClient`, `/api/Usage`, `/api/Agent`

---

## 🔧 Documentation Technique

### Architecture et Conception

- `DOCUMENTATION_COMPLETE_PROJET.md` - Section Architecture
- `docs/ARCHITECTURE.md` (si disponible)

### Base de Données

- **Migrations :** Dossier `Migrations/` (93 fichiers)
- **Scripts SQL :** Dossier `Scripts/` (61 fichiers SQL)
- **Modèles :** Dossier `Models/` (111 fichiers)

### Configuration

- `appsettings.Development.json` - Configuration développement
- `appsettings.Production.json` - Configuration production (à créer)

### Déploiement

- `DOCUMENTATION_COMPLETE_PROJET.md` - Section Déploiement
- Scripts dans `Scripts/deploy.sh` et `Scripts/deploy.ps1`

---

## 📝 Plans d'Action et Guides

### Plans d'Action Récents

- `PLAN_TRAVAIL_FILTRAGE_ARRIERES_COMMUNICATION.md` - Filtrage par arriérés (✨ NOUVEAU)
- `PLAN_ACTION_ADAPTATION_PAIEMENT_CLIENTFACTURE.md` - Adaptation Paiement
- `PLAN_ACTION_ADAPTATION_DIFFUSION_CLIENTFACTURE.md` - Adaptation Diffusion

### Guides d'Implémentation

- `IMPLEMENTATION_SOFT_DELETE.md` - Implémentation Soft Delete
- `IMPLEMENTATION_VUE_CONSOLIDEE.md` - Vues Consolidées
- `GUIDE_TEST_IMPORT_EXCEL.md` - Tests Import Excel

---

## 🧪 Tests et Collections

### Collections API

- `CongoTravel_API_Collection.postman_collection.json` - Collection Postman complète
- `CongoTravel_API.postman_collection.json` - Collection alternative

### Tests

- Dossier `CongoTravel.Tests.Unit/` - Tests unitaires
- Fichiers `test-*.http` - Tests HTTP

---

## 📚 Documentation Frontend

### Index des Modules Frontend

- `docs/INDEX_MODULES_FRONTEND.md` - Index des documentations frontend

### Documentations Frontend Disponibles

- `docs/API_DOCUMENTATION_COMMUNICATION.md` - Communication
- `docs/API_DOCUMENTATION_PLAINTE_CLIENT.md` - Plaintes
- `docs/API_DOCUMENTATION_PANNE_SIGNALEMENT.md` - Pannes
- `DOCUMENTATION_FRONTEND_PAIEMENT.md` - Paiements (si disponible)

---

## 🔍 Recherche Rapide

### Par Fonctionnalité

| Fonctionnalité | Module | Endpoint Principal |
|----------------|--------|-------------------|
| Authentification | Utilisateur | `POST /api/Utilisateur/login` |
| Créer un client | Client | `POST /api/Client` |
| Créer une facture | Facture | `POST /api/Facture` |
| Enregistrer un paiement | Paiement | `POST /api/Paiement` |
| Créer une campagne | Communication | `POST /api/CommunicationCampaign` |
| Voir les arriérés | ClientFacture | `GET /api/ClientFacture/arrieres` |
| Dashboard | Dashboard | `GET /api/Dashboard/{idSociete}` |

### Par Type d'Opération

| Opération | Endpoints |
|-----------|-----------|
| **GET (Liste)** | `/api/{Controller}` |
| **GET (Détails)** | `/api/{Controller}/{id}` |
| **POST (Créer)** | `/api/{Controller}` |
| **PUT (Modifier)** | `/api/{Controller}/{id}` |
| **DELETE (Supprimer)** | `/api/{Controller}/{id}` |
| **PATCH (Partiel)** | `/api/{Controller}/{id}/action` |

---

## 🆕 Nouveautés Récentes

### ✨ Filtrage par Nombre de Factures en Arriérés

**Date :** 15 janvier 2026

Permet de cibler les clients dans les campagnes de communication selon le nombre de factures en arriérés qu'ils possèdent.

**Documentation :** `PLAN_TRAVAIL_FILTRAGE_ARRIERES_COMMUNICATION.md`

**Exemple :**
```json
{
  "criteresCiblage": {
    "nombreFacturesArrieresMin": 3,
    "nombreFacturesArrieresMax": 5
  }
}
```

### ✨ Adaptation des Endpoints Paiement

**Date :** 15 janvier 2026

Les endpoints de paiement utilisent maintenant `ClientFacture` comme source de vérité pour les montants consolidés.

**Documentation :** `PLAN_ACTION_ADAPTATION_PAIEMENT_CLIENTFACTURE.md`

### ✨ Enrichissement des Endpoints de Diffusion

**Date :** 15 janvier 2026

Les endpoints de diffusion incluent maintenant des statistiques détaillées depuis `ClientFacture`.

**Documentation :** `PLAN_ACTION_ADAPTATION_DIFFUSION_CLIENTFACTURE.md`

---

## 📊 Statistiques du Projet

- **Contrôleurs :** 26
- **Services :** 58
- **Modèles :** 111
- **Migrations :** 93
- **Scripts SQL :** 61
- **Documentations :** 30+

---

## 🎯 Points d'Entrée Recommandés

### Pour les Développeurs Backend

1. 📖 `DOCUMENTATION_COMPLETE_PROJET.md` - Documentation complète
2. 🏗️ Section Architecture dans la documentation principale
3. 📦 Section Services pour comprendre la logique métier

### Pour les Développeurs Frontend

1. 📖 `docs/INDEX_MODULES_FRONTEND.md` - Index des modules
2. 📚 `docs/API_DOCUMENTATION_*.md` - Documentation par module
3. 🔌 Section API Endpoints dans la documentation principale

### Pour les Administrateurs

1. ⚙️ Section Configuration dans la documentation principale
2. 🚀 Section Déploiement
3. 🔐 Section Authentification et Sécurité

---

## 📞 Support

Pour toute question ou problème :
- **Documentation principale :** `DOCUMENTATION_COMPLETE_PROJET.md`
- **Issues :** Créer une issue sur le repository
- **Email :** support@kenergie.com

---

**Dernière mise à jour :** 15 janvier 2026  
**Version :** 1.0
