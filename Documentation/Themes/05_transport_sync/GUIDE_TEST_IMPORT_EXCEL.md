# 📋 Guide de Test : Import Excel Clients

## 🎯 Objectif

Ce guide permet de tester le service d'import Excel pour les clients après les refactorisations :
- Relation many-to-many Client-CategorieClient
- Déplacement de Usage vers CategorieClient

---

## ✅ Tests à Effectuer

### 1. Test du Template Excel

**Endpoint** : `GET /api/Client/template-excel`

**Actions** :
1. Télécharger le template
2. Vérifier que :
   - ✅ La ligne d'instructions est présente en haut
   - ✅ Les instructions expliquent le format des catégories multiples
   - ✅ La colonne `Usage` n'existe plus
   - ✅ La colonne `NomCategorieClient` est présente
   - ✅ Deux exemples de données sont fournis (une catégorie et plusieurs catégories)

**Résultat attendu** : Template avec instructions claires et exemples

---

### 2. Test Import avec Une Catégorie

**Endpoint** : `POST /api/Client/bulk-excel?idSociete={id}`

**Fichier Excel** :
```
NomClient | AdresseClient | Telephone | EmailClient | Zone | GenreClient | NumeroCompteur | NomCategorieClient
MUKENDI   | 123 Avenue X  | +243...   | test@...    | Gombe| M           | COMP001        | Standard
```

**Résultat attendu** :
- ✅ Client créé avec succès
- ✅ Catégorie "Standard" assignée au client
- ✅ Relation créée dans `ClientCategorieClients`
- ✅ `IdCategorieClient` défini (compatibilité)

---

### 3. Test Import avec Plusieurs Catégories (Virgule)

**Fichier Excel** :
```
NomClient | AdresseClient | Telephone | EmailClient | Zone | GenreClient | NumeroCompteur | NomCategorieClient
KABONGO   | 456 Avenue Y  | +243...   | test2@...   | Lingwala | F | COMP002 | Standard, VIP
```

**Résultat attendu** :
- ✅ Client créé avec succès
- ✅ Deux catégories assignées : "Standard" et "VIP"
- ✅ Relations créées dans `ClientCategorieClients`
- ✅ Message : "Client créé avec succès (2 catégorie(s) assignée(s))"

---

### 4. Test Import avec Plusieurs Catégories (Point-virgule)

**Fichier Excel** :
```
NomClient | AdresseClient | Telephone | EmailClient | Zone | GenreClient | NumeroCompteur | NomCategorieClient
KASENGA   | 789 Avenue Z  | +243...   | test3@...   | Kalamu | M | COMP003 | Standard; VIP; Premium
```

**Résultat attendu** :
- ✅ Client créé avec succès
- ✅ Trois catégories assignées
- ✅ Relations créées dans `ClientCategorieClients`

---

### 5. Test Catégorie Inexistante

**Fichier Excel** :
```
NomClient | AdresseClient | Telephone | EmailClient | Zone | GenreClient | NumeroCompteur | NomCategorieClient
TEST      | 111 Test St   | +243...   | test4@...   | Test | M | COMP004 | CategorieInexistante
```

**Résultat attendu** :
- ❌ Erreur avec message détaillé
- ✅ Suggestion de catégories similaires (si disponibles)
- ✅ Liste des catégories disponibles (si aucune suggestion)

**Message attendu** :
```
La catégorie 'CategorieInexistante' n'existe pas pour cette société. 
Catégories disponibles : Standard, VIP, Premium, ...
```

---

### 6. Test Catégorie Partiellement Valide

**Fichier Excel** :
```
NomClient | AdresseClient | Telephone | EmailClient | Zone | GenreClient | NumeroCompteur | NomCategorieClient
TEST2     | 222 Test St   | +243...   | test5@...   | Test | M | COMP005 | Standard, CategorieInexistante
```

**Résultat attendu** :
- ❌ Erreur pour la catégorie inexistante
- ⚠️ La catégorie "Standard" est quand même assignée (si validation permet)
- ✅ Message d'erreur détaillé

---

### 7. Test Fichier Sans Instructions (Compatibilité)

