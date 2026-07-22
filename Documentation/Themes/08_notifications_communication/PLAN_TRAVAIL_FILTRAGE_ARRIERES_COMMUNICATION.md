# 📋 Plan de Travail : Filtrage par Nombre de Factures en Arriérés dans les Communications

## 🎯 Objectif

Permettre de cibler les clients dans les campagnes de communication (ex: "Avis de coupure") en fonction du **nombre de factures en arriérés** qu'ils possèdent. Cela permettra d'envoyer des communications spécifiques aux clients ayant un certain nombre de factures impayées.

---

## 📊 Analyse de l'Existant

### Structure Actuelle

1. **`CriteresCiblageDto`** : DTO contenant les critères de ciblage
   - `IdCategorieClients` : Filtre par catégories
   - `ClientsActifs` : Filtre par statut actif
   - `IdSociete` : Filtre par société
   - `Usage` : Filtre par usage
   - `ListeIdClients` : Liste spécifique d'IDs

2. **`ClientFilterService`** : Service qui applique les critères de filtrage
   - Méthode `GetClientsByCriteriaAsync` qui filtre les clients selon les critères
   - Utilise des requêtes LINQ avec `Where` et `Include`

3. **`ClientFacture`** : Modèle contenant les informations de facturation par client
   - `MontantDu` : Montant dû (arriérés)
   - `Statut` : Statut actif/inactif
   - `IdClient` : Lien vers le client
   - `IdFacture` : Lien vers la facture

### Données Disponibles

Pour filtrer par nombre de factures en arriérés, nous pouvons utiliser :
- `ClientFacture` avec `MontantDu > 0` et `Statut == true`
- Compter le nombre de `ClientFacture` distinctes par client
- Filtrer les clients ayant un nombre de factures en arriérés dans une plage donnée

---

## 🔄 Changements Proposés

### Phase 1 : Extension du DTO de Critères

#### Objectif
Ajouter les champs nécessaires pour filtrer par nombre de factures en arriérés.

#### Modifications

**Fichier : `Models/DTOs/Communication/CriteresCiblageDto.cs`**

```csharp
public class CriteresCiblageDto
{
    // ... champs existants ...
    
    /// <summary>
    /// ✨ NOUVEAU : Nombre minimum de factures en arriérés (inclusif)
    /// Si spécifié, seuls les clients ayant au moins ce nombre de factures avec MontantDu > 0 seront ciblés
    /// </summary>
    public int? NombreFacturesArrieresMin { get; set; }
    
    /// <summary>
    /// ✨ NOUVEAU : Nombre maximum de factures en arriérés (inclusif)
    /// Si spécifié, seuls les clients ayant au plus ce nombre de factures avec MontantDu > 0 seront ciblés
    /// </summary>
    public int? NombreFacturesArrieresMax { get; set; }
}
```

**Exemples d'utilisation :**
- `NombreFacturesArrieresMin = 3` : Clients avec **au moins 3** factures en arriérés
- `NombreFacturesArrieresMax = 5` : Clients avec **au plus 5** factures en arriérés
- `NombreFacturesArrieresMin = 2` et `NombreFacturesArrieresMax = 4` : Clients avec **entre 2 et 4** factures en arriérés

---

### Phase 2 : Implémentation du Filtrage dans ClientFilterService

#### Objectif
Implémenter la logique de filtrage par nombre de factures en arriérés dans le service.

#### Modifications

**Fichier : `Services/ClientFilterService.cs`**

**Stratégie :**
1. Récupérer tous les clients correspondant aux autres critères
2. Pour chaque client, compter le nombre de `ClientFacture` avec `MontantDu > 0` et `Statut == true`
3. Filtrer selon `NombreFacturesArrieresMin` et `NombreFacturesArrieresMax`

**Approche Optimisée :**
- Utiliser un `GroupBy` sur `ClientFacture` pour compter par client
- Joindre avec la liste de clients déjà filtrés
- Appliquer les filtres min/max

**Code proposé :**

```csharp
// ✨ NOUVEAU : Filtrer par nombre de factures en arriérés
if (criteres.NombreFacturesArrieresMin.HasValue || criteres.NombreFacturesArrieresMax.HasValue)
{
    // Récupérer les IDs des clients avec le nombre de factures en arriérés
    var clientsAvecArrieres = await _context.ClientFactures
        .Where(cf => cf.Statut == true && 
                     cf.MontantDu.HasValue && 
                     cf.MontantDu.Value > 0)
        .GroupBy(cf => cf.IdClient)
        .Select(g => new
        {
            IdClient = g.Key,
            NombreFacturesArrieres = g.Count()
        })
        .ToListAsync();

    // Créer un dictionnaire pour accès rapide
    var dictArrieres = clientsAvecArrieres
        .ToDictionary(x => x.IdClient, x => x.NombreFacturesArrieres);

    // Filtrer les clients selon les critères min/max
    var idsClientsFiltres = new List<int>();
    
    foreach (var client in clients)
    {
        var nombreArrieres = dictArrieres.GetValueOrDefault(client.IdClient, 0);
        
        // Vérifier le minimum
        if (criteres.NombreFacturesArrieresMin.HasValue && 
            nombreArrieres < criteres.NombreFacturesArrieresMin.Value)
        {
            continue;
        }
        
        // Vérifier le maximum
        if (criteres.NombreFacturesArrieresMax.HasValue && 
            nombreArrieres > criteres.NombreFacturesArrieresMax.Value)
        {
            continue;
        }
        
        idsClientsFiltres.Add(client.IdClient);
    }
    
    // Filtrer la query avec les IDs valides
    query = query.Where(c => idsClientsFiltres.Contains(c.IdClient));
}
```

