# Documentation complete d'integration frontend

> **Nouveau point d'entrée** : voir [`DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md`](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md) (structure modulaire Vue.js + Flutter avec 9 fiches MODULE_01 à MODULE_09). Ce fichier reste une archive détaillée.

Ce document sert de reference d'integration pour:
- frontend web (Vue.js)
- frontend mobile (Flutter)

Il couvre l'authentification, la gestion des tokens, les endpoints critiques, les formats de payload/reponse, et les bonnes pratiques d'integration.

---

## 1) Informations generales

### Base URL
- Dev local: `http://localhost:5000/api`
- Production: `https://<votre-domaine>/api`

### Headers standards
- `Content-Type: application/json`
- `Authorization: Bearer <accessToken>` (pour les endpoints proteges)

### Formats
- JSON UTF-8
- Dates au format ISO-8601 (`2026-05-08T08:00:00Z`)
- Heures (`heureDepart`, `heureDepartVoyage`) au format string `HH:mm:ss` (ex: `08:30:00`)
- Devises: codes ISO sur 3 lettres (`CDF`, `USD`)

### Format heureDepart (Voyage)
- Le backend n'expose plus `TimeSpan` en objet detaille.
- Le frontend doit envoyer et lire `heureDepart` en chaine simple `HH:mm:ss`.

Exemple request `POST /api/Voyage`:
```json
{
  "dateDepart": "2026-05-10T00:00:00",
  "heureDepart": "08:30:00",
  "prix": 15000,
  "idVehicule": 4,
  "idDestination": 12,
  "idSociete": 1,
  "idSite": 3,
  "statut": true
}
```

Exemple response (extrait):
```json
{
  "id": 101,
  "dateDepart": "2026-05-10T00:00:00",
  "heureDepart": "08:30:00",
  "idVehicule": 4,
  "idDestination": 12,
  "idSociete": 1,
  "idSite": 3
}
```

### Voyage multi-devise (phase 2)
- Ajouter `codeDevisePrix` dans create/update voyage.
- Backend calcule et retourne aussi:
  - `codeDevisePrincipale`
  - `tauxVersDevisePrincipale`
  - `prixDevisePrincipale`

Exemple:
```json
{
  "dateDepart": "2026-05-10T00:00:00",
  "heureDepart": "08:30:00",
  "prix": 100,
  "codeDevisePrix": "USD",
  "idVehicule": 4,
  "idDestination": 12,
  "idSociete": 1,
  "idSite": 3
}
```

---

## 1.1) Multi-devise (phase 1)

### Endpoints admin
- `GET /api/Devise/devises` (devises actives)
- `PUT /api/Devise/societe/{idSociete}/devise-principale/{codeDevise}`
- `POST /api/Devise/taux-change`
- `GET /api/Devise/taux-change?idSociete=1&source=USD&cible=CDF`
- `GET /api/Devise/preview-conversion?idSociete=1&codeDeviseSource=USD&montant=25&datePaiement=2026-05-08T10:30:00Z`

### Regles importantes
- Devise principale definie par societe (`CodeDevisePrincipale`).
- Taux manuel par paire (dans les deux sens possibles).
- Taux fige au moment du paiement (`datePaiement`).
- Les paiements stockent:
  - montant en devise de paiement
  - montant converti en devise principale (snapshot)
  - taux applique (snapshot)

### Exemple creation taux
```json
{
  "idSociete": 1,
  "codeDeviseSource": "USD",
  "codeDeviseCible": "CDF",
  "taux": 2850.50,
  "dateEffet": "2026-05-08T00:00:00Z"
}
```

### Exemple creation paiement multi-devise
```json
{
  "montantAPaye": 100,
  "montantPaye": 50,
  "codeDevisePaiement": "USD",
  "datePaiement": "2026-05-08T10:30:00Z",
  "methodePaiement": "Especes",
  "referenceTransaction": "PMT-20260508-001",
  "idUtilisateur": 12,
  "idReservation": 333,
  "idSociete": 1,
  "idSite": 3
}
```

### Extrait reponse paiement
```json
{
  "idPaiement": 7001,
  "codeDevisePaiement": "USD",
  "codeDevisePrincipale": "CDF",
  "tauxVersDevisePrincipale": 2850.50,
  "montantAPaye": 100,
  "montantPaye": 50,
  "montantAPayeDevisePrincipale": 285050.00,
  "montantPayeDevisePrincipale": 142525.00,
  "resteAPayeDevisePrincipale": 142525.00
}
```

