# Postman — Congo Travel API

Collection et environnements Postman pour l'intégration front Congo Travel Web.

## Fichiers

| Fichier | Description |
|---------|-------------|
| `CongoTravel_API.postman_collection.json` | Collection principale (~60 requêtes) |
| `CongoTravel_API.postman_environment.json` | Environnement **Dev** |
| `CongoTravel_API_Prod.postman_environment.json` | Environnement **Prod** |

## Import

1. Ouvrir Postman ou Insomnia (import compatible Postman v2.1)
2. **Import** → sélectionner la collection + l'environnement dev ou prod
3. Activer l'environnement **Congo Travel API — Dev**
4. Exécuter **Auth > Login** — le script enregistre automatiquement `accessToken` et `refreshToken`
5. Les autres requêtes héritent du Bearer token de la collection

## Variables d'environnement

| Variable | Description |
|----------|-------------|
| `baseUrl` | URL API (`https://dev-congotravel.asdc-rdc.org` ou prod) |
| `accessToken` | JWT (rempli après login) |
| `refreshToken` | Refresh token (rempli après login) |
| `idSociete` | ID société pour les tests |
| `idClient` | ID client |
| `idVoyage` | ID voyage |
| `idReservation` | ID réservation |
| `orderNumber` | Numéro commande FlexPay (rempli après réservation électronique) |

## Routes officielles

Cette collection utilise les **routes legacy** (officielles). Les routes guide (`/api/Auth/*`, `*/get-all`, `*/create`) ne sont **pas** incluses car non implémentées côté backend.

## Documentation associée

- `Documentation/Themes/09_frontend_integration/CHANGELOG_API_BREAKING_CHANGES.md`
- `Documentation/Themes/09_frontend_integration/MATRICE_ENDPOINTS_FRONT_COMPLETE.md`
- Swagger live : `{baseUrl}/swagger`

## Mise à jour depuis Swagger

Pour régénérer une collection complète depuis l'API live :

```bash
# Avec l'API démarrée
curl -o swagger.json https://dev-congotravel.asdc-rdc.org/swagger/v1/swagger.json
# Puis importer dans Postman : File > Import > swagger.json
```

La collection manuelle reste la référence pour les flux métier critiques (auth, réservation+FlexPay, dashboards).
