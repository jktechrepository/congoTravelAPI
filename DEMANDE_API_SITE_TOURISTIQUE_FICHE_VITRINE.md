# Demande API — Enrichissement fiche Site Touristique (vitrine)

**Objet :** Aligner `SiteTouristiqueLieu` sur le catalogue / fiche aperçu front Congo Travel Web  
**Date :** 15 août 2026  
**Émetteur :** Équipe Front — Congo Travel Web  
**Destinataire :** Équipe Backend Congo Travel  
**Priorité :** Haute (parcours public vitrine déjà en place côté front)

---

Bonjour,

Nous avons refondu le parcours public **Sites touristiques** côté front :

1. **Catalogue** : 1 carte par **lieu** (plus 1 carte par journée)  
2. **Fiche aperçu** : `/sites-touristiques/lieu/{idSiteTouristique}`  
3. **Réservation** : choix d’une journée → `/sites-touristiques/{idSiteTouristiqueJournee}` (inchangé)

Le front est **déjà prêt** à consommer les champs ci‑dessous. Tant qu’ils ne sont pas exposés par l’API, la fiche reste partielle (nom + description courte + dates uniquement).

Merci d’étendre le DTO **SiteTouristiqueLieu** (création, mise à jour, détail, listes Published) pour aligner l’API sur ce parcours.

---

## Endpoints concernés

| Méthode | Endpoint | Besoin |
|---------|----------|--------|
| `GET` | `/api/sites-touristiques/lieux` | Liste **Published** : inclure les champs vitrine (au moins cover + ville + catégorie + description) |
| `GET` | `/api/sites-touristiques/lieux/{id}` | Détail fiche aperçu : **tous** les champs ci‑dessous |
| `POST` | `/api/sites-touristiques/lieux` | Accepter les nouveaux champs à la création |
| `PUT` | `/api/sites-touristiques/lieux/{id}` | Accepter les nouveaux champs à la mise à jour |
| `PUT` | `/api/sites-touristiques/lieux/{id}/publish` | Inchangé — le lieu publié doit exposer la fiche enrichie |

Accès catalogue / détail public : conserver l’accès **anonyme ou Client** déjà prévu pour les lieux `Published` (comme documenté Partie 3).

---

## Champs déjà utilisés (à conserver)

| Champ JSON | Type | Obligatoire | Usage front |
|------------|------|-------------|-------------|
| `idSiteTouristique` | number | oui | Clé de regroupement + route fiche |
| `nom` | string | oui | Titre carte + hero |
| `description` | string | recommandé | Extrait carte + section « À propos » (idéalement 400–800 car.) |
| `codeLieu` | string | oui (admin) | Référentiel |
| `idSite` | number | oui | Guichet marchand (paiement) |
| `status` | string | oui | `Draft` / `Published`… |
| `idSociete` | number | oui | Multi-tenant |
| `nomSociete` | string | recommandé | « Proposé par … » |

---

## Nouveaux champs demandés (fiche vitrine)

### P0 — indispensables pour un aperçu professionnel

| Champ JSON | Type | Exemple | Usage front |
|------------|------|---------|-------------|
| `coverImageUrl` | string (URL absolue HTTPS) | `"https://cdn…/cover.jpg"` | Photo hero catalogue + fiche |
| `galleryUrls` | `string[]` (URLs) | `["https://…/1.jpg", "https://…/2.jpg"]` | Galerie (3–8 photos recommandées) |
| `description` | string | texte long | Si déjà présent : autoriser une longueur suffisante (ex. max 2000–4000) |

### P1 — localisation & classification

| Champ JSON | Type | Exemple | Usage front |
|------------|------|---------|-------------|
| `categorie` | string | `"Parc"`, `"Musée"`, `"Monument"` | Badge sur carte / hero |
| `ville` | string | `"Brazzaville"` | Sous-titre localisation |
| `province` | string | `"Pool"` | Localisation |
| `adresse` | string | `"Av. de la Paix, …"` | Bloc infos pratiques |
| `latitude` | number \| null | `-4.2634` | Lien Google Maps |
| `longitude` | number \| null | `15.2429` | Lien Google Maps |
| `highlights` | `string[]` | `["Guide inclus", "Parking"]` | Liste de points forts (3–6) |

### P2 — infos pratiques (nice to have)

