# 📋 Documentation Endpoint : POST /api/Inscription/new/{nomEcole}

**Version API** : v2  
**Dernière mise à jour** : 2 Mars 2026  
**Statut** : ✅ Production Ready  

---

## 🎯 Vue d'Ensemble

L'endpoint `POST /api/Inscription/new/{nomEcole}` permet de créer une nouvelle inscription scolaire avec génération automatique de matricule basée sur le nom de l'école. Il gère automatiquement les réinscriptions, crée les comptes utilisateurs pour les tuteurs et envoie des notifications multi-canaux.

---

## 🔗 Informations Générales

| Propriété | Valeur |
|-----------|--------|
| **Méthode HTTP** | `POST` |
| **URL** | `/api/Inscription/new/{nomEcole}` |
| **Authentification** | JWT Token requis |
| **Content-Type** | `application/json` |
| **Réponse** | `201 Created` ou `400 Bad Request` |

---

## 🔐 Sécurité et Autorisations

### **Authentification Requise**
- **Token JWT** valide dans l'en-tête `Authorization: Bearer {token}`
- L'utilisateur doit être authentifié

### **Permissions Requises**
- Aucune permission spécifique au-delà de l'authentification de base
- L'utilisateur peut inscrire des élèves dans son école associée

---

## 📝 Paramètres

### **Paramètres de Route**

| Nom | Type | Obligatoire | Description |
|-----|------|-------------|-------------|
| `nomEcole` | `string` | ✅ Oui | Nom de l'école utilisé pour générer le matricule de l'élève |

**Exemples :**
- `/api/Inscription/new/Ecole%20Primaire`
- `/api/Inscription/new/Lycee%20Saint%20Joseph`

---

## 📦 Corps de la Requête (Request Body)

### **Structure JSON Complète**

```json
{
    // === DONNÉES DE L'INSCRIPTION ===
    "Type": "Inscription",
    "IdEcole": 1,
    "IdClasse": 5,
    "IdAnneeScolaire": 2,
    "DateInscription": "2025-03-02T10:30:00",
    "StatutInscription": "En attente",
    
    // === DONNÉES DE L'ÉLÈVE ===
    "NomEleve": "Jean",
    "PostnomEleve": "Pierre",
    "PrenomEleve": "Louis",
    "GenreEleve": "M",
    "DateNaissanceEleve": "2018-05-15",
    "LieuNaissanceEleve": "Kinshasa",
    "NationaliteEleve": "Congolaise",
    "PhotoEleveUrl": "https://example.com/photo.jpg",
    "CommentaireEleve": "Élève motivé",
    
    // === ADRESSE DE L'ÉLÈVE ===
    "ProvinceEleve": "Kinshasa",
    "VilleEleve": "Kinshasa",
    "CommuneEleve": "Lemba",
    "QuartierEleve": "Matete",
    "AvenueEleve": "ByPass",
    "NumeroEleve": "123",
    
    // === DONNÉES DU TUTEUR ===
    "NomCompletTuteur": "Marie Claire KABONGO",
    "GenreTuteur": "F",
    "EmailTuteur": "marie.claire@email.com",
    "TelephoneTuteur": "+243123456789",
    "NomCompletRepresentant": "Jean KABONGO",
    "TelephoneRepresentant": "+243987654321",
    "PhotoTuteurUrl": "https://example.com/tuteur.jpg",
    "PieceIdentiteTuteur": "A0123456789",
    
    // === CAS DE RÉINSCRIPTION (OPTIONNEL) ===
    "IdEleveExistant": null,
    "IdTuteurExistant": null
}
```

### **Détail des Champs**

#### 📋 **Données de l'Inscription**

| Champ | Type | Obligatoire | Description |
|--------|------|-------------|-------------|
| `Type` | `string` | ✅ Oui | Type : "Inscription" ou "Réinscription" |
| `IdEcole` | `integer` | ✅ Oui | ID de l'école |
| `IdClasse` | `integer` | ✅ Oui | ID de la classe |
| `IdAnneeScolaire` | `integer` | ✅ Oui | ID de l'année scolaire |
| `DateInscription` | `datetime` | ✅ Oui | Date de l'inscription (ISO 8601) |
| `StatutInscription` | `string` | ✅ Oui | Statut : "En attente", "Confirmé", "Annulé" |

