# 📧 Aperçus des Templates Email - CongoTravel

**Date** : 30 janvier 2025  
**Style** : Professionnel AWS

---

## 🎯 Objectif

Ces fichiers HTML permettent de prévisualiser les templates d'email avant leur envoi réel. Ils utilisent le même style que les emails AWS pour un rendu professionnel et moderne.

---

## 📁 Fichiers Disponibles

### 1. `preview-index.html`
**Page d'accueil** avec navigation vers tous les templates.

### 2. `preview-welcome.html`
**Email de bienvenue** envoyé lors de la création d'un compte.
- Identifiants de connexion
- Informations sur l'école
- Instructions de première connexion

### 3. `preview-reset-password.html`
**Email de réinitialisation de mot de passe**.
- Code de vérification bien visible
- Instructions claires
- Message d'expiration

---

## 🚀 Comment Utiliser

### Option 1 : Ouvrir directement dans le navigateur

```bash
# Ouvrir la page d'accueil
open EmailTemplates/preview-index.html

# Ou ouvrir directement un template
open EmailTemplates/preview-welcome.html
open EmailTemplates/preview-reset-password.html
```

### Option 2 : Double-cliquer sur les fichiers

1. Naviguez vers le dossier `EmailTemplates/`
2. Double-cliquez sur `preview-index.html`
3. Cliquez sur les cartes pour voir les différents templates

### Option 3 : Via un serveur local (recommandé)

```bash
# Python 3
cd EmailTemplates
python3 -m http.server 8000

# Puis ouvrir dans le navigateur
# http://localhost:8000/preview-index.html
```

---

## 🎨 Caractéristiques du Design

### Style AWS Professionnel

| Élément | Détails |
|---------|---------|
| **Header** | Gris foncé (#232f3e) avec logo "CongoTravel" |
| **Couleur d'accent** | Orange (#ff9900) pour les boutons |
| **Typographie** | Police système moderne (-apple-system, Segoe UI) |
| **Code de vérification** | 48px, gras, monospace, centré |
| **Layout** | 600px max-width, responsive |
| **Footer** | Gris clair (#f5f5f5) avec texte discret |

### Avantages

✅ **Professionnel** : Design épuré et moderne  
✅ **Lisible** : Code de vérification très visible  
✅ **Responsive** : S'adapte aux mobiles et desktop  
✅ **Cohérent** : Style uniforme sur tous les emails  
✅ **Accessible** : Bon contraste et lisibilité

---

## 📝 Notes

- Ces fichiers sont des **aperçus statiques** avec des données d'exemple
- Les emails réels sont générés dynamiquement dans `EmailService.cs`
- Les couleurs et styles correspondent exactement aux templates utilisés en production

---

## 🔄 Mise à Jour

Si vous modifiez les templates dans `Services/EmailService.cs`, vous pouvez mettre à jour ces fichiers de prévisualisation pour refléter les changements.

---

## 📞 Support

Pour toute question sur les templates d'email, consultez :
- `Services/EmailService.cs` : Code source des templates
- `README.md` : Documentation générale

---

**© 2025 CongoTravel. Tous droits réservés.**