| Champ JSON | Type | Exemple | Usage front |
|------------|------|---------|-------------|
| `dureeVisiteMinutes` | number \| null | `120` | Affiché « 2 h » |
| `horairesOuverture` | string | `"Mar–Dim 09:00–17:00"` | Infos pratiques |
| `ageMinimum` | number \| null | `5` | « 5 ans » |
| `telephone` | string | `"+24206…"` | Lien `tel:` |
| `whatsapp` | string | `"+24206…"` | Contact (optionnel UI) |

> **Nommage :** camelCase préféré (`coverImageUrl`). Le front accepte aussi PascalCase (`CoverImageUrl`) en secours, mais un contrat unique camelCase serait idéal.

---

## Exemple de réponse attendue — `GET /api/sites-touristiques/lieux/{id}`

```json
{
  "idSiteTouristique": 12,
  "idSociete": 3,
  "nomSociete": "Congo Loisirs SA",
  "codeLieu": "ST-BZV-001",
  "nom": "Modern Games",
  "description": "Espace de loisirs et attractions pour toute la famille…",
  "idSite": 45,
  "nomSite": "Guichet Modern Games",
  "status": "Published",
  "coverImageUrl": "https://cdn.example.com/sites/12/cover.jpg",
  "galleryUrls": [
    "https://cdn.example.com/sites/12/1.jpg",
    "https://cdn.example.com/sites/12/2.jpg",
    "https://cdn.example.com/sites/12/3.jpg"
  ],
  "categorie": "Parc d'attractions",
  "ville": "Brazzaville",
  "province": "Brazzaville",
  "adresse": "Avenue de la Paix",
  "latitude": -4.2634,
  "longitude": 15.2429,
  "highlights": [
    "Idéal familles",
    "Parking sur place",
    "Restauration disponible"
  ],
  "dureeVisiteMinutes": 180,
  "horairesOuverture": "Tous les jours 09:00–18:00",
  "ageMinimum": null,
  "telephone": "+242061234567",
  "whatsapp": "+242061234567"
}
```

Même structure (ou sous-ensemble P0+P1) souhaitée dans la **liste** `GET /api/sites-touristiques/lieux?status=Published` pour alimenter les cartes du catalogue sans N+1 appels détail.

---

## Création / mise à jour — payload

Étendre `POST` / `PUT` lieux pour accepter les nouveaux champs (hors `idSiteTouristique`, `status` géré via publish).

Questions ouvertes pour vous (merci de trancher) :

1. **Stockage images** : URLs externes fournies par le partenaire, ou upload vers votre CDN (`multipart`) + retour d’URL ?  
2. **Validation** : formats image acceptés, taille max, nombre max de `galleryUrls` ?  
3. **Rétrocompatibilité** : champs absents = `null` / `[]` / `""` (le front gère déjà le vide).

---

## Lien avec les journées (inchangé, à confirmer)

Le front continue d’utiliser :

- `GET /api/sites-touristiques/journees?status=Published&idSiteTouristique={id}`  
  pour la liste des dates sur la fiche lieu.

Merci de confirmer que le filtre `idSiteTouristique` est bien supporté en public / anonyme pour les journées `Published`.

Champs journées déjà consommés (pas de changement demandé) :  
`idSiteTouristiqueJournee`, `idSiteTouristique`, `nomLieu`, `dateVisite`, `inventoryMode`, `prixMin` / `prixMax`, `codeDevise`, `globalQuota`, `classQuotas`.

---

## Livrables attendus

1. Schéma DTO / OpenAPI à jour pour `SiteTouristiqueLieu`  
2. Migration BDD si nécessaire  
3. Support CRUD + lecture liste/détail Published  
4. Environnement **dev** déployé avec au moins **1 lieu Published** enrichi (photos + description + ville) pour validation front  
5. Note courte : breaking change ou non (idéalement **non breaking** : champs optionnels ajoutés)

---

## Critères d’acceptation (front)

- [ ] Carte catalogue affiche `coverImageUrl` (sinon gradient de secours)  
- [ ] Fiche `/sites-touristiques/lieu/{id}` affiche hero, galerie, à propos, infos pratiques, dates  
- [ ] Liste Published renvoie assez d’infos pour éviter un GET détail par carte  
- [ ] Champs manquants n’entraînent pas d’erreur 500 (valeurs vides tolérées)  
- [ ] CORS + accès anonyme inchangés pour lecture Published  

---

## Contact

Nous restons disponibles pour un point rapide (15–20 min) sur le contrat JSON et le stockage des médias.

Merci d’avance,  
Équipe Front Congo Travel Web
