# Endpoint d'Export des Clients

## 📋 Description

Permet d'exporter la liste complète des clients avec leurs usages au format Excel, sans limitation de pagination.

## 🚀 Endpoint

```
GET /api/Client/societe/{idSociete}/export
```

## 📝 Paramètres

### Paramètres de Route
- **`idSociete`** (int, obligatoire) : ID de la société pour filtrer les clients

### Paramètres de Query
- **`fileType`** (string, optionnel) : Type de fichier d'export
  - Valeur par défaut : `"excel"`
  - Valeurs supportées : `"excel"` (seul format implémenté actuellement)
  - Future : `"pdf"` (planifié)

- **`idAxe`** (int, optionnel) : Filtre par Axe géographique
  - Si non spécifié : exporte tous les axes de la société
  - Exemple : `?idAxe=5`

- **`includeInactive`** (bool, optionnel) : Inclure les clients inactifs
  - Valeur par défaut : `false`
  - Exemple : `?includeInactive=true`

- **`searchTerm`** (string, optionnel) : Terme de recherche
  - Recherche dans : NomClient, AdresseClient, Telephone, EmailClient, CodeCons
  - Exemple : `?searchTerm=john`

## 📊 Filtres Appliqués

### Clients Exportés
- ✅ `Statut == true` (clients non supprimés)
- ✅ `IsActif == true` (sauf si `includeInactive=true`)
- ✅ Appartenant à la société spécifiée via les usages

### Usages Exportés
- ✅ `Statut == true` (usages actifs uniquement)
- ✅ `Usage.Statut == true` (usages actifs)
- ✅ `ClientUsage.Statut == true` (relations actives)

## 📋 Colonnes Excel Exportées

### Informations Client (8 colonnes)
- **Nom Client** : Nom complet du client
- **Adresse** : Adresse complète du client
- **Téléphone** : Numéro de téléphone
- **Email** : Adresse email
- **Genre** : Genre (M/F/Autre)
- **Code Cons** : Code consommateur
- **Actif** : Statut actif/inactif (Oui/Non)
- **Date Création** : Date de création du client

### Informations Géographiques (2 colonnes)
- **Nom Axe** : Nom de l'axe géographique
- **Nom Cabine** : Nom de la cabine

### Informations Usages (4 colonnes)
- **Usages** : Liste des libellés d'usages (séparés par des points-virgules)
- **Nombre Bâtiments** : Liste des nombres de bâtiments par usage (séparés par des points-virgules)
- **Catégories Usages** : Liste des catégories (séparées par des points-virgules)
- **Nombre Usages** : Nombre total d'usages actifs

**Total : 14 colonnes (au lieu de 20 précédemment)**

## 🎯 Exemples d'Utilisation

### Export complet pour une société
```bash
GET /api/Client/societe/1/export
```

### Export avec filtre par axe
```bash
GET /api/Client/societe/1/export?idAxe=5
```

### Export avec recherche
```bash
GET /api/Client/societe/1/export?searchTerm=dupont
```

### Export incluant les clients inactifs
```bash
GET /api/Client/societe/1/export?includeInactive=true
```

### Export combiné
```bash
GET /api/Client/societe/1/export?idAxe=5&searchTerm=jean&includeInactive=false
```

## 📄 Format de Réponse

### Succès (200 OK)
```http
HTTP/1.1 200 OK
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="clients_societe_1_20250209_143022.xlsx"

[Binary Excel file content]
```

### Erreurs
```json
// Type de fichier non supporté
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Type de fichier non supporté. Utilisez 'excel' ou 'pdf'."
}

// Format non implémenté
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Seul l'export Excel est actuellement disponible."
}
```

## 🔐 Sécurité

### Authentification Requise
- **Bearer Token JWT** obligatoire dans l'en-tête `Authorization`
- **Rôles autorisés** : Tous les rôles authentifiés

### Audit Trail
- ✅ Toutes les actions d'export sont tracées dans `AuditLog`
- Informations enregistrées : utilisateur, société, filtres utilisés, timestamp

## ⚡ Performance

### Optimisations
- **Requête optimisée** avec `Include` pour éviter N+1
- **Filtrage côté serveur** pour limiter le volume de données
- **Génération en mémoire** avec EPPlus (très performant)
- **Formatage automatique** des colonnes Excel

### Limites
- **Pas de limitation** sur le nombre de clients exportés
- **Memory usage** : dépend du volume de données
- **Timeout** : configuré pour les exports massifs

## 🔄 Évolutions Prévues

### Phase 2 (Court terme)
- **Export PDF** avec mise en forme professionnelle
- **Filtres temporels** (date début/fin)
- **Tri personnalisable** des colonnes

### Phase 3 (Moyen terme)
- **Export multi-feuilles** (clients, usages, factures séparés)
- **Templates personnalisables** pour l'export
- **Export par lots** pour très gros volumes

## 🧪 Tests

### Cas de test recommandés
1. **Export basique** : Société sans filtres
2. **Filtre Axe** : Vérifier la filtration géographique
3. **Recherche texte** : Vérifier la recherche dans tous les champs
4. **Clients inactifs** : Vérifier l'inclusion/exclusion
5. **Société vide** : Vérifier le comportement avec ID inexistant
6. **Permissions** : Tester avec différents rôles utilisateur

## 📝 Notes d'Implémentation

### Dépendances
- **EPPlus 7.0.0** : déjà présent dans le projet
- **Entity Framework Core** : pour les requêtes optimisées
- **Serilog** : pour le logging des opérations

### Fichiers créés/modifiés
- `Models/DTOs/Client/ClientExportDto.cs` : DTOs pour l'export
- `Services/ClientExportService.cs` : Service métier d'export
- `Controllers/ClientController.cs` : Endpoint d'export
- `Program.cs` : Enregistrement du service
