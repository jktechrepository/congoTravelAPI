# Plan de Travail : Ajout de TotalGeneralArriere dans le Dashboard

> **OBSOLÈTE / ANNULÉ** — Ce plan concernait le modèle Kenergie (arriérés/factures).  
> Le dashboard Admin société a été migré vers le transport (`GET /api/Dashboard/{idSociete}`).  
> Voir [`DOCUMENTATION_DASHBOARDS.md`](DOCUMENTATION_DASHBOARDS.md) section « Dashboard Admin société (transport) ».

## Objectif (historique)

Ajouter un champ `TotalGeneralArriere` dans la réponse de l'endpoint `GET /api/Dashboard/{idSociete}` qui affiche le total général des arriérés pour tous les clients de la société.

---

## 📊 Analyse de l'Existant

### Structure Actuelle du Dashboard

**Endpoint :** `GET /api/Dashboard/{idSociete}`  
**DTO :** `DashboardDto`  
**Service :** `DashboardService.GetDashboardStatsAsync(int idSociete)`

**Champs actuels :**
- `TotalAgents` : Nombre d'agents
- `TotalClientsActifs` : Nombre de clients actifs
- `PaiementsDuMois` : Total des paiements du mois
- `CollecteMois` : Détails de la collecte
- `RepartitionClientsParCategorie` : Répartition par catégorie
- `Top5AgentsCollecteurs` : Top 5 agents

### Logique de Filtrage par Société

Le service utilise déjà la logique pour récupérer les clients d'une société :
1. Récupère les `categoriesIds` de la société
2. Récupère les `clientsIds` via les usages -> catégories
3. Utilise ces IDs pour filtrer les paiements

**Code existant :**
```csharp
var categoriesIds = await _context.CategorieClients
    .Where(cc => cc.IdSociete == idSociete)
    .Select(cc => cc.IdCategorie)
    .ToListAsync();

var clientsIds = await _context.Clients
    .Where(c => c.Statut == true &&
               c.ClientsUsages != null &&
               c.ClientsUsages.Any(cu => cu.Usage != null &&
                                         cu.Usage.CategorieClient != null &&
                                         categoriesIds.Contains(cu.Usage.CategorieClient.IdCategorie)))
    .Select(c => c.IdClient)
    .ToListAsync();
```

---

## 💡 Proposition

### Calcul du TotalGeneralArriere

**Formule :**
```
TotalGeneralArriere = SUM(ClientFacture.MontantDu)
WHERE ClientFacture.IdClient IN (clientsIds)
  AND ClientFacture.Statut == true
  AND ClientFacture.MontantDu > 0
```

**Avantages :**
- ✅ Utilise la table `ClientFacture` (déjà optimisée)
- ✅ Réutilise la logique existante pour récupérer les clients de la société
- ✅ Simple et performant (une seule requête SQL)

---

## 📝 Plan d'Implémentation

### Phase 1 : Ajouter le champ dans le DTO (0.25 jour)

**Fichier :** `Models/DTOs/DashboardDto.cs`

**Modification :**
```csharp
public class DashboardDto
{
    // ... champs existants ...
    
    /// <summary>
    /// Total général des arriérés pour tous les clients de la société
    /// Somme de tous les MontantDu > 0 des ClientFacture des clients de la société
    /// </summary>
    public decimal TotalGeneralArriere { get; set; }
}
```

**Checklist :**
- [ ] Ajouter le champ `TotalGeneralArriere` dans `DashboardDto`
- [ ] Ajouter les commentaires XML
- [ ] Définir le type approprié (`decimal`)

---

### Phase 2 : Implémenter le calcul dans le service (0.5 jour)

**Fichier :** `Services/DashboardService.cs`

**Logique :**
1. Réutiliser les `clientsIds` déjà calculés (ou les recalculer si nécessaire)
2. Calculer le total des arriérés :
   ```csharp
   dashboard.TotalGeneralArriere = await _context.ClientFactures
       .Where(cf => cf.Statut == true &&
                   cf.MontantDu.HasValue &&
                   cf.MontantDu.Value > 0 &&
                   clientsIds.Contains(cf.IdClient))
       .SumAsync(cf => cf.MontantDu.Value);
   ```

**Placement :**
- Ajouter le calcul après la récupération des `clientsIds` (ligne ~62)
- Avant ou après le calcul des paiements du mois

**Checklist :**
- [ ] Ajouter le calcul de `TotalGeneralArriere`
- [ ] Utiliser les `clientsIds` existants
- [ ] Gérer le cas où il n'y a pas d'arriérés (retourne 0)
- [ ] Optimiser la requête si nécessaire

---

### Phase 3 : Tests (0.25 jour)

**Scénarios à tester :**
- [ ] Société avec clients ayant des arriérés
- [ ] Société sans clients
- [ ] Société avec clients mais sans arriérés
- [ ] Vérifier que le calcul est correct (comparer avec les données réelles)

**Checklist :**
- [ ] Tester l'endpoint dans Swagger
- [ ] Vérifier la valeur retournée
- [ ] Comparer avec les données de la base

---

## 📊 Structure de la Réponse Attendue

```json
{
  "totalAgents": 10,
  "totalClientsActifs": 150,
  "paiementsDuMois": 5000000,
  "totalGeneralArriere": 2500000,  // ✨ NOUVEAU
  "collecteMois": { ... },
  "repartitionClientsParCategorie": [ ... ],
  "top5AgentsCollecteurs": [ ... ]
}
```

---

## ⚠️ Points d'Attention

### 1. Performance
- ✅ **Optimisé** : Utilise une seule requête SQL avec `SumAsync`
- ✅ **Réutilise** : Les `clientsIds` sont déjà calculés
- ⚠️ **Attention** : Si beaucoup de clients, la requête peut être lente
- 💡 **Solution** : Ajouter un index sur `ClientFacture.IdClient` et `ClientFacture.MontantDu` si nécessaire

### 2. Cohérence des Données
- ✅ Utilise `ClientFacture` (source de vérité pour les arriérés)
- ✅ Filtre par `Statut == true` (seulement les actifs)
- ✅ Filtre par `MontantDu > 0` (seulement les arriérés)

### 3. Compatibilité
- ✅ Pas de breaking changes
- ✅ Nouveau champ optionnel (valeur par défaut = 0)
- ✅ Compatible avec le frontend existant

---

## 📊 Estimation Totale

| Phase | Durée | Description |
|-------|-------|-------------|
| Phase 1 | 0.25 jour | Ajout du champ dans le DTO |
| Phase 2 | 0.5 jour | Implémentation du calcul |
| Phase 3 | 0.25 jour | Tests |
| **TOTAL** | **1 jour** | |

---

## 🎯 Résultat Attendu

Un champ `TotalGeneralArriere` dans la réponse du dashboard qui affiche :
- ✅ Le total général des arriérés pour tous les clients de la société
- ✅ Calculé à partir de `ClientFacture.MontantDu > 0`
- ✅ Filtré par les clients de la société (via catégories)
- ✅ Performance optimisée (une seule requête SQL)

---

## 📝 Notes

- Le calcul utilise la même logique de filtrage par société que les paiements
- Réutilise les `clientsIds` déjà calculés pour éviter les requêtes redondantes
- Compatible avec l'architecture existante
- Facile à tester et valider

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0  
**Statut :** 📋 Plan prêt pour implémentation
