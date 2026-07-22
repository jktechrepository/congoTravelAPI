# Documentation d'integration - Module Multi-devise

> **Guide portable (autre projet)** : voir à la racine du dépôt  
> [`Integration-MultiDevise-From-CongoTravelAPI.md`](../../../Integration-MultiDevise-From-CongoTravelAPI.md)  
> (architecture, SQL, algorithmes, checklist de portage).

## 1) Objectif

Ce document explique comment integrer le module multi-devise dans les applications web/mobile avec l'API CongoTravel.

Le module couvre:
- la gestion des devises par societe
- la devise principale (une seule par societe)
- la gestion des taux de change
- la conversion de preview
- l'impact sur les voyages, paiements, remboursements et reporting

---

## 2) Regles metier principales

- Une societe a **une seule** devise principale (`Societe.CodeDevisePrincipale`).
- Une devise est creee par societe (`IdSociete`).
- Une devise est unique par societe: `(IdSociete, CodeDevise)`.
- Une devise principale doit etre active.
- On ne peut pas desactiver la devise qui est actuellement principale sans basculer d'abord vers une autre devise.
- Les montants financiers sont en double:
  - devise d'origine (saisie/metier)
  - devise principale (consolidee)

---

## 3) Authentification et autorisation

Tous les endpoints du module devise sont proteges:
- Roles autorises: `Admin`, `Super-Admin`, `Gerant`
- `Super-Admin`: scope global
- `Admin`/`Gerant`: scope limite a leur societe

Route de base du module:
- `api/Devise`

---

## 4) Endpoints - Gestion des devises

## 4.1 Lister les devises actives

- `GET /api/Devise/devises`

Retourne les devises actives visibles selon le scope utilisateur.

Exemple de reponse:
```json
[
  {
    "idDeviseMonetaire": 12,
    "idSociete": 1,
    "codeDevise": "USD",
    "libelle": "Dollar americain",
    "symbole": "$",
    "estDevisePrincipale": false
  }
]
```

## 4.2 Creer une devise

- `POST /api/Devise/devises`

Body:
```json
{
  "idSociete": 1,
  "codeDevise": "EUR",
  "libelle": "Euro",
  "symbole": "EUR",
  "statut": true,
  "estDevisePrincipale": false
}
```

Notes:
- `codeDevise` est normalise en majuscule (3 caracteres).
- Si `estDevisePrincipale=true`, la societe bascule sa devise principale sur ce code.
- `estDevisePrincipale=true` avec `statut=false` est refuse.

## 4.3 Consulter une devise

- `GET /api/Devise/devises/{idDeviseMonetaire}`

Reponse:
```json
{
  "idDeviseMonetaire": 12,
  "idSociete": 1,
  "codeDevise": "USD",
  "libelle": "Dollar americain",
  "symbole": "$",
  "statut": true,
  "estDevisePrincipale": false,
  "dateCreation": "2026-05-08T16:00:00Z",
  "dateModification": null
}
```

## 4.4 Modifier une devise (libelle, symbole, statut, principal)

- `PUT /api/Devise/devises/{idDeviseMonetaire}`

Body:
```json
{
  "libelle": "Dollar americain",
  "symbole": "$",
  "statut": true,
  "estDevisePrincipale": true
}
```

Notes:
- `codeDevise` n'est pas modifiable.
- Si `estDevisePrincipale=true`, la societe bascule sa devise principale vers cette devise.
- Desactiver une devise principale actuelle est refuse.

## 4.5 Definir explicitement la devise principale

- `PUT /api/Devise/societe/{idSociete}/devise-principale/{codeDevise}`

Usage:
- endpoint direct de bascule devise principale, sans modifier les autres champs de la devise.

---

## 5) Endpoints - Taux de change

## 5.1 Creer un taux

- `POST /api/Devise/taux-change`

Body:
```json
{
  "idSociete": 1,
  "codeDeviseSource": "USD",
  "codeDeviseCible": "CDF",
  "taux": 2850.50,
  "dateEffet": "2026-05-08T10:30:00Z"
}
```

Regles:
- source != cible
- devises source/cible actives
- societe existante

## 5.2 Recuperer le dernier taux actif d'une paire

- `GET /api/Devise/taux-change?idSociete=1&source=USD&cible=CDF`

---

## 6) Endpoint de preview de conversion

- `GET /api/Devise/preview-conversion?idSociete=1&codeDeviseSource=USD&montant=25&datePaiement=2026-05-08T10:30:00Z`

Reponse:
```json
{
  "idSociete": 1,
  "codeDeviseSource": "USD",
  "codeDevisePrincipale": "CDF",
  "datePaiement": "2026-05-08T10:30:00Z",
  "taux": 2850.50,
  "montantSource": 25,
  "montantConverti": 71262.50
}
```

---

## 7) Impact sur les autres modules

## 7.1 Voyage

`POST /api/Voyage` inclut le code devise prix:
- `codeDevisePrix`

Le backend calcule/alimente aussi:
- `codeDevisePrincipale`
- `tauxVersDevisePrincipale`
- `prixDevisePrincipale`

## 7.2 Paiement

`POST /api/Paiement` inclut:
- `codeDevisePaiement`
- `datePaiement`

Le backend resolve le taux a date et stocke:
- `CodeDevisePrincipale`
- `TauxVersDevisePrincipale`
- `MontantAPayeDevisePrincipale`
- `MontantPayeDevisePrincipale`
- `ResteAPayeDevisePrincipale`

## 7.3 Remboursement

`POST /api/Remboursement` applique la meme logique de snapshot:
- devise remboursement
- devise principale
- taux
- montant converti devise principale

## 7.4 Reporting

- `GET /api/FinanceReporting/paiements/summary?idSociete=1&dateDebut=2026-05-01&dateFin=2026-05-31`

Les agregats sont consolides en devise principale, avec details utiles par devise d'origine.

---

## 8) Sequence d'integration recommandee (frontend)

1. Authentifier l'utilisateur (token JWT).
2. Charger `GET /api/Devise/devises`.
3. Charger la devise principale de la societe (ou endpoint metier associe).
4. Charger les taux necessaires (`GET /api/Devise/taux-change`).
5. Avant validation utilisateur, afficher une estimation via `GET /api/Devise/preview-conversion`.
6. Soumettre voyage/paiement/remboursement avec la devise source appropriee.
7. Afficher les montants retournes dans les deux devises.

---

## 9) Erreurs frequentes et gestion frontend

- `400 BadRequest`
  - code devise invalide
  - source == cible
  - devise inactive/inexistante
  - tentative de devise principale inactive
  - tentative de desactivation de la devise principale actuelle
- `403 Forbid`
  - tentative hors scope societe
- `404 NotFound`
  - societe/devise/taux introuvable
- `409 Conflict`
  - devise deja existante pour la societe (`IdSociete + CodeDevise`)

Recommendation frontend:
- afficher le `message` de l'API quand present
- mapper les statuts HTTP vers des toasts/messages utilisateur clairs

---

## 10) Checklist de test rapide

- [ ] creer une devise non principale
- [ ] creer une devise avec `estDevisePrincipale=true`
- [ ] verifier qu'une seule devise principale est active cote societe
- [ ] tenter de desactiver la devise principale actuelle (doit echouer)
- [ ] creer un taux USD->CDF et CDF->USD
- [ ] verifier `preview-conversion`
- [ ] creer un paiement en devise et verifier les champs snapshot
- [ ] lancer un remboursement et verifier les champs snapshot
- [ ] verifier le reporting consolide

