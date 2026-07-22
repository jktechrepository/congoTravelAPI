# 📧 Modèles de Notifications - Création Client

## Vue d'ensemble

Lors de la création d'un client, deux types de notifications sont envoyés automatiquement :

1. **📧 Email de bienvenue** - Envoyé si un email est fourni
2. **📱 SMS de bienvenue** - Envoyé si un téléphone est fourni

---

## 📧 Email de Bienvenue

### Fichier source
`Services/EmailService.cs` - Méthode `CreateWelcomeEmailTemplate` (lignes 303-551)

### Paramètres utilisés
- `nomComplet` : Nom complet du client
- `email` : Email du client
- `defaultUsername` : Nom d'utilisateur par défaut
- `telephone` : Numéro de téléphone
- `motDePasseParDefaut` : Mot de passe temporaire
- `role` : Rôle du client (généralement "Client")
- `nomSociete` : Nom de la société
- `genre` : Genre du client (Masculin/Féminin)

### Sujet de l'email
```
Bienvenue sur K-Energie - Vos identifiants de connexion
```

### Structure du template HTML

```html
<!DOCTYPE html>
<html lang='fr'>
<head>
    <!-- Styles CSS avec thème AWS/K-Energie -->
</head>
<body>
    <div class='email-wrapper'>
        <!-- Header avec logo K-Energie -->
        <div class='header'>
            <h1 class='header-logo'>K-Energie</h1>
        </div>
        
        <div class='content'>
            <!-- Titre -->
            <h2 class='title'>Bienvenue sur K-Energie</h2>
            
            <!-- Salutation personnalisée selon le genre -->
            <p class='greeting'>Bonjour <strong>{Salutation}</strong>,</p>
            
            <!-- Message de bienvenue -->
            <p class='message'>
                Votre compte a été créé avec succès sur la plateforme K-Energie. 
                Vous faites maintenant partie de <strong>{nomSociete}</strong> en tant que <span class='role-badge'>{role}</span>.
            </p>
            
            <!-- Section des identifiants -->
            <div class='info-section'>
                <h3 class='info-section-title'>Vos identifiants de connexion</h3>
                <div class='credential-row'>
                    <span class='credential-label'>Email :</span>
                    <span class='credential-value'>{email}</span>
                </div>
                <div class='credential-row'>
                    <span class='credential-label'>Nom d'utilisateur :</span>
                    <span class='credential-value'>{defaultUsername}</span>
                </div>
                <div class='credential-row'>
                    <span class='credential-label'>Téléphone :</span>
                    <span class='credential-value'>{telephone}</span>
                </div>
                <div class='credential-row'>
                    <span class='credential-label'>Mot de passe :</span>
                    <span class='credential-value'>{motDePasseParDefaut}</span>
                </div>
            </div>
            
            <!-- Avertissement sécurité -->
            <div class='warning-box'>
                <strong>Important :</strong> Pour des raisons de sécurité, vous devrez 
                <strong>obligatoirement changer votre mot de passe</strong> lors de votre première connexion.
            </div>
            
            <!-- Bouton de connexion -->
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://k-energie.kansaconsulting.com' class='button'>Se connecter maintenant</a>
            </div>
            
            <!-- Note sur les méthodes de connexion -->
            <p style='margin-top: 30px; font-size: 14px; color: #666666;'>
                Vous pouvez vous connecter en utilisant votre <strong>email</strong>, 
                votre <strong>nom d'utilisateur</strong> ou votre <strong>numéro de téléphone</strong>.
            </p>
        </div>
        
        <!-- Footer -->
        <div class='footer'>
            <p class='footer-text'>Cet email a été envoyé automatiquement par K-Energie Platform.</p>
            <p class='footer-text'>© 2025 K-Energie. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>
```

