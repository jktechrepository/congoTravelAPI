# Test de la création automatique d'utilisateur lors de la création d'un client

## ✅ Fonctionnalité implémentée

Lors de la création d'un client via l'API, le système crée automatiquement :
1. Un utilisateur associé au client
2. Attribution du rôle "Client"
3. Génération d'un username unique
4. Mot de passe par défaut : `123456`
5. Envoi d'un email de bienvenue (si l'email du client est fourni)

## 📋 Prérequis

1. ✅ Les migrations ont été appliquées : `dotnet ef database update --context CongoTravelDbContext`
2. ✅ L'application doit être démarrée : `dotnet run`
3. ✅ Vous devez être authentifié en tant que Super-Admin ou Admin

## 🧪 Test manuel via Swagger

### Étape 1 : Authentification
1. Ouvrez Swagger : `http://localhost:7110/swagger`
2. Utilisez l'endpoint `/api/Auth/login`
3. Connectez-vous avec :
   - Email/Telephone : `superadmin@kenergie.com`
   - Mot de passe : `SuperAdmin123!`
4. Copiez le `token` de la réponse

### Étape 2 : Créer un client
1. Cliquez sur l'endpoint `POST /api/Client`
2. Cliquez sur "Authorize" et collez votre token
3. Utilisez le body suivant :
```json
{
  "nomClient": "Test Client Automatique",
  "adresseClient": "123 Rue Test, Kinshasa",
  "telephone": "+243900000001",
  "emailClient": "test.client@example.com",
  "genreClient": "Masculin",
  "numeroCompteur": "COMPT001",
  "statut": true,
  "idCategorieClient": null
}
```
4. Exécutez la requête
5. Notez l'`idClient` de la réponse

### Étape 3 : Vérifier l'utilisateur créé
1. Utilisez l'endpoint `GET /api/Utilisateur/search?email=test.client@example.com`
2. Vérifiez que l'utilisateur existe avec :
   - `idClient` = ID du client créé
   - `email` = email du client
   - `defaultUsername` = username généré automatiquement
   - `roles` contient "Client"

### Étape 4 : Vérifier les rôles
1. Utilisez l'endpoint `GET /api/Utilisateur/{idUtilisateur}`
2. Vérifiez que `roles` contient le rôle "Client"

## 🧪 Test via curl

```bash
# 1. Authentification
TOKEN=$(curl -s -X POST http://localhost:7110/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone":"superadmin@kenergie.com","motDePasse":"SuperAdmin123!"}' \
  | python3 -c "import sys, json; print(json.load(sys.stdin).get('token', ''))")

# 2. Créer un client
CLIENT_RESPONSE=$(curl -s -X POST http://localhost:7110/api/Client \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "nomClient": "Test Client Automatique",
    "adresseClient": "123 Rue Test, Kinshasa",
    "telephone": "+243900000001",
    "emailClient": "test.client@example.com",
    "genreClient": "Masculin",
    "numeroCompteur": "COMPT001",
    "statut": true
  }')

CLIENT_ID=$(echo "$CLIENT_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin).get('idClient', ''))")
echo "Client créé avec ID: $CLIENT_ID"

# 3. Vérifier l'utilisateur
curl -s -X GET "http://localhost:7110/api/Utilisateur/search?email=test.client@example.com" \
  -H "Authorization: Bearer $TOKEN" \
  | python3 -m json.tool
```

## 🔍 Vérification SQL directe

```sql
-- Trouver le client créé
SELECT * FROM Clients WHERE EmailClient = 'test.client@example.com';

-- Trouver l'utilisateur associé
SELECT 
    u.IdUtilisateur,
    u.NomComplet,
    u.Email,
    u.DefaultUsername,
    u.IdClient,
    c.NomClient
FROM Utilisateurs u
LEFT JOIN Clients c ON u.IdClient = c.IdClient
WHERE u.IdClient = VOTRE_ID_CLIENT;

-- Vérifier le rôle assigné
SELECT 
    ur.IdUserRole,
    ur.IdUtilisateur,
    ur.IdRole,
    ur.IsPrimary,
    ur.Statut,
    r.Nom as RoleNom
FROM UserRoles ur
JOIN Roles r ON ur.IdRole = r.IdRole
WHERE ur.IdUtilisateur = VOTRE_ID_UTILISATEUR
  AND r.Nom = 'Client';
```

## ✅ Résultats attendus

1. **Client créé** avec tous les champs fournis
2. **Utilisateur créé automatiquement** avec :
   - `IdClient` = ID du client
   - `Email` = email du client (si fourni)
   - `Telephone` = téléphone du client (si fourni)
   - `NomComplet` = nom du client
   - `DefaultUsername` = nom généré (ex: "TestClient123")
   - `MotDePasseHash` = hash de "123456"
   - `DoitChangerMotDePasse` = true

3. **UserRole créé** avec :
   - `IdUtilisateur` = ID de l'utilisateur
   - `IdRole` = ID du rôle "Client"
   - `IsPrimary` = true
   - `Statut` = true

4. **Email envoyé** (si l'email du client était fourni)

## 🐛 Dépannage

### Si l'utilisateur n'est pas créé
1. Vérifiez les logs de l'application pour les erreurs
2. Vérifiez que le rôle "Client" existe dans la base de données
3. Vérifiez que l'email du client est valide (si fourni)

### Si l'email n'est pas envoyé
1. Vérifiez la configuration SMTP dans `appsettings.json`
2. Vérifiez les logs pour les erreurs d'envoi d'email
3. L'envoi d'email est asynchrone et ne bloque pas la création

### Si l'authentification échoue
1. Vérifiez que les données par défaut ont été initialisées
2. Utilisez l'endpoint `/api/Init/initialize` pour initialiser les données
3. Vérifiez que le Super-Admin existe dans la base de données

