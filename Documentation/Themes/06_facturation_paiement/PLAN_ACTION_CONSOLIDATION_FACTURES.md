# 📋 Plan d'Action : Consolidation des Factures par Client

## 🎯 Objectif

Consolider les factures pour qu'un client avec plusieurs usages reçoive **une seule facture** avec le montant total consolidé, au lieu de plusieurs factures séparées (une par usage).

---

## 📊 Options Analysées

### Option 1 : Consolidation au Niveau Facture ⭐ (Recommandée)
- **Principe** : Créer une `Facture` par client (au lieu d'une par usage)
- **Complexité** : ⭐⭐⭐⭐⭐ (Très élevée)
- **Risque** : ⭐⭐⭐⭐ (Élevé)
- **Temps estimé** : 2-3 jours

### Option 2 : Consolidation au Niveau ClientFacture
- **Principe** : Garder les `Facture` par usage, créer une seule `ClientFacture` consolidée
- **Complexité** : ⭐⭐⭐ (Moyenne)
- **Risque** : ⭐⭐⭐ (Moyen)
- **Temps estimé** : 1-2 jours

### Option 3 : Facture Hybride
- **Principe** : `Facture` consolidée par client avec détail dans `ClientFacture`
- **Complexité** : ⭐⭐⭐⭐ (Élevée)
- **Risque** : ⭐⭐⭐⭐ (Élevé)
- **Temps estimé** : 2-3 jours

---

## ✅ Option Recommandée : Option 1

### Justification
1. ✅ **Simplicité utilisateur** : Une seule facture par client
2. ✅ **Simplicité paiements** : Un seul paiement par facture
3. ✅ **Simplicité arriérés** : Une seule facture impayée par période
4. ✅ **Cohérence métier** : Un client = une facture
5. ✅ **Performance** : Moins de `ClientFacture` à gérer

---

## 📝 Plan d'Action Détaillé (Option 1)

### Phase 1 : Analyse et Préparation (4 heures)

#### 1.1 Audit des Données Existantes
- [ ] Compter le nombre de clients avec plusieurs usages
- [ ] Identifier les factures existantes à consolider
- [ ] Vérifier les paiements liés aux factures multiples
- [ ] Analyser les arriérés actuels

**Script SQL :**
```sql
-- Clients avec plusieurs usages
SELECT c.IdClient, c.CodeCons, c.NomClient, COUNT(cu.IdUsage) as NombreUsages
FROM Clients c
INNER JOIN ClientUsages cu ON c.IdClient = cu.IdClient
WHERE c.Statut = 1 AND cu.Statut = 1
GROUP BY c.IdClient, c.CodeCons, c.NomClient
HAVING COUNT(cu.IdUsage) > 1;

-- Factures à consolider (même client, même période, usages différents)
SELECT cf1.IdClient, cf1.Mois, cf1.Annees, COUNT(DISTINCT f.IdUsage) as NombreUsages
FROM ClientFactures cf1
INNER JOIN Factures f1 ON cf1.IdFacture = f1.IdFacture
INNER JOIN ClientFactures cf2 ON cf1.IdClient = cf2.IdClient 
    AND cf1.Mois = cf2.Mois AND cf1.Annees = cf2.Annees
INNER JOIN Factures f2 ON cf2.IdFacture = f2.IdFacture
WHERE cf1.Statut = 1 AND cf2.Statut = 1
    AND f1.IdUsage != f2.IdUsage
GROUP BY cf1.IdClient, cf1.Mois, cf1.Annees
HAVING COUNT(DISTINCT f.IdUsage) > 1;
```

#### 1.2 Backup des Données
- [ ] Créer un backup complet de la base de données
- [ ] Exporter les tables critiques : `Factures`, `ClientFactures`, `Paiements`

---

### Phase 2 : Modifications du Modèle de Données (6 heures)

#### 2.1 Modifier le Modèle `Facture`
```csharp
// AVANT
public int IdUsage { get; set; }
public Usage? Usage { get; set; }

// APRÈS
public int IdClient { get; set; }  // Lié à un client
public Client? Client { get; set; }
public string? DescriptionUsages { get; set; }  // JSON ou texte : détail des usages
public string? DetailUsagesJson { get; set; }  // JSON array: [{"IdUsage": 1, "Libelle": "Résidentiel", "Montant": 1000, "nombreBatiment": 2}, ...]
```

**Structure JSON proposée pour `DetailUsagesJson` :**
```json
[
  {
    "IdUsage": 1,
    "LibelleUsage": "Résidentiel",
    "MontantBase": 1000.00,
    "nombreBatiment": 2,
    "MontantTotal": 2000.00
  },
  {
    "IdUsage": 2,
    "LibelleUsage": "Commercial",
    "MontantBase": 2000.00,
    "nombreBatiment": 1,
    "MontantTotal": 2000.00
  }
]
```

#### 2.2 Migration Entity Framework
- [ ] Créer une migration : `dotnet ef migrations add ConsolidateFacturesByClient`
- [ ] Ajouter la colonne `IdClient` à `Factures`
- [ ] Ajouter les colonnes `DescriptionUsages` et `DetailUsagesJson`
- [ ] Supprimer la colonne `IdUsage` (ou la rendre nullable pour transition)
- [ ] Mettre à jour les index :
  - Supprimer : `IX_Facture_MoisEmission_AnneesEmission_IdUsage`
  - Ajouter : `IX_Facture_MoisEmission_AnneesEmission_IdClient`
  - Ajouter : `IX_Facture_IdClient`

#### 2.3 Script SQL de Migration
```sql
-- Étape 1 : Ajouter les nouvelles colonnes
ALTER TABLE Factures 
ADD COLUMN IdClient INT NULL,
ADD COLUMN DescriptionUsages VARCHAR(500) NULL,
ADD COLUMN DetailUsagesJson TEXT NULL;

-- Étape 2 : Migrer les données existantes
-- Pour chaque ClientFacture, créer une Facture consolidée par client/période
-- (Voir script détaillé dans section suivante)

-- Étape 3 : Rendre IdClient obligatoire après migration
ALTER TABLE Factures 
MODIFY COLUMN IdClient INT NOT NULL;

-- Étape 4 : Supprimer l'ancienne colonne IdUsage (après validation)
-- ALTER TABLE Factures DROP COLUMN IdUsage;
```

---

### Phase 3 : Migration des Données Existantes (8 heures)

#### 3.1 Script de Consolidation des Factures

**Logique :**
1. Grouper les `ClientFacture` par `IdClient`, `Mois`, `Annees`
2. Pour chaque groupe, créer une nouvelle `Facture` consolidée
3. Mettre à jour les `ClientFacture` pour pointer vers la nouvelle `Facture`
4. Migrer les `Paiement` vers la nouvelle `Facture`

**Script SQL (pseudo-code) :**
```sql
-- Pour chaque client avec plusieurs ClientFacture pour la même période
DECLARE @ClientId INT;
DECLARE @Mois VARCHAR(2);
DECLARE @Annee INT;

-- Boucle sur les groupes
FOR EACH (SELECT IdClient, Mois, Annees 
          FROM ClientFactures 
          WHERE Statut = 1 
          GROUP BY IdClient, Mois, Annees 
          HAVING COUNT(*) > 1) AS groupe
BEGIN
    SET @ClientId = groupe.IdClient;
    SET @Mois = groupe.Mois;
    SET @Annee = groupe.Annees;
    
    -- Calculer le montant total consolidé
    DECLARE @MontantTotal DECIMAL(18,2);
    SELECT @MontantTotal = SUM(Montant)
    FROM ClientFactures
    WHERE IdClient = @ClientId AND Mois = @Mois AND Annees = @Annee AND Statut = 1;
    
    -- Créer la nouvelle Facture consolidée
    INSERT INTO Factures (NumeroFacture, Montant, IdClient, MoisEmission, AnneesEmission, 
                         DateEmission, DetailUsagesJson, Statut, DateCreation)
    VALUES (
        -- Générer nouveau numéro basé sur CodeCons
        CONCAT('FAC-', (SELECT CodeCons FROM Clients WHERE IdClient = @ClientId), '-', @Mois, @Annee, '-', 
               LPAD((SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(NumeroFacture, '-', -1) AS UNSIGNED)), 0) + 1 
                     FROM Factures 
                     WHERE NumeroFacture LIKE CONCAT('FAC-%', '-', @Mois, @Annee, '-%')), 4, '0')),
        @MontantTotal,
        @ClientId,
        CAST(@Mois AS UNSIGNED),
        @Annee,
        (SELECT MIN(DateEmission) FROM ClientFactures WHERE IdClient = @ClientId AND Mois = @Mois AND Annees = @Annee),
        -- Construire JSON des usages
        (SELECT JSON_ARRAYAGG(
            JSON_OBJECT(
                'IdUsage', f.IdUsage,
                'LibelleUsage', u.Libelle,
                'MontantBase', f.Montant,
                'nombreBatiment', cf.nombreBatiment,
                'MontantTotal', cf.Montant
            )
        )
        FROM ClientFactures cf
        INNER JOIN Factures f ON cf.IdFacture = f.IdFacture
        INNER JOIN Usages u ON f.IdUsage = u.IdUsage
        WHERE cf.IdClient = @ClientId AND cf.Mois = @Mois AND cf.Annees = @Annee AND cf.Statut = 1),
        1,
        NOW()
    );
    
    SET @NewFactureId = LAST_INSERT_ID();
    
    -- Mettre à jour les ClientFacture pour pointer vers la nouvelle Facture
    UPDATE ClientFactures
    SET IdFacture = @NewFactureId,
        Montant = @MontantTotal  -- Montant consolidé
    WHERE IdClient = @ClientId AND Mois = @Mois AND Annees = @Annee AND Statut = 1;
    
    -- Migrer les Paiements vers la nouvelle Facture
    UPDATE Paiements p
    INNER JOIN ClientFactures cf ON p.IdFacture = cf.IdFacture AND p.IdClient = cf.IdClient
    SET p.IdFacture = @NewFactureId
    WHERE cf.IdClient = @ClientId AND cf.Mois = @Mois AND cf.Annees = @Annee;
END;
```

#### 3.2 Validation de la Migration
- [ ] Vérifier que toutes les `ClientFacture` ont été migrées
- [ ] Vérifier que tous les `Paiement` ont été migrés
- [ ] Vérifier la cohérence des montants (somme des anciennes = nouvelles)
- [ ] Vérifier qu'il n'y a pas de doublons

---

### Phase 4 : Modifications du Code (12 heures)

#### 4.1 Service `FactureService`

**Modifications :**
```csharp
// AVANT
public async Task<Facture> CreateAsync(Facture facture)
{
    // facture.IdUsage est requis
    // Crée une facture pour un usage
}

// APRÈS
public async Task<Facture> CreateAsync(Facture facture)
{
    // facture.IdClient est requis
    // Récupère tous les usages du client
    // Calcule le montant total consolidé
    // Crée une seule facture pour le client
}
```

**Nouvelle méthode `CreateClientFacturesForFactureAsync` :**
```csharp
private async Task CreateClientFacturesForFactureAsync(Facture facture)
{
    // Récupérer tous les usages du client
    var clientUsages = await _context.ClientUsages
        .Include(cu => cu.Usage)
        .Where(cu => cu.IdClient == facture.IdClient && 
                    cu.Client != null && 
                    cu.Client.Statut == true &&
                    cu.Statut == true)
        .ToListAsync();
    
    // Calculer le montant total consolidé
    decimal montantTotal = 0;
    var detailUsages = new List<object>();
    
    foreach (var clientUsage in clientUsages)
    {
        // Récupérer le montant de base pour cet usage (depuis une table de tarifs ou facture précédente)
        // Pour l'instant, on suppose qu'il y a une logique pour obtenir le montant par usage
        var montantBase = await GetMontantBaseForUsageAsync(clientUsage.IdUsage, facture.MoisEmission, facture.AnneesEmission);
        var nombreBatiment = clientUsage.nombreBatiment > 0 ? clientUsage.nombreBatiment : 1;
        var montantUsage = montantBase * nombreBatiment;
        
        montantTotal += montantUsage;
        
        detailUsages.Add(new
        {
            IdUsage = clientUsage.IdUsage,
            LibelleUsage = clientUsage.Usage?.Libelle,
            MontantBase = montantBase,
            nombreBatiment = nombreBatiment,
            MontantTotal = montantUsage
        });
    }
    
    // Mettre à jour la facture avec le montant consolidé et le détail
    facture.Montant = montantTotal;
    facture.DetailUsagesJson = JsonSerializer.Serialize(detailUsages);
    facture.DescriptionUsages = string.Join(", ", detailUsages.Select(d => $"{d.LibelleUsage} ({d.nombreBatiment} bât.)"));
    
    // Créer une seule ClientFacture pour ce client
    var clientFacture = new ClientFacture
    {
        IdFacture = facture.IdFacture,
        IdClient = facture.IdClient,
        Montant = montantTotal,
        nombreBatiment = clientUsages.Sum(cu => cu.nombreBatiment), // Total des bâtiments
        MontantPaye = 0,
        MontantDu = montantTotal,
        Mois = facture.MoisEmission.ToString("D2"),
        Annees = facture.AnneesEmission,
        DateEmission = facture.DateEmission ?? DateTime.Now,
        EstArrierePreExistant = false,
        Statut = true,
        DateCreation = DateTime.Now
    };
    
    await _clientFactureRepository.CreateAsync(clientFacture);
}
```

**Modification de `GenerateNumeroFactureAsync` :**
```csharp
// AVANT
var prefix = $"FAC-{initiales}-{dateRef:MMyy}";  // initiales de l'usage

// APRÈS
var client = await _context.Clients.FindAsync(facture.IdClient);
var codeCons = client?.CodeCons ?? "UNK";
var prefix = $"FAC-{codeCons}-{dateRef:MMyy}";  // CodeCons du client
```

#### 4.2 Service `PaiementService`
- ✅ **Pas de changement majeur** : Le paiement reste lié à `Facture`
- ✅ **Simplification** : Un paiement paie toute la facture consolidée

#### 4.3 Service `ArrieresService`
- ✅ **Pas de changement** : Utilise toujours `ClientFacture`
- ✅ **Simplification** : Un client a une seule facture par période

#### 4.4 Service `FactureNotificationService`
- [ ] Modifier la diffusion pour se faire par client (au lieu d'usage)
- [ ] Adapter le template d'email pour afficher le détail des usages

#### 4.5 Controllers
- [ ] Modifier `POST /api/Facture` : Accepter `IdClient` au lieu de `IdUsage`
- [ ] Modifier `GET /api/Facture` : Filtrer par client au lieu d'usage
- [ ] Adapter les DTOs pour inclure `IdClient` et `DetailUsagesJson`

---

### Phase 5 : Tests (8 heures)

#### 5.1 Tests Unitaires
- [ ] Test de création d'une facture consolidée
- [ ] Test de calcul du montant total
- [ ] Test de génération du numéro de facture avec CodeCons
- [ ] Test de création de ClientFacture consolidée

#### 5.2 Tests d'Intégration
- [ ] Test de création de facture pour un client avec plusieurs usages
- [ ] Test de paiement d'une facture consolidée
- [ ] Test de calcul des arriérés avec facture consolidée
- [ ] Test de diffusion d'une facture consolidée

#### 5.3 Tests de Régression
- [ ] Vérifier que les clients avec un seul usage fonctionnent toujours
- [ ] Vérifier que les arriérés pré-existants fonctionnent toujours
- [ ] Vérifier que les rapports fonctionnent toujours

---

### Phase 6 : Déploiement (4 heures)

#### 6.1 Pré-déploiement
- [ ] Backup complet de la base de données
- [ ] Exécuter les scripts de migration en environnement de test
- [ ] Valider les résultats de la migration

#### 6.2 Déploiement
- [ ] Arrêter l'application
- [ ] Exécuter les migrations SQL
- [ ] Déployer le nouveau code
- [ ] Redémarrer l'application
- [ ] Vérifier les logs d'erreur

#### 6.3 Post-déploiement
- [ ] Vérifier que les nouvelles factures sont créées correctement
- [ ] Vérifier que les paiements fonctionnent
- [ ] Vérifier que les arriérés sont calculés correctement
- [ ] Monitorer les performances

---

## ⚠️ Risques et Mitigation

### Risque 1 : Perte de Données lors de la Migration
**Probabilité** : Moyenne  
**Impact** : Critique  
**Mitigation** :
- Backup complet avant migration
- Script de rollback préparé
- Tests en environnement de test d'abord

### Risque 2 : Incohérence des Montants
**Probabilité** : Faible  
**Impact** : Élevé  
**Mitigation** :
- Validation des montants après migration
- Scripts de vérification
- Comparaison avant/après

### Risque 3 : Problèmes de Performance
**Probabilité** : Faible  
**Impact** : Moyen  
**Mitigation** :
- Index sur `IdClient` dans `Factures`
- Tests de charge
- Monitoring post-déploiement

### Risque 4 : Problèmes avec les Rapports Existants
**Probabilité** : Moyenne  
**Impact** : Moyen  
**Mitigation** :
- Tests de régression
- Adapter les rapports si nécessaire
- Documentation des changements

---

## 📊 Estimation Totale

| Phase | Durée | Complexité |
|-------|-------|------------|
| Phase 1 : Analyse | 4h | ⭐⭐ |
| Phase 2 : Modèles | 6h | ⭐⭐⭐⭐ |
| Phase 3 : Migration | 8h | ⭐⭐⭐⭐⭐ |
| Phase 4 : Code | 12h | ⭐⭐⭐⭐ |
| Phase 5 : Tests | 8h | ⭐⭐⭐ |
| Phase 6 : Déploiement | 4h | ⭐⭐⭐ |
| **TOTAL** | **42h** | **⭐⭐⭐⭐** |

**Temps estimé :** 5-6 jours de développement (1 semaine avec buffer)

---

## ✅ Checklist de Validation

- [ ] Analyse des données existantes complétée
- [ ] Backup créé
- [ ] Modèles de données modifiés
- [ ] Migration SQL créée et testée
- [ ] Code modifié et testé
- [ ] Tests unitaires passés
- [ ] Tests d'intégration passés
- [ ] Tests de régression passés
- [ ] Documentation mise à jour
- [ ] Déploiement en production réussi
- [ ] Validation post-déploiement

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0