### Preview conversion (avant paiement)
Permet de calculer le montant converti sans créer de paiement.

Exemple reponse:
```json
{
  "idSociete": 1,
  "codeDeviseSource": "USD",
  "codeDevisePrincipale": "CDF",
  "datePaiement": "2026-05-08T10:30:00Z",
  "taux": 2850.50,
  "montantSource": 25,
  "montantConverti": 71262.50
}
```

### Remboursement multi-devise (phase 3)
Endpoint: `POST /api/Remboursement`

Exemple request:
```json
{
  "idPaiement": 7001,
  "idSociete": 1,
  "idUtilisateur": 12,
  "montantRembourse": 25,
  "codeDeviseRemboursement": "USD",
  "forcerDevisePrincipale": false,
  "dateRemboursement": "2026-05-08T12:00:00Z",
  "motif": "Annulation passager"
}
```

Reporting financier:
- `GET /api/FinanceReporting/paiements/summary?idSociete=1&dateDebut=2026-05-01&dateFin=2026-05-31`

---

## 2) Flux d'authentification (obligatoire)

## 2.1 Login

### Endpoint
- `POST /api/Utilisateur/authentifier`

### Request body
```json
{
  "emailOuTelephone": "admin@congotravel.cd",
  "motDePasse": "Admin",
  "fcmToken": "optional-fcm-token",
  "deviceType": "web",
  "deviceModel": "Chrome 124",
  "osVersion": "macOS 14"
}
```

### Reponse (extrait)
```json
{
  "success": true,
  "message": "Authentification reussie",
  "accessToken": "<jwt>",
  "refreshToken": "<refresh-token>",
  "tokenType": "Bearer",
  "expiresIn": 86400,
  "expiresAt": "2026-05-09T08:00:00Z",
  "doitChangerMotDePasse": false,
  "nomRole": "Admin",
  "nomSociete": "CongoTravel",
  "permissions": ["Agent.Read", "Agent.Update"],
  "roles": [],
  "primaryRole": null,
  "utilisateur": {
    "idUtilisateur": 12,
    "nomComplet": "Admin CongoTravel",
    "email": "admin@congotravel.cd",
    "idSociete": 1,
    "idAgent": 9,
    "idClient": null,
    "idSite": 3
  },
  "agent": {
    "idAgent": 9,
    "nomComplet": "Admin CongoTravel",
    "idSociete": 1,
    "idSite": 3
  },
  "client": null
}
```

Important:
- `utilisateur.idSite` est maintenant retourne.
- `agent.idSite` est maintenant retourne.
- Le frontend peut utiliser `utilisateur.idSite` comme source canonique.

---

## 2.2 Refresh token

### Endpoint
- `POST /api/Utilisateur/refresh-token`

### Request body
```json
{
  "refreshToken": "<refresh-token>",
  "deviceInfo": "web-chrome"
}
```

### Reponse
Meme structure generale que login, avec nouveaux `accessToken` + `refreshToken`.

---

## 2.3 Deconnexion

### Endpoint
- `POST /api/Utilisateur/deconnecter`

Peut desactiver le device courant ou tous les devices selon le payload.

---

## 3) Endpoint metier recent: Affecter un agent a un site

### Endpoint
- `PUT /api/Agent/{idAgent}/AffecterAgentSite`
- Alias REST: `PUT /api/Agent/{idAgent}/site`

### Autorisations
- Roles: `Admin`, `Super-Admin`, `Gerant`

### Request body
```json
{
  "idSite": 5
}
```

### Validations backend
- agent existe
- site existe
- `site.IdSociete == agent.IdSociete`
- restrictions de role/societe appliquees

### Reponse succes
```json
{
  "message": "Agent affecte au site avec succes.",
  "idAgent": 9,
  "ancienIdSite": 2,
  "nouveauIdSite": 5
}
```

---

## 4) Module CategorieSiege (referentiel)

Endpoints disponibles:
- `GET /api/CategorieSiege/societe/{idSociete}?actifsSeulement=true|false`
- `GET /api/CategorieSiege/{idCategorieSiege}`
- `POST /api/CategorieSiege`
- `PUT /api/CategorieSiege/{idCategorieSiege}`
- `PUT /api/CategorieSiege/{idCategorieSiege}/toggle-statut`
- `DELETE /api/CategorieSiege/{idCategorieSiege}`

