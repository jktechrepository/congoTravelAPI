# Champs du rapport `Reports/Billet_A4.frx`

> **Guide d’intégration complet (front)** : [`Documentation/Themes/09_frontend_integration/MODULE_BILLET_AVION_A4.md`](../Documentation/Themes/09_frontend_integration/MODULE_BILLET_AVION_A4.md)

Template A4 **réservé aux véhicules de type aérien** (`TypeVehicule.Libelle` contenant « aérien », accents ignorés).

## Endpoints (`BilletController`)

| Méthode | Route | Description |
|---|---|---|
| `GET` | `/api/Billet/billet_d_avion_a4/{id}` | Prévisualisation HTML |
| `GET` | `/api/Billet/billet_d_avion_a4/{id}/pdf` | Téléchargement PDF |

Réponses : `404` billet introuvable · `409` véhicule non aérien · `200` fichier

---

## Mapping FRX ← modèle CongoTravel

| Champ FRX | Source |
|---|---|
| `NomClient` | `Client.NomClient` + libellé `, thank you for your booking` |
| `code_reservation` | `IdReservation` + libellé `Booking Reference :` |
| `site` | `Site.NomSite` + libellé `Issue Officer :` |
| `Text1` | Message d’intro avec `Societe.Nom` |
| `phone_number` | Tél. passager sinon client + libellé `Phone Number :` |
| `nom_passager` | `ReservationPassenger.NomComplet` |
| `email_passager` | `ReservationPassenger.Email` |
| `siege` | `CodeSiege` |
| `reference_billet` | `IdReservationPassenger` |
| `date_voyage` | `Voyage.DateDepart` (`dd/MM/yyyy`) |
| `avion` | `AliasVehicule` |
| `provenance` | `VilleDepart` |
| `heure_depart` | `HeureDepart` (`HH:mm`) |
| `destination` | `VilleArrivee` |
| `heure_arrive` | *(vide)* |
| `cabin` | *(vide)* |
| `classe_siege` | `CategorieSiege.Libelle` |
| `kilos_bagage` | `ConfigSociete.PoidsBagageParKiloOffert` |

### Images

| Champ FRX | Statut |
|---|---|
| `logo` | Embarqué dans le `.frx` ; cible future = `Societe.Logo` |
| `affiche_pub` | Plus tard |

---

## Notes

- Libellés du template **conservés** ; seules les valeurs API sont injectées.
- Ne remplace pas la billetterie terrestre / QR existante.
