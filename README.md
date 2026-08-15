# CongoTravel API - Système de Gestion Scolaire

## Description
API REST pour la gestion d'un système scolaire complet, développée avec ASP.NET Core, Entity Framework Core et **MariaDB 10.11 (LTS)**.

## 🎉 Dernière mise à jour : 23 octobre 2025
- ✅ Migration MySQL → **MariaDB 10.11** (support jusqu'en 2028)
- ✅ Code optimisé et nettoyé (modèle **Agent** unifié)
- ✅ Documentation complète (14 nouveaux guides)
- ✅ API fonctionnelle et testée

**👉 NOUVEAU ?** Commencez par lire **`START_HERE.md`** !

## Structure du Projet

### 📁 Models
Contient tous les modèles de données avec leurs propriétés de navigation et attributs de validation :

- **Eleve.cs** - Gestion des élèves
- **Ecole.cs** - Gestion des écoles
- **Classe.cs** - Gestion des classes
- **Utilisateur.cs** - Gestion des utilisateurs
- **Tuteur.cs** - Gestion des tuteurs
- **Inscription.cs** - Gestion des inscriptions
- **Note.cs** - Gestion des notes
- **Cours.cs** - Gestion des cours
- **Message.cs** - Gestion des messages
- **Presence.cs** - Gestion des présences
- **Vacation.cs** - Gestion des Vacations
- **Agent.cs** - Gestion des agents/enseignants (modèle unifié)
- **AnneeScolaire.cs** - Gestion des années scolaires
- **Frais.cs** - Gestion des frais
- **Paiement.cs** - Gestion des paiements
- **Role.cs** - Gestion des rôles
- **Section.cs** - Gestion des sections
- **Option.cs** - Gestion des options
- **GroupeMessage.cs** - Gestion des groupes de messages
- **Document.cs** - Gestion des documents
- **RessourcePedagogique.cs** - Gestion des ressources pédagogiques
- **Evaluation.cs** - Gestion des évaluations
- **Adresse.cs** - Classe abstraite pour les adresses

### 📁 Services/Repositories
Contient les interfaces et implémentations des repositories :

#### Interfaces
- **IEleveRepository.cs** - Interface pour la gestion des élèves
- **IEcoleRepository.cs** - Interface pour la gestion des écoles
- **IClasseRepository.cs** - Interface pour la gestion des classes
- **IUtilisateurRepository.cs** - Interface pour la gestion des utilisateurs

#### Services
- **EleveService.cs** - Implémentation du service des élèves
- **EcoleService.cs** - Implémentation du service des écoles
- **ClasseService.cs** - Implémentation du service des classes
- **UtilisateurService.cs** - Implémentation du service des utilisateurs

### 📁 Controllers
Contient les contrôleurs API REST :

- **EleveController.cs** - Endpoints pour la gestion des élèves
- **EcoleController.cs** - Endpoints pour la gestion des écoles
- **ClasseController.cs** - Endpoints pour la gestion des classes
- **UtilisateurController.cs** - Endpoints pour la gestion des utilisateurs

### 📁 Data
Contient le contexte de base de données :

- **CongoTravelDbContext.cs** - Contexte Entity Framework avec toutes les relations

## Fonctionnalités Principales

### 🔐 Authentification
- Gestion des utilisateurs avec rôles
- Authentification par email/mot de passe
- Hachage sécurisé des mots de passe

### 👥 Gestion des Utilisateurs
- CRUD complet pour les utilisateurs
- Gestion des rôles (Enseignant, Élève, Parent, etc.)
- Association aux écoles

### 🏫 Gestion des Écoles
- CRUD complet pour les écoles
- Gestion des classes par école
- Gestion des utilisateurs par école

### 📚 Gestion des Classes
- CRUD complet pour les classes
- Association aux sections et options
- Gestion des élèves par classe
- Gestion des cours par classe

### 👨‍🎓 Gestion des Élèves
- CRUD complet pour les élèves
- Association aux classes et tuteurs
- Gestion des notes et présences
- Gestion des paiements

### 📊 Gestion Académique
- Gestion des notes et évaluations
- Gestion des présences
- Gestion des cours et ressources pédagogiques
- Gestion des Vacations

### 💰 Gestion Financière
- Gestion des frais par classe
- Gestion des paiements
- Suivi des transactions

### 💬 Messagerie
- Gestion des messages entre utilisateurs
- Gestion des groupes de messages
- Système de messagerie interne

## Configuration

### Base de Données
Le projet utilise **MariaDB 10.11** avec Entity Framework Core (Pomelo provider). La chaîne de connexion est configurée dans `appsettings.json` :

```json
{
  "ConnectionStrings": {
    "KelasiConnection": "Server=localhost;Database=CongoTravelDb;User=kansa;Password=kansa2025;Port=3306;SslMode=none;CharSet=utf8mb4;"
  }
}
```

**📥 Installation MariaDB** : Consultez `INSTALLATION_MARIADB_WINDOWS.md`

### Ports
L'API écoute sur :
- HTTP: `http://0.0.0.0:5002`
- HTTPS: `https://0.0.0.0:7102`

## Endpoints API

### Élèves
- `GET /api/Eleve` - Récupérer tous les élèves
- `GET /api/Eleve/{id}` - Récupérer un élève par ID
- `GET /api/Eleve/reference/{reference}` - Récupérer un élève par référence
- `GET /api/Eleve/classe/{idClasse}` - Récupérer les élèves d'une classe
- `GET /api/Eleve/tuteur/{idTuteur}` - Récupérer les élèves d'un tuteur
- `POST /api/Eleve` - Créer un nouvel élève
- `PUT /api/Eleve/{id}` - Mettre à jour un élève
- `DELETE /api/Eleve/{id}` - Supprimer un élève

### Écoles
- `GET /api/Ecole` - Récupérer toutes les écoles
- `GET /api/Ecole/{id}` - Récupérer une école par ID
- `GET /api/Ecole/{id}/classes` - Récupérer les classes d'une école
- `GET /api/Ecole/{id}/utilisateurs` - Récupérer les utilisateurs d'une école
- `POST /api/Ecole` - Créer une nouvelle école
- `PUT /api/Ecole/{id}` - Mettre à jour une école
- `DELETE /api/Ecole/{id}` - Supprimer une école

### Classes
- `GET /api/Classe` - Récupérer toutes les classes
- `GET /api/Classe/{id}` - Récupérer une classe par ID
- `GET /api/Classe/ecole/{idEcole}` - Récupérer les classes d'une école
- `GET /api/Classe/section/{idSection}` - Récupérer les classes d'une section
- `GET /api/Classe/option/{idOption}` - Récupérer les classes d'une option
- `GET /api/Classe/{id}/eleves` - Récupérer les élèves d'une classe
- `GET /api/Classe/{id}/cours` - Récupérer les cours d'une classe
- `POST /api/Classe` - Créer une nouvelle classe
- `PUT /api/Classe/{id}` - Mettre à jour une classe
- `DELETE /api/Classe/{id}` - Supprimer une classe

### Utilisateurs
- `GET /api/Utilisateur` - Récupérer tous les utilisateurs
- `GET /api/Utilisateur/{id}` - Récupérer un utilisateur par ID
- `GET /api/Utilisateur/email/{email}` - Récupérer un utilisateur par email
- `GET /api/Utilisateur/reference/{reference}` - Récupérer un utilisateur par référence
- `GET /api/Utilisateur/role/{idRole}` - Récupérer les utilisateurs d'un rôle
- `GET /api/Utilisateur/ecole/{idEcole}` - Récupérer les utilisateurs d'une école
- `POST /api/Utilisateur` - Créer un nouvel utilisateur
- `POST /api/Utilisateur/authenticate` - Authentifier un utilisateur
- `PUT /api/Utilisateur/{id}` - Mettre à jour un utilisateur
- `DELETE /api/Utilisateur/{id}` - Supprimer un utilisateur

## Attributs de Validation

Tous les modèles utilisent les attributs de validation suivants :
- `[Required]` - Champs obligatoires
- `[MaxLength]` - Longueur maximale
- `[EmailAddress]` - Validation d'email
- `[Phone]` - Validation de téléphone
- `[Range]` - Validation de plage de valeurs
- `[JsonIgnore]` - Exclusion de la sérialisation JSON
- `[ValidateNever]` - Exclusion de la validation

## Relations de Base de Données

Le contexte Entity Framework configure toutes les relations entre les entités avec les comportements de suppression appropriés :
- **Cascade** - Suppression en cascade pour les relations parent-enfant
- **SetNull** - Mise à null de la clé étrangère lors de la suppression

## Technologies Utilisées

- **ASP.NET Core 6.0**
- **Entity Framework Core 6.0**
- **MariaDB 10.11 (LTS)** - Support jusqu'en 2028
- **Pomelo.EntityFrameworkCore.MySql** (Provider MariaDB)
- **BCrypt.Net** (Hashing des mots de passe)
- **JWT** (Authentification)
- **Swagger/OpenAPI** (documentation API)

## Installation et Démarrage

1. Cloner le repository
2. Configurer la chaîne de connexion dans `appsettings.json`
3. Exécuter les migrations Entity Framework
4. Lancer l'application

```bash
dotnet restore
dotnet build
dotnet run
```

L'API sera accessible sur `http://192.168.43.139:5002` et la documentation Swagger sur `http://192.168.43.139:5002/swagger`.