Reponse type:
```json
{
  "idCategorieSiege": 1,
  "idSociete": 1,
  "codeCategorieSiege": "ECO",
  "libelle": "Economique",
  "statut": true
}
```

Creation:
```json
{
  "idSociete": 1,
  "codeCategorieSiege": "VIP",
  "libelle": "Classe VIP",
  "statut": true
}
```

Mise a jour:
```json
{
  "idCategorieSiege": 12,
  "codeCategorieSiege": "PREMIERE",
  "libelle": "Premiere classe",
  "statut": true
}
```

Exemple Vue.js:
```js
// Liste
const { data } = await api.get(`/CategorieSiege/societe/${idSociete}`, {
  params: { actifsSeulement: true }
});

// Creation
await api.post('/CategorieSiege', {
  idSociete,
  codeCategorieSiege: 'VIP',
  libelle: 'Classe VIP',
  statut: true
});

// Toggle statut
await api.put(`/CategorieSiege/${idCategorieSiege}/toggle-statut`);
```

---

## 5) Synchronisation mobile offline (Flutter)

Endpoints:
- `GET /api/sync/bootstrap`
- `GET /api/sync/clients`
- `GET /api/sync/arrears`
- `GET /api/sync/deletions`
- `POST /api/sync/payments/batch`

## 4.1 Bootstrap
- Appel initial apres login pour recuperer le `watermark`.

## 4.2 Delta clients
- Utiliser `since` + `cursor`.
- Parametres principaux:
  - `pageSize` (1..5000)
  - `cursor`
  - `snapshot`
  - `since`

## 4.3 Delta arrears
- Meme logique que clients.
- `onlyOutstanding=true` pour ne remonter que les soldes non nuls.

## 4.4 Deletions
- Envoyer `since` (obligatoire) pour obtenir les IDs a supprimer localement.

## 4.5 Batch paiements offline
- Envoyer une liste de paiements en une requete.
- Reponse detaillee par item (`created`, `duplicate`, `error`).

Exemple:
```json
{
  "items": [
    {
      "clientRequestId": "0e5cf66a-2664-4e5a-8d55-b48a2c476693",
      "idClient": 1001,
      "idFacture": 778,
      "montantPaye": 20000,
      "datePaiementUtc": "2026-05-08T07:00:00Z",
      "methodePaiement": "Especes",
      "referenceTransaction": "PAY-778-20260508-001",
      "deviceId": "android-01"
    }
  ]
}
```

---

## 6) Module Vehicule (repartition des sieges par categorie)

Le backend supporte maintenant une repartition explicite des sieges par categorie lors de la creation/mise a jour d'un vehicule.

### Endpoints concernes
- `POST /api/Vehicule`
- `PUT /api/Vehicule/{id}`

### Nouveau champ payload
- `repartitionCategorieSieges` (optionnel, mais recommande):
  - `idCategorieSiege`
  - `nombreSiegeParCategorie`

### Photos vehicule (0 a 3)

Stockage : table `PhotoVehicules`, colonne binaire `PhotoData` (MEDIUMBLOB), exposee en API via `photoBase64`.

**Format recommande** dans `POST /api/Vehicule` / `PUT /api/Vehicule/{id}` :

```json
"photos": [
  {
    "photoBase64": "data:image/jpeg;base64,...",
    "ordre": 1,
    "fileName": "bus-avant.jpg"
  }
]
```

Variantes acceptees par le backend :
- `photos` : tableau de **chaines** base64 (sans objet intermediaire) ;
- `photo` (singulier, legacy) : une seule image ;
- dans chaque objet : alias `photo`, `filePath`, `image`, `base64` a la place de `photoBase64`.

Apres creation, alternative : `POST /api/Vehicule/{id}/photos` avec un `AddPhotoVehiculeDto`.

