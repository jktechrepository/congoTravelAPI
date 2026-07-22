# 📋 Plan de Travail : Endpoint Arriérés Consolidés Globaux

## 🎯 Objectif

Créer un endpoint `GET /api/ClientFacture/arrieres-consolides` qui retourne un rapport consolidé des arriérés pour **tous les clients** (sans filtre par `idClient`), avec une structure similaire à l'endpoint existant `/api/ClientFacture/client/{idClient}/arrieres-consolides`.

---

## 📊 Structure de Réponse Proposée

### Option 1 : Rapport Global avec Liste de Clients (Recommandée)

```json
{
  "totalGeneralGlobal": 500000,
  "nombreTotalClients": 15,
  "nombreTotalFactures": 120,
  "nombreTotalPeriodes": 45,
  "arrieresParClient": [
    {
      "idClient": 1,
      "nomClient": "Kalambayi Jonathan",
      "codeCons": "B/b1/0001",
      "totalGeneral": 210000,
      "nombreTotalFactures": 9,
      "nombreTotalPeriodes": 3,
      "arrieresParPeriode": [
        {
          "mois": "02",
          "annees": 2026,
          "nombreUsages": 3,
          "nombreFactures": 3,
          "montantDuTotal": 26000,
          "detailFactures": [...]
        }
      ]
    },
    {
      "idClient": 2,
      "nomClient": "Kabeya Garry",
      "codeCons": "B/b1/0002",
      "totalGeneral": 120000,
      "nombreTotalFactures": 7,
      "nombreTotalPeriodes": 2,
      "arrieresParPeriode": [...]
    }
  ]
}
```

**Avantages :**
- ✅ Structure cohérente avec l'endpoint par client
- ✅ Totaux globaux visibles en haut
- ✅ Détails par client réutilisant la structure existante
- ✅ Facile à afficher dans un tableau ou une liste déroulante

**Inconvénients :**
- ⚠️ Peut être volumineux si beaucoup de clients

---

### Option 2 : Liste Simple de Clients avec Arriérés

```json
[
  {
    "idClient": 1,
    "nomClient": "Kalambayi Jonathan",
    "codeCons": "B/b1/0001",
    "totalGeneral": 210000,
    "nombreTotalFactures": 9,
    "nombreTotalPeriodes": 3,
    "arrieresParPeriode": [...]
  },
  ...
]
```

**Avantages :**
- ✅ Plus simple
- ✅ Moins de données si pas besoin de totaux globaux

**Inconvénients :**
- ⚠️ Pas de totaux globaux
- ⚠️ Moins structuré

---

## ✅ Recommandation : Option 1

L'**Option 1** est recommandée car :
1. Elle fournit des totaux globaux utiles pour le reporting
2. Elle réutilise la structure existante (`ArrieresConsolidesResponseDto`)
3. Elle est cohérente avec les autres endpoints de reporting global

---

## 📝 Plan d'Implémentation

### Phase 1 : Création du DTO (0.5 jour)

#### 1.1. Créer `ArrieresConsolidesGlobauxResponseDto.cs`

**Fichier :** `Models/DTOs/ClientFacture/ArrieresConsolidesGlobauxResponseDto.cs`

**Structure :**
```csharp
public class ArrieresConsolidesGlobauxResponseDto
{
    [JsonPropertyOrder(1)]
    public decimal TotalGeneralGlobal { get; set; }
    
    [JsonPropertyOrder(2)]
    public int NombreTotalClients { get; set; }
    
    [JsonPropertyOrder(3)]
    public int NombreTotalFactures { get; set; }
    
    [JsonPropertyOrder(4)]
    public int NombreTotalPeriodes { get; set; }
    
    [JsonPropertyOrder(5)]
    public List<ArrieresConsolidesResponseDto> ArrieresParClient { get; set; }
}
```

**Checklist :**
- [ ] Créer le fichier DTO
- [ ] Ajouter les commentaires XML
- [ ] Utiliser `[JsonPropertyOrder]` pour garantir l'ordre
- [ ] Réutiliser `ArrieresConsolidesResponseDto` pour les clients

---

### Phase 2 : Implémentation du Service (1 jour)

#### 2.1. Ajouter la méthode dans l'interface

**Fichier :** `Services/Repositories/IClientFactureRepository.cs`

**Méthode :**
```csharp
/// <summary>
/// Récupère les arriérés consolidés pour tous les clients
/// </summary>
Task<ArrieresConsolidesGlobauxResponseDto> GetArrieresConsolidesGlobauxAsync();
```

**Checklist :**
- [ ] Ajouter la signature dans l'interface
- [ ] Ajouter les commentaires XML

---

#### 2.2. Implémenter la méthode dans le service

**Fichier :** `Services/ClientFactureService.cs`

**Logique :**
1. Récupérer toutes les `ClientFacture` avec `MontantDu > 0` (tous clients)
2. Grouper par `IdClient`
3. Pour chaque client :
   - Appeler `GetArrieresConsolidesByClientAsync(idClient)` (réutiliser la logique existante)
   - Ajouter à la liste `ArrieresParClient`
4. Calculer les totaux globaux :
   - `TotalGeneralGlobal` = Somme de tous les `TotalGeneral` des clients
   - `NombreTotalClients` = Nombre de clients avec arriérés
   - `NombreTotalFactures` = Somme de tous les `NombreTotalFactures` des clients
   - `NombreTotalPeriodes` = Nombre de périodes distinctes (tous clients confondus)
