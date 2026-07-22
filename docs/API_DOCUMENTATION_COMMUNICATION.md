# 📢 Documentation API - Module Communication

## Vue d'ensemble

Le module **Communication** permet aux sociétés d'envoyer des messages ciblés (push, SMS, email, in-app) à leurs clients selon différents critères de filtrage (catégorie, zone, statut actif, usage).

---

## 🔐 Authentification

**Tous les endpoints nécessitent une authentification JWT.**

**Header requis :**
```
Authorization: Bearer {votre_token_jwt}
```

---

## 📋 Endpoints

### 1. Créer une campagne de communication

**`POST /api/CommunicationCampaign`**

Crée une nouvelle campagne de communication et l'exécute immédiatement si aucune date d'envoi n'est spécifiée.

#### Request Body

```json
{
  "titre": "Avis de coupure",
  "contenu": "Avis de coupure programmée le 15 décembre 2025",
  "typeCampagne": "ALERTE",
  "idSociete": 1,
  "criteresCiblage": {
    "idCategorieClients": [2, 3],
    "zones": ["Kinshasa", "Lubumbashi"],
    "clientsActifs": true,
    "idSociete": 1,
    "usage": ["Domestique", "Commercial"],
    "listeIdClients": null
  },
  "activerPush": true,
  "activerSms": true,
  "activerEmail": true,
  "activerInApp": true,
  "dateEnvoi": "2025-12-15T10:00:00Z"
}
```

#### Paramètres

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `titre` | string | ✅ | Titre de la campagne (max 200 caractères) |
| `contenu` | string | ✅ | Contenu du message (max 2000 caractères) |
| `typeCampagne` | string | ❌ | Type de campagne (ex: "ALERTE", "INFO", "PROMOTION") |
| `idSociete` | int? | ❌ | ID de la société (optionnel) |
| `criteresCiblage` | object | ❌ | Critères de ciblage (voir ci-dessous) |
| `activerPush` | boolean | ❌ | Activer les notifications push (défaut: true) |
| `activerSms` | boolean | ❌ | Activer les SMS (défaut: false) |
| `activerEmail` | boolean | ❌ | Activer les emails (défaut: false) |
| `activerInApp` | boolean | ❌ | Activer les notifications in-app (défaut: true) |
| `dateEnvoi` | datetime? | ❌ | Date d'envoi programmé (null = envoi immédiat) |

#### Critères de ciblage (`criteresCiblage`)

| Champ | Type | Description |
|-------|------|-------------|
| `idCategorieClients` | int[] | IDs des catégories de clients à cibler |
| `zones` | string[] | Zones géographiques à cibler |
| `clientsActifs` | boolean? | Filtrer uniquement les clients actifs (IsActif = true) |
| `idSociete` | int? | ID de la société (optionnel) |
| `usage` | string[] | Types d'usage à cibler |
| `listeIdClients` | int[] | Liste spécifique d'IDs clients (si fourni, ignore les autres critères) |

**Note importante :** Seuls les clients avec `Statut = true` ET `IsActif = true` recevront la communication.

#### Response 201 Created

```json
{
  "idCampagne": 1,
  "titre": "Avis de coupure",
  "contenu": "Avis de coupure programmée le 15 décembre 2025",
  "typeCampagne": "ALERTE",
  "idSociete": 1,
  "idUtilisateurCreateur": 2,
  "criteresCiblage": "{\"IdCategorieClients\":[2,3],\"Zones\":[\"Kinshasa\",\"Lubumbashi\"],\"ClientsActifs\":true}",
  "activerPush": true,
  "activerSms": true,
  "activerEmail": true,
  "activerInApp": true,
  "dateEnvoi": "2025-12-15T10:00:00Z",
  "estProgrammee": true,
  "estEnCours": false,
  "estTerminee": false,
  "nombreDestinataires": 0,
  "nombreEnvoyes": 0,
  "nombreSucces": 0,
  "nombreEchecs": 0,
  "dateCreation": "2025-12-14T18:01:40.252314+02:00",
  "dateDerniereModification": "2025-12-14T18:01:40.252365+02:00",
  "dateEnvoiEffectif": null
}
```