### Exemple creation vehicule
```json
{
  "marques": "Mercedes",
  "aliasVehicule": "BUS-01",
  "idTypeVehicule": 2,
  "nombreSiege": 50,
  "idSociete": 1,
  "numeroDePlaque": "ABC-1234",
  "statut": true,
  "photos": [
    { "photoBase64": "data:image/jpeg;base64,...", "ordre": 1, "fileName": "bus.jpg" }
  ],
  "repartitionCategorieSieges": [
    { "idCategorieSiege": 1, "nombreSiegeParCategorie": 40 },
    { "idCategorieSiege": 2, "nombreSiegeParCategorie": 10 }
  ]
}
```

### Regles metier
- La somme de `nombreSiegeParCategorie` doit correspondre au total des places.
- Les categories doivent appartenir a la meme societe que le vehicule.
- Les categories doivent etre actives.
- Si `repartitionCategorieSieges` est absent, backend applique un fallback (legacy).

### Impact sur CodeSiege
- `CodeSiege` est genere par categorie (`CodeCategorieSiege/index`) au lieu de `AliasVehicule/index`.
- Exemples: `ECO/1`, `ECO/2`, `PREMIERE/1`.

### Exemple Vue.js
```js
await api.post('/Vehicule', {
  marques: 'Mercedes',
  aliasVehicule: 'BUS-01',
  idTypeVehicule: 2,
  nombreSiege: 50,
  idSociete: 1,
  numeroDePlaque: 'ABC-1234',
  statut: true,
  repartitionCategorieSieges: [
    { idCategorieSiege: 1, nombreSiegeParCategorie: 40 },
    { idCategorieSiege: 2, nombreSiegeParCategorie: 10 }
  ]
});
```

---

## 7) Integration Vue.js (Axios)

## 5.1 Client Axios central
```js
import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL + "/api",
  headers: { "Content-Type": "application/json" }
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export default api;
```

## 5.2 Login
```js
const { data } = await api.post("/Utilisateur/authentifier", {
  emailOuTelephone: email,
  motDePasse: password,
  fcmToken: "",
  deviceType: "web",
  deviceModel: "Vue App",
  osVersion: "1.0.0"
});

localStorage.setItem("accessToken", data.accessToken);
localStorage.setItem("refreshToken", data.refreshToken);
localStorage.setItem("currentUser", JSON.stringify(data.utilisateur));
```

## 5.3 Affectation agent->site
```js
await api.put(`/Agent/${idAgent}/site`, { idSite });
```

---

## 8) Integration Flutter (Dio)

## 6.1 Client Dio
```dart
final dio = Dio(BaseOptions(
  baseUrl: '${dotenv.env['API_BASE_URL']}/api',
  headers: {'Content-Type': 'application/json'},
));

dio.interceptors.add(InterceptorsWrapper(
  onRequest: (options, handler) async {
    final token = await secureStorage.read(key: 'accessToken');
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  },
));
```

## 6.2 Login
```dart
final resp = await dio.post('/Utilisateur/authentifier', data: {
  'emailOuTelephone': login,
  'motDePasse': password,
  'fcmToken': fcmToken ?? '',
  'deviceType': 'android',
  'deviceModel': deviceModel,
  'osVersion': osVersion,
});

await secureStorage.write(key: 'accessToken', value: resp.data['accessToken']);
await secureStorage.write(key: 'refreshToken', value: resp.data['refreshToken']);
```

## 6.3 Sync offline (boucle type)
1. `GET /sync/bootstrap`
2. Boucle `GET /sync/clients` tant que `hasMore=true`
3. Boucle `GET /sync/arrears` tant que `hasMore=true`
4. `GET /sync/deletions`
5. Upload offline: `POST /sync/payments/batch`

## 6.4 Creation vehicule avec repartition de categories
```dart
final payload = {
  'marques': 'Mercedes',
  'aliasVehicule': 'BUS-01',
  'idTypeVehicule': 2,
  'nombreSiege': 50,
  'idSociete': 1,
  'numeroDePlaque': 'ABC-1234',
  'photo': null,
  'statut': true,
  'repartitionCategorieSieges': [
    {'idCategorieSiege': 1, 'nombreSiegeParCategorie': 40},
    {'idCategorieSiege': 2, 'nombreSiegeParCategorie': 10},
  ]
};

final resp = await dio.post('/Vehicule', data: payload);
final vehicule = resp.data;
```

