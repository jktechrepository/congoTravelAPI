# 📝 Documentation API - Module Plainte Client

## Vue d'ensemble

Le module **Plainte Client** permet aux clients de signaler des problèmes ou des plaintes à l'équipe d'intervention de la société. Les plaintes peuvent être liées à un signalement de panne existant ou être des plaintes générales.

---

## 🔐 Authentification

**Tous les endpoints nécessitent une authentification JWT.**

**Header requis :**
```
Authorization: Bearer {votre_token_jwt}
```

---

## 📋 Endpoints

### 1. Créer une plainte (Client)

**`POST /api/PlainteClient`**

Permet à un client de créer une plainte. L'utilisateur connecté est automatiquement associé comme créateur.

#### Request Body

```json
{
  "idClient": 1,
  "idPanneSignalement": 2,
  "titre": "Problème de facturation",
  "description": "Ma facture du mois dernier est incorrecte",
  "typePanne": "Facturation",
  "niveauImportance": "Élevé",
  "risquesPrincipaux": "Risque de coupure",
  "priorite": "Urgente",
  "estUrgente": true
}
```

#### Paramètres

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `idClient` | int | ✅ | ID du client qui dépose la plainte |
| `idPanneSignalement` | int? | ❌ | ID du signalement de panne lié (optionnel) |
| `titre` | string | ✅ | Titre de la plainte (max 200 caractères) |
| `description` | string | ❌ | Description détaillée (max 2000 caractères) |
| `typePanne` | string | ❌ | Type de panne/problème (max 200 caractères) |
| `niveauImportance` | string | ❌ | Niveau d'importance (ex: "Faible", "Moyen", "Élevé", "Critique") |
| `risquesPrincipaux` | string | ❌ | Risques principaux identifiés (max 500 caractères) |
| `priorite` | string | ❌ | Priorité (ex: "Faible", "Moyenne", "Élevée", "Urgente") |
| `estUrgente` | boolean | ❌ | Marquer comme urgente (défaut: false) |

#### Response 201 Created

```json
{
  "idPlainte": 1,
  "idClient": 1,
  "idPanneSignalement": 2,
  "titre": "Problème de facturation",
  "description": "Ma facture du mois dernier est incorrecte",
  "typePanne": "Facturation",
  "niveauImportance": "Élevé",
  "risquesPrincipaux": "Risque de coupure",
  "statutPlainte": "En attente",
  "priorite": "Urgente",
  "idAgentAssigné": null,
  "idUtilisateurCreateur": 2,
  "commentaireResolution": null,
  "dateResolution": null,
  "estUrgente": true,
  "dateCreation": "2025-12-14T18:30:00Z",
  "dateDerniereModification": "2025-12-14T18:30:00Z"
}
```

