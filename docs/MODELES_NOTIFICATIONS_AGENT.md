# 📧 Modèles de Notifications - Création Agent

## Vue d'ensemble

Lors de la création d'un agent, une notification est envoyée automatiquement :

1. **📧 Email de bienvenue** - Envoyé si un email est fourni
2. **📱 SMS de bienvenue** - ❌ **NON ENVOYÉ** (contrairement aux clients)

---

## 📧 Email de Bienvenue

### Fichier source
`Services/AgentService.cs` - Méthode `CreateDefaultAgentUserAsync` (lignes 906-937)  
`Services/EmailService.cs` - Méthode `CreateWelcomeEmailTemplate` (même template que les clients)

### Paramètres utilisés
- `email` : Email de l'agent
- `nomComplet` : Nom complet de l'agent
- `defaultUsername` : Nom d'utilisateur par défaut
- `telephone` : Numéro de téléphone
- `motDePasseParDefaut` : Mot de passe temporaire
- `agentRole.Nom` : Rôle de l'agent (ex: "Admin", "Technicien", "Gerant")
- `nomSociete` : Nom de la société
- `agent.Genre` : Genre de l'agent (Masculin/Féminin)
- `fonction` : Fonction de l'agent (ex: "Technicien", "Gestionnaire")
- `matricule` : Matricule de l'agent

### Sujet de l'email
```
Bienvenue sur K-Energie - Vos identifiants de connexion
```

### Structure du template HTML