## 6.5 Mise a jour vehicule avec nouvelle repartition
```dart
final payload = {
  'idVehicule': 12,
  'marques': 'Mercedes',
  'aliasVehicule': 'BUS-01',
  'idTypeVehicule': 2,
  'nombreSiege': 52,
  'idSociete': 1,
  'numeroDePlaque': 'ABC-1234',
  'photo': null,
  'statut': true,
  'repartitionCategorieSieges': [
    {'idCategorieSiege': 1, 'nombreSiegeParCategorie': 42},
    {'idCategorieSiege': 2, 'nombreSiegeParCategorie': 10},
  ]
};

await dio.put('/Vehicule/12', data: payload);
```

## 6.6 Validation cote app avant envoi
- Verifier que la somme des `nombreSiegeParCategorie` = `nombreSiege`.
- Verifier qu'il n'y a pas de doublon de `idCategorieSiege`.
- Verifier que chaque categorie a un nombre strictement positif.

## 6.7 Utilitaire Dart: validateVehiculeDistribution
```dart
class VehiculeCategorieAllocationInput {
  final int idCategorieSiege;
  final int nombreSiegeParCategorie;

  VehiculeCategorieAllocationInput({
    required this.idCategorieSiege,
    required this.nombreSiegeParCategorie,
  });
}

class ValidationResult {
  final bool isValid;
  final List<String> errors;

  const ValidationResult({required this.isValid, required this.errors});
}

ValidationResult validateVehiculeDistribution({
  required int nombreSiege,
  required List<VehiculeCategorieAllocationInput> repartition,
}) {
  final errors = <String>[];

  if (nombreSiege <= 0) {
    errors.add('nombreSiege doit être supérieur à 0.');
  }

  if (repartition.isEmpty) {
    errors.add('La répartition des catégories est obligatoire.');
    return ValidationResult(isValid: false, errors: errors);
  }

  final seenCategories = <int>{};
  var somme = 0;

  for (final item in repartition) {
    if (item.idCategorieSiege <= 0) {
      errors.add('Chaque idCategorieSiege doit être supérieur à 0.');
    }

    if (!seenCategories.add(item.idCategorieSiege)) {
      errors.add('Doublon détecté pour idCategorieSiege=${item.idCategorieSiege}.');
    }

    if (item.nombreSiegeParCategorie <= 0) {
      errors.add(
        'Chaque nombreSiegeParCategorie doit être supérieur à 0 (catégorie ${item.idCategorieSiege}).',
      );
    }

    somme += item.nombreSiegeParCategorie;
  }

  if (somme != nombreSiege) {
    errors.add(
      'La somme des sièges par catégorie ($somme) doit être égale à nombreSiege ($nombreSiege).',
    );
  }

  return ValidationResult(
    isValid: errors.isEmpty,
    errors: errors,
  );
}
```

Exemple d'utilisation:
```dart
final validation = validateVehiculeDistribution(
  nombreSiege: 50,
  repartition: [
    VehiculeCategorieAllocationInput(idCategorieSiege: 1, nombreSiegeParCategorie: 40),
    VehiculeCategorieAllocationInput(idCategorieSiege: 2, nombreSiegeParCategorie: 10),
  ],
);

if (!validation.isValid) {
  // afficher les erreurs dans l'UI
  for (final err in validation.errors) {
    debugPrint(err);
  }
}
```

