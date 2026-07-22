# Backend Contract Frontends (Vue.js + Flutter)

Specification de contrat API orientee integration frontend.

Conventions:
- Tous les payloads sont en JSON.
- Dates au format ISO-8601 UTC.
- Les endpoints proteges necessitent `Authorization: Bearer <accessToken>`.
- Les exemples montrent la structure, pas forcement tous les champs.

---

## 1. Authentification

## 1.1 POST `/api/Utilisateur/authentifier`

### Description
Authentifie un utilisateur via email, username ou telephone + mot de passe.

### Request body
| Champ | Type | Requis | Description |
|---|---|---:|---|
| `emailOuTelephone` | string | oui | Email, username par defaut, ou telephone |
| `motDePasse` | string | oui | Mot de passe brut |
| `fcmToken` | string | non | Token push (mobile/web) |
| `deviceType` | string | non | Type device (web/android/ios) |
| `deviceModel` | string | non | Modele/appareil |
| `osVersion` | string | non | Version OS |

### Response 200
| Champ | Type | Description |
|---|---|---|
| `success` | bool | true si login OK |
| `message` | string | Message utilisateur |
| `accessToken` | string | JWT d'acces |
| `refreshToken` | string | Token de renouvellement |
| `tokenType` | string | `Bearer` |
| `expiresIn` | number | Duree en secondes |
| `expiresAt` | string(date) | Expiration UTC |
| `doitChangerMotDePasse` | bool | Flag changement mot de passe |
| `nomRole` | string | Role principal |
| `nomSociete` | string | Nom societe |
| `permissions` | string[] | Permissions agrégées |
| `roles` | object[] | Roles actifs |
| `primaryRole` | object/null | Role principal |
| `utilisateur` | object | Infos utilisateur |
| `agent` | object/null | Infos agent associe |
| `client` | object/null | Infos client associe |

### Objet `utilisateur` (extrait utile front)
| Champ | Type |
|---|---|
| `idUtilisateur` | number |
| `nomComplet` | string |
| `email` | string/null |
| `defaultUsername` | string/null |
| `telephone` | string/null |
| `idSociete` | number/null |
| `idRole` | number/null |
| `idAgent` | number/null |
| `idClient` | number/null |
| `idSite` | number/null |
| `statut` | bool/null |

### Objet `agent` (si utilisateur agent)
| Champ | Type |
|---|---|
| `idAgent` | number |
| `nomComplet` | string/null |
| `idSociete` | number/null |
| `idSite` | number/null |
| `roleAgent` | string/null |
| `fonction` | string/null |

### Erreurs
- `400`: payload invalide.
- `401`: identifiants invalides / compte desactive.
- `404`: informations utilisateur non trouvees apres auth.
- `500`: erreur serveur.

---

## 1.2 POST `/api/Utilisateur/refresh-token`

### Request body
| Champ | Type | Requis |
|---|---|---:|
| `refreshToken` | string | oui |
| `deviceInfo` | string | non |

### Response 200
Meme schema que `authentifier` (nouveaux `accessToken` + `refreshToken`).

### Erreurs
- `400`: refresh token manquant/invalide.
- `401`: refresh token non autorise.
- `500`: erreur serveur.

---

## 1.3 POST `/api/Utilisateur/deconnecter`

### Request body (optionnel selon besoin)
| Champ | Type | Requis | Description |
|---|---|---:|---|
| `supprimerTousLesDevices` | bool | non | Deconnecter tous les devices |
| `idUserDevice` | number | non | Deconnecter un device cible |
| `fcmToken` | string | non | Deconnecter device par token |

### Response 200
Confirmation de deconnexion.

### Erreurs
- `401`: token invalide.
- `404`: utilisateur introuvable.
- `500`: erreur serveur.

---

## 2. Agents / Sites

## 2.1 PUT `/api/Agent/{idAgent}/AffecterAgentSite`
## 2.2 PUT `/api/Agent/{idAgent}/site` (alias)

