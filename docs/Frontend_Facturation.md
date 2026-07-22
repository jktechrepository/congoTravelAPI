## Facturation – Guide d’intégration Frontend

### Base
- URL prod : `https://mombongo.asdc-rdc.org`
- URL dev local : `https://localhost:7110`
- Auth : `Authorization: Bearer <token>`

### Endpoints clés
- `GET /api/Facture/societe/{idSociete}` : liste non paginée.
- `GET /api/Facture/societe/{idSociete}/paged?pageNumber=1&pageSize=20&searchTerm=&sortBy=Montant&sortDescending=false` : liste paginée avec tri/recherche.
- `POST /api/Facture/{idFacture}/societe/{idSociete}/diffusion?forcer=false` : met en file la diffusion multi-canal (push, in-app, email, SMS) vers tous les clients de la catégorie de la facture.
- `GET /api/Facture/{idFacture}/diffusion/statistiques` : stats de la dernière diffusion.

### Permissions (rôles)
- Diffusion : `Super-Admin, Admin, Gerant`
- Lecture paginée : généralement authentifié ; vérifier les guards côté app.

### Modèle Facture (champs importants)
- `idFacture`, `numeroFacture`, `montant`, `dateEmission`, `moisEmission`, `anneesEmission`, `idCategorie`, `estDiffusee`, `dateDiffusion`, `statut` (bool).

### Flux UI recommandé
1) Lister (paged) avec recherche par numéro/montant/date.
2) Bouton “Diffuser” sur une facture non diffusée (`estDiffusee=false`), avec option “forcer” si besoin.
3) Afficher le retour immédiat du queueing et un lien vers les stats.
4) Rafraîchir périodiquement les stats (`/diffusion/statistiques`).

### Exemples cURL
Lister paginé :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Facture/societe/1/paged?pageNumber=1&pageSize=20"
```
Diffuser :
```
curl -X POST -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Facture/1/societe/1/diffusion?forcer=false"
```

### États et messages
- `estDiffusee=true` + `dateDiffusion` indiquent une diffusion déjà lancée.
- Conflit si déjà diffusée et `forcer=false`.
- Le message de réponse précise le nombre de clients ciblés et le temps estimé.

### Bonnes pratiques Front
- Toujours paginer pour les listes.
- Gérer le conflit 409 (déjà diffusée) en proposant “forcer”.
- Afficher les canaux activés selon les préférences utilisateur (retour en back déjà filtré).
- Vérifier `statut` (true) pour afficher uniquement les factures actives.

