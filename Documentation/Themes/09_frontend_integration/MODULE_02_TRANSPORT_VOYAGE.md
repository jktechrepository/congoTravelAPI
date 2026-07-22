# MODULE 02 — Transport et voyage

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)

---

## Endpoints principaux

| Ressource | Base route | Permissions typiques |
|-----------|------------|---------------------|
| Voyage | `/api/Voyage` | `Voyage.Read`, `Voyage.Create`, `Voyage.Update` |
| Destination | `/api/Destination` | `Destination.Read` |
| Véhicule | `/api/Vehicule` | `Vehicule.Read` |
| Catégorie siège | `/api/CategorieSiege` | `CategorieSiege.Read` |
| Planification | `/api/PlanificationVoyage` | `Voyage.Create` |
| Type véhicule | `/api/TypeVehicule` | Référentiel |

---

## Format `heureDepart` (critique)

Le backend expose `heureDepart` en **string** `HH:mm:ss`, pas en objet.

```json
{
  "dateDepart": "2026-05-10T00:00:00",
  "heureDepart": "08:30:00",
  "prix": 15000,
  "codeDevisePrix": "CDF",
  "idVehicule": 4,
  "idDestination": 12,
  "idSociete": 1,
  "idSite": 3,
  "statut": true
}
```

---

## Recherche voyages (app client)

```
GET /api/Voyage/search?villeDepart=Kinshasa&villeArrivee=Matadi&date=2026-05-10&idSociete=1&pageNumber=1&pageSize=20
```

Query optionnelles : `periode` (`Jour`, `Hebdomadaire`, `Mensuel`, `Tout`), `searchTerm`, `sortBy`, `sortDescending`.

### Vue.js

```js
const { data } = await api.get('/Voyage/search', {
  params: { villeDepart, villeArrivee, date, idSociete: user.idSociete, pageNumber: 1, pageSize: 20 }
});
```

### Flutter

```dart
final resp = await api.get('/Voyage/search', queryParameters: {
  'villeDepart': villeDepart,
  'villeArrivee': villeArrivee,
  'date': date.toIso8601String(),
  'idSociete': idSociete,
  'pageNumber': 1,
  'pageSize': 20,
});
final voyages = resp.data['items'] ?? resp.data;
```

---

## Voyages du jour (app agent)

```
GET /api/Voyage/site/{idSite}/paged?date=2026-05-10&periode=Jour
```

---

## Sièges disponibles

```
GET /api/Voyage/{idVoyage}/sieges-disponibles
GET /api/Voyage/{idVoyage}/sieges-indisponibles
```

Utiliser avant réservation pour afficher le plan de bus.

---

## Tarification par catégorie

```
GET  /api/Voyage/{id}/tarifs-categorie-siege
PATCH /api/Voyage/{id}/tarifs-categorie-siege/{idCategorieSiege}
PUT  /api/Voyage/{id}/tarifs-categorie-siege
```

Voir [`DOCUMENTATION_TARIFICATION_VOYAGE.md`](../05_transport_sync/DOCUMENTATION_TARIFICATION_VOYAGE.md).

---

## Véhicule — répartition catégories sièges

Lors de la création véhicule, envoyer `repartitionCategorieSieges` :

```json
{
  "marques": "Mercedes",
  "aliasVehicule": "BUS-01",
  "idTypeVehicule": 2,
  "nombreSiege": 50,
  "idSociete": 1,
  "repartitionCategorieSieges": [
    { "idCategorieSiege": 1, "nombreSiegeParCategorie": 40 },
    { "idCategorieSiege": 2, "nombreSiegeParCategorie": 10 }
  ]
}
```

La somme des sièges par catégorie doit égaler `nombreSiege`.

---

## Planification récurrente

```
POST /api/PlanificationVoyage/{id}/generer
```

Body : `{ "mode": "SemaineCourante" | "MoisCourant" | "MoisProchain" | "PeriodePersonnalisee", ... }`

---

## Multi-devise voyage

Champs additionnels sur create/update :
- `codeDevisePrix` (ex. `USD`)
- Réponse : `codeDevisePrincipale`, `tauxVersDevisePrincipale`, `prixDevisePrincipale`

---

## Références backend

- [`SPEC_TECHNIQUE_WORKFLOW_RESERVATION_VOYAGE_V2.md`](../05_transport_sync/SPEC_TECHNIQUE_WORKFLOW_RESERVATION_VOYAGE_V2.md)
- [`DOCUMENTATION_PLANIFICATION_VOYAGE.md`](../05_transport_sync/DOCUMENTATION_PLANIFICATION_VOYAGE.md)
- [`DOCUMENTATION_TARIFICATION_VOYAGE.md`](../05_transport_sync/DOCUMENTATION_TARIFICATION_VOYAGE.md)
