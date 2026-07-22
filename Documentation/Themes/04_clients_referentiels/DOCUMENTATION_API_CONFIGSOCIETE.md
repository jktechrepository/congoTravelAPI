# API ConfigSociete — règles de gestion centralisées

## Vue d'ensemble

Les règles métier suivantes sont centralisées dans **`ConfigSocietes`** (relation 1:1 avec `Societe`) :

| Champ | Défaut | Description |
|-------|--------|-------------|
| `dureeValiditeBilletJours` | 0 | Validité billet à partir du jour de départ (0 = jour du départ) |
| `penaliteReaffectationPourcentage` | 0 | Pénalité de réaffectation en **%** (0–100) du montant payé pour le billet, si départ manqué |
| `joursAvanceMaxReservation` | 60 | Horizon max de réservation (jours à partir d'aujourd'hui UTC) ; `null` = illimité |
| `heuresLimiteReaffectation` | 2 | Heures avant départ source pour autoriser réaffectation (0–72) |
| `heuresOuvertureEmbarquementAvantDepart` | 3 | Ouverture embarquement (heures avant minuit du jour de départ) |
| `heuresFermetureEmbarquementApresJourDepart` | 24 | Fermeture embarquement (heures après minuit du jour de départ) |
| `dureeHoldFlexPayMinutes` | 15 | Durée hold sièges FlexPay par société |
| `reaffectationActive` | true | Kill-switch réaffectation |

## Breaking changes

### API Voyage

Les champs suivants ont été **retirés** de `POST/PUT /api/Voyage` et des DTOs voyage :

- `penaliteReaffectation`
- `dureeValiditeBilletJours`
- `heuresLimiteReaffectation`

### API ConfigSociete (v2)

- `penaliteReaffectation` (montant fixe) → **`penaliteReaffectationPourcentage`** (0–100)
- Les anciennes valeurs en base (montants fixes) sont remises à **0 %** lors de la migration ; reconfigurer manuellement par société.
- `joursAvanceMaxReservation` : défaut **60** à la création bootstrap ; `null` reste possible via PUT pour illimité.

Utiliser **`GET/PUT /api/Societe/{id}/config`** pour toutes ces règles.

## Endpoints

| Méthode | Route | Permission |
|---------|-------|------------|
| GET | `/api/Societe/{id}/config` | `ConfigSociete.Read` |
| PUT | `/api/Societe/{id}/config` | `ConfigSociete.Update` |

### Exemple PUT

```json
{
  "dureeValiditeBilletJours": 7,
  "penaliteReaffectationPourcentage": 10,
  "joursAvanceMaxReservation": 60,
  "heuresLimiteReaffectation": 2,
  "heuresOuvertureEmbarquementAvantDepart": 3,
  "heuresFermetureEmbarquementApresJourDepart": 24,
  "dureeHoldFlexPayMinutes": 15,
  "reaffectationActive": true
}
```

Pour horizon illimité : `"joursAvanceMaxReservation": null`

## Pénalité réaffectation (%)

- Base : **montant réellement payé** pour le billet (prorata si réservation multi-passagers avec remise globale).
- Fallback sans paiement retrouvable : tarif catalogue du siège (`VoyageTarifService`).
- `Billet.PenaliteOverride` reste un **montant fixe** (exception opérateur).
- Réponse `POST .../reaffecter` expose aussi `penalitePourcentageApplique` et `montantPayeReference`.

## Consommateurs backend

- `BilletService` — validité, fenêtre embarquement, réaffectation
- `BilletEmissionService` — dates validité à l'émission
- `ReservationWithPaiementService`, `ReservationService`, `FlexPayReservationService` — horizon réservation + hold FlexPay

## Migration production

1. `Scripts/audit_configsociete_voyage_divergences.sql`
2. `Scripts/production_configsociete.sql`
3. Migration EF `20260530121511_ConfigSocietePenalitePourcentage` (rename colonne + reset %)

## Bootstrap société

`POST /api/Societe/create-with-bootstrap` crée automatiquement une ligne `ConfigSociete` avec les défauts (horizon 60 jours).