#### 👶 **Données de l'Élève**

| Champ | Type | Obligatoire | Description |
|--------|------|-------------|-------------|
| `NomEleve` | `string` | ✅ Oui | Nom de famille de l'élève |
| `PostnomEleve` | `string` | ✅ Oui | Post-nom de l'élève |
| `PrenomEleve` | `string` | ✅ Oui | Prénom de l'élève |
| `GenreEleve` | `string` | ✅ Oui | Genre : "M" ou "F" |
| `DateNaissanceEleve` | `datetime` | ✅ Oui | Date de naissance (ISO 8601) |
| `LieuNaissanceEleve` | `string` | ✅ Oui | Lieu de naissance |
| `NationaliteEleve` | `string` | ✅ Oui | Nationalité |
| `PhotoEleveUrl` | `string` | ❌ Non | URL de la photo de l'élève |
| `CommentaireEleve` | `string` | ❌ Non | Commentaires sur l'élève |

#### 🏠 **Adresse de l'Élève**

| Champ | Type | Obligatoire | Description |
|--------|------|-------------|-------------|
| `ProvinceEleve` | `string` | ❌ Non | Province de résidence |
| `VilleEleve` | `string` | ❌ Non | Ville de résidence |
| `CommuneEleve` | `string` | ❌ Non | Commune de résidence |
| `QuartierEleve` | `string` | ❌ Non | Quartier de résidence |
| `AvenueEleve` | `string` | ❌ Non | Avenue de résidence |
| `NumeroEleve` | `string` | ❌ Non | Numéro de maison |

#### 👨‍👩‍👧‍👦 **Données du Tuteur**

| Champ | Type | Obligatoire | Description |
|--------|------|-------------|-------------|
| `NomCompletTuteur` | `string` | ✅ Oui | Nom complet du tuteur |
| `GenreTuteur` | `string` | ✅ Oui | Genre : "M" ou "F" |
| `EmailTuteur` | `string` | ❌ Non | Email du tuteur |
| `TelephoneTuteur` | `string` | ❌ Non | Téléphone du tuteur |
| `NomCompletRepresentant` | `string` | ❌ Non | Nom du représentant légal |
| `TelephoneRepresentant` | `string` | ❌ Non | Téléphone du représentant |
| `PhotoTuteurUrl` | `string` | ❌ Non | URL de la photo du tuteur |
| `PieceIdentiteTuteur` | `string` | ❌ Non | Numéro de pièce d'identité |

#### 🔄 **Cas de Réinscription**

| Champ | Type | Obligatoire | Description |
|--------|------|-------------|-------------|
| `IdEleveExistant` | `integer` | ❌ Non | ID d'un élève existant à réinscrire |
| `IdTuteurExistant` | `integer` | ❌ Non | ID d'un tuteur existant à associer |

---

## 📤 Réponses

### ✅ **Réponse Succès (201 Created)**

```json
{
    "success": true,
    "message": "Inscription effectuée avec succès",
    "idInscription": 1234,
    "idEleve": 5678,
    "idTuteur": 9012,
    "inscription": {
        "idInscription": 1234,
        "type": "Inscription",
        "idEleve": 5678,
        "idEcole": 1,
        "idClasse": 5,
        "idAnneeScolaire": 2,
        "dateInscription": "2025-03-02T10:30:00",
        "statutInscription": "En attente",
        "statut": true,
        "eleve": {
            "idEleve": 5678,
            "referenceEleve": "550e8b0a-1234-5678-9abc-def123456789",
            "nom": "Jean",
            "postnom": "Pierre",
            "prenom": "Louis",
            "nomComplet": "Jean Pierre Louis",
            "genre": "M",
            "dateNaissance": "2018-05-15",
            "lieuNaissance": "Kinshasa",
            "nationalite": "Congolaise",
            "matricule": "EKL25-A1B2C3",
            "statut": true
        },
        "classe": {
            "idClasse": 5,
            "nomClasse": "3ème Primaire",
            "niveau": "Primaire"
        },
        "ecole": {
            "idEcole": 1,
            "nomEcole": "École Primaire KELASI",
            "referenceEcole": "EKL-2025"
        },
        "anneeScolaire": {
            "idAnneeScolaire": 2,
            "libelleAnneeScolaire": "2024-2025"
        }
    },
    "compteUtilisateurTuteur": {
        "username": "marie.claire.kabongo",
        "password": "Temp@2025!",
        "email": "marie.claire@email.com",
        "nomUtilisateur": "Marie Claire KABONGO",
        "role": "Parent"
    }
}
```

