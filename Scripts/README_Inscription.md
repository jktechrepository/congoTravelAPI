# Système d'Inscription Avancé

## Vue d'ensemble

Ce système d'inscription utilise une procédure stockée SQL Server pour gérer les règles métier complexes d'inscription selon trois scénarios différents.

## Règles de Gestion

### Note 1: Nouveau Élève + Nouveau Tuteur
- **Action**: Créer un nouveau tuteur, un nouvel élève et une nouvelle inscription
- **Cas d'usage**: Première inscription d'un élève avec un tuteur qui n'existe pas dans le système

### Note 2: Nouveau Élève + Ancien Tuteur
- **Action**: Créer un nouvel élève et une nouvelle inscription, réactiver le tuteur existant
- **Cas d'usage**: Famille avec plusieurs enfants (même tuteur pour différents élèves)

### Note 3: Ancien Élève (Réinscription)
- **Action**: Créer une nouvelle inscription, réactiver l'élève et le tuteur existants
- **Cas d'usage**: Élève qui se réinscrit pour une nouvelle année scolaire

## Installation

### 1. Créer la procédure stockée
Exécutez le script `sp_CreateInscription.sql` dans votre base de données SQL Server.

### 2. Tester la procédure
Exécutez le script `ExecuteStoredProcedure.sql` pour tester les différents scénarios.

## Utilisation via API

### Endpoint
```
POST /api/Inscription/advanced
```

### Exemple de requête JSON

#### Cas 1: Nouveau élève avec nouveau tuteur
```json
{
  "type": "Inscription",
  "idEcole": 1,
  "idClasse": 1,
  "idAnneeScolaire": 1,
  "dateInscription": "2024-09-01T00:00:00",
  "statutInscription": "En attente",
  
  "nomEleve": "KABILA",
  "postnomEleve": "Joseph",
  "prenomEleve": "Kabila",
  "genreEleve": "M",
  "dateNaissanceEleve": "2010-05-15T00:00:00",
  "lieuNaissanceEleve": "Kinshasa",
  "nationaliteEleve": "Congolaise",
  "commentaireEleve": "Aucun commentaire",
  
  "nomCompletTuteur": "KABILA Laurent-Désiré",
  "genreTuteur": "M",
  "emailTuteur": "laurent.kabila@email.com",
  "telephoneTuteur": "+243123456789"
}
```

#### Cas 2: Nouveau élève avec tuteur existant
```json
{
  "type": "Inscription",
  "idEcole": 1,
  "idClasse": 2,
  "idAnneeScolaire": 1,
  "dateInscription": "2024-09-01T00:00:00",
  "statutInscription": "En attente",
  
  "nomEleve": "KABILA",
  "postnomEleve": "Josephine",
  "prenomEleve": "Kabila",
  "genreEleve": "F",
  "dateNaissanceEleve": "2012-08-20T00:00:00",
  "lieuNaissanceEleve": "Kinshasa",
  "nationaliteEleve": "Congolaise",
  "commentaireEleve": "Sœur du premier élève",
  
  "nomCompletTuteur": "KABILA Laurent-Désiré",
  "genreTuteur": "M",
  "emailTuteur": "laurent.kabila@email.com",
  "telephoneTuteur": "+243123456789"
}
```

#### Cas 3: Réinscription d'un élève existant
```json
{
  "type": "Réinscription",
  "idEcole": 1,
  "idClasse": 3,
  "idAnneeScolaire": 2,
  "dateInscription": "2024-09-01T00:00:00",
  "statutInscription": "En attente",
  
  "idEleveExistant": 1,
  "idTuteurExistant": 1
}
```

### Réponse de l'API

```json
{
  "success": true,
  "message": "Inscription effectuée avec succès. Nouveau tuteur créé.",
  "idInscription": 1,
  "idEleve": 1,
  "idTuteur": 1,
  "inscription": {
    "idInscription": 1,
    "type": "Inscription",
    "idEleve": 1,
    "idEcole": 1,
    "idClasse": 1,
    "idAnneeScolaire": 1,
    "dateInscription": "2024-09-01T00:00:00",
    "statutInscription": "En attente",
    "dateCreation": "2024-01-15T10:30:00",
    "eleve": {
      "idEleve": 1,
      "nom": "KABILA",
      "postnom": "Joseph",
      "prenom": "Kabila",
      "nomComplet": "KABILA Joseph Kabila",
      "genre": "M",
      "dateNaissance": "2010-05-15T00:00:00",
      "lieuNaissance": "Kinshasa",
      "nationalite": "Congolaise",
      "statut": "True"
    }
  }
}
```

## Gestion des Erreurs

La procédure stockée gère automatiquement :
- Les transactions (rollback en cas d'erreur)
- La validation des données
- Les messages d'erreur détaillés
- Le logging des erreurs (si la table ErrorLog existe)

## Avantages de cette Approche

1. **Cohérence des données**: Transactions garanties
2. **Performance**: Une seule requête pour toute l'opération
3. **Maintenance**: Logique centralisée dans la base de données
4. **Sécurité**: Validation côté serveur
5. **Flexibilité**: Facile à modifier sans redéployer l'application

## Notes Techniques

- La procédure utilise `SCOPE_IDENTITY()` pour récupérer les IDs générés
- Les statuts sont automatiquement mis à jour à "True" lors de l'activation
- La recherche de tuteur existant se fait par nom, téléphone et école
- Les adresses par défaut sont définies dans la procédure (à adapter selon vos besoins)
