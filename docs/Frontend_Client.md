## Client – Guide d'intégration Frontend

### Base
- URL prod : `https://mombongo.asdc-rdc.org`
- URL dev local : `https://localhost:7110`
- Auth : `Authorization: Bearer <token>` + permission `Client.ReadAll` pour les listes par société

### Endpoints clés
- `GET /api/Client/societe/{idSociete}` : liste non paginée des clients **ayant au moins une réservation** dans la société (`Reservations.Statut = true`).
- `GET /api/Client/societe/{idSociete}/paged?pageNumber=1&pageSize=20&searchTerm=&sortBy=NomClient&sortDescending=false` : même périmètre, paginé avec tri/recherche.
- `GET /api/Client/societe/{idSociete}/recherche?searchTerm=...` : recherche multi-champs sur le même périmètre.
- `GET /api/Client/{id}/factures-payees/paged?pageNumber=1&pageSize=20` : factures payées d'un client (paginé).

**Scope société :** le JWT doit correspondre à `idSociete` (sauf Super-Admin), sinon `403`.

### Modèle Client (champs utiles Front)
- `idClient`, `nomClient`, `telephone`, `emailClient`, `adresseClient`, `idSociete` (renseigné sur les routes `/societe/...`), `statut`, `isActif`, `dateCreation`.

### Création de client
- La création déclenche l'envoi d'un SMS de bienvenue (avec URL front configurable) et la création/mise à jour d'un compte utilisateur lié au client.
- Un client créé ou inscrit **sans réservation** n'apparaît pas dans les listes `/societe/{idSociete}`.

### Tri / Recherche (paged)
- Tri : `NomClient`, `DateCreation`, `IdClient` (par défaut : `idClient`).
- Recherche (`searchTerm`) : nom, adresse, email, téléphone, genre.

### Exemples cURL
Paged :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Client/societe/1/paged?pageNumber=1&pageSize=20&searchTerm=jo"
```
Recherche rapide :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Client/societe/1/recherche?searchTerm=dupont"
```
Factures payées (client) :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Client/4/factures-payees/paged?pageNumber=1&pageSize=10"
```

### Bonnes pratiques Front
- Toujours paginer en liste principale (`/paged`).
- Afficher `statut`, `isActif` et `dateCreation`.
- Pour chercher un client **avant** sa première réservation, utiliser un autre flux (ex. création client ou recherche globale staff si exposée).
- Après création, informer l'utilisateur que le SMS de bienvenue a été envoyé automatiquement.
