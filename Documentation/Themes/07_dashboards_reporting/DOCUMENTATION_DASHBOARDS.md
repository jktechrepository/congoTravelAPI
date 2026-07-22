# 📊 Documentation Complète des Dashboards et Statistiques - CongoTravel API

## 🎯 Vue d'ensemble

Cette documentation couvre tous les endpoints de dashboards et statistiques de l'API CongoTravel avec des exemples d'intégration pour Flutter (mobile) et Vue.js (web).

---

## 🔐 Authentification

Tous les endpoints nécessitent un token JWT d'authentification.

### Endpoint d'authentification
```
POST /api/Utilisateur/authentifier
```

### Corps de la requête
```json
{
  "emailOuTelephone": "admin@kenergie.cd",
  "motDePasse": "Admin",
  "fcmToken": "string",
  "deviceType": "string",
  "deviceModel": "string",
  "osVersion": "string"
}
```

### Réponse
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 7200,
  "utilisateur": {
    "idUtilisateur": 2,
    "nomComplet": "Administrateur CongoTravel",
    "email": "admin@kenergie.cd",
    "idSociete": 1,
    "roles": ["Admin", "Financier", "Caissier"]
  }
}
```

---

## Dashboard Super-Admin (transport)

### Endpoint
```
GET /api/SuperAdminDashboard
```

### Paramètres query (pagination des réservations)

Mêmes paramètres que `POST /api/Reservation/paged` (`PagedRequest`), passés en query string :

| Paramètre | Défaut | Description |
|-----------|--------|-------------|
| `pageNumber` | 1 | Numéro de page |
| `pageSize` | 20 | Taille de page |
| `searchTerm` | — | Recherche textuelle |
| `sortBy` | — | Tri (`date`, `statut`, `utilisateur`, `client`) |
| `sortDescending` | false | Tri décroissant |

Exemple : `GET /api/SuperAdminDashboard?pageNumber=1&pageSize=20&sortBy=date&sortDescending=true`

### Rôles autorisés
- Super-Admin uniquement (`IsSuperAdmin` dans le JWT)

### Réponse (extrait)
```json
{
  "globalStatistiques": {
    "totalSocietes": 3,
    "societesActives": 2,
    "totalClient": 150,
    "totalClientActif": 87,
    "totalReservation": 420,
    "totalVoyagesActifs": 45,
    "voyagesAujourdhui": 5,
    "voyagesSemaine": 12,
    "totalReservationsConfirmeesMois": 120,
    "totalReservationsConfirmeesJour": 8,
    "totalBilletsEmisMois": 115,
    "chiffreAffairesMois": 1250000.00,
    "nombreTransactionsMois": 98
  },
  "societes": [
    {
      "idSociete": 1,
      "nom": "CongoTravel Demo",
      "codeDevisePrincipale": "CDF",
      "voyagesMois": 20,
      "reservationsConfirmeesMois": 80,
      "billetsEmisMois": 75,
      "chiffreAffairesMois": 900000.00,
      "derniereActivite": "2026-05-30T10:15:00Z"
    }
  ],
  "top5SocietesCa": [
    { "rang": 1, "idSociete": 1, "nom": "CongoTravel Demo", "chiffreAffairesMois": 900000.00, "codeDevisePrincipale": "CDF" }
  ],
  "transactionsRecentes": [],
  "reservations": {
    "data": [
      {
        "idReservation": 101,
        "idClient": 12,
        "nomClient": "Jean Dupont",
        "statutReservation": "CONFIRMEE",
        "dateReservation": "2026-05-28T08:00:00Z",
        "nombreDePlace": 2
      }
    ],
    "totalCount": 420,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 21,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "collecteParOrigineGroupe": [],
  "collecteOrigineGroupeSynthese": {
    "partDigitalPourcentage": 25.0,
    "partGuichetPourcentage": 75.0,
    "montantClassifie": 1250000.00,
    "montantNonClassifie": 0
  },
  "dateGeneration": "2026-05-30T12:00:00Z"
}
```

**Distinction métriques réservations :**
- `totalReservation` : nombre cumulé de réservations actives (`statut == true`), toutes sociétés confondues.
- `totalReservationsConfirmeesMois` / `totalReservationsConfirmeesJour` : réservations confirmées filtrées par période.
- `reservations` : liste paginée (`PagedResult<ReservationResponseDto>`), même forme que `POST /api/Reservation/paged` (toutes réservations, sans filtre `statut`).

**Note :** l'ancien endpoint `GET /api/SuperAdmin/dashboard` (modèle Kenergie factures/arriérés) est remplacé par cette route transport.

---

## Dashboard Admin société (transport)

### Endpoint
```
GET /api/Dashboard/{idSociete}
```

### Rôles / permissions
- Permission `Dashboard.ReadAll`
- `idSociete` doit correspondre au `idSociete` du JWT (403 sinon)

### Réponse (extrait)
```json
{
  "codeDevisePrincipale": "CDF",
  "totalAgents": 15,
  "totalClientsActifs": 320,
  "transportStatistiques": {
    "voyagesActifs": 12,
    "voyagesAujourdhui": 2,
    "voyagesSemaine": 5,
    "voyagesMois": 18,
    "reservationsConfirmeesMois": 45,
    "reservationsConfirmeesJour": 3,
    "billetsEmisMois": 42
  },
  "collecteMois": {
    "moisLabel": "mai 2026",
    "montant": 2500000.00,
    "montantMoisPrecedent": 2200000.00,
    "variationPourcentage": 13.64,
    "nombrePaiements": 450,
    "ticketMoyen": 5555.56,
    "variationTicketMoyen": 2.5
  },
  "collecteParOrigineGroupe": [
    {
      "origineGroupe": "AGENT",
      "montant": 1800000.00,
      "nombrePaiements": 320,
      "montantMoisPrecedent": 1700000.00,
      "variationPourcentage": 5.88,
      "partPourcentage": 72.0
    },
    {
      "origineGroupe": "CLIENT",
      "montant": 600000.00,
      "nombrePaiements": 95,
      "montantMoisPrecedent": 450000.00,
      "variationPourcentage": 33.33,
      "partPourcentage": 24.0
    },
    {
      "origineGroupe": "INCONNU",
      "montant": 100000.00,
      "nombrePaiements": 35,
      "montantMoisPrecedent": 80000.00,
      "variationPourcentage": 25.0,
      "partPourcentage": 4.0
    }
  ],
  "collecteOrigineGroupeSynthese": {
    "partDigitalPourcentage": 25.0,
    "partGuichetPourcentage": 75.0,
    "montantClassifie": 2400000.00,
    "montantNonClassifie": 100000.00
  },
  "top5AgentsCollecteurs": [
    {
      "idAgent": 1,
      "matricule": "AGT001",
      "nomComplet": "Jean Dupont",
      "montantCollecte": 500000.00,
      "nombrePaiements": 120
    }
  ],
  "dateGeneration": "2026-05-30T12:00:00Z"
}
```

**Breaking change v1 transport :** champs supprimés — `paiementsDuMois`, `totalGeneralArriere`, `factureMois`, `repartitionClientsParCategorie`. Utiliser `collecteMois.montant` et `transportStatistiques`.

**Collecte Client vs Agent :** `collecteParOrigineGroupe` contient toujours 3 entrées (`CLIENT`, `AGENT`, `INCONNU`). Les KPIs `partDigitalPourcentage` / `partGuichetPourcentage` excluent `INCONNU` du dénominateur (données pré-migration).

---

## Dashboard Financier (transport)

### Endpoint
```
GET /api/FinancierDashboard
```

### Rôles autorisés
- `HasFinanceAccess` : Super-Admin, Gérant, Financier
- Super-Admin : agrégation **toutes sociétés actives**
- Financier / Gérant : scope **idSociete du JWT** uniquement

### Réponse (extrait)
```json
{
  "globalStatistiques": {
    "chiffreAffairesMois": 1250000.00,
    "chiffreAffairesMoisPrecedent": 980000.00,
    "variationPourcentage": 27.55,
    "montantReservationsNonPayees": 320000.00,
    "tauxPaiementGlobal": 79.62,
    "nombreTotalTransactions": 98,
    "moyenneTransaction": 12755.10,
    "nombreTotalReservations": 120,
    "nombreTotalVoyages": 45,
    "tauxRemplissageMoyen": 68.50
  },
  "societesFinancieres": [
    {
      "idSociete": 1,
      "nomSociete": "CongoTravel Demo",
      "codeDevisePrincipale": "CDF",
      "chiffreAffairesMois": 900000.00,
      "montantReservationsNonPayees": 150000.00,
      "tauxPaiement": 85.71,
      "nombreTransactions": 75,
      "nombreReservations": 80,
      "nombreVoyages": 20,
      "statutFinancier": "Bon",
      "tauxRemplissageMoyen": 72.00,
      "collecteParOrigineGroupe": [],
      "collecteOrigineGroupeSynthese": {
        "partDigitalPourcentage": 25.0,
        "partGuichetPourcentage": 75.0,
        "montantClassifie": 900000.00,
        "montantNonClassifie": 0
      }
    }
  ],
  "collecteParOrigineGroupe": [],
  "collecteOrigineGroupeSynthese": {
    "partDigitalPourcentage": 25.0,
    "partGuichetPourcentage": 75.0,
    "montantClassifie": 1250000.00,
    "montantNonClassifie": 0
  },
  "transactionsRecentes": [],
  "alertesFinancieres": [],
  "tendances": {
    "revenusTransport": [{ "mois": "juin 2025", "annee": 2025, "valeur": 0 }],
    "encaissements": [],
    "tauxPaiement": [],
    "nombreReservations": [],
    "nombreVoyages": []
  },
  "dateGeneration": "2026-05-30T12:00:00Z"
}
```

**Breaking change v1 :** route unique — supprimer les appels aux sous-routes `/statistiques-globales`, `/societes-financieres`, etc. Champs renommés : `revenusTransport` → `chiffreAffairesMois`, suppression de `montantEncaisse`.

---

## Dashboard Gérant (transport)

### Endpoint
```
GET /api/GerantDashboard
```

### Rôles autorisés
- Gérant ou Super-Admin uniquement
- Société : `idSociete` du JWT (403 si absent/invalide)

### Scope données
Toutes les métriques sont filtrées sur **le site du gérant connecté** (`IdSite` du JWT), en plus de la société. Un gérant ne voit que les paiements, réservations, voyages, billets et clients liés à son site.

**Repli société :** si le token ne contient pas d'`IdSite` valide, le dashboard agrège les données de **toute la société** (comportement legacy).

**Super-Admin :** autorisé sur la route, mais les chiffres restent ceux du site du token (utiliser `SuperAdminDashboard` ou `FinancierDashboard` pour une vue globale).

**Données legacy :** en mode site, les enregistrements avec `IdSite` null sont exclus du périmètre.

### Réponse (extrait)
```json
{
  "societeStatistiques": {
    "nomSociete": "CongoTravel Demo",
    "codeDevisePrincipale": "CDF",
    "totalClients": 320,
    "clientsActifs": 280,
    "chiffreAffairesMois": 900000.00,
    "chiffreAffairesMoisPrecedent": 750000.00,
    "variationPourcentage": 20.00,
    "montantReservationsNonPayees": 150000.00,
    "tauxPaiement": 85.71
  },
  "clientsStatistiques": {
    "totalClients": 320,
    "clientsActifs": 280,
    "nouveauxClientsMois": 12,
    "clientsAvecReservationsNonPayees": 18
  },
  "transportStatistiques": {
    "voyagesActifs": 12,
    "voyagesAujourdhui": 2,
    "reservationsConfirmeesMois": 80,
    "billetsEmisMois": 75
  },
  "top5ClientsCA": [
    { "rang": 1, "idClient": 1, "nomClient": "Client A", "valeur": 50000.00 }
  ],
  "top5ClientsNonPayes": [
    { "rang": 1, "idClient": 2, "nomClient": "Client B", "valeur": 12000.00 }
  ],
  "alertesSociete": [],
  "tendances": {
    "evolutionChiffreAffaires": [{ "mois": "mai 2026", "annee": 2026, "valeur": 900000.00 }],
    "evolutionTauxPaiement": [{ "mois": "mai 2026", "annee": 2026, "valeur": 85.71 }],
    "evolutionReservationsConfirmees": [{ "mois": "mai 2026", "annee": 2026, "valeur": 80 }]
  },
  "paiementsStatistiques": {
    "paiementsJour": 50000.00,
    "paiementsMois": 900000.00,
    "nombrePaiementsMois": 75
  },
  "collecteParOrigineGroupe": [],
  "collecteOrigineGroupeSynthese": {
    "partDigitalPourcentage": 25.0,
    "partGuichetPourcentage": 75.0,
    "montantClassifie": 900000.00,
    "montantNonClassifie": 0
  },
  "dateGeneration": "2026-05-30T12:00:00Z"
}
```

**Breaking change v1 :** route unique `GET /api/GerantDashboard` — supprimer `/societe/{idSociete}`, `/statistiques`, `/alertes`. Champs supprimés : arriérés, factures, catégories clients, `top5ClientsArrieres` → `top5ClientsNonPayes`.

---

## Dashboard Caissier (transport)

### Endpoint
```
GET /api/CaissierDashboard
```

### Rôles autorisés
- Caissier ou Super-Admin uniquement
- Société : `idSociete` du JWT (403 si absent/invalide)

### Scope données
Toutes les métriques sont filtrées sur **le caissier connecté** (`IdUtilisateur` du JWT), en plus de la société. Un caissier ne voit que ses propres encaissements et réservations.

**Exception** : alertes « Voyage bientôt complet » = scope **société entière** (places restantes demain).

**Super-Admin** : autorisé sur la route, mais les chiffres restent ceux du `UserId` du token (utiliser `SuperAdminDashboard` pour la vue globale).

**Date d'encaissement** : filtres journaliers sur `DatePaiement` (repli `DateCreation` si `DatePaiement` non renseigné) — aligné avec les dashboards société.

### Réponse (extrait)
```json
{
  "statistiquesJournalieres": {
    "totalRevenusTransport": 125000.00,
    "nombreTransactions": 18,
    "moyenneTransaction": 6944.44,
    "plusGrosMontant": 25000.00,
    "plusPetitMontant": 5000.00,
    "nombrePassagers": 22,
    "totalReservationsNonPayees": 45000.00,
    "nombreBilletsVendus": 15,
    "reservationsConfirmeesJour": 15,
    "billetsEmisJour": 12,
    "tauxRemplissageMoyen": 30.00
  },
  "paiementsEnCours": [],
  "paiementsRecents": [],
  "recettesJournalieres": [],
  "alertesCaissier": [],
  "resumeCaisse": {
    "totalEntrees": 125000.00,
    "dateCloture": "2026-05-28T12:00:00Z",
    "statutCaisse": "Ouverte",
    "totalBilletsVendus": 15,
    "reservationsConfirmeesJour": 15,
    "billetsEmisJour": 12,
    "reservationsConfirmees": 15,
    "reservationsEnAttente": 2,
    "tauxRemplissageMoyen": 30.00
  },
  "performancesMensuelles": {
    "moisEnCours": {
      "periodeDebut": "2026-05-01T00:00:00Z",
      "periodeFin": "2026-06-01T00:00:00Z",
      "libelle": "mai 2026",
      "totalEncaissements": 450000.00,
      "nombreTransactions": 62,
      "moyenneTransaction": 7258.06,
      "nombrePassagers": 78,
      "reservationsConfirmees": 55,
      "billetsEmis": 48,
      "joursEcoules": 28,
      "moyenneEncaissementsJournaliers": 16071.43,
      "recetteEspece": 300000.00,
      "recetteMobileMoney": 100000.00,
      "recetteVirement": 20000.00,
      "recetteCarte": 25000.00,
      "recetteAutre": 5000.00
    },
    "moisPrecedent": {
      "periodeDebut": "2026-04-01T00:00:00Z",
      "periodeFin": "2026-05-01T00:00:00Z",
      "libelle": "avril 2026",
      "totalEncaissements": 400000.00,
      "nombreTransactions": 58,
      "moyenneTransaction": 6896.55,
      "nombrePassagers": 70,
      "reservationsConfirmees": 52,
      "billetsEmis": 46,
      "joursEcoules": null,
      "moyenneEncaissementsJournaliers": null,
      "recetteEspece": 280000.00,
      "recetteMobileMoney": 90000.00,
      "recetteVirement": 15000.00,
      "recetteCarte": 12000.00,
      "recetteAutre": 3000.00
    },
    "synthese": {
      "variationEncaissementsPourcentage": 12.50,
      "variationTransactionsPourcentage": 6.90,
      "variationReservationsPourcentage": 5.77,
      "variationBilletsEmisPourcentage": 4.35
    }
  },
  "codeDevisePrincipale": "CDF",
  "dateGeneration": "2026-05-28T12:00:00Z"
}
```

**Performances mensuelles** : mois calendaire UTC en cours vs mois précédent, scope caissier. Encaissements sur `DatePaiement` (repli `DateCreation`). `joursEcoules` et `moyenneEncaissementsJournaliers` uniquement sur `moisEnCours`. Variations % via la même formule que Gérant/Financier.

**Breaking change v1 :** route unique — supprimer les sous-routes `/statistiques-journalieres`, `/paiements-en-cours`, `/paiements-recents`, `/recettes-journalieres`, `/alertes-caissier`, `/resume-caisse`. Champs supprimés de `resumeCaisse` : `soldeInitial`, `totalSorties`, `soldeFinal`, `ecart` (module caisse physique non implémenté v1). Ajout de `codeDevisePrincipale` à la racine. Montants agrégés via `MontantPayeDevisePrincipale` (fallback `MontantPaye`).

**KPIs clarifiés (audit cohérence)** :
- `nombreBilletsVendus` : alias legacy = `reservationsConfirmeesJour` (réservations confirmées du jour, pas la table `Billets`).
- `billetsEmisJour` : billets réellement émis (`Billets.DateGeneration` du jour).
- `recetteAutre` : méthodes de paiement hors espèces / mobile / virement / carte.
- `tauxRemplissageMoyen` : places vendues / capacité des voyages concernés (une fois par voyage).

---

## 👨‍💼 Dashboard Technicien

### Endpoint
```
GET /api/Technicien/dashboard
```

### Rôles autorisés
- Technicien
- Super-Admin

### Réponse complète
```json
{
  "statistiquesPersonnelles": {
    "totalInterventions": 0,
    "interventionsMois": 0,
    "interventionsTerminees": 0,
    "interventionsEnCours": 0,
    "tempsMoyenIntervention": 0,
    "tauxResolution": 0
  },
  "interventionsRecentes": [],
  "pannesSignalees": [],
  "alertesTechnicien": [],
  "performanceTechnicien": {
    "interventionsJour": 0,
    "interventionsSemaine": 0,
    "interventionsMois": 0,
    "tempsMoyenResolution": 0,
    "satisfactionClient": 0
  },
  "dateGeneration": "2026-02-15T22:45:15.291346+02:00"
}
```

---

## Dashboard Client (transport v1)

### Endpoint
```
GET /api/ClientDashboard
```

### Rôles autorisés
- Client ou Super-Admin uniquement
- Scope : `IdClient` du JWT (claim `IdClient` / fallback `Utilisateur.IdClient`)
- `403` si le client ne peut pas être résolu

### Scope données
Toutes les métriques sont filtrées sur **le client connecté** (`IdClient`). Un client peut réserver chez plusieurs sociétés : les paiements ne sont **pas** filtrés par `SocieteId` JWT.

### Réponse (extrait)
```json
{
  "statistiques": {
    "montantTotalReservations": 5000.00,
    "montantTotalPaye": 5000.00,
    "montantTotalDu": 0,
    "nombreReservations": 1,
    "nombreReservationsPayees": 1,
    "nombreReservationsEnRetard": 0,
    "tauxPaiement": 100.00,
    "nombreVoyagesEffectues": 0,
    "destinationFavorite": "Goma"
  },
  "reservationsRecentes": [],
  "paiementsRecents": [],
  "voyagesClient": [],
  "alertesClient": [],
  "resumeClient": {
    "statutCompte": "Actif",
    "nombreReservationsActives": 1,
    "nombreVoyagesCeMois": 1,
    "depensesCeMois": 5000.00,
    "destinationFavorite": "Goma"
  },
  "codeDevisePrincipale": "CDF",
  "dateGeneration": "2026-05-28T12:00:00Z"
}
```

**Breaking change v1 :** route unique — supprimer les sous-routes `/statistiques`, `/reservations-recentes`, `/paiements-recents`, `/voyages-client`, `/alertes-client`, `/resume-client`. Champs supprimés : `moyenneEvaluations`, `evaluation` (voyage), `derniereConnexion`. Ajout de `codeDevisePrincipale` à la racine. Montants agrégés via `MontantPayeDevisePrincipale` / `PrixDevisePrincipale` (fallback `MontantPaye` / `Prix`).

---

## Statistiques transport v1

### Endpoint
```
GET /api/Statistiques/{idSociete}?debut=&fin=
```

### Rôles / permissions
- Permission `Statistiques.ReadAll`
- Super-Admin : toute société active
- Autres rôles : `idSociete` route doit correspondre au `SocieteId` JWT (403 sinon)

### Période
- Query `debut` / `fin` optionnelles (UTC)
- Défaut : mois courant UTC

### Réponse (extrait)
```json
{
  "generales": {
    "totalClients": 120,
    "totalReservations": 85,
    "totalVoyages": 12,
    "totalBillets": 80,
    "totalPaiements": 500000.00,
    "montantReservationsNonPayees": 45000.00,
    "tauxPaiement": 91.74,
    "totalPaiementsCount": 75
  },
  "financieres": {
    "chiffreAffaires": 500000.00,
    "montantPaye": 500000.00,
    "montantDu": 45000.00,
    "evolutionMensuelle": [],
    "repartitionPaiements": []
  },
  "operationnelles": {
    "repartitionParDestination": [],
    "repartitionParTypeVehicule": [],
    "statistiquesVoyagesMois": [],
    "clientActivite": {},
    "transportStatistiques": {}
  },
  "performance": {
    "tauxPaiementGlobal": 91.74,
    "topAgents": [],
    "performanceMensuelle": []
  },
  "periode": { "dateDebut": "2026-05-01", "dateFin": "2026-05-31", "libellePeriode": "mai 2026" },
  "codeDevisePrincipale": "CDF",
  "dateGeneration": "2026-05-28T12:00:00Z"
}
```

**Breaking change v1 :** route unique — supprimer `/generales`, `/financieres`, `/operationnelles`, `/performance`, `/consolidees`. Champs Kenergie supprimés : factures, arriérés, recouvrement, catégories clients, axes/cabines. Module désormais implémenté via `StatistiquesService` (DI activée).