### Caractéristiques du design
- **Thème** : Style AWS (couleurs #232f3e et #ff9900)
- **Responsive** : Adapté aux mobiles
- **Accessibilité** : Contraste élevé, texte lisible
- **Version texte brut** : Disponible en fallback

### Salutation personnalisée
- **Féminin** : "Madame {nomComplet}"
- **Masculin** : "Monsieur {nomComplet}"

---

## 📱 SMS de Bienvenue

### Fichier source
`Services/ClientService.cs` - Méthode `CreateWelcomeSmsMessage` (lignes 691-714)

### Format du message

#### Version standard (≤ 160 caractères)
```
{nomSociete}: Bienvenue ! Votre compte a été créé. Connectez-vous sur {url}. Vos identifiants ont été envoyés sur votre mail.
```

#### Version courte (si > 160 caractères)
```
{nomSociete}: Bienvenue ! Votre compte a été créé. {url}. Vos identifiants ont été envoyés sur votre mail.
```

#### Version ultra-courte (si toujours > 160 caractères)
```
{nomSociete}: Bienvenue ! Compte créé. Identifiants: email envoyé. {url}
```

### Paramètres utilisés
- `nomSociete` : Nom de la société (ex: "K-Energie")
- `_baseUrl` : URL du frontend (configuré dans `appsettings.json`)

### Exemple de message
```
K-Energie: Bienvenue ! Votre compte a été créé. Connectez-vous sur https://k-energie.kansaconsulting.com. Vos identifiants ont été envoyés sur votre mail.
```

### Caractéristiques
- **Longueur maximale** : 160 caractères (limite SMS standard)
- **Adaptatif** : Réduit automatiquement si trop long
- **Informations** : Ne contient pas les identifiants (sécurité)
- **Référence** : Dirige vers l'email pour les identifiants complets

---

## 🔄 Flux d'envoi

### Lors de la création d'un client

1. **Création du client** dans la base de données
2. **Création automatique d'un compte utilisateur** avec :
   - Email (si fourni)
   - Téléphone (si fourni)
   - Nom d'utilisateur généré
   - Mot de passe temporaire
3. **Envoi des notifications** (asynchrone, ne bloque pas la création) :
   - ✅ Email si `email` est fourni
   - ✅ SMS si `telephone` est fourni

### Code source
`Services/ClientService.cs` - Méthode `CreateAsync` (lignes 157-193) et `CreateDefaultClientUserAsync` (lignes 596-665)

---

## ⚙️ Configuration

### URL du frontend
Configuré dans `appsettings.json` :
```json
{
  "FrontendSettings": {
    "BaseUrl": "https://k-energie.kansaconsulting.com"
  }
}
```

### Paramètres SMTP
Configuré dans `appsettings.json` :
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": "587",
    "SenderEmail": "...",
    "Password": "...",
    "SenderName": "K-Energie"
  }
}
```

### Paramètres SMS (Twilio)
Configuré dans `appsettings.json` :
```json
{
  "Twilio": {
    "AccountSid": "...",
    "AuthToken": "...",
    "FromPhoneNumber": "..."
  }
}
```

---

## 📝 Modifications possibles

### Modifier le template email

**Fichier :** `Services/EmailService.cs`
**Méthode :** `CreateWelcomeEmailTemplate` (lignes 303-551)

**Éléments modifiables :**
- Contenu du message
- Design et couleurs
- Structure HTML
- Informations affichées

### Modifier le message SMS

**Fichier :** `Services/ClientService.cs`
**Méthode :** `CreateWelcomeSmsMessage` (lignes 691-714)

**Éléments modifiables :**
- Format du message
- Longueur maximale
- Informations incluses
- Logique de réduction

---

## 🔍 Exemples de notifications

### Email reçu par le client

**Sujet :** `Bienvenue sur K-Energie - Vos identifiants de connexion`

**Contenu :**
- Salutation personnalisée
- Message de bienvenue
- Identifiants de connexion (email, username, téléphone, mot de passe)
- Avertissement de changement de mot de passe
- Bouton de connexion
- Footer avec copyright

### SMS reçu par le client

**Message :**
```
K-Energie: Bienvenue ! Votre compte a été créé. Connectez-vous sur https://k-energie.kansaconsulting.com. Vos identifiants ont été envoyés sur votre mail.
```

---

## ⚠️ Notes importantes

1. **Sécurité** : Le mot de passe est envoyé par email uniquement, pas par SMS
2. **Asynchrone** : Les notifications sont envoyées en arrière-plan (ne bloquent pas la création)
3. **Gestion d'erreurs** : Les échecs d'envoi sont loggés mais n'empêchent pas la création du client
4. **Conditionnel** : Email envoyé seulement si email fourni, SMS seulement si téléphone fourni
5. **Personnalisation** : La salutation s'adapte au genre du client

---

## 🛠️ Personnalisation

### Ajouter des informations supplémentaires

Pour ajouter des informations dans l'email (ex: numéro de compteur, zone, etc.) :

1. Modifier la signature de `SendWelcomeEmailAsync` dans `IEmailService.cs`
2. Modifier l'implémentation dans `EmailService.cs`
3. Modifier le template HTML dans `CreateWelcomeEmailTemplate`
4. Modifier l'appel dans `ClientService.cs`

### Changer le design

Le template HTML utilise des styles inline. Pour modifier le design :
- Modifier les styles CSS dans `CreateWelcomeEmailTemplate`
- Changer les couleurs (#232f3e, #ff9900)
- Modifier la structure HTML

---

**Version :** 1.0  
**Dernière mise à jour :** 15 décembre 2025  
**Fichiers concernés :**
- `Services/EmailService.cs`
- `Services/ClientService.cs`
- `Services/Repositories/IEmailService.cs`