## 6.8 Variante orientee Form Flutter (champ -> erreurs)
```dart
class FormValidationResult {
  final bool isValid;
  final Map<String, List<String>> fieldErrors;

  const FormValidationResult({
    required this.isValid,
    required this.fieldErrors,
  });

  List<String> errorsFor(String field) => fieldErrors[field] ?? const [];
  String? firstErrorFor(String field) =>
      fieldErrors[field] != null && fieldErrors[field]!.isNotEmpty
          ? fieldErrors[field]!.first
          : null;
}

FormValidationResult validateVehiculeDistributionForForm({
  required int? nombreSiege,
  required List<VehiculeCategorieAllocationInput> repartition,
}) {
  final errors = <String, List<String>>{};

  void addError(String field, String message) {
    errors.putIfAbsent(field, () => <String>[]).add(message);
  }

  if (nombreSiege == null || nombreSiege <= 0) {
    addError('nombreSiege', 'Le nombre total de sièges doit être supérieur à 0.');
  }

  if (repartition.isEmpty) {
    addError('repartitionCategorieSieges', 'La répartition des catégories est obligatoire.');
    return FormValidationResult(isValid: false, fieldErrors: errors);
  }

  final seen = <int>{};
  var somme = 0;

  for (var i = 0; i < repartition.length; i++) {
    final item = repartition[i];
    final prefix = 'repartitionCategorieSieges[$i]';

    if (item.idCategorieSiege <= 0) {
      addError('$prefix.idCategorieSiege', 'Sélectionnez une catégorie valide.');
    }

    if (!seen.add(item.idCategorieSiege)) {
      addError('$prefix.idCategorieSiege', 'Cette catégorie est déjà utilisée.');
    }

    if (item.nombreSiegeParCategorie <= 0) {
      addError(
        '$prefix.nombreSiegeParCategorie',
        'Le nombre de sièges doit être supérieur à 0.',
      );
    }

    somme += item.nombreSiegeParCategorie;
  }

  if (nombreSiege != null && nombreSiege > 0 && somme != nombreSiege) {
    addError(
      'repartitionCategorieSieges',
      'La somme des sièges par catégorie ($somme) doit être égale à nombreSiege ($nombreSiege).',
    );
  }

  return FormValidationResult(
    isValid: errors.isEmpty,
    fieldErrors: errors,
  );
}
```

Exemple d'usage avec formulaire:
```dart
final result = validateVehiculeDistributionForForm(
  nombreSiege: int.tryParse(nombreSiegeController.text),
  repartition: allocations,
);

if (!result.isValid) {
  final globalError = result.firstErrorFor('repartitionCategorieSieges');
  final nombreSiegeError = result.firstErrorFor('nombreSiege');

  // Exemple: erreur sur le 1er item de la liste
  final firstCategorieError =
      result.firstErrorFor('repartitionCategorieSieges[0].idCategorieSiege');

  setState(() {
    formErrors = result.fieldErrors;
    globalMessage = globalError;
  });
}
```

## 6.9 Variante Riverpod / Bloc (sans setState)

Principe:
- La validation retourne `FormValidationResult`.
- Le state manager stocke `fieldErrors` + `globalMessage`.
- L'UI lit ce state et affiche les erreurs inline.

Exemple Riverpod (StateNotifier simplifie):
```dart
class VehiculeFormState {
  final Map<String, List<String>> fieldErrors;
  final String? globalMessage;
  final bool isSubmitting;

  const VehiculeFormState({
    this.fieldErrors = const {},
    this.globalMessage,
    this.isSubmitting = false,
  });

  VehiculeFormState copyWith({
    Map<String, List<String>>? fieldErrors,
    String? globalMessage,
    bool? isSubmitting,
  }) {
    return VehiculeFormState(
      fieldErrors: fieldErrors ?? this.fieldErrors,
      globalMessage: globalMessage,
      isSubmitting: isSubmitting ?? this.isSubmitting,
    );
  }
}

class VehiculeFormController extends StateNotifier<VehiculeFormState> {
  VehiculeFormController() : super(const VehiculeFormState());

  Future<void> submit({
    required int? nombreSiege,
    required List<VehiculeCategorieAllocationInput> repartition,
  }) async {
    final validation = validateVehiculeDistributionForForm(
      nombreSiege: nombreSiege,
      repartition: repartition,
    );

    if (!validation.isValid) {
      state = state.copyWith(
        fieldErrors: validation.fieldErrors,
        globalMessage: validation.firstErrorFor('repartitionCategorieSieges'),
      );
      return;
    }

    state = state.copyWith(fieldErrors: {}, globalMessage: null, isSubmitting: true);
    try {
      // await repository.createVehicule(...);
      state = state.copyWith(isSubmitting: false);
    } catch (_) {
      state = state.copyWith(
        isSubmitting: false,
        globalMessage: "Erreur lors de l'enregistrement du vehicule.",
      );
    }
  }
}
```

Exemple Bloc/Cubit (idee identique):
```dart
// 1) emit(state.copyWith(fieldErrors: validation.fieldErrors))
// 2) stop submit si invalid
// 3) sinon submit API puis emit success/error
```

Pattern UI recommande:
- `TextFormField(..., errorText: state.fieldErrors['nombreSiege']?.first)`
- Liste dynamique: `state.fieldErrors['repartitionCategorieSieges[$i].idCategorieSiege']`
- Bannière globale: `state.globalMessage`

