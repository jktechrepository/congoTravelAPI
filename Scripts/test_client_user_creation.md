# Test de création automatique d'utilisateur pour un client

## Prérequis
1. L'application doit être démarrée
2. Les migrations doivent être appliquées : `dotnet ef database update --context CongoTravelDbContext`
3. Vous devez être authentifié en tant que Super-Admin ou Admin

## Étapes de test

### 1. Authentification
```bash
curl -X POST http://localhost:7110/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "emailOuTelephone": "superadmin@kenergie.com",
    "motDePasse": "SuperAdmin123!"
  }'
```

Copiez le `token` de la réponse.

### 2. Créer un client avec email
```bash
TOKEN="VOTRE_TOKEN_ICI"

curl -X POST http://localhost:7110/api/Client \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "nomClient": "Test Client",
    "adresseClient": "123 Rue Test, Kinshasa",
    "telephone": "+243900000000",
    "emailClient": "test.client@example.com",
    "genreClient": "Masculin",
    "numeroCompteur": "COMPT001",
    "statut": true,
    "idCategorieClient": null
  }'
```

Notez l'`idClient` de la réponse.

### 3. Vérifier que l'utilisateur a été créé

#### Via l'API (recherche par email)
```bash
curl -X GET "http://localhost:7110/api/Utilisateur/search?email=test.client@example.com" \
  -H "Authorization: Bearer $TOKEN"
```

#### Via SQL direct
```sql
-- Vérifier l'utilisateur créé
SELECT 
    u.IdUtilisateur,
    u.NomComplet,
    u.Email,
    u.DefaultUsername,
    u.IdClient,
    u.IdSociete,
    c.NomClient,
    r.Nom as RoleNom
FROM Utilisateurs u
LEFT JOIN Clients c ON u.IdClient = c.IdClient
LEFT JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
LEFT JOIN Roles r ON ur.IdRole = r.IdRole
WHERE u.IdClient = VOTRE_ID_CLIENT
  AND ur.Statut = 1;
```

### 4. Vérifier les rôles assignés
```sql
-- Vérifier que le rôle "Client" a été assigné
SELECT 
    ur.IdUserRole,
    ur.IdUtilisateur,
    ur.IdRole,
    ur.IsPrimary,
    ur.Statut,
    r.Nom as RoleNom
FROM UserRoles ur
JOIN Roles r ON ur.IdRole = r.IdRole
WHERE ur.IdUtilisateur = VOTRE_ID_UTILISATEUR;
```

### 5. Vérifier l'email envoyé
Si l'email du client était fourni, un email de bienvenue devrait avoir été envoyé. Vérifiez les logs de l'application pour voir si l'email a été envoyé avec succès.

## Résultats attendus

1. ✅ Un utilisateur doit être créé automatiquement avec :
   - `IdClient` = ID du client créé
   - `Email` = email du client (si fourni)
   - `Telephone` = téléphone du client (si fourni)
   - `NomComplet` = nom du client
   - `DefaultUsername` = nom généré automatiquement (ex: "TestClient123")
   - `MotDePasseHash` = hash du mot de passe par défaut "123456"
   - `DoitChangerMotDePasse` = true

2. ✅ Un `UserRole` doit être créé avec :
   - `IdUtilisateur` = ID de l'utilisateur créé
   - `IdRole` = ID du rôle "Client"
   - `IsPrimary` = true
   - `Statut` = true

3. ✅ Un email de bienvenue doit être envoyé (si l'email du client était fourni)

## Cas de test supplémentaires

### Test 1 : Client sans email
Créer un client sans `emailClient`. L'utilisateur doit quand même être créé, mais sans email.

### Test 2 : Client avec utilisateur existant
Si un utilisateur existe déjà avec le même email ou téléphone, le système doit :
- Ajouter le rôle "Client" à l'utilisateur existant
- Mettre à jour `IdClient` de l'utilisateur
- Ne pas créer un nouvel utilisateur

### Test 3 : Client avec email invalide
Le système doit créer l'utilisateur mais ne pas envoyer d'email si l'email est invalide.