**Fichier Excel** (ancien format, sans ligne d'instructions) :
```
NomClient | AdresseClient | Telephone | EmailClient | Zone | GenreClient | NumeroCompteur | NomCategorieClient
OLD       | 333 Old St     | +243...   | old@...     | Old  | M | COMP006 | Standard
```

**Résultat attendu** :
- ✅ Fichier lu correctement
- ✅ Client créé avec succès
- ✅ Compatible avec les anciens fichiers

---

### 8. Test Validation des Données

**Fichier Excel avec erreurs** :
```
NomClient | AdresseClient | Telephone | EmailClient | Zone | GenreClient | NumeroCompteur | NomCategorieClient
          | 444 Empty St   | +243...   | test7@...   | Test | M | COMP007 | Standard
LONG      | [500+ chars]   | +243...   | test8@...   | Test | M | COMP008 | Standard
TEST3     | 555 Test St    | invalid   | invalid@   | Test | X | COMP009 | Standard
```

**Résultat attendu** :
- ❌ Erreurs de validation pour chaque ligne
- ✅ Messages d'erreur clairs :
  - "Le nom du client est obligatoire"
  - "L'adresse du client ne peut pas dépasser 500 caractères"
  - "Le format du téléphone n'est pas valide"
  - "L'email du client n'est pas valide"
  - "Le genre du client doit être M ou F"

---

### 9. Test Doublons dans le Fichier

**Fichier Excel** :
```
NomClient | AdresseClient | Telephone | EmailClient | Zone | GenreClient | NumeroCompteur | NomCategorieClient
DOUBLON   | 666 Dup St     | +243...   | dup@...     | Test | M | COMP010 | Standard
DOUBLON   | 777 Dup St     | +243...   | dup2@...    | Test | F | COMP010 | VIP
```

**Résultat attendu** :
- ❌ Erreur de doublon détectée
- ✅ Message : "Doublon détecté dans le fichier (même nom et/ou numéro de compteur déjà traité)"

---

### 10. Test Client Existant dans la Base

**Fichier Excel** :
```
NomClient | AdresseClient | Telephone | EmailClient | Zone | GenreClient | NumeroCompteur | NomCategorieClient
EXISTANT  | 888 Exist St   | +243...   | exist@...   | Test | M | COMP001 | Standard
```

**Résultat attendu** :
- ❌ Erreur : client existant détecté
- ✅ Message : "Un client avec ce nom ou numéro de compteur existe déjà (ID: {id})"

---

## 🔍 Vérifications Post-Import

Après chaque import réussi, vérifier dans la base de données :

### 1. Table `Clients`
```sql
SELECT * FROM Clients WHERE NomClient = 'MUKENDI';
```
- ✅ Client créé
- ✅ `IdCategorieClient` défini (catégorie principale)

### 2. Table `ClientCategorieClients`
```sql
SELECT * FROM ClientCategorieClients WHERE IdClient = {idClient};
```
- ✅ Relations créées pour toutes les catégories
- ✅ `DateAttribution` définie

### 3. Vérification via API
```http
GET /api/Client/{id}/categories
```
- ✅ Retourne toutes les catégories du client

---

## 📊 Résultats Attendus

### Format de Réponse

```json
{
  "success": true,
  "message": "Traitement terminé : 5 client(s) créé(s) sur 10 ligne(s), 5 échouée(s)",
  "totalLignes": 10,
  "lignesReussies": 5,
  "lignesEchouees": 5,
  "doublonsDetectes": 0,
  "lignesAvecErreurs": [
    {
      "numeroLigne": 3,
      "nomClient": "TEST",
      "erreurs": [
        "La catégorie 'CategorieInexistante' n'existe pas pour cette société. Catégories disponibles : Standard, VIP"
      ]
    }
  ],
  "clientsCrees": [
    {
      "success": true,
      "idClient": 123,
      "nomClient": "MUKENDI",
      "numeroCompteur": "COMP001",
      "message": "Client créé avec succès (1 catégorie(s) assignée(s))"
    }
  ],
  "dateTraitement": "2025-12-20T15:30:00Z"
}
```

---

## ⚠️ Points d'Attention

1. **Catégories multiples** : Vérifier que toutes les catégories sont bien assignées
2. **Messages d'erreur** : Vérifier que les suggestions de catégories fonctionnent
3. **Compatibilité** : Vérifier que les anciens fichiers (sans instructions) fonctionnent toujours
4. **Performance** : Pour de gros fichiers (1000+ lignes), vérifier les temps de traitement

---

## 🐛 Cas de Test Edge

1. **Fichier vide** : Doit retourner une erreur claire
2. **Fichier avec seulement des lignes vides** : Doit être ignoré
3. **Catégories avec espaces** : `"Standard , VIP"` doit fonctionner (trim)
4. **Catégories vides** : `"Standard, , VIP"` doit ignorer les vides
5. **Caractères spéciaux** : Vérifier l'encodage UTF-8

---

## ✅ Checklist de Validation

- [ ] Template téléchargeable et lisible
- [ ] Instructions présentes dans le template
- [ ] Import avec une catégorie fonctionne
- [ ] Import avec plusieurs catégories (virgule) fonctionne
- [ ] Import avec plusieurs catégories (point-virgule) fonctionne
- [ ] Messages d'erreur pour catégories inexistantes sont informatifs
- [ ] Suggestions de catégories fonctionnent
- [ ] Compatibilité avec anciens fichiers
- [ ] Validations des champs fonctionnent
- [ ] Détection de doublons fonctionne
- [ ] Relations many-to-many créées correctement
- [ ] Pas de colonne Usage dans le template
- [ ] Performance acceptable pour gros fichiers

---

## 📝 Notes

- Les catégories sont maintenant liées à la catégorie, pas au client
- L'usage est automatiquement hérité de la catégorie
- Si un client a plusieurs catégories avec des usages différents, l'usage de la catégorie principale est utilisé pour la compatibilité
