# 📊 Analyse d'Impact : Refactorisation vers Usage

## 🎯 Modifications Demandées

### 1. Modèle Usage
- ✅ **Existe déjà** mais à vérifier :
  - `IdUsage` (int, auto-increment) ✅
  - `Libelle` (string, not null) ✅
  - `Description` (string, nullable) ✅
  - `IdCategorieClient` (clé étrangère) ✅
  - ⚠️ **À RETIRER** : `nombreBatiment` (doit être dans ClientUsage, pas dans Usage)

### 2. Supprimer IdCategorieClient de Client
- ⚠️ **Impact majeur** : Tous les services qui utilisent `IdCategorieClient` doivent être adaptés
- ⚠️ **Migration complexe** : Données existantes à migrer vers ClientUsage

### 3. Modifier ClientUsage
- ⚠️ **Changement de structure** :
  - Actuellement : Clé composite (IdClient, IdUsage)
  - Demandé : `IdClientUsage` (int, auto-increment) + clé unique (IdClient, IdUsage)
  - Ajouter : `nombreBatiment` (int, not null)

### 4. Facturation par Usage
- ✅ **Déjà fait** : `Facture` utilise `IdUsage`
- ⚠️ **À adapter** : `FactureService` utilise encore `IdCategorie` dans certaines méthodes

---

## ⚠️ Points d'Attention Critiques

### 1. Migration des Données Existantes

**Problème** : Comment migrer les données de `ClientCategorieClient` vers `ClientUsage` ?

**Solution proposée** :
- Pour chaque relation Client-CategorieClient :
  1. Trouver les Usages liés à cette catégorie
  2. Créer des relations ClientUsage pour chaque usage
  3. Déterminer `nombreBatiment` (par défaut = 1 ou depuis une autre source)

### 2. Impact sur ArrieresService

**Actuellement** : Calcule les arriérés en filtrant par catégories du client
**Nouveau** : Doit filtrer par usages du client via `ClientUsage`

**Impact** : Refactorisation majeure de la logique de calcul

### 3. Impact sur FactureService

**Actuellement** : Utilise `IdCategorie` pour générer les numéros de facture
**Nouveau** : Doit utiliser `IdUsage`

**Impact** : Adapter `GenerateNumeroFactureAsync` et toutes les méthodes de filtrage

### 4. Impact sur ExcelClientService

**Actuellement** : Importe les clients avec catégories
**Nouveau** : Doit importer avec usages

**Impact** : 
- Modifier le template Excel
- Adapter la logique de résolution (catégorie → usages)
- Gérer `nombreBatiment` dans l'import

### 5. Suppression de Usage dans CategorieClient

**Impact** : 
- Tous les endroits qui utilisent `CategorieClient.Usage` doivent être adaptés
- Les requêtes qui filtrent par usage doivent passer par le modèle Usage

---

## 📋 Plan d'Action Proposé

### Phase 1 : Préparation
1. ✅ Analyser tous les usages de `IdCategorieClient` dans le code
2. ✅ Analyser tous les usages de `CategorieClient.Usage`
3. ✅ Créer un plan de migration des données

### Phase 2 : Modèles
1. Modifier `ClientUsage` :
   - Ajouter `IdClientUsage` (int, auto-increment, primary key)
   - Ajouter `nombreBatiment` (int, not null)
   - Garder clé unique (IdClient, IdUsage)
2. Retirer `nombreBatiment` de `Usage` (si présent)
3. Retirer `Usage` de `CategorieClient`
4. Retirer `IdCategorieClient` de `Client`
5. Retirer `ClientCategorieClient` (remplacé par `ClientUsage`)

### Phase 3 : Services
1. Adapter `ArrieresService` : Filtrer par usages via `ClientUsage`
2. Adapter `FactureService` : Utiliser `IdUsage` partout
3. Adapter `ClientService` : Gérer `ClientUsage` au lieu de `ClientCategorieClient`
4. Adapter `ExcelClientService` : Importer avec usages et `nombreBatiment`
5. Adapter `ClientFilterService` : Filtrer par usages

### Phase 4 : Migration des Données
1. Créer les Usages depuis `CategorieClient.Usage`
2. Migrer `ClientCategorieClient` → `ClientUsage`
3. Supprimer les anciennes données

---

## 🔍 Questions à Clarifier

1. **nombreBatiment** : 
   - Comment déterminer la valeur lors de la migration ?
   - Par défaut = 1 pour tous ?
   - Source de données existante ?

2. **Migration ClientCategorieClient → ClientUsage** :
   - Si un client a la catégorie "Standard" qui a 3 usages, créer 3 relations ClientUsage ?
   - Ou créer une seule relation avec l'usage principal ?

3. **Template Excel** :
   - Comment représenter les usages dans le template ?
   - Format : "Usage1, Usage2" ou colonnes séparées ?
   - Comment gérer `nombreBatiment` dans l'import ?

4. **Compatibilité** :
   - Faut-il garder `IdCategorieClient` temporairement pour compatibilité ?
   - Ou suppression complète immédiate ?

---

## 💡 Recommandations

### ✅ Points Positifs

1. **Séparation des responsabilités** : Usage devient une entité à part entière
2. **Flexibilité** : Un client peut avoir plusieurs usages avec différents `nombreBatiment`
3. **Facturation précise** : Facturation par usage est plus granulaire

### ⚠️ Points d'Attention

1. **Complexité de migration** : Migration des données sera complexe
2. **Impact sur services** : Beaucoup de services à adapter
3. **Risque de régression** : Tests approfondis nécessaires

### 🎯 Recommandation Finale

**Cette refactorisation est logique et améliore l'architecture**, mais nécessite :
- Un plan de migration détaillé
- Des tests complets
- Une migration progressive si possible

**Je recommande de procéder par étapes** :
1. D'abord créer/modifier les modèles
2. Ensuite adapter les services
3. Enfin migrer les données

Souhaitez-vous que je procède à cette refactorisation ?
