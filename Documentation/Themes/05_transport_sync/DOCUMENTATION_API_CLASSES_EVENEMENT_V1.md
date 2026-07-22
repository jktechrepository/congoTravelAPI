# API classes événement — CongoTravelAPI V1

Référentiel `EvenementClasses` (ex. VIP, STD) utilisé par les modes d'inventaire **ClassQuota** et **SeatNumbered**.

Base route : `api/events/classes`

## Permissions

| Action | Permission |
|--------|------------|
| Liste, détail | `Evenement.Session.Read` |
| Création, mise à jour, toggle statut | `Evenement.Session.Write` |

Tenancy : société du JWT ; Super-Admin peut passer `?idSociete=` sur la liste.

## Endpoints

### Liste par société (route explicite)

```
GET /api/events/classes/societe/{idSociete}?actifsSeulement=false
```

Même résultat que `GET /api/events/classes?idSociete={id}` — utile pour alignement avec les référentiels transport (`CategorieSiege`).

### Liste (société JWT ou query Super-Admin)

```
GET /api/events/classes?idSociete={optional}&actifsSeulement=false
```

Retourne les classes triées par `codeClasse`. `actifsSeulement=true` exclut `statut=false`.

### Recherche par libellé

```
GET /api/events/classes/by-libelle?libelle=Zone%20VIP&idSociete={optional}
```

Correspondance **exacte** sur `libelle` (insensible à la casse), dans la société effective. `404` si aucun résultat.

### Détail

```
GET /api/events/classes/{id}
```

### Création

```
POST /api/events/classes
```

Body : `codeClasse`, `libelle`, `description` (optionnel). `codeClasse` unique par société.

### Mise à jour

```
PUT /api/events/classes/{id}
```

Body : `libelle`, `description`, `statut`. **`codeClasse` immuable** après création.

### Désactivation / réactivation

```
PUT /api/events/classes/{id}/toggle-statut
```

Inverse `statut` (pas de `DELETE` physique).

## Exemple

```json
POST /api/events/classes
{
  "codeClasse": "VIP",
  "libelle": "Zone VIP",
  "description": "Accès premium"
}
```

## Tests

- `EvenementClasseServiceTests`
- Filtre : `dotnet test --filter "FullyQualifiedName~EvenementClasse"`