### Description
Affecte un agent a un site.

### Autorisations
- Roles autorises: `Admin`, `Super-Admin`, `Gerant`.

### Path params
| Param | Type | Requis |
|---|---|---:|
| `idAgent` | number | oui |

### Request body
| Champ | Type | Requis | Contraintes |
|---|---|---:|---|
| `idSite` | number | oui | `> 0` |

### Validations metier backend
- Agent doit exister.
- Site doit exister.
- Site et agent doivent appartenir a la meme societe.
- Controle de perimetre role/societe (non super-admin).

### Response 200
| Champ | Type |
|---|---|
| `message` | string |
| `idAgent` | number |
| `ancienIdSite` | number/null |
| `nouveauIdSite` | number |

### Erreurs
- `400`: donnees invalides ou site hors societe.
- `403`: non autorise.
- `404`: agent ou site introuvable.
- `500`: erreur serveur.

---

## 3. CategorieSiege

## 3.1 GET `/api/CategorieSiege/societe/{idSociete}`

### Description
Retourne les categories de siege d'une societe.

### Query params
| Param | Type | Requis | Description |
|---|---|---:|---|
| `actifsSeulement` | bool | non | Si true, filtre `statut=true` |

### Response 200
`CategorieSiegeResponseDto[]`

### `CategorieSiegeResponseDto`
| Champ | Type |
|---|---|
| `idCategorieSiege` | number |
| `idSociete` | number |
| `codeCategorieSiege` | string |
| `libelle` | string |
| `statut` | bool |

### Erreurs
- `403`: acces hors societe (non super-admin).

---

## 3.2 GET `/api/CategorieSiege/{idCategorieSiege}`

### Response 200
`CategorieSiegeResponseDto`

### Erreurs
- `403`: acces hors societe.
- `404`: categorie introuvable.

---

## 3.3 POST `/api/CategorieSiege`

### Roles autorises
- `Admin`, `Super-Admin`, `Gerant`

### Request body (`CreateCategorieSiegeDto`)
| Champ | Type | Requis | Contraintes |
|---|---|---:|---|
| `idSociete` | number | oui | `> 0` |
| `codeCategorieSiege` | string | oui | max 40 |
| `libelle` | string | oui | max 120 |
| `statut` | bool | non | defaut `true` |

### Response 201
`CategorieSiegeResponseDto`

### Erreurs
- `400`: payload invalide.
- `403`: tentative creation hors societe.
- `409`: code deja existant pour la societe.

---

## 3.4 PUT `/api/CategorieSiege/{idCategorieSiege}`

### Roles autorises
- `Admin`, `Super-Admin`, `Gerant`

### Request body (`UpdateCategorieSiegeDto`)
| Champ | Type | Requis | Contraintes |
|---|---|---:|---|
| `idCategorieSiege` | number | oui | doit matcher l'ID URL |
| `codeCategorieSiege` | string | oui | max 40 |
| `libelle` | string | oui | max 120 |
| `statut` | bool | non | |

### Response 200
`CategorieSiegeResponseDto`

### Erreurs
- `400`: ID URL != payload ou payload invalide.
- `403`: tentative update hors societe.
- `404`: categorie introuvable.
- `409`: code deja utilise par une autre categorie de la societe.

---

## 3.5 PUT `/api/CategorieSiege/{idCategorieSiege}/toggle-statut`

### Roles autorises
- `Admin`, `Super-Admin`, `Gerant`

### Response 200
`CategorieSiegeResponseDto` avec `statut` inverse.

### Erreurs
- `403`: hors societe.
- `404`: categorie introuvable.

---

## 3.6 DELETE `/api/CategorieSiege/{idCategorieSiege}`

### Roles autorises
- `Admin`, `Super-Admin`, `Gerant`

### Response 200
```json
{
  "message": "Categorie de siege supprimée avec succès."
}
```

### Erreurs
- `403`: hors societe.
- `404`: categorie introuvable.
- `500`: suppression impossible si contrainte referentielle (selon donnees liees).