Le template est **identique** à celui des clients, mais avec des informations supplémentaires :

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
                Vous faites maintenant partie de <strong>{nomSociete}</strong> en tant que 
                <span class='role-badge'>{fonction}</span>.
                <!-- Note: Pour les agents, la fonction est affichée au lieu du rôle -->
            </p>
            
            <!-- Matricule (si fourni) -->
            {(!string.IsNullOrWhiteSpace(matricule) ? $@"
            <div class='highlight-box'>
                <p class='highlight-box-text'><strong>Matricule :</strong> {matricule}</p>
            </div>" : "")}
            
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

### Différences avec le template client

1. **Fonction affichée** : Pour les agents, la `fonction` est affichée au lieu du `role` dans le badge
2. **Matricule** : Si un matricule est fourni, il est affiché dans une section highlight-box
3. **Pas de section enfant** : Les agents n'ont pas de section pour les enfants (réservée aux tuteurs)

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

### ❌ Non implémenté

**Contrairement aux clients, les agents ne reçoivent PAS de SMS de bienvenue lors de la création.**

**Code source :** `Services/AgentService.cs` - Méthode `CreateDefaultAgentUserAsync` (lignes 906-942)

**Observation :**
- Seul l'email est envoyé si un email est fourni
- Aucun SMS n'est envoyé, même si un téléphone est fourni
- Un log d'avertissement est généré si aucun email n'est fourni : "Notification SMS sera envoyée ultérieurement" (mais ce n'est pas implémenté)

---

## 🔄 Flux d'envoi

### Lors de la création d'un agent

1. **Création de l'agent** dans la base de données
2. **Génération automatique du matricule** (si non fourni)
3. **Création automatique d'un compte utilisateur** avec :
   - Email (si fourni)
   - Téléphone (si fourni)
   - Nom d'utilisateur généré
   - Mot de passe temporaire
   - Rôle basé sur `RoleAgent`
4. **Envoi de notification** (asynchrone, ne bloque pas la création) :
   - ✅ **Email** si `email` est fourni
   - ❌ **SMS** : Non envoyé (même si téléphone fourni)

### Code source
`Services/AgentService.cs` - Méthode `CreateAsync` (lignes 87-157) et `CreateDefaultAgentUserAsync` (lignes 906-937)

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

---

## 📝 Comparaison Client vs Agent

| Aspect | Client | Agent |
|--------|--------|-------|
| **Email de bienvenue** | ✅ Oui | ✅ Oui |
| **SMS de bienvenue** | ✅ Oui | ❌ Non |
| **Template email** | Standard | Standard + Matricule |
| **Fonction affichée** | Non | Oui |
| **Matricule affiché** | Non | Oui (si fourni) |

---

## 🔍 Exemple de notification

### Email reçu par l'agent

**Sujet :** `Bienvenue sur K-Energie - Vos identifiants de connexion`

**Contenu :**
- Salutation personnalisée (Monsieur/Madame)
- Message de bienvenue avec nom de la société
- Badge avec la fonction (ex: "Technicien", "Gestionnaire")
- Matricule (si fourni)
- Identifiants de connexion (email, username, téléphone, mot de passe)
- Avertissement de changement de mot de passe
- Bouton de connexion
- Footer avec copyright

---

## ⚠️ Notes importantes

1. **Sécurité** : Le mot de passe est envoyé par email uniquement
2. **Asynchrone** : Les notifications sont envoyées en arrière-plan (ne bloquent pas la création)
3. **Gestion d'erreurs** : Les échecs d'envoi sont loggés mais n'empêchent pas la création de l'agent
4. **Conditionnel** : Email envoyé seulement si email fourni
5. **Personnalisation** : La salutation s'adapte au genre de l'agent
6. **SMS manquant** : Contrairement aux clients, les agents ne reçoivent pas de SMS

---

## 🛠️ Personnalisation

### Ajouter un SMS de bienvenue pour les agents

Pour ajouter un SMS de bienvenue similaire aux clients :

1. **Créer une méthode** dans `AgentService.cs` :
   ```csharp
   private string CreateWelcomeSmsMessage(
       string nomComplet,
       string defaultUsername,
       string motDePasseParDefaut,
       string nomSociete,
       string fonction,
       string matricule)
   {
       // Format du message SMS
       var message = $"{nomSociete}: Bienvenue {fonction} ! Votre compte a été créé. Connectez-vous sur {_baseUrl}. Vos identifiants ont été envoyés sur votre mail.";
       
       // Gestion de la longueur (max 160 caractères)
       if (message.Length > 160)
       {
           message = $"{nomSociete}: Bienvenue ! Compte créé. Identifiants: email envoyé. {_baseUrl}";
       }
       
       return message;
   }
   ```

2. **Ajouter l'envoi SMS** dans `CreateDefaultAgentUserAsync` :
   ```csharp
   // Envoyer le SMS de bienvenue (si téléphone fourni)
   if (!string.IsNullOrWhiteSpace(telephone))
   {
       string nomSociete = societe.Nom ?? "K-Energie";
       
       string messageSms = CreateWelcomeSmsMessage(
           nomComplet,
           defaultUsername,
           motDePasseParDefaut,
           nomSociete,
           fonction,
           matricule
       );
       
       _ = Task.Run(async () =>
       {
           try
           {
               var smsLog = await _smsService.EnvoyerSmsAsync(
                   telephone,
                   messageSms,
                   "BIENVENUE_AGENT"
               );
           }
           catch (Exception smsEx)
           {
               _logger.LogWarning(smsEx, "⚠️ Échec de l'envoi du SMS à {Telephone}", telephone);
           }
       });
   }
   ```

### Modifier le template email

**Fichier :** `Services/EmailService.cs`  
**Méthode :** `CreateWelcomeEmailTemplate` (lignes 303-551)

**Éléments modifiables :**
- Contenu du message
- Design et couleurs
- Structure HTML
- Informations affichées
- Ajout de sections spécifiques aux agents

---

## 📊 Résumé

### Notifications envoyées

| Type | Client | Agent |
|------|--------|-------|
| Email | ✅ | ✅ |
| SMS | ✅ | ❌ |

### Informations dans l'email

| Information | Client | Agent |
|-------------|--------|-------|
| Salutation personnalisée | ✅ | ✅ |
| Nom de la société | ✅ | ✅ |
| Rôle/Fonction | Rôle | Fonction |
| Matricule | ❌ | ✅ (si fourni) |
| Identifiants complets | ✅ | ✅ |
| Avertissement sécurité | ✅ | ✅ |
| Bouton de connexion | ✅ | ✅ |

---

**Version :** 1.0  
**Dernière mise à jour :** 15 décembre 2025  
**Fichiers concernés :**
- `Services/AgentService.cs`
- `Services/EmailService.cs`
- `Services/Repositories/IEmailService.cs`









