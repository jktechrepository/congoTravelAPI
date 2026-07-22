# Configuration SMTP CongoTravel (EmailSettings)

## Paramètres recommandés (LWS)

| Clé | Valeur |
|-----|--------|
| `EmailSettings:SmtpServer` | `mail.rusa-travel.com` (secours : `mail94.lwspanel.com`) |
| `EmailSettings:Port` | `587` (STARTTLS — compatible avec `SmtpClient.EnableSsl`) |
| `EmailSettings:SenderEmail` | `no-reply@rusa-travel.com` |
| `EmailSettings:SenderName` | `CongoTravel` |
| `EmailSettings:ReplyToEmail` | `no-reply@rusa-travel.com` |
| `FrontendSettings:BaseUrl` | URL du front (liens dans les emails) |

Port `465` (SSL implicite) : à tester seulement si le port 587 échoue.

## Mot de passe (ne pas committer)

`appsettings.json` est ignoré par Git. Ne pas versionner le mot de passe en clair.

### Développement local (User Secrets)

```bash
cd "/Users/mac/Documents/Developpement/Projet Kansa/CongoTravelAPI"
dotnet user-secrets set "EmailSettings:Password" "VOTRE_MOT_DE_PASSE" --project CongoTravel.csproj
```

Le projet utilise `UserSecretsId` : `congotravel-api-local` (voir [`CongoTravel.csproj`](../../../CongoTravel.csproj)).

### Production

Variable d'environnement :

```bash
export EmailSettings__Password="VOTRE_MOT_DE_PASSE"
```

Ou entrée dans `appsettings.Production.json` (fichier gitignoré).

## Fichiers de référence

- Template : [`appsettings.template.json`](../../../appsettings.template.json)
- Service : [`Services/EmailService.cs`](../../../Services/EmailService.cs)

## Test manuel

1. Configurer `EmailSettings` + mot de passe (secrets ou Development).
2. Démarrer l'API.
3. Déclencher un envoi :
   - `POST /api/Utilisateur/authentifier` avec reset password, ou
   - création utilisateur avec email de bienvenue.
4. Vérifier la boîte destinataire : expéditeur `no-reply@rusa-travel.com`, liens vers `FrontendSettings:BaseUrl`.

En cas d'échec, consulter les logs `Erreur SMTP` (auth, firewall port 587, certificat).