### ❌ **Réponse Erreur (400 Bad Request)**

#### **Erreur de Validation**
```json
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
        "NomEleve": ["Le champ NomEleve est obligatoire."],
        "DateNaissanceEleve": ["La date de naissance est invalide."]
    }
}
```

#### **Erreur Métier**
```json
{
    "error": "Un élève avec les mêmes informations existe déjà dans le système."
}
```

### 🔒 **Réponse Non Autorisé (401 Unauthorized)**
```json
{
    "message": "Token JWT invalide ou expiré"
}
```

---

## 🔄 Fonctionnalités Automatiques

### 🎫 **Génération Automatique de Matricule**

Le matricule est généré automatiquement selon le format :
```
{CodeEcole}{Annee}-{GUID}
```

**Exemples :**
- `EKL25-A1B2C3` pour École KELASI en 2025
- `LCS25-B4D5E6` pour Lycée Saint Joseph en 2025

**Processus :**
1. Extraire les 3 premières lettres du nom de l'école
2. Ajouter les 2 derniers chiffres de l'année actuelle
3. Générer un GUID unique de 6 caractères
4. Vérifier l'unicité dans la base de données

### 🔄 **Gestion Intelligente des Réinscriptions**

Le système recherche automatiquement si l'élève existe déjà :

**Critères de recherche :**
- Nom complet normalisé de l'élève
- Date de naissance
- Nom complet normalisé du tuteur

**Si trouvé :**
- ✅ Réutilisation de l'élève existant
- ✅ Réactivation du statut si nécessaire
- ✅ Mise à jour de la classe
- ✅ Type changé en "Réinscription"

### 👤 **Création Automatique de Compte Tuteur**

Si le tuteur n'a pas de compte utilisateur :
- ✅ Génération automatique de nom d'utilisateur
- ✅ Génération de mot de passe temporaire
- ✅ Attribution du rôle "Parent"
- ✅ Envoi des identifiants par email

### 📢 **Notifications Multi-canaux**

Après inscription réussie :
- 📧 **Email** au tuteur avec informations et identifiants
- 📱 **SMS** si numéro fourni
- 🔔 **Notification Push** Firebase si device enregistré
- 📡 **SignalR** pour dashboard en temps réel

---

## 🧪 Exemples d'Utilisation

### **Exemple 1 : Première Inscription**

```bash
curl -X POST "https://api.kelasinabiso.com/api/Inscription/new/Ecole%20Primaire" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -d '{
    "Type": "Inscription",
    "IdEcole": 1,
    "IdClasse": 5,
    "IdAnneeScolaire": 2,
    "DateInscription": "2025-03-02T10:30:00",
    "StatutInscription": "En attente",
    "NomEleve": "Jean",
    "PostnomEleve": "Pierre",
    "PrenomEleve": "Louis",
    "GenreEleve": "M",
    "DateNaissanceEleve": "2018-05-15",
    "LieuNaissanceEleve": "Kinshasa",
    "NationaliteEleve": "Congolaise",
    "NomCompletTuteur": "Marie Claire KABONGO",
    "GenreTuteur": "F",
    "EmailTuteur": "marie.claire@email.com",
    "TelephoneTuteur": "+243123456789"
  }'
```

### **Exemple 2 : Réinscription avec ID Existant**