---

## 3.7 Clients par société (liste, pagination, recherche)

**Permission :** `Client.ReadAll`  
**Scope JWT :** `idSociete` route = société du token (sauf Super-Admin), sinon `403`.

### Périmètre commun (3 routes)

Un client n'apparaît que s'il a **au moins une réservation** avec `Reservations.Statut = true` et `Reservations.IdSociete = idSociete`.

**Ne pas utiliser** ces endpoints pour chercher un client avant sa première réservation chez l'opérateur. Utiliser plutôt :
- `POST /api/Client` ou `POST /api/Client/simple` (création staff)
- `POST /api/Client/register` (auto-inscription)
- puis le flux réservation habituel

### 3.7.1 GET `/api/Client/societe/{idSociete}`

Liste complète, tri `dateCreation` descendant.

### 3.7.2 GET `/api/Client/societe/{idSociete}/paged`

| Query | Description |
|---|---|
| `pageNumber`, `pageSize` | Pagination |
| `searchTerm` | Filtre nom, adresse, téléphone, email, genre |
| `includeInactive`, `isActif` | Filtre `IsActif` |
| `sortBy`, `sortDescending` | Tri (`NomClient`, `DateCreation`, `IdClient`) |

### 3.7.3 GET `/api/Client/societe/{idSociete}/recherche`

| Query | Description |
|---|---|
| `searchTerm` | Obligatoire pour filtrer ; vide → même liste que 3.7.1 |
| `includeInactive` | Inclure clients `IsActif = false` |

### Response `ClientResponseDto` (extrait)
| Champ | Type | Note |
|---|---|---|
| `idClient` | number | |
| `nomClient` | string | |
| `telephone` | string/null | |
| `emailClient` | string/null | |
| `idSociete` | number | Renseigné (= `idSociete` route) sur ces 3 endpoints |

### Erreurs
- `401` : non authentifié
- `403` : permission manquante ou société hors scope JWT
- `200` + liste vide : aucun client voyageur pour cette société / critères

---

## 4. Synchronisation offline (mobile prioritaire)

## 4.1 GET `/api/sync/bootstrap`

### Response 200
| Champ | Type | Description |
|---|---|---|
| `watermark` | string | Reference delta future |
| `clients` | array | Peut etre vide au bootstrap |
| `arrears` | array | Peut etre vide au bootstrap |
| `reservationWorkflowV2` | object | Hints API workflow reservation |

---

## 4.2 GET `/api/sync/clients`

Clients ayant au moins une réservation non supprimée dans la société du JWT (même périmètre que `GET /api/Client/societe/{idSociete}` et variantes `/paged`, `/recherche`).

### Query params
| Param | Type | Requis | Description |
|---|---|---:|---|
| `cursor` | string | non | Pagination cursor opaque |
| `pageSize` | number | non | 1..5000 (defaut 1000) |
| `snapshot` | string | non | Cohérence session |
| `since` | string | non | Watermark delta |

### Response 200
| Champ | Type |
|---|---|
| `snapshot` | string |
| `items` | `ClientSyncDto[]` |
| `nextCursor` | string/null |
| `hasMore` | bool |
| `nextSince` | string/null |

### `ClientSyncDto`
| Champ | Type |
|---|---|
| `idClient` | number |
| `nomClient` | string |
| `adresseClient` | string |
| `telephone` | string/null |
| `emailClient` | string/null |
| `genreClient` | string/null |
| `idSociete` | number |
| `idCategorieClient` | number/null |
| `isActif` | bool |
| `statut` | bool |
| `isDeleted` | bool |
| `updatedAt` | string(date) |

---

## 4.3 GET `/api/sync/arrears`

### Query params
| Param | Type | Requis |
|---|---|---:|
| `cursor` | string | non |
| `pageSize` | number | non |
| `snapshot` | string | non |
| `since` | string | non |
| `onlyOutstanding` | bool | non |

