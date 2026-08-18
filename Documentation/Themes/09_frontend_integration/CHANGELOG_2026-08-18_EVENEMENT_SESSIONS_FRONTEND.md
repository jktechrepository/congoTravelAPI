# Changelog Frontend — Evenement Sessions (2026-08-18)

> Public cible : équipes **Web Vue.js** et **Mobile Flutter**
>
> API concernée : `GET /api/events/sessions`, `GET /api/events/sessions/{id}`, `POST /api/events/sessions`

Ce document résume les changements apportés ce matin sur la partie **événement** et explique comment intégrer ces nouveautés côté front.

---

## 1. Résumé des changements

Les sessions événement exposent maintenant de nouveaux champs :

- `Description`
- `TypeEvenement`
- `NomOrganisateur`
- `TelephoneOrganisateur`
- `MailOrganisateur`
- `Ville`
- `Commune`
- `Quartier`
- `Avenue`
- `Numero`

Ces champs sont portés directement par la **session événement**. Ils peuvent donc varier d’un événement à l’autre, même pour une même société.

---

## 2. Impact API

### 2.1 Création de session

Endpoint :

```http
POST /api/events/sessions
```

Nouveaux champs acceptés dans le body :

```json
{
  "description": "Une grande soiree culturelle et musicale",
  "typeEvenement": "Music",
  "nomOrganisateur": "Kansa Events",
  "telephoneOrganisateur": "+243900000001",
  "mailOrganisateur": "events@kansa.cd",
  "ville": "Kinshasa",
  "commune": "Lingwala",
  "quartier": "Socimat",
  "avenue": "Avenue Tombalbaye",
  "numero": "12A"
}
```

Rappels utiles :

- `description` est **optionnelle**
- `typeEvenement` est **optionnel**
- si `typeEvenement` est absent, la valeur par défaut est `Autres`
- les champs organisateur et adresse sont **optionnels**
- les valeurs texte sont normalisées côté API (`trim`)

### 2.2 Liste publique / catalogue

Endpoint :

```http
GET /api/events/sessions
```

Comportement actuel :

- côté **public / client**, le endpoint retourne par défaut uniquement les sessions `Published`
- le filtre `typeEvenement` est **optionnel**
- si `typeEvenement` n’est pas fourni, la liste retourne **tous les types**

Exemple :

```http
GET /api/events/sessions?typeEvenement=Music
```

### 2.3 Détail session

Endpoint :

```http
GET /api/events/sessions/{id}
```

Le détail retourne aussi `description`, ainsi que les nouveaux champs organisateur et adresse.

---

## 3. Valeurs acceptées pour `typeEvenement`

Valeurs possibles :

```json
[
  "Sport",
  "Music",
  "Art",
  "Cinema",
  "Formation",
  "Conference",
  "Spectacle",
  "Festival",
  "Autres"
]
```

Recommandation frontend :

- utiliser une liste fermée (`select`, `dropdown`, `enum`)
- éviter un champ texte libre

---

## 4. Contrat frontend conseillé

### 4.1 Modèle Web Vue.js

Exemple de modèle formulaire :

```ts
export interface EventSessionForm {
  codeSession: string
  libelle: string
  description?: string | null
  idSite: number
  startAtUtc: string
  endAtUtc?: string | null
  inventoryMode: 'GlobalQuota' | 'ClassQuota' | 'SeatNumbered'
  typeEvenement?: 'Sport' | 'Music' | 'Art' | 'Cinema' | 'Formation' | 'Conference' | 'Spectacle' | 'Festival' | 'Autres'
  nomOrganisateur?: string
  telephoneOrganisateur?: string
  mailOrganisateur?: string
  ville?: string
  commune?: string
  quartier?: string
  avenue?: string
  numero?: string
}
```

### 4.2 Modèle Flutter

Exemple de modèle Dart :