5. Retourner `ArrieresConsolidesGlobauxResponseDto`

**Optimisation :**
- ⚠️ **Attention** : Appeler `GetArrieresConsolidesByClientAsync` pour chaque client peut être coûteux
- ✅ **Alternative** : Implémenter une logique optimisée qui traite tous les clients en une seule requête

**Checklist :**
- [ ] Implémenter `GetArrieresConsolidesGlobauxAsync`
- [ ] Optimiser les requêtes (éviter N+1)
- [ ] Calculer les totaux globaux
- [ ] Gérer le cas où aucun client n'a d'arriérés

---

### Phase 3 : Création de l'Endpoint (0.5 jour)

#### 3.1. Ajouter l'endpoint dans le controller

**Fichier :** `Controllers/ClientFactureController.cs`

**Endpoint :**
```csharp
// GET: api/ClientFacture/arrieres-consolides
/// <summary>
/// ✨ NOUVEAU : Récupère un rapport consolidé des arriérés pour tous les clients
/// Retourne les arriérés groupés par client et par période (mois/année) avec totaux globaux
/// </summary>
/// <returns>Rapport global des arriérés consolidés</returns>
[HttpGet("arrieres-consolides")]
[Authorize(Roles = "Super-Admin,Admin,Financier")]
public async Task<ActionResult<ArrieresConsolidesGlobauxResponseDto>> GetArrieresConsolidesGlobaux()
{
    var result = await _clientFactureRepository.GetArrieresConsolidesGlobauxAsync();
    return Ok(result);
}
```

**Placement :**
- ⚠️ **Important** : Placer cet endpoint **AVANT** `GET /api/ClientFacture/client/{idClient}/arrieres-consolides` pour éviter les conflits de routage

**Checklist :**
- [ ] Ajouter l'endpoint dans le controller
- [ ] Placer l'endpoint au bon endroit (avant les routes avec paramètres)
- [ ] Ajouter les commentaires XML
- [ ] Configurer l'autorisation (rôles appropriés)

---

### Phase 4 : Optimisation (Optionnel - 0.5 jour)

#### 4.1. Optimisation des performances

**Problème potentiel :**
- Si on appelle `GetArrieresConsolidesByClientAsync` pour chaque client, on fait N requêtes

**Solution :**
- Implémenter une logique qui traite tous les clients en une seule requête
- Grouper par client puis par période en mémoire ou en base

**Checklist :**
- [ ] Analyser les performances
- [ ] Optimiser si nécessaire
- [ ] Ajouter des index si besoin

---

### Phase 5 : Tests (1 jour)

#### 5.1. Tests unitaires

**Fichier :** `CongoTravel.Tests.Unit/Services/ClientFactureServiceTests.cs`

**Scénarios :**
- [ ] Aucun client avec arriérés
- [ ] Un seul client avec arriérés
- [ ] Plusieurs clients avec arriérés
- [ ] Vérification des totaux globaux
- [ ] Vérification du nombre de clients
- [ ] Vérification du nombre de factures
- [ ] Vérification du nombre de périodes

**Checklist :**
- [ ] Créer les tests unitaires
- [ ] Tester tous les scénarios
- [ ] Vérifier les calculs

---

#### 5.2. Tests d'intégration

**Scénarios :**
- [ ] Tester l'endpoint avec Swagger
- [ ] Vérifier la structure de la réponse
- [ ] Vérifier l'ordre des champs
- [ ] Tester avec des données réelles

**Checklist :**
- [ ] Tester l'endpoint
- [ ] Valider la réponse JSON
- [ ] Vérifier les performances

---

## 📊 Estimation Totale

| Phase | Durée | Description |
|-------|-------|-------------|
| Phase 1 | 0.5 jour | Création du DTO |
| Phase 2 | 1 jour | Implémentation du service |
| Phase 3 | 0.5 jour | Création de l'endpoint |
| Phase 4 | 0.5 jour | Optimisation (optionnel) |
| Phase 5 | 1 jour | Tests |
| **TOTAL** | **3.5 jours** | (2.5 jours sans optimisation) |

---

## ⚠️ Points d'Attention

### 1. Performance
- ⚠️ Si beaucoup de clients, la réponse peut être volumineuse
- 💡 **Solution** : Ajouter une pagination ou un filtre par société si nécessaire

### 2. Autorisation
- ⚠️ Cet endpoint expose des données sensibles (tous les clients)
- ✅ **Solution** : Restreindre aux rôles `Super-Admin, Admin, Financier`

### 3. Filtrage
- 💡 **Futur** : Ajouter des filtres optionnels (par société, par période, etc.)

### 4. Compatibilité
- ✅ Pas de breaking changes
- ✅ Nouvel endpoint, n'affecte pas les endpoints existants

---

## 🎯 Résultat Attendu

Un endpoint `GET /api/ClientFacture/arrieres-consolides` qui retourne :
- ✅ Totaux globaux (total général, nombre de clients, factures, périodes)
- ✅ Liste de tous les clients avec leurs arriérés consolidés
- ✅ Structure cohérente avec l'endpoint par client
- ✅ Performance optimisée

---

## 📝 Notes

- Réutiliser au maximum la logique existante (`GetArrieresConsolidesByClientAsync`)
- Garantir l'ordre des champs avec `[JsonPropertyOrder]`
- Documenter l'endpoint dans Swagger
- Considérer l'ajout de filtres optionnels dans une version future

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0  
**Statut :** 📋 Plan prêt pour implémentation
