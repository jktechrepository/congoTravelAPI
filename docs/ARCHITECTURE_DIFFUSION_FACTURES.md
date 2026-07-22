# Architecture de Diffusion Multi-Canal des Factures

## 📋 Vue d'ensemble

Ce document décrit l'architecture pour diffuser les factures aux clients via plusieurs canaux :
- **SMS** (via Twilio)
- **Email** (via SMTP)
- **Notification Push** (via Firebase)
- **Notification In-App** (via SignalR)

## 🏗️ Architecture Actuelle

### Services existants
1. **NotificationSender** : Gère Push, In-App, SMS
2. **EmailService** : Gère l'envoi d'emails
3. **TwilioSmsService** : Gère l'envoi de SMS
4. **FirebaseNotificationService** : Gère les notifications push
5. **SignalRNotificationService** : Gère les notifications in-app

### Modèles existants
- `NotificationMessage` : Contient Push, Sms, InApp (manque Email)
- `NotificationContext` : Contexte de la notification
- `NotificationDispatchResult` : Résultat de la préparation

## 🎯 Solution Proposée

### 1. Extension du système de notifications

#### Ajouter Email à NotificationMessage
```csharp
public class EmailNotificationMessage
{
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string PlainTextBody { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}

public class NotificationMessage
{
    public PushNotificationMessage? Push { get; init; }
    public SmsNotificationMessage? Sms { get; init; }
    public InAppNotificationMessage? InApp { get; init; }
    public EmailNotificationMessage? Email { get; init; } // ✨ NOUVEAU
}
```

#### Étendre NotificationSender pour inclure Email
- Ajouter `IEmailService` dans le constructeur
- Ajouter la logique d'envoi d'email dans `SendAsync`

### 2. Service de diffusion de factures

Créer `FactureNotificationService` qui :
- Prépare les messages pour chaque canal
- Utilise `NotificationSender` pour envoyer via tous les canaux
- Gère les préférences du client (quels canaux activer)
- Log les résultats de chaque canal

### 3. Intégration dans FactureController

Lors de la création/mise à jour d'une facture :
1. Récupérer le client associé
2. Récupérer l'utilisateur du client
3. Appeler `FactureNotificationService.DiffuserFactureAsync()`
4. Envoyer via tous les canaux activés

## 📊 Flux de données

```
FactureController.CreateFacture()
    ↓
FactureNotificationService.DiffuserFactureAsync(facture, client, utilisateur)
    ↓
Préparation des messages (Email, SMS, Push, In-App)
    ↓
NotificationSender.SendAsync(dispatchResult)
    ↓
Envoi parallèle via :
    - EmailService.SendEmailAsync()
    - TwilioSmsService.EnvoyerSmsAsync()
    - FirebaseNotificationService.EnvoyerNotificationAUtilisateurAsync()
    - SignalRNotificationService.SendCustomNotificationAsync()
```

## 🔧 Préférences de canal

Chaque client peut avoir des préférences :
- Activer/désactiver certains canaux
- Ordre de priorité des canaux
- Format préféré (SMS court/long, Email HTML/texte)

## 📝 Templates de messages

### Email
- Template HTML avec détails de la facture
- Pièce jointe PDF (optionnel)
- Lien vers le portail client

### SMS
- Message court avec numéro de facture et montant
- Lien court vers le portail

### Push
- Titre : "Nouvelle facture disponible"
- Corps : "Facture #{numero} - {montant} FC"
- Data : { type: "FACTURE", idFacture: "123" }

### In-App
- Titre : "Nouvelle facture"
- Contenu : Détails de la facture
- ActionLink : "/factures/{idFacture}"

## 🚀 Implémentation

Voir les fichiers suivants :
- `Services/FactureNotificationService.cs` (nouveau)
- `Services/Notifications/NotificationModels.cs` (modifié)
- `Services/Notifications/NotificationSender.cs` (modifié)
- `Controllers/FactureController.cs` (modifié)