```dart
class EventSessionPayload {
  final String codeSession;
  final String libelle;
  final String? description;
  final int idSite;
  final String startAtUtc;
  final String? endAtUtc;
  final String inventoryMode;
  final String? typeEvenement;
  final String? nomOrganisateur;
  final String? telephoneOrganisateur;
  final String? mailOrganisateur;
  final String? ville;
  final String? commune;
  final String? quartier;
  final String? avenue;
  final String? numero;

  EventSessionPayload({
    required this.codeSession,
    required this.libelle,
    this.description,
    required this.idSite,
    required this.startAtUtc,
    this.endAtUtc,
    required this.inventoryMode,
    this.typeEvenement,
    this.nomOrganisateur,
    this.telephoneOrganisateur,
    this.mailOrganisateur,
    this.ville,
    this.commune,
    this.quartier,
    this.avenue,
    this.numero,
  });
}
```

---

## 5. Recommandations UI — Vue.js

### 5.1 Formulaire création / édition brouillon

Ajouter 4 blocs :

1. **Description**
   - champ `textarea`
   - contenu libre optionnel
   - utile pour la fiche détail et certains écrans catalogue riches

2. **Type d’événement**
   - champ `select`
   - valeur par défaut UI possible : `Autres`

3. **Organisateur**
   - `nomOrganisateur`
   - `telephoneOrganisateur`
   - `mailOrganisateur`

4. **Adresse du lieu**
   - `ville`
   - `commune`
   - `quartier`
   - `avenue`
   - `numero`

### 5.2 Catalogue / cartes sessions

Champs utiles à afficher :

- `description` si tu veux un extrait
- `typeEvenement`
- `nomOrganisateur`
- `ville`
- `commune`

Exemples d’usage :

- badge `Music`, `Sport`, `Conference`
- sous-titre `Kinshasa • Lingwala`
- bloc info `Organisé par Kansa Events`

### 5.3 Validation front

Validation recommandée :

- `description` : textarea optionnelle, longueur libre côté front avec limite UI raisonnable
- `mailOrganisateur` : email valide si renseigné
- `telephoneOrganisateur` : texte simple, sans bloquer trop fort côté front
- `ville`, `commune`, `quartier` : champs texte optionnels
- `avenue`, `numero` : champs texte optionnels

Important :

- laisser l’API rester source de vérité
- ne pas forcer ces champs si le métier ne les rend pas obligatoires

---

## 6. Recommandations UI — Flutter

### 6.1 Catalogue mobile

Ajouter dans la carte événement :

- extrait court de `description` si disponible
- badge `typeEvenement`
- `ville`
- `nomOrganisateur` si disponible

Exemple affichage :

- `Festival`
- `Kinshasa, Gombe`
- `Par Congo Culture Events`

### 6.2 Détail événement

Prévoir une section :

- **Description** : `description`
- **Type** : `typeEvenement`
- **Organisateur** : `nomOrganisateur`
- **Contact** : `telephoneOrganisateur`, `mailOrganisateur`
- **Lieu** : `ville`, `commune`, `quartier`, `avenue`, `numero`

### 6.3 Parsing JSON

Les nouveaux champs étant optionnels, le parsing doit accepter :

- `null`
- chaîne absente
- chaîne vide

Exemple :

```dart
typeEvenement: json['typeEvenement'] as String?,
description: json['description'] as String?,
nomOrganisateur: json['nomOrganisateur'] as String?,
telephoneOrganisateur: json['telephoneOrganisateur'] as String?,
mailOrganisateur: json['mailOrganisateur'] as String?,
ville: json['ville'] as String?,
commune: json['commune'] as String?,
quartier: json['quartier'] as String?,
avenue: json['avenue'] as String?,
numero: json['numero'] as String?,
```

---

## 7. Exemple complet de body

```json
{
  "codeSession": "FEST-KIN-2026-001",
  "libelle": "Festival de musique urbaine de Kinshasa",
  "description": "Une grande soiree de musique live avec plusieurs artistes invites, animations culturelles et espace VIP.",
  "idSite": 1,
  "startAtUtc": "2026-08-22T18:00:00Z",
  "endAtUtc": "2026-08-22T23:30:00Z",
  "inventoryMode": "GlobalQuota",
  "typeEvenement": "Music",
  "nomOrganisateur": "Kansa Events",
  "telephoneOrganisateur": "+243900000001",
  "mailOrganisateur": "events@kansa.cd",
  "ville": "Kinshasa",
  "commune": "Lingwala",
  "quartier": "Socimat",
  "avenue": "Avenue de la Gombe",
  "numero": "12A",
  "globalQuota": {
    "capaciteTotale": 500,
    "prixUnitaire": 25,
    "codeDevise": "USD"
  },
  "photos": []
}
```

