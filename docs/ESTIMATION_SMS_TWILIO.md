# Estimation des SMS avec Solde Twilio

## 📊 Informations de Base

- **Solde actuel Twilio** : $17.9881 USD
- **Prix par SMS** : $0.0467 USD (configuré dans `appsettings.json`)
- **Date d'analyse** : Décembre 2025

---

## 📱 Formats SMS du Projet

### 1. SMS de Facture
**Format** :
```
{nomSociete}: Votre facture de (12/2025) est de 15,000.00 FC. N FACT-2025-001.
```

**Caractéristiques** :
- Longueur : **75 caractères**
- Nombre de SMS : **1 SMS** (bien sous la limite de 160)
- Coût : **$0.0467** par SMS
- **Optimisation** : Utilise "N" au lieu de "N°" pour éviter l'encodage Unicode

**Exemple** :
```
K-Energie: Votre facture de (12/2025) est de 15,000.00 FC. N FACT-2025-001.
```

---

### 2. SMS de Bienvenue
**Format** :
```
{nomSociete}: Bienvenue ! Votre compte a été créé. Connectez-vous sur {url}. Vos identifiants ont été envoyés sur votre mail.
```

**Caractéristiques** :
- Longueur : **154 caractères** (format par défaut)
- Nombre de SMS : **1 SMS** (sous la limite de 160)
- Coût : **$0.0467** par SMS
- Système adaptatif : Si nom de société trop long, version courte automatique

**Exemple** :
```
K-Energie: Bienvenue ! Votre compte a été créé. Connectez-vous sur https://k-energie.kansaconsulting.com. Vos identifiants ont été envoyés sur votre mail.
```

---

## 💰 Calcul d'Estimation

### Calcul Basique
```
Solde : $17.9881
Prix par SMS : $0.0467
Nombre de SMS = $17.9881 / $0.0467 = 385 SMS
Reste : $0.0086
```

**Résultat** : **385 SMS** peuvent être envoyés avec le solde actuel.

---

## 📈 Scénarios d'Utilisation

### Scénario 1 : 100% SMS de Facture
- **Nombre de SMS** : 385
- **Coût total** : $17.9795
- **Reste** : $0.0086
- **Utilisation** : Idéal pour la diffusion de factures

### Scénario 2 : 100% SMS de Bienvenue
- **Nombre de SMS** : 385
- **Coût total** : $17.9795
- **Reste** : $0.0086
- **Utilisation** : Idéal pour l'onboarding de nouveaux clients

### Scénario 3 : Mix 50% Facture / 50% Bienvenue
- **SMS facture** : 192
- **SMS bienvenue** : 192
- **Total SMS** : 384
- **Coût total** : $17.9328
- **Reste** : $0.0553
- **Utilisation** : Équilibre entre factures et nouveaux clients

---

## 🎯 Estimation Recommandée (avec Marge de Sécurité)

Pour éviter d'épuiser complètement le solde, il est recommandé d'utiliser **90% du solde** :

- **Solde utilisable** : $16.1893 (90% de $17.9881)
- **Nombre de SMS recommandé** : **346 SMS**
- **Reste de sécurité** : $1.8299

### Répartition Recommandée

Avec **346 SMS** disponibles, une répartition typique serait :

- **SMS de facture** : **276 SMS** (80%)
  - Permet de diffuser des factures à ~276 clients
  - Coût : $12.8892

- **SMS de bienvenue** : **69 SMS** (20%)
  - Permet d'accueillir ~69 nouveaux clients
  - Coût : $3.2223

- **Total** : 345 SMS
- **Coût total** : $16.1115
- **Reste** : $1.8766

---

## 📊 Tableau Récapitulatif

| Type de SMS | Longueur | Coût/SMS | Nombre Max | Coût Total |
|-------------|----------|----------|------------|------------|
| **Facture** | 76 chars | $0.0467 | 385 | $17.9795 |
| **Bienvenue** | 154 chars | $0.0467 | 385 | $17.9795 |
| **Mix (80/20)** | - | $0.0467 | 346 | $16.1115 |

---

## ⚠️ Points Importants

### 1. Tous les SMS sont en 1 partie
- ✅ SMS de facture : 76 chars → **1 SMS**
- ✅ SMS de bienvenue : 154 chars → **1 SMS**
- ✅ Aucun SMS ne dépasse 160 caractères
- ✅ Pas de coût supplémentaire pour SMS multipartes

### 2. Système Adaptatif
Le SMS de bienvenue a un système adaptatif qui s'active si le nom de la société est trop long :
- Format par défaut : 154 chars
- Format adaptatif 1 : ~135 chars (si nom long)
- Format adaptatif 2 : ~101 chars (ultra-court)

**Impact** : Aucun impact sur le coût, tous restent en 1 SMS.

### 3. Coût Unitaire
- **Prix fixe** : $0.0467 par SMS
- **Indépendant de la longueur** (tant que ≤ 160 chars)
- **Pas de frais supplémentaires** pour SMS multipartes dans votre cas

---

## 💡 Recommandations

### Pour Maximiser l'Utilisation
1. **Utiliser 90% du solde** : Garder une marge de sécurité
2. **Prioriser les factures** : 80% factures, 20% bienvenue
3. **Surveiller le solde** : Configurer des alertes Twilio à $5 et $2
4. **Recharger à l'avance** : Éviter les interruptions de service

### Pour Optimiser les Coûts
1. **Format optimisé** : Vos SMS sont déjà optimisés (76 et 154 chars)
2. **Pas de SMS multipartes** : Économie garantie
3. **Système adaptatif** : Gère automatiquement les cas limites

---

## 📅 Projection Mensuelle

### Estimation pour 1 Mois
Avec un solde de **$17.9881** :

- **Si 100% factures** : ~385 factures/mois
- **Si 100% bienvenue** : ~385 nouveaux clients/mois
- **Si mix 80/20** : ~276 factures + 69 nouveaux clients/mois

### Coût Mensuel Estimé
Pour maintenir un service continu, prévoir :
- **Budget mensuel recommandé** : $20-25 USD
- **Permet** : ~430-535 SMS/mois
- **Marge de sécurité** : ~15-20%

---

## 🔄 Rechargement Recommandé

Pour un service continu sans interruption :

- **Seuil d'alerte** : $5.00 (recharger à ce niveau)
- **Montant de recharge** : $20-25 USD
- **Fréquence** : Mensuelle ou selon l'usage

---

## ✅ Conclusion

Avec votre solde actuel de **$17.9881** :

- ✅ **385 SMS** peuvent être envoyés
- ✅ Tous vos formats SMS sont optimisés (≤ 160 chars)
- ✅ **Recommandation** : Utiliser **346 SMS** (90% du solde)
- ✅ **Répartition** : 276 factures + 69 bienvenue

Vos formats SMS sont **parfaitement optimisés** pour minimiser les coûts tout en restant informatifs et professionnels.

