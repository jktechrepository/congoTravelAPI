# MODULE 03 — Réservation et billet embarquement

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)

---

## Workflow réservation multi-passagers

```
1. Sélection voyage + sièges par passager
2. POST /api/Reservation/with-passengers-and-paiement  (ou alias reservation_with_paiement)
3. Réponse : reservation + paiement + billets (BilletResponseDto[])
```

Alias acceptés :
- `POST /api/Reservation/with-passengers-and-paiement`
- `POST /api/Reservation/reservation_with_paiement`

---

## Format `BilletResponseDto` (aligné partout)

Même contrat pour :
- `GET /api/Billet/reservation/{idReservation}`
- `POST /api/Reservation/with-passengers-and-paiement` (champs `billet` / `billets`)

```json
{
  "idBillet": 910,
  "isUsed": false,
  "idReservation": 120,
  "idReservationPassenger": 333,
  "idSiege": 55,
  "codeSiege": "VIP-01",
  "nomPassager": "Jean Dupont",
  "qrCode": "QRCODE-ABC",
  "nomClient": "Acheteur Parent",
  "telephoneClient": "+243111",
  "dateVoyage": "2026-05-10T00:00:00",
  "heureVoyage": "08:30:00",
  "prixVoyage": 15000,
  "villeDepart": "Kinshasa",
  "villeArrivee": "Matadi"
}
```

> **Attention sémantique** : sur les routes **check** et **qrcode** (embarquement), `nomClient` / `telephoneClient` = **passager réel**. Sur les autres routes (liste admin, GET par id), `nomClient` = **acheteur payeur**.

---

## Check billet (scan QR) — CRITIQUE embarquement

```
GET /api/Billet/{QrCode}/check?idVoyageCible={optional}
```

`QrCode` = valeur exacte du QR (pas l'id numérique).

### Response `BilletCheckResponseDto`

```json
{
  "idBillet": 502,
  "isUsed": false,
  "statut": "Valide",
  "message": "Billet valide pour embarquement.",
  "embarquementAutorise": true,
  "idReservation": 202,
  "statutReservation": "CONFIRMEE",
  "dateDepartVoyage": "2026-05-10T00:00:00",
  "heureDepartVoyage": "08:30:00",
  "nomClient": "Passager Réel",
  "telephoneClient": "+243999"
}
```

### Codes `statut`

| Code | HTTP | Action UI |
|------|------|-----------|
| `Valide` | 200 | Afficher passager, bouton embarquer |
| `ValideSansReservation` | 200 | Billet sans réservation liée |
| `DejaUtilise` | 409 | Billet déjà scanné |
| `EmbarquementDejaEnregistre` | 409 | Doublon embarquement |
| `HorsFenetreEmbarquement` | 400 | Hors fenêtre J-3h / J+24h |
| `BilletExpire` | 400 | Validité billet dépassée |
| `ReservationInactive` | 400 | Réservation désactivée |
| `NonReconnu` | 200 | QR inconnu (`idBillet: null`) |

### Flutter — scan et affichage

```dart
Future<void> onQrScanned(String qr) async {
  final resp = await api.get('/Billet/$qr/check');
  final data = resp.data;
  // nomClient = passager réel sur cette route
  showDialog(
    context: context,
    builder: (_) => AlertDialog(
      title: Text(data['nomClient'] ?? 'Inconnu'),
      content: Text('Tel: ${data['telephoneClient']}\n${data['message']}'),
      actions: data['embarquementAutorise'] == true
          ? [TextButton(onPressed: () => embarquer(...), child: Text('Embarquer'))]
          : null,
    ),
  );
}
```

---

## Embarquement

```
POST /api/Billet/societe/{idSociete}/passager/{idReservationPassenger}/billet/{idBillet}/embarquer?idVoyageCible={optional}
```

Corps vide. Réponse 200 : `EmbarquerBilletResponseDto` avec billet mappé.

---

## Consultation par QR (tableau)

```
GET /api/Billet/qrcode/{qrCode}
```

Retourne `BilletResponseDto[]`. Même sémantique identité que check (`nomClient` = passager).

---

## Réaffectation billet

```
POST /api/Billet/societe/{idSociete}/billet/{idBillet}/reaffecter
```

Body : `idVoyageCible`, `confirmerPaiementDifferentiel`, `methodePaiement`, etc.

---

## Références backend

- [`DOCUMENTATION_EMBARQUEMENT_BILLET_ET_PASSAGERS_VOYAGE.md`](../05_transport_sync/DOCUMENTATION_EMBARQUEMENT_BILLET_ET_PASSAGERS_VOYAGE.md)
- [`GUIDE_MIGRATION_IDENTITE_PASSAGER_CHECK_BILLET.md`](../05_transport_sync/GUIDE_MIGRATION_IDENTITE_PASSAGER_CHECK_BILLET.md)
