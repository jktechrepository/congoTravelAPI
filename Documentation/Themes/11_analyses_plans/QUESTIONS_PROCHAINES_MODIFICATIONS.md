# ❓ Questions pour les Prochaines Modifications

## 📋 Table des matières
1. [Factures par Usage](#1-factures-par-usage)
2. [Multiplication par nombreBatiment](#2-multiplication-par-nombrebatiment)
3. [Calcul dynamique](#3-calcul-dynamique)
4. [Performance (N+1 queries)](#4-performance-n1-queries)
5. [Questions transversales](#5-questions-transversales)

---

## 1. Factures par Usage

### 🎯 Point clé
**Une facture est créée pour un Usage, tous les clients avec cet usage la reçoivent.**

### ❓ Questions à se poser

#### 1.1. Architecture et modèle de données

**Q1.1.1 : Faut-il garder le modèle actuel (facture par Usage) ?**
- ✅ **Avantages actuels :**
  - Une seule facture pour tous les clients d'un usage
  - Gestion centralisée
  - Facilite les modifications de tarifs
- ⚠️ **Inconvénients actuels :**
  - Impossible d'avoir des factures personnalisées par client
  - Si un client change d'usage, les anciennes factures restent liées à l'ancien usage
  - Tous les clients paient le même montant de base (avant multiplication par nombreBatiment)

**Q1.1.2 : Faut-il permettre des factures spécifiques à un client ?**
- Option A : **Garder uniquement factures par Usage** (modèle actuel)
- Option B : **Permettre factures par Client** (nouveau modèle)
- Option C : **Modèle hybride** (factures par Usage par défaut, possibilité de factures personnalisées)

**Q1.1.3 : Comment gérer les changements d'usage d'un client ?**
- Si un client change d'usage (ex: de "Résidentiel" à "Commercial") :
  - Les anciennes factures restent-elles liées à l'ancien usage ?
  - Faut-il recalculer les arriérés avec le nouvel usage ?
  - Comment gérer la transition ?

#### 1.2. Diffusion et notification

**Q1.2.1 : La diffusion automatique aux clients est-elle toujours souhaitée ?**
- Actuellement : Lors de la création d'une facture, tous les clients avec cet usage reçoivent la facture
- Faut-il permettre de :
  - Désactiver la diffusion pour certains clients ?
  - Diffuser à un sous-ensemble de clients seulement ?
  - Diffuser avec un délai personnalisé par client ?

**Q1.2.2 : Comment gérer les clients qui n'ont plus l'usage au moment de la diffusion ?**
- Scénario : Un client a l'usage "Résidentiel" en janvier, mais le perd en février
- Une facture de janvier est créée en mars
- Le client doit-il recevoir cette facture rétroactive ?

#### 1.3. Tarification et personnalisation

**Q1.3.1 : Faut-il permettre des tarifs différents par client pour le même usage ?**
- Exemple : Client VIP avec réduction de 10%
- Comment gérer cela avec le modèle actuel (facture par Usage) ?

**Q1.3.2 : Faut-il permettre des factures avec montants personnalisés ?**
- Actuellement : Tous les clients paient `facture.Montant × nombreBatiment`
- Faut-il permettre : `facture.Montant × nombreBatiment × coefficient_personnalisé` ?

---

## 2. Multiplication par nombreBatiment

### 🎯 Point clé
**Le montant de la facture est multiplié par `nombreBatiment` lors du calcul des arriérés.**

### ❓ Questions à se poser

#### 2.1. Logique de calcul

**Q2.1.1 : Le nombreBatiment doit-il être fixe ou variable dans le temps ?**
- Actuellement : Si un client change de `nombreBatiment`, les anciennes factures sont recalculées avec le nouveau nombre
- Faut-il :
  - **Option A :** Conserver le `nombreBatiment` au moment de la facture (snapshot)
  - **Option B :** Toujours utiliser le `nombreBatiment` actuel (comportement actuel)
  - **Option C :** Permettre de modifier le `nombreBatiment` avec effet rétroactif ou non

**Q2.1.2 : Comment gérer les changements de nombreBatiment rétroactifs ?**
- Exemple :
  - Janvier : Client avec `nombreBatiment = 1`, facture de 10 000 FCFA → doit 10 000 FCFA
  - Février : Client change à `nombreBatiment = 3`
  - La facture de janvier doit-elle être recalculée à 30 000 FCFA ?
  - Ou rester à 10 000 FCFA (valeur au moment de l'émission) ?

**Q2.1.3 : Faut-il stocker le nombreBatiment utilisé pour chaque facture ?**
- Créer une table `FactureClient` qui stocke :
  - `IdFacture`
  - `IdClient`
  - `nombreBatiment` (au moment de la facture)
  - `montantTotal` (calculé une fois)
- Avantage : Pas de recalcul, valeurs figées
- Inconvénient : Complexité supplémentaire

#### 2.2. Validation et cohérence

**Q2.2.1 : Faut-il valider que nombreBatiment > 0 ?**
- Actuellement : Valeur par défaut = 1, mais peut être modifiée
- Faut-il empêcher `nombreBatiment = 0` ou négatif ?

**Q2.2.2 : Faut-il permettre nombreBatiment décimal ?**
- Exemple : 1.5 bâtiment (un bâtiment et demi)
- Actuellement : `int nombreBatiment` (entier uniquement)

#### 2.3. Historique et audit

**Q2.3.1 : Faut-il tracker l'historique des changements de nombreBatiment ?**
- Créer une table `ClientUsageHistory` :
  - `IdClientUsage`
  - `nombreBatiment` (ancienne valeur)
  - `DateChangement`
  - `RaisonChangement`
- Permet de voir l'évolution et de comprendre les recalculs

**Q2.3.2 : Comment auditer les recalculs d'arriérés suite à un changement ?**
- Si `nombreBatiment` change, les arriérés changent
- Faut-il logger ces changements ?
- Faut-il notifier le client du changement de montant dû ?

---

## 3. Calcul dynamique

### 🎯 Point clé
**Le montant payé est calculé dynamiquement depuis la table `Paiements`.**

### ❓ Questions à se poser

#### 3.1. Performance et optimisation

**Q3.1.1 : Faut-il pré-calculer et stocker le montant payé total ?**
- Option A : **Garder calcul dynamique** (actuel)
  - Avantage : Toujours à jour, pas de désynchronisation
  - Inconvénient : Performance dégradée avec beaucoup de paiements
- Option B : **Stocker dans Facture.MontantPayeTotal**
  - Avantage : Performance meilleure
  - Inconvénient : Risque de désynchronisation, nécessite triggers ou calcul à chaque paiement

**Q3.1.2 : Faut-il utiliser un champ calculé en base de données ?**
- MySQL/MariaDB supporte les colonnes générées (GENERATED COLUMN)
- Exemple : `MontantPayeTotal AS (SELECT SUM(MontantPaye) FROM Paiements WHERE ...)`
- Avantage : Calcul automatique, toujours synchronisé
- Inconvénient : Performance peut être dégradée selon l'implémentation

#### 3.2. Cohérence et intégrité

**Q3.2.1 : Faut-il garantir la cohérence transactionnelle ?**
- Actuellement : Calcul fait après chargement des données
- Faut-il utiliser des transactions pour garantir la cohérence ?
- Comment gérer les paiements simultanés sur la même facture ?

**Q3.2.2 : Faut-il valider que le montant payé ne dépasse pas le montant total ?**
- Actuellement : Pas de validation
- Faut-il :
  - **Option A :** Valider strictement (rejeter les surpaiements)
  - **Option B :** Permettre les surpaiements (créer un avoir automatiquement)
  - **Option C :** Avertir mais accepter (log pour audit)

#### 3.3. Historique et snapshot

**Q3.3.1 : Faut-il stocker un snapshot du montant payé à chaque paiement ?**
- Dans `Paiement.MontantAPaye` et `Paiement.ResteAPaye`
- Permet de voir l'évolution : "Avant ce paiement, il restait X, après il reste Y"
- Utile pour audit et historique

**Q3.3.2 : Faut-il permettre de voir l'historique des calculs ?**
- Table `FactureCalculHistory` :
  - `IdFacture`
  - `DateCalcul`
  - `MontantTotal`
  - `MontantPaye`
  - `MontantDu`
  - `nombreBatiment` (utilisé)
- Permet de tracer les changements dans le temps

---

## 4. Performance (N+1 queries)

### 🎯 Point clé
**Problème N+1 queries dans le calcul des arriérés.**

### ❓ Questions à se poser

#### 4.1. Optimisation immédiate

**Q4.1.1 : Quelle solution d'optimisation choisir ?**
- **Option A :** Requête LINQ avec GROUP BY (Solution A de l'analyse)
  - Avantage : Flexibilité, maintenabilité
  - Inconvénient : Requête SQL complexe générée
- **Option B :** Vue SQL matérialisée (Solution B)
  - Avantage : Performance maximale
  - Inconvénient : Moins flexible, maintenance SQL
- **Option C :** Requête SQL brute (Solution C)
  - Avantage : Contrôle total, performance
  - Inconvénient : Moins maintenable, dépendant du SGBD

**Q4.1.2 : Faut-il optimiser toutes les méthodes ou seulement les plus utilisées ?**
- `GetArrieresByClientAsync` : Utilisé fréquemment → **Priorité HAUTE**
- `GetFacturesImpayeesByClientPagedAsync` : Utilisé pour pagination → **Priorité MOYENNE**
- `GetArrieresGlobalAsync` : Utilisé pour rapports → **Priorité BASSE** (mais impact élevé)

#### 4.2. Mise en cache

**Q4.2.1 : Faut-il mettre en cache les résultats des arriérés ?**
- Option A : **Pas de cache** (calcul toujours à jour)
- Option B : **Cache avec expiration** (ex: 5 minutes)
- Option C : **Cache invalidé à chaque paiement**

**Q4.2.2 : Quel mécanisme de cache utiliser ?**
- **Option A :** Mémoire (IMemoryCache)
  - Avantage : Rapide, simple
  - Inconvénient : Perdu au redémarrage, limité à une instance
- **Option B :** Redis/Distributed Cache
  - Avantage : Partageable entre instances, persistant
  - Inconvénient : Complexité supplémentaire, dépendance externe
- **Option C :** Cache en base (table dédiée)
  - Avantage : Persistant, accessible à tous
  - Inconvénient : Performance moindre que mémoire

#### 4.3. Pagination et chargement progressif

**Q4.3.1 : Faut-il optimiser la pagination des arriérés ?**
- Actuellement : Toutes les factures sont chargées, puis pagination en mémoire
- Faut-il : Pagination en base (LIMIT/OFFSET) avant calcul ?

**Q4.3.2 : Faut-il permettre le chargement progressif (lazy loading) ?**
- Charger d'abord les totaux, puis les détails à la demande
- Réduit le temps de réponse initial

---

## 5. Questions transversales

### 🎯 Questions qui impactent plusieurs aspects

#### 5.1. Gestion des erreurs et cas limites

**Q5.1.1 : Comment gérer les factures avec Montant = NULL ?**
- Actuellement : `(facture.Montant ?? 0)` → traité comme 0
- Faut-il :
  - Rejeter les factures sans montant ?
  - Permettre les factures à 0 (gratuites) ?
  - Exiger un montant lors de la création ?

**Q5.1.2 : Comment gérer les paiements partiels multiples ?**
- Exemple : Facture de 10 000 FCFA
- Paiement 1 : 3 000 FCFA
- Paiement 2 : 2 000 FCFA
- Paiement 3 : 5 000 FCFA
- Actuellement : Géré correctement
- Faut-il permettre de voir l'historique des paiements partiels ?

**Q5.1.3 : Comment gérer les remboursements ?**
- Si un client a payé trop, comment rembourser ?
- Faut-il créer un type de paiement "Remboursement" avec montant négatif ?
- Ou créer une table séparée `Remboursements` ?

#### 5.2. Statuts et workflow

**Q5.2.1 : Faut-il un workflow de statuts pour les factures ?**
- Statuts possibles : Brouillon, Émise, En attente, Payée, Annulée, Remboursée
- Actuellement : Seulement `Statut` (booléen) et `EstDiffusee`

**Q5.2.2 : Faut-il un workflow de statuts pour les paiements ?**
- Statuts actuels : "Validé", "En attente", "Rejeté" (string libre)
- Faut-il :
  - **Option A :** Enum strict (Validé, EnAttente, Rejeté, Remboursé)
  - **Option B :** Garder string libre mais avec validation
  - **Option C :** Ajouter un champ `TypePaiement` (Paiement, Remboursement, Avoir)

#### 5.3. Reporting et analytics

**Q5.3.1 : Quels rapports sont nécessaires ?**
- Rapport des arriérés par période ?
- Rapport des paiements par méthode ?
- Rapport des factures impayées par usage ?
- Dashboard avec indicateurs clés ?

**Q5.3.2 : Faut-il pré-calculer des statistiques ?**
- Table `FactureStats` avec agrégations pré-calculées
- Mise à jour en temps réel ou batch quotidien ?

#### 5.4. Intégrité et audit

**Q5.4.1 : Faut-il garantir l'intégrité référentielle stricte ?**
- Actuellement : `Paiement.IdClient` est optionnel
- Faut-il rendre obligatoire avec contrainte FK ?

**Q5.4.2 : Faut-il un système d'audit complet pour les paiements ?**
- Table `PaiementAudit` :
  - `IdPaiement`
  - `Action` (Créé, Modifié, Supprimé, Annulé)
  - `AncienneValeur`
  - `NouvelleValeur`
  - `DateModification`
  - `IdUtilisateur`

#### 5.5. Multi-société et isolation

**Q5.5.1 : Comment garantir l'isolation entre sociétés ?**
- Actuellement : Filtrage via Usage → CategorieClient → Societe
- Faut-il :
  - Ajouter `IdSociete` directement dans `Facture` ?
  - Ajouter `IdSociete` directement dans `Paiement` ?
  - Garder le filtrage via les relations ?

**Q5.5.2 : Faut-il permettre des factures partagées entre sociétés ?**
- Cas d'usage : Coopérative, partenariats
- Actuellement : Non supporté

---

## 📊 Matrice de décision

### Priorisation des questions

| Question | Impact | Urgence | Complexité | Priorité |
|----------|--------|---------|------------|----------|
| **Q4.1.1** (Optimisation N+1) | 🔴 Élevé | 🔴 Urgent | 🟡 Moyenne | **P1** |
| **Q3.2.2** (Validation montants) | 🔴 Élevé | 🟡 Moyen | 🟢 Faible | **P2** |
| **Q2.1.1** (nombreBatiment fixe/variable) | 🟡 Moyen | 🟡 Moyen | 🟡 Moyenne | **P3** |
| **Q1.1.2** (Factures personnalisées) | 🟡 Moyen | 🟢 Faible | 🔴 Élevée | **P4** |
| **Q3.1.1** (Pré-calcul montant payé) | 🟡 Moyen | 🟢 Faible | 🟡 Moyenne | **P5** |

### Critères de décision

**Impact :**
- 🔴 **Élevé** : Affecte directement les utilisateurs ou les performances
- 🟡 **Moyen** : Améliore l'expérience mais pas critique
- 🟢 **Faible** : Nice to have

**Urgence :**
- 🔴 **Urgent** : Problème actuel qui bloque ou dégrade l'expérience
- 🟡 **Moyen** : Amélioration souhaitable dans les prochains mois
- 🟢 **Faible** : Amélioration future, pas de pression

**Complexité :**
- 🔴 **Élevée** : Nécessite refactoring important, migrations complexes
- 🟡 **Moyenne** : Modifications modérées, quelques jours de travail
- 🟢 **Faible** : Modifications simples, quelques heures

---

## 🎯 Plan d'action recommandé

### Phase 1 : Optimisations critiques (1-2 semaines)
1. ✅ **Optimiser N+1 queries** (Q4.1.1 - Solution A)
2. ✅ **Ajouter validation des montants** (Q3.2.2)
3. ✅ **Rendre IdClient obligatoire** (avec déduction auto si possible)

### Phase 2 : Améliorations importantes (2-4 semaines)
4. ✅ **Décider sur MontantAPaye/ResteAPaye** (calculer ou supprimer)
5. ✅ **Gérer nombreBatiment fixe/variable** (Q2.1.1)
6. ✅ **Améliorer la gestion des statuts** (Q5.2.1, Q5.2.2)

### Phase 3 : Évolutions fonctionnelles (1-2 mois)
7. ⚠️ **Évaluer besoin de factures personnalisées** (Q1.1.2)
8. ⚠️ **Système d'audit complet** (Q5.4.2)
9. ⚠️ **Rapports et analytics** (Q5.3.1, Q5.3.2)

---

## 📝 Notes importantes

### Questions nécessitant une décision métier

Ces questions nécessitent une discussion avec les parties prenantes :

1. **Q1.1.2** : Factures personnalisées par client
   - Impact métier : Permet des tarifs négociés, réductions personnalisées
   - Impact technique : Refactoring important du modèle

2. **Q2.1.1** : nombreBatiment fixe ou variable
   - Impact métier : Comment gérer les changements de nombre de bâtiments
   - Impact technique : Nécessite peut-être une table `FactureClient`

3. **Q3.2.2** : Validation des montants
   - Impact métier : Politique de gestion des surpaiements
   - Impact technique : Validation à ajouter, gestion d'avoir si nécessaire

### Questions techniques pures

Ces questions peuvent être décidées par l'équipe technique :

1. **Q4.1.1** : Solution d'optimisation (LINQ, Vue SQL, SQL brut)
2. **Q4.2.1** : Mise en cache ou non
3. **Q3.1.1** : Pré-calcul ou calcul dynamique

---

## 🔄 Prochaines étapes

1. **Réviser ce document** avec l'équipe
2. **Prioriser les questions** selon les besoins métier
3. **Prendre des décisions** sur les questions P1 et P2
4. **Planifier l'implémentation** des optimisations critiques
5. **Documenter les décisions** prises pour référence future