### Response 200
| Champ | Type |
|---|---|
| `snapshot` | string |
| `items` | `ArrearSyncDto[]` |
| `nextCursor` | string/null |
| `hasMore` | bool |
| `nextSince` | string/null |

### `ArrearSyncDto` (contrat actuel backend)
| Champ | Type |
|---|---|
| `idClientFacture` | number |
| `idFacture` | number/null |
| `idClient` | number |
| `numeroFacture` | string/null |
| `dateEmission` | string(date) |
| `mois` | string/null |
| `annees` | number/null |
| `montantTotal` | number |
| `montantPaye` | number |
| `montantDu` | number |
| `libelleUsage` | string/null |
| `estArrierePreExistant` | bool |
| `dateModification` | string(date) |

---

## 4.4 GET `/api/sync/deletions`

### Query params
| Param | Type | Requis |
|---|---|---:|
| `since` | string | oui |
| `snapshot` | string | non |

### Response 200
| Champ | Type |
|---|---|
| `snapshot` | string |
| `deletedClientIds` | number[] |
| `removedClientFactureIds` | number[] |
| `deletedPaymentIds` | number[] |
| `nextSince` | string/null |

---

## 4.5 POST `/api/sync/payments/batch`

### Request body
| Champ | Type | Requis |
|---|---|---:|
| `items` | `PaymentRequestDto[]` | oui |

### `PaymentRequestDto`
| Champ | Type | Requis |
|---|---|---:|
| `clientRequestId` | string | oui |
| `idClient` | number | oui |
| `idClientFacture` | number/null | non |
| `idFacture` | number/null | non |
| `montantPaye` | number | oui |
| `datePaiementUtc` | string(date) | oui |
| `methodePaiement` | string | oui |
| `referenceTransaction` | string/null | non |
| `commentaire` | string/null | non |
| `deviceId` | string/null | non |
| `agentId` | number/null | non |

### Response 200
| Champ | Type |
|---|---|
| `results` | `PaymentResultDto[]` |
| `summary` | `PaymentSummaryDto` |

### `PaymentResultDto`
| Champ | Type |
|---|---|
| `clientRequestId` | string |
| `status` | string (`created`/`duplicate`/`rejected`/`error`) |
| `idPaiement` | number/null |
| `newMontantDu` | number/null |
| `message` | string |
| `errorCode` | string/null |

### `PaymentSummaryDto`
| Champ | Type |
|---|---|
| `total` | number |
| `created` | number |
| `duplicates` | number |
| `rejected` | number |
| `errors` | number |

---

## 5. Codes erreurs standards

| Code | Signification | Action frontend recommandee |
|---:|---|---|
| 400 | Requete invalide/metier | Afficher `message` backend |
| 401 | Non authentifie/token expire | Tenter refresh token puis relogin |
| 403 | Interdit (role/perimetre) | Bloquer action, message metier |
| 404 | Ressource introuvable | Afficher et proposer rafraichir |
| 500 | Erreur serveur | Message generique + retry |

---

## 5.1 Multi-devise (phase 1)

## 5.1.1 GET `/api/Devise/devises`

### Description
Retourne le catalogue des devises actives.

### Response 200
| Champ | Type |
|---|---|
| `codeDevise` | string(3) |
| `libelle` | string |
| `symbole` | string/null |

---

## 5.1.2 PUT `/api/Devise/societe/{idSociete}/devise-principale/{codeDevise}`

### Roles
`Admin`, `Super-Admin`, `Gerant`

### Regle d'acces
- `Super-Admin`: toutes societes
- autres roles: uniquement leur `SocieteId`

### Response 200
| Champ | Type |
|---|---|
| `message` | string |
| `idSociete` | number |
| `codeDevisePrincipale` | string(3) |

---

## 5.1.3 POST `/api/Devise/taux-change`