---

## 8. Checklist d’intégration

### Vue.js

- mettre à jour le type TS de session
- mettre à jour le formulaire de création
- ajouter la `description`
- ajouter le `select` `typeEvenement`
- ajouter les champs organisateur
- ajouter les champs adresse
- afficher `typeEvenement` dans les listes si utile
- afficher `ville` / `commune` / `nomOrganisateur` sur la fiche détail

### Flutter

- mettre à jour le modèle Dart
- mettre à jour le parsing JSON
- ajouter `description`
- ajouter les nouveaux champs dans l’écran détail
- ajouter le badge `typeEvenement`
- afficher l’adresse si disponible

---

## 9. Compatibilité

Ces champs sont **additifs**.

Conséquences :

- les anciens écrans continuent de fonctionner
- les nouveaux champs peuvent être intégrés progressivement
- si un front ne les envoie pas encore, l’API continue d’accepter la création

---

## 10. Checklist QA frontend

### Vue.js

- vérifier que le formulaire accepte `description` sans la rendre obligatoire
- vérifier que `typeEvenement` s’affiche bien dans le `select` avec toutes les valeurs attendues
- vérifier qu’une création sans `typeEvenement` fonctionne encore
- vérifier qu’une création sans `description` fonctionne encore
- vérifier que les champs `nomOrganisateur`, `telephoneOrganisateur`, `mailOrganisateur` restent facultatifs
- vérifier que `ville`, `commune`, `quartier`, `avenue`, `numero` restent facultatifs
- vérifier que les espaces saisis au début/à la fin n’impactent pas l’affichage après retour API
- vérifier que la fiche détail affiche bien `description` quand elle existe
- vérifier que la liste/carte session n’explose pas visuellement si `description` est longue

### Flutter

- vérifier que le parsing JSON ne casse pas si `description` vaut `null`
- vérifier que le parsing JSON ne casse pas si `typeEvenement` est absent
- vérifier que les nouveaux champs s’affichent correctement sur la fiche événement
- vérifier qu’un texte long dans `description` se replie correctement sur mobile
- vérifier que l’adresse reste lisible même si certains champs sont absents
- vérifier que l’UI ne montre pas de labels vides quand un champ optionnel n’est pas renseigné
- vérifier que le badge `typeEvenement` reste propre sur petits écrans

### Tests métier suggérés

- créer une session avec tous les champs remplis
- créer une session avec seulement les champs obligatoires
- lister les sessions sans filtre `typeEvenement`
- lister les sessions avec filtre `typeEvenement=Music`
- ouvrir le détail d’une session avec `description`
- ouvrir le détail d’une session sans `description`

---

## 11. Points bloquants pour la mise en prod frontend

- le front doit tolérer l’absence de `description`, `typeEvenement`, organisateur et adresse sur les anciennes sessions
- le formulaire de création ne doit pas rendre ces nouveaux champs obligatoires
- le parsing mobile et web doit accepter des champs absents ou `null`
- le catalogue public doit rester compatible avec le filtre `typeEvenement` optionnel
- les écrans liste ne doivent pas casser visuellement si `description` est longue

---

## 12. Points d’attention

- `typeEvenement` n’est pas un texte libre
- le catalogue public reste limité à `Published`
- l’absence de `typeEvenement` ne doit pas filtrer la liste
- les champs organisateur et adresse sont au niveau **session**, pas au niveau société
- `description` est optionnelle et peut être absente des anciens enregistrements

---

## 13. Références utiles

- [MODULE_05_EVENEMENT_BILLETTERIE.md](MODULE_05_EVENEMENT_BILLETTERIE.md)
- [DOCUMENTATION_API_SESSIONS_EVENEMENT_V1.md](../05_transport_sync/DOCUMENTATION_API_SESSIONS_EVENEMENT_V1.md)
