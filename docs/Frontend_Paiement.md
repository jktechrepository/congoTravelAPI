## Paiement – Guide d’intégration Frontend

### Base
- URL prod : `https://mombongo.asdc-rdc.org`
- URL dev local : `https://localhost:7110`
- Auth : `Authorization: Bearer <token>`

### Endpoints clés
- `GET /api/Paiement/societe/{idSociete}` : liste non paginée (paiements d’une société).
- `GET /api/Paiement/societe/{idSociete}/paged?pageNumber=1&pageSize=20&searchTerm=&sortBy=DatePaiement&sortDescending=true` : liste paginée avec tri/recherche.

### Modèle Paiement (champs importants)
- `idPaiement`, `idFacture`, `idClient`, `montantPaye`, `montantAPaye`, `resteAPaye`, `datePaiement`, `methodePaiement`, `referenceTransaction`, `commentaire`, `statut`, `idUtilisateurEnregistrement`.

### Tri / Recherche
- Tri : `DatePaiement`, `MontantPaye`, `Statut`, `MethodePaiement` (par défaut : `idPaiement`).
- Recherche (`searchTerm`) : référence transaction, méthode, commentaire, numéro de facture, nom client.

### Flux UI recommandé
1) Lister (paged) filtré par société et recherche libre.
2) Afficher montant payé, reste à payer, méthode et statut.
3) Lien vers la facture associée (`idFacture`) et vers le client (`idClient`) si besoin.

### Exemples cURL
Paged :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Paiement/societe/1/paged?pageNumber=1&pageSize=20&sortBy=DatePaiement&sortDescending=true"
```
Non pagé :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Paiement/societe/1"
```

### Bonnes pratiques Front
- Utiliser la pagination en liste principale, garder la non paginée pour des exports courts.
- Surveiller `statut` (ex : `Validé`) avant d’afficher comme payé.
- Quand `resteAPaye` ou `montantAPaye` sont renseignés, les mettre en avant pour la relance.