### Request body
| Champ | Type | Requis | Contraintes |
|---|---|---:|---|
| `idSociete` | number | oui | `> 0` |
| `codeDeviseSource` | string | oui | ISO 3 lettres |
| `codeDeviseCible` | string | oui | ISO 3 lettres |
| `taux` | number | oui | `> 0` |
| `dateEffet` | string(date) | non | defaut `UtcNow` |

### Validation
- source != cible
- devises actives
- controle de perimetre societe

---

## 5.1.4 GET `/api/Devise/taux-change?idSociete=...&source=...&cible=...`

Retourne le dernier taux actif pour la paire.

---

## 5.1.5 GET `/api/Devise/preview-conversion`

### Query params
| Param | Type | Requis | Description |
|---|---|---:|---|
| `idSociete` | number | oui | Société cible |
| `codeDeviseSource` | string(3) | oui | Devise saisie (`USD`, `CDF`...) |
| `montant` | number | oui | Montant à convertir |
| `datePaiement` | string(date) | non | Date de référence taux (defaut `UtcNow`) |

### Response 200
| Champ | Type |
|---|---|
| `idSociete` | number |
| `codeDeviseSource` | string(3) |
| `codeDevisePrincipale` | string(3) |
| `datePaiement` | string(date) |
| `taux` | number |
| `montantSource` | number |
| `montantConverti` | number |

### Erreurs
- `403`: hors périmètre société
- `404`: société introuvable ou taux absent
- `400`: montant/devise invalides

---

## 5.1.6 Paiement multi-devise (champs ajoutes)

### Request `POST /api/Paiement`
| Champ | Type | Requis | Notes |
|---|---|---:|---|
| `codeDevisePaiement` | string(3) | oui | ex: `CDF`, `USD` |
| `datePaiement` | string(date) | non | defaut `UtcNow` |

### Response `Paiement`
| Champ | Type | Description |
|---|---|---|
| `codeDevisePaiement` | string(3) | devise originale de saisie |
| `codeDevisePrincipale` | string(3) | devise principale societe (snapshot) |
| `tauxVersDevisePrincipale` | number | taux applique (snapshot) |
| `montantAPayeDevisePrincipale` | number | montant converti |
| `montantPayeDevisePrincipale` | number/null | montant converti |
| `resteAPayeDevisePrincipale` | number/null | reste converti |
| `datePaiement` | string(date) | date metier taux |

### Regle de conversion
Le backend cherche le taux actif le plus recent dont `DateEffet <= datePaiement`.

---

## 5.2 Voyage multi-devise (phase 2)

### Champs ajoutes Create/Update Voyage
| Champ | Type | Requis |
|---|---|---:|
| `codeDevisePrix` | string(3) | oui |

### Champs ajoutes Voyage response
| Champ | Type |
|---|---|
| `codeDevisePrix` | string(3) |
| `codeDevisePrincipale` | string(3) |
| `tauxVersDevisePrincipale` | number |
| `prixDevisePrincipale` | number |

---

## 5.3 Remboursement & reporting (phase 3)

## 5.3.1 POST `/api/Remboursement`

### Request body
| Champ | Type | Requis |
|---|---|---:|
| `idPaiement` | number | oui |
| `idSociete` | number | oui |
| `idUtilisateur` | number | oui |
| `montantRembourse` | number | oui |
| `codeDeviseRemboursement` | string(3) | non |
| `forcerDevisePrincipale` | bool | non |
| `dateRemboursement` | string(date) | non |
| `motif` | string | non |

### Regles
- Ne peut pas dépasser le total déjà payé (en devise principale).
- Conversion figée via taux à la date de remboursement.

---

## 5.3.2 GET `/api/FinanceReporting/paiements/summary`

### Query params
| Param | Type | Requis |
|---|---|---:|
| `idSociete` | number | oui |
| `dateDebut` | string(date) | non |
| `dateFin` | string(date) | non |

### Response (extrait)
| Champ | Type |
|---|---|
| `totalTransactions` | number |
| `totalPayeDevisePrincipale` | number |
| `totalResteDevisePrincipale` | number |
| `byDevise[]` | array |

---