```bash
curl -X POST "https://api.kelasinabiso.com/api/Inscription/new/Ecole%20Primaire" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -d '{
    "Type": "Réinscription",
    "IdEcole": 1,
    "IdClasse": 6,
    "IdAnneeScolaire": 2,
    "DateInscription": "2025-03-02T10:30:00",
    "StatutInscription": "Confirmé",
    "IdEleveExistant": 5678,
    "IdTuteurExistant": 9012
  }'
```

---

## 🔍 Codes d'Erreur

| Code HTTP | Description | Solution |
|-----------|-------------|-----------|
| `400` | Erreur de validation des données | Vérifier les champs obligatoires et formats |
| `401` | Token JWT manquant ou invalide | Fournir un token valide |
| `403` | Permissions insuffisantes | Vérifier les droits de l'utilisateur |
| `404` | École ou classe non trouvée | Vérifier les IDs fournis |
| `409` | Conflit (élève déjà inscrit) | Utiliser l'endpoint de réinscription |
| `500` | Erreur interne du serveur | Contacter l'administrateur |

---

## 📊 Audit et Logging

### **Informations d'Audit Enregistrées**

Chaque inscription crée automatiquement un audit trail avec :
- 👤 **Utilisateur** : ID et nom de l'utilisateur qui effectue l'inscription
- 🏫 **École** : ID de l'école concernée
- 🌐 **IP Address** : Adresse IP du client
- 💻 **User Agent** : Navigateur ou application utilisée
- 📅 **Timestamp** : Date et heure de l'action
- 🔄 **Action** : "Création d'inscription"
- 📝 **Détails** : IDs de l'inscription, élève et tuteur créés

### **Logs Techniques**

- ✅ **Génération matricule** : Log du matricule généré
- ✅ **Réinscription** : Log si élève existant réutilisé
- ✅ **Création compte tuteur** : Log des identifiants générés
- ✅ **Notifications** : Log des envois email/SMS/push

---

## ⚡ Performance et Limites

### **Limites Actuelles**
- 📝 **Taille max requête** : 10 MB
- 🔄 **Timeout** : 30 secondes
- 📊 **Rate limiting** : 100 requêtes/minute par IP

### **Optimisations**
- ✅ **Transaction base de données** pour garantir la cohérence
- ✅ **Recherche optimisée** avec index sur nom et date de naissance
- ✅ **Cache** pour les données fréquemment accédées
- ✅ **Async/await** pour les opérations I/O

---

## 🔧 Intégration et Tests

### **Environnement de Développement**
```
URL: https://localhost:7102/api/Inscription/new/{nomEcole}
Swagger: https://localhost:7102/swagger
```

### **Environnement de Production**
```
URL: https://api.kelasinabiso.com/api/Inscription/new/{nomEcole}
Swagger: https://api.kelasinabiso.com/swagger
```

### **Postman Collection**
Une collection Postman est disponible dans :
`KelasiNaBiso_API_Collection.postman_collection.json`

---

## 📞 Support et Assistance

### **Documentation Complémentaire**
- 📖 [Guide Documentation API](../01_demarrage/DOCUMENTATION_API_ENDPOINTS_COMPLETE.md)
- 🔐 [Guide Securite JWT](../02_securite_auth/SECURISATION_COMPLETE_JWT.md)
- 📧 [Guide Notifications](../../../docs/AMELIORATIONS_NOTIFICATIONS.md)

### **Support Technique**
- 🐛 **Bugs** : Créer une issue sur le repository
- 📧 **Questions** : Contacter l'équipe technique
- 📚 **Documentation** : Consulter le wiki du projet

---

## 📈 Changelog

### **Version 2.0 (2 Mars 2026)**
- ✅ Ajout génération automatique de matricule
- ✅ Amélioration gestion réinscriptions
- ✅ Création automatique compte tuteur
- ✅ Notifications multi-canaux
- ✅ Audit trail complet

### **Version 1.0 (15 Octobre 2025)**
- ✅ Version initiale de l'endpoint
- ✅ CRUD de base des inscriptions

---

## 📄 Licence

Cette documentation fait partie du projet KelasiNaBiso API et est soumise à la même licence que le projet principal.

---

**Dernière mise à jour : 2 Mars 2026**  
**Version document : 2.0**  
**Auteur : Équipe Technique KelasiNaBiso**
