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

## Formats date / heure (scan)

Contrat actuel (pas de breaking) — sérialisation JSON ASP.NET + convertisseur `TimeSpan` global `HH:mm:ss`.

| Route | Champ | Type / format | Exemple |
|-------|-------|---------------|---------|
| `GET /api/Billet/{QrCode}/check` | `dateDepartVoyage` | `DateTime` ISO (souvent minuit pour la date de voyage) | `"2026-05-10T00:00:00"` |
| | `heureDepartVoyage` | string `HH:mm:ss` | `"08:30:00"` |
| `GET /api/Billet/qrcode/{qrCode}` | `dateVoyage`, `dateGeneration`, `dateValiditeDebut`, `dateValiditeFin`, … | `DateTime` ISO | `"2026-05-10T00:00:00"` |
| | `heureVoyage` | string `HH:mm:ss` | `"08:30:00"` |

Côté app : parser l’ISO pour la date et formater `HH:mm:ss` pour l’affichage (ex. `10/05/2026 · 08:30`). Ne pas attendre un fuseau forcé (`Z`) ni un objet TimeSpan.

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
String formatDepart(dynamic dateIso, dynamic heureHms) {
  // dateDepartVoyage: "2026-05-10T00:00:00" → date locale lisible
  // heureDepartVoyage: "08:30:00" → HH:mm pour l'UI
  final date = dateIso != null ? DateTime.tryParse(dateIso.toString()) : null;
  final dateTxt = date == null
      ? '—'
      : '${date.day.toString().padLeft(2, '0')}/'
          '${date.month.toString().padLeft(2, '0')}/'
          '${date.year}';
  final heureTxt = (heureHms?.toString() ?? '').length >= 5
      ? heureHms.toString().substring(0, 5) // "08:30"
      : (heureHms?.toString() ?? '—');
  return '$dateTxt · $heureTxt';
}

Future<void> onQrScanned(String qr) async {
  final resp = await api.get('/Billet/$qr/check');
  final data = resp.data;
  final depart = formatDepart(
    data['dateDepartVoyage'],
    data['heureDepartVoyage'],
  );
  // nomClient = passager réel sur cette route
  showDialog(
    context: context,
    builder: (_) => AlertDialog(
      title: Text(data['nomClient'] ?? 'Inconnu'),
      content: Text(
        'Tel: ${data['telephoneClient']}\n'
        'Départ: $depart\n'
        '${data['message']}',
      ),
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

Extrait minimal (dates / heures) :

```json
[
  {
    "idBillet": 502,
    "qrCode": "ABC-123",
    "nomClient": "Passager Réel",
    "dateVoyage": "2026-05-10T00:00:00",
    "heureVoyage": "08:30:00",
    "dateGeneration": "2026-05-01T14:22:00",
    "dateValiditeDebut": "2026-05-10T00:00:00",
    "dateValiditeFin": "2026-05-11T00:00:00",
    "villeDepart": "Kinshasa",
    "villeArrivee": "Matadi"
  }
]
```

Affichage UI : même logique que le check (`dateVoyage` + `heureVoyage`).

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