## 5.3.3 GET `/api/FinanceReporting/rapport-caisse`

Rapport de caisse paramétrable avec séparation claire entre espèces et paiements électroniques.

### Query params
| Param | Type | Requis |
|---|---|---:|
| `idSociete` | number | oui |
| `idUtilisateur` | number | non |
| `datePrecise` | string(date) | non |
| `dateDebut` | string(date) | non |
| `dateFin` | string(date) | non |

### Règles de période
- Si `dateDebut` et `dateFin` sont fournis: mode `intervalle` (et `datePrecise` est ignorée).
- Si un seul des deux est fourni: réponse `400`.
- Sinon, mode `jour` avec `datePrecise` si fournie, ou la date UTC du jour par défaut.

### Réponse (extrait)
| Champ | Type |
|---|---|
| `modePeriode` | `jour` \\| `intervalle` |
| `periodeDebut` | string(date-time) |
| `periodeFin` | string(date-time) |
| `synthese.totalEncaisse` | number |
| `especes.montantDevisePrincipale` | number |
| `electronique.montantDevisePrincipale` | number |
| `electronique.detail.mobileMoney` | object |
| `electronique.detail.carte` | object |
| `electronique.detail.virement` | object |
| `electronique.detail.autre` | object |
| `parDevise[]` | array |

Note: ici, le mot `origine` ne représente pas `Paiement.Origine` (CLIENT/AGENT), mais la ventilation caisse par méthode de paiement.

**Caissier** : utiliser `GET /api/CaissierDashboard/rapport-caisse` (scope JWT, pas de `idSociete`/`idUtilisateur` en query).  
**Financier / Admin** : utiliser cette route `FinanceReporting/rapport-caisse` (permission `FinanceReporting.ReadAll`).

---

### Exemple de réponse complète (200)

Requête : `GET /api/FinanceReporting/rapport-caisse?idSociete=1&idUtilisateur=42&datePrecise=2026-05-28`

```json
{
  "idSociete": 1,
  "idUtilisateur": 42,
  "modePeriode": "jour",
  "periodeDebut": "2026-05-28T00:00:00Z",
  "periodeFin": "2026-05-28T23:59:59.9999999Z",
  "codeDevisePrincipale": "CDF",
  "synthese": {
    "totalEncaisse": 1850000,
    "nombreTransactions": 24,
    "partEspecesPourcentage": 54.05,
    "partElectroniquePourcentage": 45.95
  },
  "especes": {
    "montantDevisePrincipale": 1000000,
    "nombreTransactions": 14
  },
  "electronique": {
    "montantDevisePrincipale": 850000,
    "nombreTransactions": 10,
    "detail": {
      "mobileMoney": {
        "montantDevisePrincipale": 620000,
        "nombreTransactions": 7
      },
      "carte": {
        "montantDevisePrincipale": 180000,
        "nombreTransactions": 2
      },
      "virement": {
        "montantDevisePrincipale": 50000,
        "nombreTransactions": 1
      },
      "autre": {
        "montantDevisePrincipale": 0,
        "nombreTransactions": 0
      }
    }
  },
  "parDevise": [
    {
      "codeDevisePaiement": "CDF",
      "especes": {
        "montantPaye": 950000,
        "count": 13
      },
      "electronique": {
        "montantPaye": 800000,
        "count": 9
      }
    },
    {
      "codeDevisePaiement": "USD",
      "especes": {
        "montantPaye": 20,
        "count": 1
      },
      "electronique": {
        "montantPaye": 10,
        "count": 1
      }
    }
  ]
}
```

**Lecture UI recommandée**
- Afficher les totaux principaux depuis `synthese`, `especes` et `electronique` (montants en `codeDevisePrincipale`).
- Utiliser `electronique.detail.*` pour le détail du bloc électronique (Mobile Money, Carte, Virement, Autre).
- Utiliser `parDevise[]` pour l’affichage comptable par devise réellement encaissée (`montantPaye` brut).
- Ne pas confondre avec `byOrigineGroupe` de `paiements/summary` (canal CLIENT vs guichet).