#### Response 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Titre": ["Le titre de la campagne est requis"],
    "Contenu": ["Le contenu de la campagne est requis"]
  }
}
```

#### Response 401 Unauthorized

```json
{
  "message": "Utilisateur non authentifié"
}
```

---

### 2. Lister toutes les campagnes

**`GET /api/CommunicationCampaign`**

Récupère toutes les campagnes de communication.

#### Response 200 OK

```json
[
  {
    "idCampagne": 1,
    "titre": "Avis de coupure",
    "contenu": "Avis de coupure programmée",
    "typeCampagne": "ALERTE",
    "idSociete": 1,
    "idUtilisateurCreateur": 2,
    "estProgrammee": false,
    "estEnCours": false,
    "estTerminee": true,
    "nombreDestinataires": 150,
    "nombreEnvoyes": 150,
    "nombreSucces": 148,
    "nombreEchecs": 2,
    "dateCreation": "2025-12-14T18:01:40Z",
    "dateEnvoiEffectif": "2025-12-14T18:01:45Z"
  }
]
```

---

### 3. Lister les campagnes avec pagination

**`GET /api/CommunicationCampaign/paged?page=1&pageSize=10`**

Récupère les campagnes avec pagination.

#### Query Parameters

| Paramètre | Type | Requis | Description |
|-----------|------|--------|-------------|
| `page` | int | ❌ | Numéro de page (défaut: 1) |
| `pageSize` | int | ❌ | Taille de page (défaut: 10) |
| `searchTerm` | string | ❌ | Recherche dans titre et contenu |

#### Response 200 OK

```json
{
  "data": [
    {
      "idCampagne": 1,
      "titre": "Avis de coupure",
      "contenu": "Avis de coupure programmée",
      "typeCampagne": "ALERTE",
      "estTerminee": true,
      "nombreDestinataires": 150,
      "nombreSucces": 148
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

---

### 4. Récupérer une campagne par ID

**`GET /api/CommunicationCampaign/{id}`**

Récupère les détails d'une campagne spécifique.

#### Response 200 OK

```json
{
  "idCampagne": 1,
  "titre": "Avis de coupure",
  "contenu": "Avis de coupure programmée le 15 décembre 2025",
  "typeCampagne": "ALERTE",
  "idSociete": 1,
  "idUtilisateurCreateur": 2,
  "criteresCiblage": "{\"IdCategorieClients\":[2],\"ClientsActifs\":true}",
  "activerPush": true,
  "activerSms": true,
  "activerEmail": true,
  "activerInApp": true,
  "dateEnvoi": "2025-12-15T10:00:00Z",
  "estProgrammee": true,
  "estEnCours": false,
  "estTerminee": false,
  "nombreDestinataires": 150,
  "nombreEnvoyes": 0,
  "nombreSucces": 0,
  "nombreEchecs": 0,
  "dateCreation": "2025-12-14T18:01:40Z",
  "dateDerniereModification": "2025-12-14T18:01:40Z",
  "dateEnvoiEffectif": null
}
```

#### Response 404 Not Found

```json
{
  "message": "Campagne 999 introuvable"
}
```

---

### 5. Prévisualiser les clients ciblés

**`GET /api/CommunicationCampaign/{id}/preview`**

Affiche la liste des clients qui seront ciblés par la campagne (sans envoyer de notifications).

#### Response 200 OK

```json
{
  "count": 150,
  "clients": [
    {
      "idClient": 1,
      "nomClient": "Client Test",
      "emailClient": "client@example.com",
      "telephone": "+243900000001",
      "zone": "Kinshasa",
      "statut": true,
      "isActif": true
    }
  ]
}
```

---

### 6. Exécuter une campagne manuellement

**`POST /api/CommunicationCampaign/{id}/execute`**

Exécute une campagne immédiatement (même si elle est programmée).

#### Response 200 OK

```json
{
  "idCampagne": 1,
  "dateExecution": "2025-12-14T18:05:00Z",
  "nombreDestinataires": 150,
  "nombreEnvoyes": 150,
  "nombreSucces": 148,
  "nombreEchecs": 2,
  "detailsParCanal": {
    "push": { "envoyes": 150, "succes": 148, "echecs": 2 },
    "sms": { "envoyes": 0, "succes": 0, "echecs": 0 },
    "email": { "envoyes": 0, "succes": 0, "echecs": 0 },
    "inApp": { "envoyes": 150, "succes": 150, "echecs": 0 }
  },
  "messageErreur": null
}
```

---

### 7. Modifier une campagne

**`PUT /api/CommunicationCampaign/{id}`**

Modifie une campagne existante.

#### Request Body

```json
{
  "titre": "Avis de coupure - Mise à jour",
  "contenu": "Nouveau contenu",
  "typeCampagne": "ALERTE",
  "idSociete": 1,
  "criteresCiblage": {
    "idCategorieClients": [2],
    "clientsActifs": true
  },
  "activerPush": true,
  "activerSms": false,
  "activerEmail": true,
  "activerInApp": true,
  "dateEnvoi": "2025-12-16T10:00:00Z"
}
```

**Note :** Tous les champs sont optionnels. Seuls les champs fournis seront mis à jour.

#### Response 200 OK

Retourne la campagne mise à jour (même format que GET /api/CommunicationCampaign/{id}).

---

### 8. Supprimer une campagne

**`DELETE /api/CommunicationCampaign/{id}`**

**⚠️ Nécessite le rôle `Super-Admin` ou `Admin`**

Supprime une campagne de communication.

#### Response 204 No Content

Aucun contenu retourné.

#### Response 404 Not Found

```json
{
  "message": "Campagne 999 introuvable"
}
```

---

## 📊 Statuts des campagnes

| Statut | Description |
|--------|-------------|
| `estProgrammee` | La campagne a une date d'envoi future |
| `estEnCours` | La campagne est en cours d'exécution |
| `estTerminee` | La campagne a terminé son exécution |

---

## 🔔 Canaux de communication

Les campagnes peuvent utiliser 4 canaux :

1. **Push** : Notifications push via FCM (Firebase Cloud Messaging)
2. **SMS** : SMS via Twilio
3. **Email** : Emails via le service email
4. **In-App** : Notifications in-app via SignalR

**Note :** Au moins un canal doit être activé.

---

## 📝 Exemples d'utilisation

### Exemple 1 : Envoi immédiat à tous les clients actifs

```json
{
  "titre": "Maintenance programmée",
  "contenu": "Une maintenance est prévue demain de 10h à 12h",
  "typeCampagne": "INFO",
  "criteresCiblage": {
    "clientsActifs": true
  },
  "activerPush": true,
  "activerInApp": true,
  "activerSms": false,
  "activerEmail": false
}
```

### Exemple 2 : Campagne programmée pour une catégorie spécifique

```json
{
  "titre": "Promotion spéciale",
  "contenu": "Réduction de 20% pour les clients commerciaux",
  "typeCampagne": "PROMOTION",
  "criteresCiblage": {
    "idCategorieClients": [3],
    "clientsActifs": true
  },
  "activerPush": true,
  "activerSms": true,
  "activerEmail": true,
  "activerInApp": true,
  "dateEnvoi": "2025-12-20T08:00:00Z"
}
```

### Exemple 3 : Envoi à une liste spécifique de clients

```json
{
  "titre": "Invitation événement",
  "contenu": "Vous êtes invité à notre événement",
  "typeCampagne": "INFO",
  "criteresCiblage": {
    "listeIdClients": [1, 5, 10, 15, 20]
  },
  "activerPush": true,
  "activerInApp": true
}
```

### Exemple 4 : Envoi par zone géographique

```json
{
  "titre": "Coupure prévue",
  "contenu": "Coupure d'électricité prévue dans votre zone",
  "typeCampagne": "ALERTE",
  "criteresCiblage": {
    "zones": ["Kinshasa", "Gombe"],
    "clientsActifs": true
  },
  "activerPush": true,
  "activerSms": true,
  "activerInApp": true
}
```

---

## ⚠️ Codes d'erreur

| Code | Description | Solution |
|------|-------------|----------|
| 400 | Erreur de validation | Vérifier les champs requis et leur format |
| 401 | Non authentifié | Vérifier le token JWT dans le header Authorization |
| 403 | Accès refusé | Vérifier les permissions de l'utilisateur |
| 404 | Campagne introuvable | Vérifier l'ID de la campagne |
| 500 | Erreur serveur | Consulter les logs serveur |

---

## 🔍 Notes importantes

1. **Filtrage automatique** : Seuls les clients avec `Statut = true` ET `IsActif = true` sont ciblés, même si `clientsActifs` n'est pas spécifié.

2. **Exécution automatique** : Si `dateEnvoi` est null ou dans le passé, la campagne s'exécute immédiatement en arrière-plan.

3. **Priorité des critères** : Si `listeIdClients` est fourni, tous les autres critères sont ignorés.

4. **Statistiques** : Les statistiques (`nombreDestinataires`, `nombreEnvoyes`, etc.) sont mises à jour après l'exécution.

5. **Canal SMS** : L'envoi SMS nécessite une configuration Twilio valide.

6. **Canal Email** : L'envoi d'email nécessite une configuration SMTP valide.

---

## 📞 Support

Pour toute question ou problème, contactez l'équipe backend.

**Version API :** 2.0  
**Dernière mise à jour :** 14 décembre 2025