---

## 9) Gestion des erreurs frontend

Codes frequents:
- `400`: payload invalide / regle metier
- `401`: token invalide/expire
- `403`: non autorise pour ce role/perimetre
- `404`: ressource introuvable
- `500`: erreur interne

Pattern recommande:
- afficher `response.data.message` si present
- fallback message generique sinon

---

## 10) Recommandations importantes

- Toujours stocker `accessToken` et `refreshToken` en stockage securise.
- Toujours gerer le refresh automatique sur `401`.
- Utiliser `utilisateur.idSite` comme contexte courant si votre UX est mono-site.
- En backoffice multi-site, afficher explicitement les changements d'affectation (`Agent/{id}/site`).
- Pour Flutter offline, persister `watermark`, `snapshot`, `cursor` localement.
- Pour la creation/mise a jour vehicule, preferer `repartitionCategorieSieges` explicite.

---

## 11) Ordre recommande des appels frontend

## 10.1 Sequence web (Vue.js)
1. `POST /api/Utilisateur/authentifier`
2. Sauvegarder `accessToken`, `refreshToken`, `utilisateur`
3. Lire `utilisateur.idSociete` et `utilisateur.idSite`
4. Charger le referentiel `CategorieSiege`:
   - `GET /api/CategorieSiege/societe/{idSociete}?actifsSeulement=true`
5. Avant creation vehicule, construire `repartitionCategorieSieges` depuis les categories chargees
6. Charger les ecrans metier (voyages, reservations, paiements...)
7. En cas de `401`, tenter `POST /api/Utilisateur/refresh-token` puis rejouer la requete

## 10.2 Sequence mobile (Flutter online + offline)
1. `POST /api/Utilisateur/authentifier`
2. Sauvegarder `accessToken` + `refreshToken`
3. Online:
   - Charger `CategorieSiege` de la societe