**Alternative Optimisée (requête unique) :**

```csharp
// ✨ NOUVEAU : Filtrer par nombre de factures en arriérés
if (criteres.NombreFacturesArrieresMin.HasValue || criteres.NombreFacturesArrieresMax.HasValue)
{
    // Sous-requête pour compter les factures en arriérés par client
    var minArrieres = criteres.NombreFacturesArrieresMin ?? 0;
    var maxArrieres = criteres.NombreFacturesArrieresMax ?? int.MaxValue;
    
    var clientsIdsAvecArrieres = await _context.ClientFactures
        .Where(cf => cf.Statut == true && 
                     cf.MontantDu.HasValue && 
                     cf.MontantDu.Value > 0)
        .GroupBy(cf => cf.IdClient)
        .Where(g => g.Count() >= minArrieres && g.Count() <= maxArrieres)
        .Select(g => g.Key)
        .ToListAsync();
    
    // Filtrer la query avec les IDs valides
    query = query.Where(c => clientsIdsAvecArrieres.Contains(c.IdClient));
}
```

**Recommandation :** Utiliser l'**alternative optimisée** car elle est plus performante (une seule requête) et évite de charger tous les clients en mémoire.

---

### Phase 3 : Tests et Validation

#### Objectif
S'assurer que le filtrage fonctionne correctement dans différents scénarios.

#### Scénarios de Test

1. **Test 1 : Filtre minimum uniquement**
   - Créer des clients avec 0, 2, 5 factures en arriérés
   - Filtrer avec `NombreFacturesArrieresMin = 3`
   - **Résultat attendu** : Seuls les clients avec ≥ 3 factures en arriérés

2. **Test 2 : Filtre maximum uniquement**
   - Créer des clients avec 0, 2, 5, 10 factures en arriérés
   - Filtrer avec `NombreFacturesArrieresMax = 4`
   - **Résultat attendu** : Seuls les clients avec ≤ 4 factures en arriérés

3. **Test 3 : Filtre min et max**
   - Créer des clients avec 0, 2, 5, 10 factures en arriérés
   - Filtrer avec `NombreFacturesArrieresMin = 3` et `NombreFacturesArrieresMax = 7`
   - **Résultat attendu** : Seuls les clients avec entre 3 et 7 factures en arriérés

4. **Test 4 : Combinaison avec autres critères**
   - Filtrer par société + nombre de factures en arriérés
   - **Résultat attendu** : Clients de la société ET avec le nombre de factures requis

5. **Test 5 : Client sans factures en arriérés**
   - Client avec `MontantDu = 0` ou `null`
   - **Résultat attendu** : Non inclus si `NombreFacturesArrieresMin >= 1`

---

### Phase 4 : Documentation et Exemples

#### Objectif
Documenter la nouvelle fonctionnalité pour les développeurs frontend et les utilisateurs.

#### Modifications

**Fichier : `docs/API_DOCUMENTATION_COMMUNICATION.md`**

Ajouter une section sur le filtrage par nombre de factures en arriérés :

```markdown
### Filtrage par Nombre de Factures en Arriérés

Vous pouvez cibler les clients en fonction du nombre de factures qu'ils ont en arriérés (MontantDu > 0).

**Exemple : Cibler les clients avec au moins 3 factures en arriérés**

```json
{
  "titre": "Avis de coupure",
  "contenu": "Vous avez plusieurs factures en arriérés. Veuillez régulariser votre situation.",
  "typeCampagne": "ALERTE",
  "criteresCiblage": {
    "idSociete": 1,
    "nombreFacturesArrieresMin": 3
  }
}
```

**Exemple : Cibler les clients avec entre 2 et 5 factures en arriérés**

```json
{
  "titre": "Rappel de paiement",
  "contenu": "Vous avez des factures en attente de paiement.",
  "typeCampagne": "ALERTE",
  "criteresCiblage": {
    "idSociete": 1,
    "nombreFacturesArrieresMin": 2,
    "nombreFacturesArrieresMax": 5
  }
}
```
```

---

## 📈 Avantages de cette Approche

### 1. **Flexibilité**
- ✅ Permet de cibler précisément selon le nombre de factures en arriérés
- ✅ Peut être combiné avec les autres critères existants
- ✅ Supporte des plages (min/max) pour un ciblage granulaire

### 2. **Performance**
- ✅ Utilise une requête optimisée avec `GroupBy` en base de données
- ✅ Évite de charger tous les clients en mémoire
- ✅ Utilise les index sur `ClientFacture` (IdClient, Statut, MontantDu)

