# ⚡ Documentation API - Module Panne Signalement

## Vue d'ensemble

Le module **Panne Signalement** permet aux clients de signaler des pannes ou problèmes techniques sur le réseau électrique. Ces signalements peuvent ensuite être liés à des plaintes clients.

---

## 🔐 Authentification

**Tous les endpoints nécessitent une authentification JWT.**

**Header requis :**
```
Authorization: Bearer {votre_token_jwt}
```

---

## 📋 Endpoints

### 1. Créer un signalement de panne

**`POST /api/PanneSignalement`**

Crée un nouveau signalement de panne.

#### Request Body

```json
{
  "description": "Coupure d'électricité dans le quartier depuis ce matin",
  "typePanne": "Coupure totale",
  "niveauImportance": "Critique",
  "risquesPrincipaux": "Risque d'incendie, perte de données"
}
```

#### Paramètres

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `description` | string | ✅ | Description détaillée de la panne (max 2000 caractères) |
| `typePanne` | string | ❌ | Type de panne (max 200 caractères) |
| `niveauImportance` | string | ❌ | Niveau d'importance ("Faible", "Moyen", "Élevé", "Critique") |
| `risquesPrincipaux` | string | ❌ | Risques principaux identifiés (max 500 caractères) |

#### Response 201 Created

```json
{
  "idPanneSignalement": 1,
  "description": "Coupure d'électricité dans le quartier depuis ce matin",
  "statut": true,
  "typePanne": "Coupure totale",
  "niveauImportance": "Critique",
  "risquesPrincipaux": "Risque d'incendie, perte de données"
}
```