#### Response 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Titre": ["Le titre est requis"],
    "IdClient": ["L'ID du client est requis"]
  }
}
```

---

### 2. Récupérer mes plaintes (Client)

**`GET /api/PlainteClient/mes-plaintes`**

Récupère toutes les plaintes du client connecté.

#### Response 200 OK

```json
[
  {
    "idPlainte": 1,
    "idClient": 1,
    "titre": "Problème de facturation",
    "description": "Ma facture du mois dernier est incorrecte",
    "statutPlainte": "En attente",
    "priorite": "Urgente",
    "estUrgente": true,
    "dateCreation": "2025-12-14T18:30:00Z"
  },
  {
    "idPlainte": 2,
    "idClient": 1,
    "titre": "Coupure d'électricité",
    "statutPlainte": "En cours",
    "priorite": "Élevée",
    "dateCreation": "2025-12-13T10:00:00Z"
  }
]
```

---

### 3. Récupérer une plainte par ID

**`GET /api/PlainteClient/{id}`**

Récupère les détails d'une plainte spécifique.

**Note :** Un client ne peut voir que ses propres plaintes. Les admins/agents peuvent voir toutes les plaintes.

#### Response 200 OK

```json
{
  "idPlainte": 1,
  "idClient": 1,
  "idPanneSignalement": 2,
  "titre": "Problème de facturation",
  "description": "Ma facture du mois dernier est incorrecte",
  "typePanne": "Facturation",
  "niveauImportance": "Élevé",
  "risquesPrincipaux": "Risque de coupure",
  "statutPlainte": "En attente",
  "priorite": "Urgente",
  "idAgentAssigné": null,
  "idUtilisateurCreateur": 2,
  "commentaireResolution": null,
  "dateResolution": null,
  "estUrgente": true,
  "dateCreation": "2025-12-14T18:30:00Z",
  "dateDerniereModification": "2025-12-14T18:30:00Z",
  "client": {
    "idClient": 1,
    "nomClient": "Client Test"
  },
  "panneSignalement": {
    "idPanneSignalement": 2,
    "description": "Panne signalée précédemment"
  }
}
```

#### Response 403 Forbidden

```json
{
  "message": "Accès refusé : Vous ne pouvez voir que vos propres plaintes"
}
```

#### Response 404 Not Found

```json
{
  "message": "Plainte 999 introuvable"
}
```

---

### 4. Lister toutes les plaintes (Équipe d'intervention)

**`GET /api/PlainteClient`**

Récupère toutes les plaintes (réservé aux admins/agents).

#### Response 200 OK

```json
[
  {
    "idPlainte": 1,
    "idClient": 1,
    "titre": "Problème de facturation",
    "statutPlainte": "En attente",
    "priorite": "Urgente",
    "estUrgente": true,
    "dateCreation": "2025-12-14T18:30:00Z"
  }
]
```

---

### 5. Lister les plaintes avec pagination et filtres

**`GET /api/PlainteClient/paged?page=1&pageSize=10&statut=En attente&priorite=Urgente&idAgent=1&searchTerm=facturation`**

Récupère les plaintes avec pagination et filtres avancés.

#### Query Parameters

| Paramètre | Type | Requis | Description |
|-----------|------|--------|-------------|
| `page` | int | ❌ | Numéro de page (défaut: 1) |
| `pageSize` | int | ❌ | Taille de page (défaut: 10) |
| `statut` | string | ❌ | Filtrer par statut ("En attente", "En cours", "Résolu", "Fermé") |
| `priorite` | string | ❌ | Filtrer par priorité ("Faible", "Moyenne", "Élevée", "Urgente") |
| `idAgent` | int | ❌ | Filtrer par agent assigné |
| `idClient` | int | ❌ | Filtrer par client |
| `estUrgente` | boolean | ❌ | Filtrer les plaintes urgentes |
| `searchTerm` | string | ❌ | Recherche dans titre, description, typePanne |

#### Response 200 OK

```json
{
  "data": [
    {
      "idPlainte": 1,
      "idClient": 1,
      "titre": "Problème de facturation",
      "statutPlainte": "En attente",
      "priorite": "Urgente",
      "estUrgente": true,
      "dateCreation": "2025-12-14T18:30:00Z"
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

---

### 6. Récupérer les plaintes en attente

**`GET /api/PlainteClient/en-attente`**

Récupère uniquement les plaintes avec le statut "En attente", triées par urgence et date de création.

#### Response 200 OK

```json
[
  {
    "idPlainte": 1,
    "idClient": 1,
    "titre": "Problème de facturation",
    "statutPlainte": "En attente",
    "priorite": "Urgente",
    "estUrgente": true,
    "dateCreation": "2025-12-14T18:30:00Z"
  }
]
```

---

### 7. Récupérer les plaintes assignées à un agent

**`GET /api/PlainteClient/assignees/{idAgent}`**

Récupère toutes les plaintes assignées à un agent spécifique.

#### Response 200 OK

```json
[
  {
    "idPlainte": 1,
    "idClient": 1,
    "titre": "Problème de facturation",
    "statutPlainte": "En cours",
    "priorite": "Urgente",
    "idAgentAssigné": 1,
    "dateCreation": "2025-12-14T18:30:00Z"
  }
]
```

---

### 8. Modifier une plainte

**`PUT /api/PlainteClient/{id}`**

Modifie une plainte existante.

**Note :** Un client ne peut modifier que ses propres plaintes avec le statut "En attente". Les admins/agents peuvent modifier toutes les plaintes.

#### Request Body

```json
{
  "titre": "Problème de facturation - Mise à jour",
  "description": "Description mise à jour",
  "typePanne": "Facturation",
  "niveauImportance": "Critique",
  "risquesPrincipaux": "Risque de coupure immédiate",
  "priorite": "Urgente",
  "estUrgente": true
}
```

**Note :** Tous les champs sont optionnels. Seuls les champs fournis seront mis à jour.

#### Response 200 OK

Retourne la plainte mise à jour.

---

### 9. Assigner un agent à une plainte

**`PATCH /api/PlainteClient/{id}/assigner`**

**Réservé aux admins/agents**

Assigne un agent à une plainte et change automatiquement le statut à "En cours".

#### Request Body

```json
{
  "idAgentAssigné": 1
}
```

#### Response 200 OK

```json
{
  "idPlainte": 1,
  "idAgentAssigné": 1,
  "statutPlainte": "En cours",
  "dateDerniereModification": "2025-12-14T19:00:00Z"
}
```

---

### 10. Changer le statut d'une plainte

**`PATCH /api/PlainteClient/{id}/statut`**

**Réservé aux admins/agents**

Change le statut d'une plainte.

#### Request Body

```json
{
  "statutPlainte": "En cours"
}
```

#### Statuts possibles

- `"En attente"` : Plainte créée, en attente de traitement
- `"En cours"` : Plainte prise en charge par un agent
- `"Résolu"` : Plainte résolue
- `"Fermé"` : Plainte fermée

#### Response 200 OK

Retourne la plainte avec le nouveau statut.

---

### 11. Résoudre une plainte

**`PATCH /api/PlainteClient/{id}/resoudre`**

**Réservé aux admins/agents**

Marque une plainte comme résolue avec un commentaire de résolution.

#### Request Body

```json
{
  "commentaireResolution": "Problème résolu. Facture corrigée et crédit appliqué."
}
```

#### Response 200 OK

```json
{
  "idPlainte": 1,
  "statutPlainte": "Résolu",
  "commentaireResolution": "Problème résolu. Facture corrigée et crédit appliqué.",
  "dateResolution": "2025-12-14T20:00:00Z",
  "dateDerniereModification": "2025-12-14T20:00:00Z"
}
```

---

### 12. Supprimer une plainte

**`DELETE /api/PlainteClient/{id}`**

**⚠️ Nécessite le rôle `Super-Admin` ou `Admin`**

Supprime une plainte.

#### Response 204 No Content

Aucun contenu retourné.

#### Response 404 Not Found

```json
{
  "message": "Plainte 999 introuvable"
}
```

---

## 📊 Statuts des plaintes

| Statut | Description | Peut être changé par |
|--------|-------------|----------------------|
| `En attente` | Plainte créée, en attente de traitement | Client (modification), Admin/Agent (assignation) |
| `En cours` | Plainte prise en charge par un agent | Admin/Agent |
| `Résolu` | Plainte résolue avec commentaire | Admin/Agent |
| `Fermé` | Plainte fermée | Admin/Agent |

---

## 🎯 Priorités

| Priorité | Description |
|----------|-------------|
| `Faible` | Problème mineur, peut attendre |
| `Moyenne` | Problème normal, traitement standard |
| `Élevée` | Problème important, traitement prioritaire |
| `Urgente` | Problème critique, traitement immédiat |

---

## 🔔 Notifications

Lors de la création d'une plainte, l'équipe d'intervention est automatiquement notifiée via :
- **Push notifications** : Tous les agents actifs
- **Notifications in-app** : Agents avec les rôles "Technicien", "Intervention", "Agent"

---

## 📝 Exemples d'utilisation

### Exemple 1 : Plainte simple (sans panne liée)

```json
{
  "idClient": 1,
  "titre": "Service client insatisfaisant",
  "description": "Le service client n'a pas répondu à mes questions",
  "typePanne": "Service",
  "priorite": "Moyenne",
  "estUrgente": false
}
```

### Exemple 2 : Plainte liée à un signalement de panne

```json
{
  "idClient": 1,
  "idPanneSignalement": 5,
  "titre": "Panne non résolue",
  "description": "La panne signalée il y a 3 jours n'est toujours pas résolue",
  "typePanne": "Réseau électrique",
  "niveauImportance": "Critique",
  "risquesPrincipaux": "Risque d'incendie",
  "priorite": "Urgente",
  "estUrgente": true
}
```

### Exemple 3 : Plainte urgente

```json
{
  "idClient": 1,
  "titre": "Coupure d'urgence",
  "description": "Coupure d'électricité depuis ce matin",
  "typePanne": "Coupure",
  "priorite": "Urgente",
  "estUrgente": true
}
```

---

## ⚠️ Codes d'erreur

| Code | Description | Solution |
|------|-------------|----------|
| 400 | Erreur de validation | Vérifier les champs requis et leur format |
| 401 | Non authentifié | Vérifier le token JWT dans le header Authorization |
| 403 | Accès refusé | Vérifier les permissions (client ne peut voir que ses plaintes) |
| 404 | Plainte introuvable | Vérifier l'ID de la plainte |
| 500 | Erreur serveur | Consulter les logs serveur |

---

## 🔍 Notes importantes

1. **Création automatique** : L'`idUtilisateurCreateur` est automatiquement rempli avec l'ID de l'utilisateur connecté.

2. **Lien avec PanneSignalement** : Si `idPanneSignalement` est fourni, la plainte est liée au signalement. Aucune création automatique de `PanneSignalement` n'est effectuée.

3. **Assignation d'agent** : Aucune assignation automatique d'agent n'est effectuée. L'assignation doit être faite manuellement par un admin/agent.

4. **Notifications** : Les notifications sont envoyées uniquement à la création de la plainte, pas lors des modifications.

5. **Filtrage** : Les clients ne peuvent voir que leurs propres plaintes. Les admins/agents peuvent voir toutes les plaintes.

---

## 📞 Support

Pour toute question ou problème, contactez l'équipe backend.

**Version API :** 2.0  
**Dernière mise à jour :** 14 décembre 2025

