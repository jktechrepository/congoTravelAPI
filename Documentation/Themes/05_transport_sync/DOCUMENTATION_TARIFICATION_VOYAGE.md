# Tarification voyage par catégorie de siège

## Principe

- **Un prix = une catégorie de siège** (`idCategorieSiege`).
- Le montant facturé à la **réservation** provient de la table `VoyageTarifsCategorieSiege`, pas du seul champ `voyage.prix`.
- `voyage.prix` est un **prix de référence** (tarif ECO ou minimum des tarifs), recalculé automatiquement après modification des tarifs.

## Endpoints

| Méthode | Route | Permission | Usage |
|---------|-------|------------|-------|
| GET | `/api/Voyage/{id}/tarifs-categorie-siege` | `Voyage.Read` | Charger la grille tarifaire |
| PATCH | `/api/Voyage/{id}/tarifs-categorie-siege/{idCategorieSiege}` | `Voyage.Update` | Modifier **une** catégorie |
| PUT | `/api/Voyage/{id}/tarifs-categorie-siege` | `Voyage.Update` | Remplacer **toutes** les lignes |
| PUT | `/api/Voyage/{id}` avec `tarifs[]` | `Voyage.Update` | Mise à jour voyage + tarifs |

## Règle PUT `/api/Voyage/{id}`

Si le voyage possède déjà des tarifs catégorie :

- Modifier `prix` **sans** envoyer `tarifs[]` → **400 Bad Request**
- Modifier date, véhicule, site avec `prix` inchangé → **OK**

Message d'erreur type :

```json
{
  "message": "Pour modifier le prix, précisez la catégorie de siège via tarifs[], PUT /api/Voyage/{id}/tarifs-categorie-siege ou PATCH /api/Voyage/{id}/tarifs-categorie-siege/{idCategorieSiege}."
}
```

## Intégration frontend

### Écran « Modifier les tarifs »

1. `GET /api/Voyage/{id}/tarifs-categorie-siege`
2. Afficher une grille : `{ idCategorieSiege, codeCategorieSiege, libelleCategorie, prix }`
3. Sauvegarde d'une ligne : `PATCH /api/Voyage/{id}/tarifs-categorie-siege/{idCategorieSiege}`

```json
{ "prix": 10000 }
```

4. Sauvegarde globale : `PUT /api/Voyage/{id}/tarifs-categorie-siege`

```json
{
  "tarifs": [
    { "idCategorieSiege": 1, "prix": 7000 },
    { "idCategorieSiege": 2, "prix": 11000 }
  ]
}
```

### Écran « Modifier voyage » (date, véhicule, site…)

- Ne pas modifier `prix` seul si des tarifs catégorie existent.
- Si les tarifs changent dans le même formulaire, inclure le tableau `tarifs` complet dans le body PUT voyage.

### Affichage liste des voyages

- Si plusieurs catégories : afficher `tarifs[]` ou « à partir de {prix} » (référence ECO).
- Ne pas afficher un seul montant global si VIP ≠ ECO.

### Réservation / paiement

Le montant attendu = somme des tarifs des sièges attribués (selon la catégorie de chaque siège).

## Exemples curl

### Lire les tarifs

```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://api.example.com/api/Voyage/42/tarifs-categorie-siege
```

### Modifier le tarif ECO uniquement

```bash
curl -X PATCH -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"prix":7500}' \
  https://api.example.com/api/Voyage/42/tarifs-categorie-siege/3
```

### Erreur si prix global modifié sans tarifs

```bash
curl -X PUT -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"id":42,"prix":8000,...}' \
  https://api.example.com/api/Voyage/42
# → 400 si des tarifs catégorie existent déjà
```

## Voir aussi

- `SPEC_TECHNIQUE_WORKFLOW_RESERVATION_VOYAGE_V2.md` — calcul montant réservation
- `DOCUMENTATION_PLANIFICATION_VOYAGE.md` — tarifs au moment de la génération (template ≠ voyages existants)
