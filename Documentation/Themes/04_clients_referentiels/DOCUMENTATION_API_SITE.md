# API Site (Phase 1)

## Modèle

- Un **Site** est rattaché à une **Société** (`IdSociete`).
- Unicité métier : **`(IdSociete, CodeSite)`**.
- Champs principaux : `CodeSite`, `NomSite`, `Ville`, `Adresse`, `Telephone`, `NumeroMobileMoney` (optionnel), `Statut`, `IsSitePrincipal`, `DateCreation`, `DateModification`.

## Site principal (`IsSitePrincipal`)

- **Un seul** site avec `isSitePrincipal = true` par société (unicité applicative).
- Le site créé lors du **bootstrap société** (`POST /api/Societe/create-with-bootstrap`) ou de l’**init** est marqué principal par défaut.
- Les sites additionnels créés via `POST /api/Site` ont `isSitePrincipal = false` par défaut.
- **Transfert** : `PUT /api/Site/{id}` avec `"isSitePrincipal": true` remet les autres sites de la société à `false` (transaction).
- **Désactivation** : impossible de désactiver (`statut: false` ou `toggle-statut`) un site encore principal — transférer d’abord le statut principal à un autre site actif.
- **FlexPay** : si un site satellite n’a pas de ligne `InfoPaiementSociete`, le backend utilise les credentials du site principal actif (voir doc FlexPay). Si le site principal n’a pas non plus de config, **repli élargi** : toute `InfoPaiementSociete` active d’un autre site actif de la société. L’`IdSite` opérationnel sur réservation / paiement reste celui du guichet demandeur.

## Référence optionnelle `IdSite`

Les entités suivantes ont un **`IdSite` nullable** pour migration progressive :

- `Utilisateur` (affectation opérationnelle)
- `Reservation`, `Paiement`, `Billet`

### Règle de cohérence

Si `IdSite` est renseigné, le site doit **exister** et avoir le **même `IdSociete`** que l’entité ou l’opération concernée.

Pour **réservation + paiement unifiés** (`CreateReservationWithPaiement`) :

- Les deux blocs peuvent omettre `IdSite` (comportement historique inchangé).
- Si les deux renseignent `IdSite`, les valeurs **doivent être identiques**.
- La société de référence pour la validation est celle du **voyage** (`Voyage.IdSociete`), qui doit être alignée avec les `IdSociete` fournis dans les DTO lorsqu’ils sont > 0.

## Endpoints CRUD

Base : `/api/Site` (JWT + permissions RBAC `Site.*`).

| Méthode | Route | Permission |
|--------|--------|------------|
| GET | `/api/Site` | `Site.ReadAll` |
| GET | `/api/Site/{id}` | `Site.Read` |
| GET | `/api/Site/societe/{idSociete}` | `Site.ReadAll` |
| POST | `/api/Site` | `Site.Create` |
| PUT | `/api/Site/{id}` | `Site.Update` |
| PUT | `/api/Site/toggle-statut/{id}` | `Site.Update` |
| DELETE | `/api/Site/{id}` | `Site.Delete` |

### Corps JSON (création)

```json
{
  "idSociete": 1,
  "codeSite": "KIN-CENTRE",
  "nomSite": "Kinshasa Centre",
  "ville": "Kinshasa",
  "adresse": "…",
  "telephone": "+243…",
  "numeroMobileMoney": "+243…",
  "statut": true,
  "isSitePrincipal": false
}
```

Mise à jour (`PUT`) : `"isSitePrincipal"` est **optionnel** — omis = inchangé ; `true` = transfert du statut principal vers ce site.

## Utilisateur

- **Création** : `CreateUtilisateurDto.idSite` (optionnel).
- **Mise à jour admin** : `UpdateUtilisateurAdminDto.idSite` (optionnel) ou `desassocierSite: true` pour retirer l’affectation.

## Paiement (API directe)

- **Création** : `CreatePaiementDto.idSite` (optionnel).
- **Mise à jour** : `UpdatePaiementDto.idSite` ou `desassocierSite: true`.

## Migration base de données

Migration EF : **`Phase4_Site`** (table `Sites`, colonnes `IdSite` nullable sur les tables liées).