---

## 6. Notes d'integration importantes

- Utiliser `utilisateur.idSite` comme site courant apres login.
- Si present, `agent.idSite` doit etre coherent avec `utilisateur.idSite`.
- En UI admin, proposer reassignment via `PUT /api/Agent/{idAgent}/site`.
- Pour le referentiel local, synchroniser aussi `CategorieSiege` par societe.
- Pour `POST/PUT Vehicule`, fournir `repartitionCategorieSieges` pour un resultat deterministe.
- Cote Flutter offline, persister:
  - `accessToken`
  - `refreshToken`
  - `snapshot`
  - `since`
  - `cursor`

---

## 7. Vehicule (contract categorie/sieges)

## 7.1 POST `/api/Vehicule`
## 7.2 PUT `/api/Vehicule/{id}`

### Champs additionnels supportes (payload)
| Champ | Type | Requis | Description |
|---|---|---:|---|
| `repartitionCategorieSieges` | array | non | Liste des categories et volumes de sieges |

### `repartitionCategorieSieges[]`
| Champ | Type | Requis | Contraintes |
|---|---|---:|---|
| `idCategorieSiege` | number | oui | `> 0` |
| `nombreSiegeParCategorie` | number | oui | `> 0` |

### Regles metier backend
- Si `repartitionCategorieSieges` est fournie:
  - somme des `nombreSiegeParCategorie` doit correspondre au total vehicule.
  - categories doivent exister, etre actives, et appartenir a la societe du vehicule.
- Generation des sieges:
  - `IdCategorieSiege` attribue selon la repartition.
  - `CodeSiege` genere via `CodeCategorieSiege/index` (ex. `ECO/1`, `PREMIERE/1`).
- Si `repartitionCategorieSieges` absente:
  - fallback legacy applique.

### Erreurs metier frequentes
- `400`: somme invalide, categorie inactive/inexistante/hors societe.
- `409`: conflit d'unicite vehicule (alias).

---

## 8. Ordre recommande des appels (contract d'orchestration)

## 8.1 Web (Vue.js)
1. `POST /api/Utilisateur/authentifier`
2. Persister tokens + `utilisateur`
3. Resoudre le contexte (`idSociete`, `idSite`)
4. Charger `GET /api/CategorieSiege/societe/{idSociete}`
5. Pour creation/edition vehicule: construire `repartitionCategorieSieges`
6. Charger les modules fonctionnels necessaires
7. Sur `401`, faire `POST /api/Utilisateur/refresh-token`, puis retry

## 8.2 Mobile (Flutter)
1. `POST /api/Utilisateur/authentifier`
2. Persister tokens et contexte utilisateur
3. Charger referentiels (dont `CategorieSiege`)
4. Pour creation/edition vehicule: envoyer `repartitionCategorieSieges`
5. Initialiser sync:
   - `GET /api/sync/bootstrap`
   - `GET /api/sync/clients` (pagination)
   - `GET /api/sync/arrears` (pagination)
   - `GET /api/sync/deletions`
6. Envoyer operations offline:
   - `POST /api/sync/payments/batch`

## 8.3 Admin (affectation agent/site)
1. Selection agent
2. Selection site (meme societe)
3. `PUT /api/Agent/{idAgent}/site`
4. Rafraichir les donnees agent cote UI

---

## 9. Dashboards — collecte Client vs Agent

Champs additifs sur `GET /api/Dashboard/{idSociete}`, `GET /api/GerantDashboard`, `GET /api/FinancierDashboard`, `GET /api/SuperAdminDashboard` (pas sur CaissierDashboard).

