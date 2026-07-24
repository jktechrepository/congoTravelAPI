# Index des documentations API — Modules Frontend

## Point d'entrée principal

**Documentation complète Vue.js + Flutter** :

[`Documentation/Themes/09_frontend_integration/DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md`](../Documentation/Themes/09_frontend_integration/DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)

### Fiches modules

| # | Fiche | Description |
|---|-------|-------------|
| 01 | [MODULE_01_AUTH_ET_PERMISSIONS.md](../Documentation/Themes/09_frontend_integration/MODULE_01_AUTH_ET_PERMISSIONS.md) | Login, JWT, RBAC |
| 02 | [MODULE_02_TRANSPORT_VOYAGE.md](../Documentation/Themes/09_frontend_integration/MODULE_02_TRANSPORT_VOYAGE.md) | Voyages, destinations, véhicules |
| 03 | [MODULE_03_RESERVATION_BILLET.md](../Documentation/Themes/09_frontend_integration/MODULE_03_RESERVATION_BILLET.md) | Réservation, scan QR, embarquement |
| 04 | [MODULE_04_PAIEMENT_FLEXPAY.md](../Documentation/Themes/09_frontend_integration/MODULE_04_PAIEMENT_FLEXPAY.md) | Cash, FlexPay, multi-devise |
| 05 | [MODULE_05_EVENEMENT_BILLETTERIE.md](../Documentation/Themes/09_frontend_integration/MODULE_05_EVENEMENT_BILLETTERIE.md) | Billetterie événement — Vue + Flutter (`with-paiement` / électronique, catalogue, gate) |
| 06 | [MODULE_06_CLIENT_APP_VOYAGEUR.md](../Documentation/Themes/09_frontend_integration/MODULE_06_CLIENT_APP_VOYAGEUR.md) | Inscription client, app voyageur |
| 07 | [MODULE_07_DASHBOARDS_ADMIN.md](../Documentation/Themes/09_frontend_integration/MODULE_07_DASHBOARDS_ADMIN.md) | Dashboards Vue.js |
| 08 | [MODULE_08_SYNC_OFFLINE_AGENT.md](../Documentation/Themes/09_frontend_integration/MODULE_08_SYNC_OFFLINE_AGENT.md) | Sync offline Flutter agent |
| 09 | [MODULE_09_REFERENTIELS_ET_COMMUNICATION.md](../Documentation/Themes/09_frontend_integration/MODULE_09_REFERENTIELS_ET_COMMUNICATION.md) | Société, agent, campagnes |

---

## Documentations complémentaires

### Communication
- [`API_DOCUMENTATION_COMMUNICATION.md`](./API_DOCUMENTATION_COMMUNICATION.md) — campagnes push/SMS/email

### Plaintes client
- [`API_DOCUMENTATION_PLAINTE_CLIENT.md`](./API_DOCUMENTATION_PLAINTE_CLIENT.md)

### Paiement et facturation
- [`Frontend_Paiement.md`](./Frontend_Paiement.md)
- [`Frontend_Facturation.md`](./Frontend_Facturation.md)
- [`INTEGRATION_FLUTTER_FLEXPAY.md`](../Documentation/Themes/09_frontend_integration/INTEGRATION_FLUTTER_FLEXPAY.md)

### Client
- [`Frontend_Client.md`](./Frontend_Client.md)

### SignalR
- [`SIGNALR_FRONTEND_GUIDE.md`](./SIGNALR_FRONTEND_GUIDE.md)
- [`SignalR-Integration.md`](./SignalR-Integration.md)

### Catalogue endpoints
- [`DOCUMENTATION_API_ENDPOINTS_COMPLETE.md`](../Documentation/Themes/01_demarrage/DOCUMENTATION_API_ENDPOINTS_COMPLETE.md)

---

## Authentification commune

**`POST /api/Utilisateur/authentifier`**

```json
{
  "emailOuTelephone": "user@congotravel.cd",
  "motDePasse": "secret",
  "fcmToken": "optional",
  "deviceType": "web",
  "deviceModel": "Vue App",
  "osVersion": "1.0"
}
```

Header sur toutes les requêtes protégées :
```
Authorization: Bearer {accessToken}
```

Détails : [MODULE_01_AUTH_ET_PERMISSIONS.md](../Documentation/Themes/09_frontend_integration/MODULE_01_AUTH_ET_PERMISSIONS.md)

---

## Codes HTTP

| Code | Action frontend |
|------|-----------------|
| 200/201 | Succès |
| 400 | Afficher `message` |
| 401 | Refresh token ou login |
| 403 | Accès refusé |
| 404 | Ressource introuvable |
| 409 | Conflit métier |
| 429 | Rate limit — lire `retryAfter` |
| 500 | Erreur serveur |

---

## Outils

- Swagger : `https://localhost:7110/swagger`
- Postman : `CongoTravel_API_Collection.postman_collection.json`

---

**Dernière mise à jour** : juillet 2026 — aligné sur CongoTravelAPI (transport + événementiel)