#### Response 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Description": ["La description est requise"]
  }
}
```

---

### 2. Lister tous les signalements

**`GET /api/PanneSignalement`**

Récupère tous les signalements de panne.

#### Response 200 OK

```json
[
  {
    "idPanneSignalement": 1,
    "description": "Coupure d'électricité dans le quartier",
    "statut": true,
    "typePanne": "Coupure totale",
    "niveauImportance": "Critique",
    "risquesPrincipaux": "Risque d'incendie"
  },
  {
    "idPanneSignalement": 2,
    "description": "Surtension dans le réseau",
    "statut": false,
    "typePanne": "Surtension",
    "niveauImportance": "Élevé",
    "risquesPrincipaux": null
  }
]
```

---

### 3. Lister les signalements avec pagination et filtres

**`GET /api/PanneSignalement/paged?page=1&pageSize=10&statut=true&searchTerm=coupure`**

Récupère les signalements avec pagination et filtres.

#### Query Parameters

| Paramètre | Type | Requis | Description |
|-----------|------|--------|-------------|
| `page` | int | ❌ | Numéro de page (défaut: 1) |
| `pageSize` | int | ❌ | Taille de page (défaut: 10) |
| `statut` | boolean | ❌ | Filtrer par statut (true = actif/ouvert, false = clôturé) |
| `searchTerm` | string | ❌ | Recherche dans description, typePanne, niveauImportance |

#### Response 200 OK

```json
{
  "data": [
    {
      "idPanneSignalement": 1,
      "description": "Coupure d'électricité dans le quartier",
      "statut": true,
      "typePanne": "Coupure totale",
      "niveauImportance": "Critique"
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

---

### 4. Récupérer un signalement par ID

**`GET /api/PanneSignalement/{id}`**

Récupère les détails d'un signalement spécifique.

#### Response 200 OK

```json
{
  "idPanneSignalement": 1,
  "description": "Coupure d'électricité dans le quartier depuis ce matin",
  "statut": true,
  "typePanne": "Coupure totale",
  "niveauImportance": "Critique",
  "risquesPrincipaux": "Risque d'incendie, perte de données"
}
```

#### Response 404 Not Found

```json
{
  "message": "Signalement 999 introuvable"
}
```

---

### 5. Modifier un signalement

**`PUT /api/PanneSignalement/{id}`**

Modifie un signalement existant.

#### Request Body

```json
{
  "description": "Coupure d'électricité - Mise à jour : problème résolu",
  "typePanne": "Coupure totale",
  "niveauImportance": "Faible",
  "risquesPrincipaux": "Aucun risque",
  "statut": false
}
```

**Note :** Tous les champs sont optionnels. Seuls les champs fournis seront mis à jour.

#### Response 200 OK

Retourne le signalement mis à jour.

---

### 6. Supprimer un signalement

**`DELETE /api/PanneSignalement/{id}`**

**⚠️ Nécessite le rôle `Super-Admin` ou `Admin`**

Supprime un signalement de panne.

#### Response 204 No Content

Aucun contenu retourné.

#### Response 404 Not Found

```json
{
  "message": "Signalement 999 introuvable"
}
```

---

## 📊 Statut des signalements

| Statut | Description |
|--------|-------------|
| `true` | Signalement actif/ouvert |
| `false` | Signalement clôturé/résolu |

---

## 🎯 Niveaux d'importance

| Niveau | Description |
|--------|-------------|
| `Faible` | Impact minimal, peut attendre |
| `Moyen` | Impact modéré, traitement standard |
| `Élevé` | Impact important, traitement prioritaire |
| `Critique` | Impact majeur, traitement urgent |

---

## 🔗 Relation avec PlainteClient

Un signalement de panne peut être lié à une ou plusieurs plaintes clients via le champ `idPanneSignalement` dans le modèle `PlainteClient`.

**Exemple :**
```json
{
  "idClient": 1,
  "idPanneSignalement": 5,  // Lien vers le signalement
  "titre": "Panne non résolue",
  "description": "La panne signalée n'est toujours pas résolue"
}
```

---

## 📝 Exemples d'utilisation

### Exemple 1 : Signalement de coupure

```json
{
  "description": "Coupure d'électricité complète dans le quartier depuis 2 heures",
  "typePanne": "Coupure totale",
  "niveauImportance": "Critique",
  "risquesPrincipaux": "Risque d'incendie, perte de données, interruption des services"
}
```

### Exemple 2 : Signalement de surtension

```json
{
  "description": "Surtension détectée dans le réseau, plusieurs appareils endommagés",
  "typePanne": "Surtension",
  "niveauImportance": "Élevé",
  "risquesPrincipaux": "Dommages aux équipements électriques"
}
```

### Exemple 3 : Signalement de panne partielle

```json
{
  "description": "Panne partielle dans une zone spécifique",
  "typePanne": "Panne partielle",
  "niveauImportance": "Moyen",
  "risquesPrincipaux": null
}
```

### Exemple 4 : Clôturer un signalement

```json
{
  "statut": false
}
```

---

## 🔍 Filtrage par défaut

Par défaut, les endpoints de liste retournent uniquement les signalements avec `statut = true` (actifs/ouverts).

Pour inclure les signalements clôturés, utilisez le paramètre de requête :
```
GET /api/PanneSignalement/paged?statut=false
```

Pour obtenir tous les signalements (actifs et clôturés), ne spécifiez pas le paramètre `statut` ou utilisez une pagination complète.

---

## ⚠️ Codes d'erreur

| Code | Description | Solution |
|------|-------------|----------|
| 400 | Erreur de validation | Vérifier les champs requis et leur format |
| 401 | Non authentifié | Vérifier le token JWT dans le header Authorization |
| 403 | Accès refusé | Vérifier les permissions de l'utilisateur |
| 404 | Signalement introuvable | Vérifier l'ID du signalement |
| 500 | Erreur serveur | Consulter les logs serveur |

---

## 🔍 Notes importantes

1. **Statut par défaut** : Les nouveaux signalements sont créés avec `statut = true` (actif/ouvert).

2. **Clôture** : Pour clôturer un signalement, modifiez le `statut` à `false`.

3. **Lien avec PlainteClient** : Un signalement peut être référencé dans plusieurs plaintes clients.

4. **Filtrage automatique** : Par défaut, seuls les signalements actifs (`statut = true`) sont retournés dans les listes.

5. **Recherche** : La recherche textuelle fonctionne sur `description`, `typePanne`, et `niveauImportance`.

---

## 📞 Support

Pour toute question ou problème, contactez l'équipe backend.

**Version API :** 2.0  
**Dernière mise à jour :** 14 décembre 2025

