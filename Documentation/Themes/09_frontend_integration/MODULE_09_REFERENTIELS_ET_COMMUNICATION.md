# MODULE 09 — Référentiels et communication

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)
>
> Persona principal : **back-office Vue.js admin**

---

## Société et site

| Ressource | Routes clés |
|-----------|-------------|
| Société | `GET/POST/PUT /api/Societe`, `GET /api/Societe/{id}` |
| Site | `GET/POST/PUT /api/Site`, `GET /api/Site/societe/{idSociete}` |
| Config société | `GET/PUT /api/Societe/{id}/config` |

Bootstrap création société : [`DOCUMENTATION_API_SOCIETE_CREATE_BOOTSTRAP.md`](../04_clients_referentiels/DOCUMENTATION_API_SOCIETE_CREATE_BOOTSTRAP.md)

---

## Agents

| Méthode | Route |
|---------|-------|
| GET | `/api/Agent/societe/{idSociete}` |
| POST | `/api/Agent` |
| PUT | `/api/Agent/{id}` |
| PUT | `/api/Agent/{idAgent}/site` | Affectation site |
| PUT | `/api/Agent/{idAgent}/serial-number` | Device agent |

### Affectation agent → site (Vue)

```js
await api.put(`/Agent/${idAgent}/site`, { idSite: 5 });
```

Validation : `site.IdSociete == agent.IdSociete`.

---

## Rôles et permissions

| Route | Description |
|-------|-------------|
| `GET /api/Role` | Liste rôles |
| `GET /api/Permission` | Liste permissions |
| `POST /api/Agent/{id}/add-role` | Ajouter rôle agent |
| `PUT /api/Agent/{id}/replace-role` | Remplacer rôles |

---

## Clients (admin)

| Route | Description |
|-------|-------------|
| `GET /api/Client/search` | Recherche multi-critères |
| `GET /api/Client/societe/{idSociete}/paged` | Liste paginée |
| `POST /api/Client` | Création admin |
| `GET /api/Client/export` | Export Excel |

---

## Campagnes communication

```
POST   /api/CommunicationCampaign
GET    /api/CommunicationCampaign
GET    /api/CommunicationCampaign/{id}
POST   /api/CommunicationCampaign/{id}/execute
GET    /api/CommunicationCampaign/{id}/preview
```

### Créer une campagne

```json
{
  "titre": "Promotion été",
  "contenu": "Réduction sur les trajets Kinshasa-Matadi",
  "typeCampagne": "INFO",
  "criteresCiblage": {
    "zones": ["Kinshasa"],
    "clientsActifs": true
  },
  "activerPush": true,
  "activerSms": false,
  "activerEmail": true,
  "activerInApp": true
}
```

---

## Préférences notifications

```
GET  /api/NotificationPreference
PUT  /api/NotificationPreference
```

Permet au client/agent de configurer les canaux (push, email, SMS).

---

## Info paiement société

```
GET  /api/InfoPaiementSociete/site/{idSite}
POST /api/InfoPaiementSociete
PUT  /api/InfoPaiementSociete/{id}
```

Coordonnées bancaires / Mobile Money pour reversements par site.

---

## Reversement site

```
POST /api/ReversementSite
GET  /api/ReversementSite/{id}
GET  /api/ReversementSite/site/{idSite}
GET  /api/ReversementSite/verifier/{orderNumber}
```

Reversements FlexPay vers les sites — admin financier.

---

## Références backend

- [`DOCUMENTATION_API_SITE.md`](../04_clients_referentiels/DOCUMENTATION_API_SITE.md)
- [`ENDPOINTS_RECHERCHE_CLIENTS.md`](../04_clients_referentiels/ENDPOINTS_RECHERCHE_CLIENTS.md)
- [`API_DOCUMENTATION_COMMUNICATION.md`](../../../docs/API_DOCUMENTATION_COMMUNICATION.md)
- [`GestionAgentsAuthentificationMultiRole.md`](../../GestionAgentsAuthentificationMultiRole.md)
