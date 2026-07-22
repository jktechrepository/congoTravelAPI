# MODULE 06 — App client voyageur

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)

---

## Inscription client (public)

```
POST /api/client/register
```

**Sans authentification.** Rate limit multi-scope (email / device / IP).

### Headers recommandés

```
Content-Type: application/json
X-Device-Id: <uuid-stable-par-installation>
```

### Request

```json
{
  "nomClient": "Jean Dupont",
  "telephone": "+243900000001",
  "emailClient": "jean@example.com",
  "adresseClient": "Kinshasa",
  "acceptTerms": true
}
```

### Response 200

```json
{
  "success": true,
  "message": "Inscription reussie",
  "idClient": 42,
  "emailVerificationRequired": true
}
```

### Gestion 429 (rate limit)

```json
{
  "success": false,
  "message": "Trop de tentatives pour cet email. Veuillez réessayer plus tard.",
  "retryAfter": 900
}
```

**Flutter** — persister device ID :

```dart
Future<String> getDeviceId() async {
  var id = await prefs.getString('deviceId');
  if (id == null) {
    id = const Uuid().v4();
    await prefs.setString('deviceId', id);
  }
  return id;
}

await api.post('/client/register',
  data: payload,
  options: Options(headers: {'X-Device-Id': await getDeviceId()}),
);
```

Ne pas réessayer en boucle sur le même email bloqué.

---

## Dashboard client

```
GET /api/ClientDashboard
```

Auth : JWT client. Retourne KPIs personnels (réservations, billets, etc.).

---

## Mes réservations / billets

Via login client, utiliser :
- `GET /api/Billet/reservation/{idReservation}` — billets d'une réservation
- `GET /api/Reservation/...` — historique (selon permissions client)

---

## Plaintes client

```
POST /api/PlainteClient
GET  /api/PlainteClient/mes-plaintes
```

### Créer une plainte

```json
{
  "titre": "Retard bus",
  "description": "Le bus a eu 2h de retard",
  "priorite": "Normale"
}
```

---

## Profil client

```
GET  /api/Client/{id}
PUT  /api/Client/{id}
GET  /api/Client/search?...
```

---

## Parcours complet app voyageur

1. Inscription (`register` + `X-Device-Id`)
2. Vérification email si requis
3. Login → tokens
4. `GET /Voyage/search` — recherche trajets
5. Réservation + FlexPay → [MODULE_04](MODULE_04_PAIEMENT_FLEXPAY.md)
6. Affichage billets QR → [MODULE_03](MODULE_03_RESERVATION_BILLET.md)
7. SignalR notifications paiement

---

## Références backend

- [`CLIENT_REGISTRATION_API_GUIDE.md`](../03_utilisateurs_roles_agents/CLIENT_REGISTRATION_API_GUIDE.md)
- [`GUIDE_MIGRATION_VERROU_INSCRIPTION_CLIENT.md`](../02_securite_auth/GUIDE_MIGRATION_VERROU_INSCRIPTION_CLIENT.md)
- [`API_DOCUMENTATION_PLAINTE_CLIENT.md`](../../../docs/API_DOCUMENTATION_PLAINTE_CLIENT.md)
