# 📋 Guide de Migration : Client-CategorieClient Many-to-Many

## 📌 Vue d'ensemble

Ce guide explique comment migrer votre base de données de production pour activer la relation **many-to-many** entre `Client` et `CategorieClient`.

### Objectif
Permettre à un client d'appartenir à **plusieurs catégories** simultanément, tout en conservant la compatibilité avec l'ancien système (catégorie principale via `IdCategorieClient`).

---

## 📁 Scripts disponibles

### 1. `production_migrate_client_categories_many_to_many.sql`
**Script principal de migration** - À exécuter en production
- ✅ Crée la table `ClientCategorieClients`
- ✅ Migre les données existantes
- ✅ Vérifie l'intégrité
- ✅ **IDEMPOTENT** (peut être exécuté plusieurs fois sans erreur)

### 2. `diagnostic_client_categories_many_to_many.sql`
**Script de diagnostic** - Pour vérifier l'état avant/après
- 🔍 Vérifie l'existence de la table
- 📊 Affiche les statistiques
- ⚠️ Détecte les incohérences
- 💡 Donne des recommandations

### 3. `rollback_client_categories_many_to_many.sql`
**Script de rollback** - En cas de problème (sécurisé par défaut)
- 🗑️ Supprime la table `ClientCategorieClients`
- ⚠️ **DESTRUCTIF** - À utiliser avec précaution
- 🔒 Désactivé par défaut (commenté)

---

## 🚀 Procédure de migration

### Étape 1 : Préparation

1. **Sauvegarder la base de données**
   ```bash
   mysqldump -u root -p FactureNormaliseeRDC > backup_avant_migration_$(date +%Y%m%d_%H%M%S).sql
   ```

2. **Tester sur un environnement de staging** (recommandé)

3. **Vérifier l'état actuel**
   ```bash
   mysql -u root -p FactureNormaliseeRDC < Scripts/diagnostic_client_categories_many_to_many.sql
   ```

### Étape 2 : Migration

1. **Exécuter le script de migration**
   ```bash
   mysql -u root -p FactureNormaliseeRDC < Scripts/production_migrate_client_categories_many_to_many.sql
   ```

2. **Vérifier les résultats**
   - Le script affiche automatiquement :
     - ✅ Statut de création de la table
     - ✅ Nombre de relations créées
     - ✅ Statistiques détaillées
     - ✅ Vérifications d'intégrité

3. **Relancer le diagnostic**
   ```bash
   mysql -u root -p FactureNormaliseeRDC < Scripts/diagnostic_client_categories_many_to_many.sql
   ```

### Étape 3 : Vérification post-migration

1. **Vérifier que l'application fonctionne**
   - Tester les endpoints API
   - Vérifier les calculs d'arriérés
   - Tester l'import Excel avec plusieurs catégories

2. **Surveiller les logs** pour détecter d'éventuelles erreurs

---

## 📊 Ce que fait la migration

### Avant la migration
- Un client a **une seule catégorie** via `IdCategorieClient`
- Les factures sont liées à une catégorie
- Les arriérés sont calculés sur une seule catégorie

### Après la migration
- Un client peut avoir **plusieurs catégories** via `ClientCategorieClients`
- La catégorie principale (`IdCategorieClient`) est **conservée** pour compatibilité
- Les arriérés sont calculés sur **toutes les catégories** du client
- Les factures restent liées à **une seule catégorie** (inchangé)

---

## 🔧 Utilisation de l'API

### Ajouter une catégorie à un client
```http
POST /api/Client/{idClient}/categories/{idCategorie}
Authorization: Bearer {token}
```

### Lister les catégories d'un client
```http
GET /api/Client/{idClient}/categories
Authorization: Bearer {token}
```

### Retirer une catégorie d'un client
```http
DELETE /api/Client/{idClient}/categories/{idCategorie}
Authorization: Bearer {token}
```

---

## 📝 Format Excel mis à jour

La colonne `NomCategorieClient` peut maintenant contenir **plusieurs catégories** :

**Format accepté :**
- `"Standard, VIP"` (séparées par virgule)
- `"Standard; VIP"` (séparées par point-virgule)
- `"Résidentiel, Commercial, VIP"` (plusieurs catégories)

**Exemple :**
| NomClient | NomCategorieClient |
|-----------|-------------------|
| Jean Dupont | Résidentiel, Commercial |
| Marie Martin | VIP |

---

## ⚠️ Points d'attention

### Compatibilité ascendante
- ✅ `IdCategorieClient` est **conservé** dans la table `Clients`
- ✅ Les anciens endpoints continuent de fonctionner
- ✅ Les données existantes sont **automatiquement migrées**

### Calculs d'arriérés
- Les arriérés sont maintenant calculés sur **toutes les catégories** d'un client
- La somme inclut les factures de toutes les catégories auxquelles le client appartient

### Factures
- ⚠️ Les factures restent liées à **une seule catégorie** (inchangé)
- Une facture ne peut pas être liée à plusieurs catégories

---

## 🔄 Rollback (en cas de problème)

### ⚠️ ATTENTION : Le rollback est DESTRUCTIF

1. **Ouvrir le script de rollback**
   ```bash
   nano Scripts/rollback_client_categories_many_to_many.sql
   ```

2. **Décommenter les sections PARTIE 3 et PARTIE 4**

3. **Exécuter le rollback**
   ```bash
   mysql -u root -p FactureNormaliseeRDC < Scripts/rollback_client_categories_many_to_many.sql
   ```

4. **Résultat :**
   - ✅ La table `ClientCategorieClients` est supprimée
   - ✅ Les clients conservent leur `IdCategorieClient`
   - ❌ Toutes les relations many-to-many sont **perdues**

---

## 📞 Support

En cas de problème :
1. Vérifier les logs de l'application
2. Exécuter le script de diagnostic
3. Vérifier l'intégrité des données
4. Consulter les logs MySQL

---

## ✅ Checklist de migration

- [ ] Sauvegarde de la base de données effectuée
- [ ] Test sur environnement de staging (si disponible)
- [ ] Diagnostic pré-migration exécuté
- [ ] Script de migration exécuté
- [ ] Diagnostic post-migration exécuté
- [ ] Vérification des endpoints API
- [ ] Test des calculs d'arriérés
- [ ] Test de l'import Excel avec plusieurs catégories
- [ ] Surveillance des logs post-migration

---

## 📅 Historique

- **2025-12-20** : Création du guide et des scripts de migration

---

**✅ Migration prête à être déployée en production**
