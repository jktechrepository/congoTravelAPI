# Spécification — Réservation aller-retour Transport V1

Extension additive du workflow V2 ([`SPEC_TECHNIQUE_WORKFLOW_RESERVATION_VOYAGE_V2.md`](SPEC_TECHNIQUE_WORKFLOW_RESERVATION_VOYAGE_V2.md)).

---

## Objectif

Permettre la réservation **A→B + B→A** sur **2 voyages planifiés distincts**, avec **1 paiement unique** et **sièges indépendants** par leg.

Les endpoints single-leg existants restent **inchangés**.

---

## Règles métier V1 (cadrage)

| Sujet | Décision V1 |
|-------|-------------|
| Paiement | Unique, atomique (cash ou FlexPay) |
| Sièges | Allocation indépendante par voyage (pas de miroir obligatoire) |
| Compatibilité géographique | `VilleArrivee_aller` = `VilleDepart_retour` et `VilleDepart_aller` = `VilleArrivee_retour` (insensible à la casse) |
| Dates | Départ retour ≥ départ aller (instant UTC combiné date+heure) ; même jour autorisé |
| Société | Les 2 voyages doivent appartenir à la **même société** |
| Tarif phase 1 | `montantAttendu = tarif_aller + tarif_retour` (catégories par passager) |
| Supplément électronique FlexPay | `supplément × nombreDePlace × 2` (2 legs) |
| Annulation agrégat | **Atomique** : les 2 legs ensemble (pas d'annulation partielle du retour) |
| Passagers | **Strictement identiques** sur aller et retour (même liste, même N places) |
| Sync offline guichet | **Hors scope V1** — online only |
| Reversement auto | Montant **total** AR sur `IdSite` / réservation **aller** (rétrocompat) |
| Reporting CA | Un seul `Paiement` (`IdReservation` = aller). Le retour n'a pas de paiement dédié → pas de double-compte. Grouper un dossier via `IdReservationAllerRetour`. |

---

## Modèle de données

### `ReservationAllerRetour` (agrégat)

- `IdReservationAllerRetour`, `IdVoyageAller`, `IdVoyageRetour`
- `IdReservationAller`, `IdReservationRetour` (après confirmation)
- `IdPaiement`, `IdCommandeReservationEnAttente` (FlexPay)
- `Statut` : `EN_ATTENTE_PAIEMENT`, `CONFIRMEE`, `ANNULEE`
- `IdSociete`, `IdClient`, `IdUtilisateur`, `IdSite`

### `Reservation` (évolution)

- `IdReservationAllerRetour` (FK nullable)
- `AllerRetourLeg` : `Aller` | `Retour` | null (single-leg)

### `Paiement` (évolution)

- `IdReservationAllerRetour` (FK nullable) — `IdReservation` reste la réservation **aller** pour rétrocompat

### `CommandeReservationEnAttente` (évolution)

- `TypeCommande` : `Single` (défaut) | `AllerRetour`

---

## API (additive)

| Méthode | Route |
|---------|-------|
| POST | `/api/Reservation/reservation_aller_retour_with_paiement` |
| POST | `/api/Reservation/reservation_aller_retour_with_paiement_electronique` |
| GET | `/api/Reservation/aller-retour/{id}` |
| POST | `/api/Reservation/aller-retour/{id}/cancel` |

---

## Flux cash

1. Valider compatibilité des 2 voyages + passagers + capacité sur **chaque** leg
2. Créer agrégat + 2 réservations (EN_ATTENTE)
3. Passagers dupliqués par leg ; allocation sièges indépendante
4. 1 paiement (`IdReservation` = aller, `IdReservationAllerRetour` = agrégat)
5. Si complet → CONFIRMEE + N billets aller + N billets retour

---

## Flux FlexPay

1. Holds sièges sur **les 2 voyages** (même `IdCommandeReservationEnAttente`)
2. Commande `TypeCommande = AllerRetour`, payload JSON dédié
3. Callback : finalise agrégat + 2 réservations + allocations + billets + reversement

---

## Reporting / reversement

- `Paiement.IdReservation` = réservation **aller** (montant = aller + retour [+ supplément FlexPay]).
- `Paiement.IdReservationAllerRetour` = agrégat (clé de regroupement dossier).
- Les métriques existantes qui joignent `Paiement.IdReservation` → `Reservation` n'attribuent le CA qu'à l'aller ; elles ne double-comptent pas.
- Reversement auto FlexPay inchangé : déclenché sur le paiement + réservation aller.

## Références code

- `Services/AllerRetourReservationService.cs`
- `Helpers/Transport/AllerRetourVoyageCompatibilityHelper.cs`
- `Services/FlexPayCallbackService.cs` — branche `TypeCommande = AllerRetour`