### 3. **Cohérence**
- ✅ Utilise `ClientFacture` comme source de vérité (cohérent avec le reste du système)
- ✅ Respecte le `Statut` de `ClientFacture` (soft delete)
- ✅ Utilise `MontantDu > 0` pour identifier les arriérés

### 4. **Compatibilité**
- ✅ Les champs sont optionnels (pas de breaking changes)
- ✅ Si non spécifiés, le comportement reste inchangé
- ✅ Peut être combiné avec tous les autres critères existants

---

## ⚠️ Points d'Attention

### 1. **Performance sur Grandes Volumes**
- ⚠️ Si beaucoup de clients et de `ClientFacture`, la requête `GroupBy` peut être coûteuse
- ✅ **Solution** : S'assurer que les index sont présents sur `ClientFacture.IdClient`, `ClientFacture.Statut`, `ClientFacture.MontantDu`
- ✅ **Optimisation future** : Ajouter un champ calculé `NombreFacturesArrieres` dans la table `Client` (nécessiterait une migration)

### 2. **Factures Sans IdClient**
- ⚠️ Les `ClientFacture` sans `IdClient` ne seront pas comptées
- ✅ **Comportement attendu** : Ces cas sont rares et ne devraient pas exister normalement

### 3. **Factures Pré-existantes**
- ⚠️ Les arriérés pré-existants (`EstArrierePreExistant = true`) sont inclus dans le comptage
- ✅ **Comportement attendu** : C'est le comportement souhaité pour un ciblage complet

### 4. **Clients Sans Factures**
- ⚠️ Les clients sans aucune `ClientFacture` auront 0 factures en arriérés
- ✅ **Comportement attendu** : Ils ne seront pas inclus si `NombreFacturesArrieresMin >= 1`

---

## 🧪 Tests Recommandés

### Tests Unitaires
1. ✅ Tester `GetClientsByCriteriaAsync` avec `NombreFacturesArrieresMin`
2. ✅ Tester `GetClientsByCriteriaAsync` avec `NombreFacturesArrieresMax`
3. ✅ Tester `GetClientsByCriteriaAsync` avec min et max
4. ✅ Tester la combinaison avec d'autres critères

### Tests d'Intégration
1. ✅ Créer une campagne avec filtrage par nombre de factures en arriérés
2. ✅ Vérifier que seuls les clients correspondants reçoivent la communication
3. ✅ Tester avec différents scénarios (0, 1, 5, 10 factures en arriérés)

### Tests de Performance
1. ✅ Tester avec un grand nombre de clients (1000+)
2. ✅ Tester avec un grand nombre de `ClientFacture` (10000+)
3. ✅ Mesurer le temps d'exécution de la requête

---

## 📝 Résumé des Actions

| Phase | Action | Fichier | Priorité |
|-------|--------|---------|----------|
| **Phase 1** | Ajouter `NombreFacturesArrieresMin` et `NombreFacturesArrieresMax` au DTO | `CriteresCiblageDto.cs` | 🔴 **Haute** |
| **Phase 2** | Implémenter le filtrage dans `ClientFilterService` | `ClientFilterService.cs` | 🔴 **Haute** |
| **Phase 3** | Tests et validation | Tests unitaires/intégration | 🟡 **Moyenne** |
| **Phase 4** | Documentation | `API_DOCUMENTATION_COMMUNICATION.md` | 🟢 **Basse** |

---

## 🎯 Exemple d'Utilisation Final

### Scénario : "Avis de coupure pour clients avec 3+ factures en arriérés"

**Requête :**
```json
POST /api/CommunicationCampaign
{
  "titre": "Avis de coupure imminente",
  "contenu": "Vous avez plusieurs factures en arriérés. Veuillez régulariser votre situation sous 7 jours, faute de quoi une coupure sera effectuée.",
  "typeCampagne": "ALERTE",
  "idSociete": 1,
  "criteresCiblage": {
    "idSociete": 1,
    "nombreFacturesArrieresMin": 3
  },
  "activerPush": true,
  "activerSms": true,
  "activerEmail": true
}
```

**Résultat :**
- Seuls les clients de la société 1 ayant **au moins 3 factures** avec `MontantDu > 0` recevront cette communication
- Les clients avec 0, 1 ou 2 factures en arriérés ne seront **pas** ciblés

---

## 🎯 Conclusion

Cette fonctionnalité permettra un **ciblage précis** des clients selon leur situation d'arriérés, particulièrement utile pour :
- ✅ **Avis de coupure** : Cibler les clients avec plusieurs factures en arriérés
- ✅ **Rappels de paiement** : Cibler les clients avec un nombre modéré d'arriérés
- ✅ **Alertes préventives** : Cibler les clients avant qu'ils accumulent trop d'arriérés

L'implémentation est **non-intrusive** (champs optionnels), **performante** (requête optimisée), et **cohérente** avec l'architecture existante utilisant `ClientFacture`.

---

**Date de création :** 2026-01-15  
**Auteur :** Analyse automatique  
**Statut :** 📋 En attente d'approbation
