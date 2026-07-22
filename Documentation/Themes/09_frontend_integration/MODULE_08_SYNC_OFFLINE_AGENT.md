# MODULE 08 — Sync offline agent (Flutter)

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)
>
> Persona principal : **agent terrain Flutter** (mode hors-ligne)

---

## Vue d'ensemble

Le module sync permet à l'agent de :
1. Télécharger les données clients / arriérés en local
2. Enregistrer des paiements offline
3. Remonter les paiements en batch quand le réseau revient

Base route : `/api/sync`

---

## Séquence recommandée

```
1. GET  /api/sync/bootstrap          → métadonnées initiales
2. GET  /api/sync/clients            → boucle tant que hasMore=true
3. GET  /api/sync/arrears            → boucle tant que hasMore=true
4. GET  /api/sync/deletions          → suppressions depuis dernier sync
5. POST /api/sync/payments/batch       → upload paiements offline
```

---

## Bootstrap

```
GET /api/sync/bootstrap?idSociete=1
```

Retourne versions, curseurs, horodatages pour initialiser la base locale.

---

## Delta clients

```
GET /api/sync/clients?idSociete=1&cursor={cursor}&pageSize=500
```

```json
{
  "items": [ /* ClientSyncDto[] */ ],
  "hasMore": true,
  "nextCursor": "abc123"
}
```

Boucler jusqu'à `hasMore: false`.

---

## Delta arriérés

```
GET /api/sync/arrears?idSociete=1&cursor={cursor}&pageSize=500
```

Même pattern pagination par curseur.

---

## Deletions

```
GET /api/sync/deletions?idSociete=1&since={isoDate}
```

Liste des IDs supprimés depuis `since` — purger la base locale.

---

## Batch paiements offline

```
POST /api/sync/payments/batch
```

```json
{
  "idSociete": 1,
  "idAgent": 9,
  "paiements": [
    {
      "localId": "offline-uuid-1",
      "idClient": 42,
      "montantPaye": 5000,
      "datePaiement": "2026-05-08T10:00:00Z",
      "methodePaiement": "Especes",
      "referenceTransaction": "OFF-001"
    }
  ]
}
```

Réponse : succès / échecs par `localId` pour réconciliation.

---

## Flutter — boucle sync

```dart
Future<void> fullSync(int idSociete) async {
  await api.get('/sync/bootstrap', queryParameters: {'idSociete': idSociete});

  var cursor = '';
  var hasMore = true;
  while (hasMore) {
    final r = await api.get('/sync/clients', queryParameters: {
      'idSociete': idSociete,
      'cursor': cursor,
      'pageSize': 500,
    });
    await localDb.upsertClients(r.data['items']);
    hasMore = r.data['hasMore'] == true;
    cursor = r.data['nextCursor'] ?? '';
  }

  // Idem pour arrears, puis deletions, puis upload batch
}
```

---

## Bonnes pratiques

- Stocker `lastSyncAt` en local (SharedPreferences / SQLite)
- Ne pas bloquer l'UI : sync en arrière-plan (Isolate ou workmanager)
- Gérer les conflits : le serveur fait foi sur `referenceTransaction` dupliquée
- Auth JWT requise sur tous les endpoints sync

---

## Références backend

- [`DOCUMENTATION_ENDPOINTS_SYNC.md`](../05_transport_sync/DOCUMENTATION_ENDPOINTS_SYNC.md)
- [`EXEMPLES_UTILISATION_SYNC.md`](../05_transport_sync/EXEMPLES_UTILISATION_SYNC.md)
- [`DOCUMENTATION_INTEGRATION_FRONTENDS_VUE_FLUTTER.md`](DOCUMENTATION_INTEGRATION_FRONTENDS_VUE_FLUTTER.md) §5
