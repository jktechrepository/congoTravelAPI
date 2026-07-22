# Améliorations des Notifications de Factures

## ✅ Améliorations Implémentées

### 1. Configuration du Lien (appsettings.json)

**Avant** : Lien hardcodé dans le code
```csharp
var lienFacture = $"https://k-energie.kansaconsulting.com/factures/{facture.IdFacture}";
```

**Après** : Lien configurable via `appsettings.json`
```csharp
_baseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "https://k-energie.kansaconsulting.com";
_facturePath = _configuration["FrontendSettings:FacturePath"] ?? "/factures";
var lienFacture = $"{_baseUrl.TrimEnd('/')}{_facturePath.TrimStart('/')}/{facture.IdFacture}";
```

**Configuration dans appsettings.json** :
```json
{
  "FrontendSettings": {
    "BaseUrl": "https://k-energie.kansaconsulting.com",
    "FacturePath": "/factures"
  }
}
```

**Avantages** :
- ✅ Facilement modifiable selon l'environnement (dev/staging/prod)
- ✅ Pas besoin de recompiler pour changer l'URL
- ✅ Support de différents domaines par société

---

### 2. Optimisation du SMS

#### A. Ajout de la Période

**Avant** :
```
K-Energie: Nouvelle facture FACT-2025-001 - 15,000.00 FC. Voir: https://k-energie.kansaconsulting.com/factures/123
```

**Après** :
```
K-Energie: Facture FACT-2025-001 (12/2025) - 15,000.00 FC. https://k-energie.kansaconsulting.com/f/123
```

**Améliorations** :
- ✅ Période (mois/année) ajoutée : `(12/2025)`
- ✅ Format plus concis : "Facture" au lieu de "Nouvelle facture"
- ✅ URL raccourcie : `/f/123` au lieu de `/factures/123`
- ✅ Suppression de "Voir:" pour économiser des caractères

#### B. Système Adaptatif

Le système ajuste automatiquement le message si la longueur dépasse 160 caractères :

1. **Version complète** (avec période) : `K-Energie: Facture FACT-2025-001 (12/2025) - 15,000.00 FC. {url}`
2. **Version sans période** (si trop long) : `K-Energie: Facture FACT-2025-001 - 15,000.00 FC. {url}`
3. **Version ultra-courte** (sans URL) : `K-Energie: Facture FACT-2025-001 (12/2025) - 15,000.00 FC`

**Gain de caractères** :
- Avant : ~120 caractères
- Après : ~115 caractères (avec période incluse !)
- Économie : ~15 caractères grâce à l'URL courte

---

### 3. Amélioration du Push Notification

#### A. Titre avec Nom de Société

**Avant** :
```
Titre: Nouvelle facture disponible
Corps: Facture FACT-2025-001 - 15,000.00 FC
```

**Après** :
```
Titre: Nouvelle facture - K-Energie
Corps: Facture FACT-2025-001 (12/2025) - 15,000.00 FC
```

**Avantages** :
- ✅ Identification immédiate de la société source
- ✅ Utile pour les utilisateurs avec plusieurs comptes
- ✅ Cohérent avec le format SMS

#### B. Métadonnées Enrichies

**Avant** :
```json
{
    "type": "FACTURE",
    "idFacture": "123",
    "numeroFacture": "FACT-2025-001",
    "montant": "15,000.00",
    "lien": "https://k-energie.kansaconsulting.com/factures/123"
}
```

**Après** :
```json
{
    "type": "FACTURE",
    "idFacture": "123",
    "numeroFacture": "FACT-2025-001",
    "montant": "15,000.00",
    "moisAnnee": "12/2025",
    "dateEmission": "15/12/2025",
    "nomSociete": "K-Energie",
    "lien": "https://k-energie.kansaconsulting.com/factures/123"
}
```

**Avantages** :
- ✅ Plus de contexte dans le corps
- ✅ Nom de société dans le titre pour identification immédiate
- ✅ Métadonnées complètes pour traitement côté client
- ✅ Filtrage possible par période et par société

---

## 📊 Comparaison Avant/Après

| Canal | Avant | Après | Amélioration |
|-------|-------|-------|--------------|
| **SMS** | 120 chars, pas de période, URL longue | 115 chars, période incluse, URL courte | ✅ -5 chars, +période |
| **Push** | Titre générique, corps simple | Titre avec société, corps avec période + métadonnées enrichies | ✅ +identification société, +contexte |
| **Email** | Lien hardcodé | Lien configurable | ✅ Flexibilité |
| **In-App** | Inchangé (déjà optimal) | Inchangé | ✅ Déjà bon |

---

## 🔧 Configuration Requise

### appsettings.json

Assurez-vous que la section suivante existe :

```json
{
  "FrontendSettings": {
    "BaseUrl": "https://k-energie.kansaconsulting.com",
    "FacturePath": "/factures"
  }
}
```

### Route Frontend

Pour que l'URL courte `/f/{id}` fonctionne, le frontend doit avoir une route qui redirige vers `/factures/{id}` :

```javascript
// Exemple Vue Router
{
  path: '/f/:id',
  redirect: to => `/factures/${to.params.id}`
}
```

---

## 📝 Exemples de Messages Optimisés

### SMS (Format Final)
```
K-Energie: Votre facture de (12/2025) est de 15,000.00 FC. N° FACT-2025-001.
```
**Longueur** : 76 caractères ✅ (très confortable, 84 caractères de marge)

**Note** : Le format sans URL a été choisi pour :
- ✅ Économiser des caractères (50 caractères économisés)
- ✅ Message plus court et plus lisible
- ✅ Le client peut accéder à la facture via les autres canaux (push, email, in-app) qui contiennent le lien

### Push
```
Titre: Nouvelle facture - K-Energie
Corps: Facture FACT-2025-001 (12/2025) - 15,000.00 FC
```

---

## ✅ Résultat

Toutes les améliorations prioritaires ont été implémentées :
- ✅ Lien configurable
- ✅ SMS optimisé avec période
- ✅ Push enrichi
- ✅ Code maintenable et flexible

Les notifications sont maintenant **plus informatives**, **plus courtes** (SMS), et **plus flexibles** (configuration).