| Champ | Type | Description |
|---|---|---|
| `collecteParOrigineGroupe` | array | Toujours 3 items : `CLIENT`, `AGENT`, `INCONNU` |
| `collecteParOrigineGroupe[].origineGroupe` | string | `CLIENT` \| `AGENT` \| `INCONNU` |
| `collecteParOrigineGroupe[].montant` | number | Encaissements validés du mois (devise principale) |
| `collecteParOrigineGroupe[].nombrePaiements` | number | Nombre de transactions |
| `collecteParOrigineGroupe[].montantMoisPrecedent` | number | Même groupe, mois précédent |
| `collecteParOrigineGroupe[].variationPourcentage` | number | Variation vs mois précédent |
| `collecteParOrigineGroupe[].partPourcentage` | number | Part du total mois (CLIENT+AGENT+INCONNU) |
| `collecteOrigineGroupeSynthese.partDigitalPourcentage` | number | CLIENT / (CLIENT+AGENT) |
| `collecteOrigineGroupeSynthese.partGuichetPourcentage` | number | AGENT / (CLIENT+AGENT) |
| `collecteOrigineGroupeSynthese.montantClassifie` | number | CLIENT + AGENT |
| `collecteOrigineGroupeSynthese.montantNonClassifie` | number | INCONNU (données pré-migration) |

**FinancierDashboard** : champs présents au niveau racine (scope agrégé) et sur chaque élément de `societesFinancieres[]`.

**UI recommandée :** donut `partDigitalPourcentage` / `partGuichetPourcentage` ; badge « non classé » si `montantNonClassifie > 0`.

---

## 10. CaissierDashboard — notes cohérence

`GET /api/CaissierDashboard` — scope **caissier JWT** (`IdUtilisateur`), pas de split `origineGroupe`.

### 10.1 GET `/api/CaissierDashboard/rapport-caisse`

Rapport caisse personnel du caissier connecté (espèces vs électronique).

| Param | Type | Requis |
|---|---|---:|
| `datePrecise` | string(date) | non |
| `dateDebut` | string(date) | non |
| `dateFin` | string(date) | non |

- Pas de `idSociete` ni `idUtilisateur` : dérivés du JWT.
- Accès : rôle **Caissier** ou Super-Admin.
- Réponse : même structure que `FinanceReporting/rapport-caisse` (`RapportCaisseDto`).
- Filtre date aligné dashboard caissier (`DatePaiement`, repli `DateCreation`).

Exemple :

```http
GET /api/CaissierDashboard/rapport-caisse?datePrecise=2026-06-30
Authorization: Bearer <token caissier>
```

| Champ | Sémantique |
|---|---|
| `statistiquesJournalieres.*` | Jour UTC ; encaissements filtrés sur `DatePaiement` (repli `DateCreation`) |
| `reservationsConfirmeesJour` | Réservations confirmées créées aujourd'hui par le caissier |
| `billetsEmisJour` | Lignes `Billets` émises aujourd'hui pour ces réservations |
| `nombreBilletsVendus` | **Legacy** — même valeur que `reservationsConfirmeesJour` |
| `recettesJournalieres[].recetteAutre` | Méthodes hors espèces / mobile / virement / carte |
| `performancesMensuelles.moisEnCours` | KPIs mois UTC en cours (scope caissier) |
| `performancesMensuelles.moisPrecedent` | KPIs mois UTC précédent |
| `performancesMensuelles.synthese.*` | Variations % mois en cours vs précédent |
| `performancesMensuelles.*.totalEncaissements` | Somme paiements validés (`DatePaiement`, repli `DateCreation`) |
| `performancesMensuelles.*.reservationsConfirmees` | Réservations confirmées du caissier (`DateReservation` dans le mois) |
| `performancesMensuelles.*.billetsEmis` | Billets émis (`Billets.DateGeneration` dans le mois) |
| `performancesMensuelles.moisEnCours.joursEcoules` | Jours écoulés dans le mois courant (UTC) |
| `performancesMensuelles.moisEnCours.moyenneEncaissementsJournaliers` | `totalEncaissements / joursEcoules` |
| `alertesCaissier` (voyage complet) | Scope société, pas caissier |
| Super-Admin sur cette route | Métriques du `UserId` token uniquement |