4. Si creation/edition vehicule: envoyer `repartitionCategorieSieges`
5. Offline sync:
   - `GET /api/sync/bootstrap`
   - Boucle `GET /api/sync/clients` (jusqu'a `hasMore=false`)
   - Boucle `GET /api/sync/arrears` (jusqu'a `hasMore=false`)
   - `GET /api/sync/deletions`
6. Upload des operations offline:
   - `POST /api/sync/payments/batch`

## 10.3 Sequence admin (affectation agent/site)
1. Lister agents (ecran admin)
2. Charger la liste des sites de la societe
3. Appeler `PUT /api/Agent/{idAgent}/site`
4. Rafraichir la fiche agent / session si besoin

---

## 12) Checklist d'integration

- [ ] Login OK et token stocke
- [ ] Refresh token automatique operationnel
- [ ] `utilisateur.idSite` exploite dans l'app
- [ ] Endpoint `PUT /api/Agent/{idAgent}/site` integre
- [ ] CRUD `CategorieSiege` integre
- [ ] `POST/PUT /api/Vehicule` integre avec `repartitionCategorieSieges`
- [ ] Gestion d'erreurs 400/401/403/404/500
- [ ] (Flutter) Sync bootstrap + delta + deletions + batch paiements valide

---

## 13) Endpoints references (resume)

- Auth:
  - `POST /api/Utilisateur/authentifier`
  - `POST /api/Utilisateur/refresh-token`
  - `POST /api/Utilisateur/deconnecter`
- Agent/Site:
  - `PUT /api/Agent/{idAgent}/AffecterAgentSite`
  - `PUT /api/Agent/{idAgent}/site`
- CategorieSiege:
  - `GET /api/CategorieSiege/societe/{idSociete}`
  - `GET /api/CategorieSiege/{idCategorieSiege}`
  - `POST /api/CategorieSiege`
  - `PUT /api/CategorieSiege/{idCategorieSiege}`
  - `PUT /api/CategorieSiege/{idCategorieSiege}/toggle-statut`
  - `DELETE /api/CategorieSiege/{idCategorieSiege}`
- Vehicule:
  - `POST /api/Vehicule`
  - `PUT /api/Vehicule/{id}`
- Destination:
  - `GET /api/Destination`
  - `GET /api/Destination/{id}`
  - `GET /api/Destination/societe/{idSociete}`
  - `POST /api/Destination`
  - `PUT /api/Destination/{id}`
  - `DELETE /api/Destination/{id}`
- Sync:
  - `GET /api/sync/bootstrap`
  - `GET /api/sync/clients`
  - `GET /api/sync/arrears`
  - `GET /api/sync/deletions`
  - `POST /api/sync/payments/batch`

---

## 14) Destination - contrat payload (maj)

Le champ officiel pour le jour est `jourDepart` (nullable string).  
Ne plus envoyer `jourDeLaSemaine` dans les payloads frontend.

### 14.1 Create Destination (`POST /api/Destination`)

```json
{
  "villeDepart": "Kinshasa",
  "villeArrivee": "Matadi",
  "montant": 15000,
  "idSociete": 1,
  "jourDepart": "Lundi"
}
```

### 14.2 Update Destination (`PUT /api/Destination/{id}`)

```json
{
  "montant": 18000,
  "jourDepart": "Vendredi"
}
```

### 14.3 Response Destination (extrait)

```json
{
  "idDestination": 12,
  "villeDepart": "Kinshasa",
  "villeArrivee": "Matadi",
  "montant": 18000,
  "jourDepart": "Vendredi",
  "statut": true,
  "idSociete": 1
}
```

---

## 15) Reservation + Paiement - format billet aligne

Pour `POST /api/Reservation/with-passengers-and-paiement` (alias `POST /api/Reservation/reservation_with_paiement`), les champs `billet` et `billets` utilisent maintenant le **meme contrat** que:

- `GET /api/Billet/reservation/{idReservation}`

### 15.1 Avant (ancien format simplifie)

```json
{
  "billet": {
    "idBillet": 910,
    "qrCode": "QRCODE-ABC",
    "urlBillet": "/api/billet/910",
    "idReservationPassenger": 333
  }
}
```

### 15.2 Apres (format aligne endpoint Billet)

```json
{
  "billet": {
    "idBillet": 910,
    "isUsed": false,
    "idReservation": 120,
    "idReservationPassenger": 333,
    "idSiege": 55,
    "codeSiege": "VIP-01",
    "nomPassager": null,
    "qrCode": "QRCODE-ABC",
    "dateGeneration": "2026-05-08T17:00:00Z",
    "idSociete": 1,
    "idSite": 2,
    "dateCreation": "2026-05-08T17:00:00Z",
    "dateModification": null,
    "statutReservation": null,
    "dateReservation": null,
    "nomUtilisateur": null,
    "emailUtilisateur": null,
    "nomClient": null,
    "telephoneClient": null,
    "dateVoyage": null,
    "heureVoyage": null,
    "prixVoyage": null,
    "aliasVehicule": null,
    "villeDepart": null,
    "villeArrivee": null
  },
  "billets": [
    {
      "idBillet": 910,
      "isUsed": false,
      "idReservation": 120,
      "idReservationPassenger": 333,
      "idSiege": 55,
      "codeSiege": "VIP-01",
      "nomPassager": null,
      "qrCode": "QRCODE-ABC",
      "dateGeneration": "2026-05-08T17:00:00Z",
      "idSociete": 1,
      "idSite": 2,
      "dateCreation": "2026-05-08T17:00:00Z",
      "dateModification": null,
      "statutReservation": null,
      "dateReservation": null,
      "nomUtilisateur": null,
      "emailUtilisateur": null,
      "nomClient": null,
      "telephoneClient": null,
      "dateVoyage": null,
      "heureVoyage": null,
      "prixVoyage": null,
      "aliasVehicule": null,
      "villeDepart": null,
      "villeArrivee": null
    }
  ]
}
```

### 15.3 Impact frontend

- Vous pouvez reutiliser le meme modele DTO `BilletResponseDto` pour:
  - `GET /api/Billet/reservation/{idReservation}`
  - `POST /api/Reservation/with-passengers-and-paiement`
- Plus besoin de mapper un format billet specifique au workflow reservation+paiement.

---

## 16) Couverture complete de l'API

Tu as raison: ce document est oriente integration (flux et endpoints critiques) et ne listait pas toutes les routes.

Pour couvrir 100% des endpoints exposes par les controllers backend, utilise aussi:

- `DOCUMENTATION_API_ENDPOINTS_COMPLETE.md`

Contenu de cette annexe:
- index complet par controller
- methode HTTP + pattern de route
- regle de resolution des routes `api/[controller]`

