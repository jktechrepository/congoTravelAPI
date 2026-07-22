# 📋 Guide API - Inscription Publique des Clients

> **Version 1.4.0** | Date: 24/04/2026  
> Guide complet pour l'auto-inscription des clients CongoTravel

---

## 🎯 Vue d'Ensemble

Le système permet maintenant l'auto-inscription des clients via des endpoints publics sécurisés :

```
Client → POST /api/client/register → Compte créé ✅
Client → POST /api/client/check-email → Validation email ✅
```

### **🔧 Architecture Sécurisée**
- **Endpoints publics** avec `[AllowAnonymous]`
- **Validations robustes** côté serveur
- **Rate limiting** anti-abus
- **Audit complet** des inscriptions
- **RGPD compliant** avec consentement explicite

---

## 📡 Endpoints API

### **1. Inscription Client**

#### **Endpoint**
```http
POST /api/client/register
Authorization: Aucune (publique)
Content-Type: application/json
Rate Limit: 3 requêtes/IP/10min
```

#### **Corps de la Requête**
```json
{
  "nomClient": "Jean Dupont",
  "emailClient": "jean.dupont@example.com",
  "telephone": "+243123456789",
  "adresseClient": "123 Avenue Test, Kinshasa",
  "genreClient": "M",
  "province": "Kinshasa",
  "ville": "Kinshasa",
  "commune": "Lemba",
  "avenue": "Huile",
  "numero": "123",
  "acceptTerms": true,
  "subscribeNewsletter": true,
  "marketingConsent": false
}
```

#### **Champs Obligatoires**
- `nomClient` : 2-200 caractères, lettres et espaces
- `telephone` : Format international (+243...)
- `acceptTerms` : `true` obligatoire

#### **Champs Optionnels**
- `emailClient` : Email valide et unique **si renseigné** ; peut être omis ou `null` (contact principal = téléphone)
- `adresseClient` : texte libre, max 500 caractères (peut être omis ; les champs structurés `province`/`ville`/`commune`/`avenue`/`numero` peuvent compléter l'adresse)
- `genreClient` : "M", "F", ou "Autre"
- `province`, `ville`, `commune`, `avenue`, `numero` : Adresse détaillée
- `subscribeNewsletter` : `false` par défaut
- `marketingConsent` : `false` par défaut

#### **Réponse Succès (201)**
```json
{
  "success": true,
  "data": {
    "idClient": 1,
    "nomClient": "Jean Dupont",
    "emailClient": "jean.dupont@example.com",
    "telephone": "+243123456789",
    "dateCreation": "2026-04-24T08:45:00Z",
    "isActif": true,
    "statut": true,
    "message": "Inscription réussie !",
    "welcomeMessage": "Bienvenue sur CongoTravel ! Votre compte a été créé avec succès. Vous pouvez maintenant faire des réservations."
  }
}
```

#### **Réponses d'Erreur**
```json
// 400 - Données invalides
{
  "success": false,
  "message": "Données invalides",
  "errors": ["Le nom est obligatoire", "L'email doit être valide"]
}

// 409 - Email déjà utilisé
{
  "success": false,
  "message": "Cet email est déjà utilisé par un autre client"
}

// 429 - Trop de tentatives
{
  "success": false,
  "message": "Trop de tentatives. Veuillez réessayer plus tard.",
  "retryAfter": 600
}
```

---

### **2. Vérification Disponibilité Email**

#### **Endpoint**
```http
POST /api/client/check-email
Authorization: Aucune (publique)
Content-Type: application/json
Rate Limit: 10 requêtes/IP/5min
```

#### **Corps de la Requête**
```json
{
  "email": "jean.dupont@example.com"
}
```

#### **Réponse Succès (200)**
```json
{
  "success": true,
  "data": {
    "email": "jean.dupont@example.com",
    "isAvailable": true,
    "message": "Cet email est disponible"
  }
}
```

#### **Réponse Email Non Disponible**
```json
{
  "success": true,
  "data": {
    "email": "existing@example.com",
    "isAvailable": false,
    "message": "Cet email est déjà utilisé"
  }
}
```

---

## 🔒 Sécurité et Validations

### **Validations Côté Serveur**

#### **Email**
- Format RFC standard avec regex
- Unicité garantie en base de données
- Normalisation en minuscules
- Longueur max 256 caractères

#### **Téléphone**
- Format international accepté
- Regex : `^\+?[0-9\s\-\(\)]{8,20}$`
- Longueur 8-20 caractères

#### **Nom et Adresse**
- Support caractères accentués français
- Validation des caractères spéciaux
- Longueurs minimales et maximales

#### **Conditions d'Utilisation**
- Acceptation obligatoire (`acceptTerms: true`)
- Validation côté serveur
- Audit de consentement

### **Rate Limiting**

> Guide de migration complet (reproduction dans un autre projet) : [`GUIDE_MIGRATION_VERROU_INSCRIPTION_CLIENT.md`](../02_securite_auth/GUIDE_MIGRATION_VERROU_INSCRIPTION_CLIENT.md)

#### **Protection Anti-Abus**
```csharp
// Inscription : rate limit multi-scope (email principal, device optionnel, IP filet anti-flood)
[ClientRegistrationRateLimit]

// Vérification email : 10 tentatives / 5 minutes / IP
[EmailCheckRateLimit]
```

#### **Scopes inscription (recommandé prod)**
| Scope | Seuil | Rôle |
|-------|-------|------|
| email | 3 / 15 min | Anti-abus ciblé (évite blocage global NAT) |
| device (`X-Device-Id`) | 6 / 15 min | Limite par appareil si header présent |
| ip | 40 / 15 min | Filet anti-flood |

#### **Détection IP**
- `X-Forwarded-For` (proxy)
- `X-Real-IP` (load balancer)
- `Connection.RemoteIpAddress` (fallback)

---

## 📱 Exemples Clients

### **Flutter - Service Complet**

```dart
// services/client_registration_service.dart
import 'dart:convert';
import 'package:http/http.dart' as http;

class ClientRegistrationService {
  final String baseUrl = 'https://api.congotravel.cd/api';

  Future<RegistrationResult> registerClient(RegisterClientDto client) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/client/register'),
        headers: {
          'Content-Type': 'application/json',
        },
        body: jsonEncode(client.toJson()),
      );

      final data = jsonDecode(response.body);
      
      if (response.statusCode == 201) {
        return RegistrationResult.success(
          data['data'],
          data['data']['message']
        );
      } else {
        return RegistrationResult.error(
          data['message'] ?? 'Erreur lors de l\'inscription'
        );
      }
    } on HttpException catch (e) {
      return RegistrationResult.error('Erreur réseau: ${e.message}');
    } catch (e) {
      return RegistrationResult.error('Erreur inconnue: $e');
    }
  }

  Future<EmailAvailabilityResult> checkEmailAvailability(String email) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/client/check-email'),
        headers: {
          'Content-Type': 'application/json',
        },
        body: jsonEncode({'email': email}),
      );

      final data = jsonDecode(response.body);
      
      if (response.statusCode == 200) {
        return EmailAvailabilityResult(
          available: data['data']['isAvailable'],
          message: data['data']['message']
        );
      } else {
        return EmailAvailabilityResult(
          available: false,
          message: data['message'] ?? 'Erreur de vérification'
        );
      }
    } catch (e) {
      return EmailAvailabilityResult(
        available: false,
        message: 'Erreur de connexion'
      );
    }
  }
}

class RegisterClientDto {
  final String nomClient;
  final String emailClient;
  final String telephone;
  final String adresseClient;
  final String? genreClient;
  final String? province;
  final String? ville;
  final String? commune;
  final String? avenue;
  final String? numero;
  final bool acceptTerms;
  final bool subscribeNewsletter;
  final bool marketingConsent;

  RegisterClientDto({
    required this.nomClient,
    required this.emailClient,
    required this.telephone,
    required this.adresseClient,
    this.genreClient,
    this.province,
    this.ville,
    this.commune,
    this.avenue,
    this.numero,
    required this.acceptTerms,
    this.subscribeNewsletter = false,
    this.marketingConsent = false,
  });

  Map<String, dynamic> toJson() {
    return {
      'nomClient': nomClient.trim(),
      'emailClient': emailClient.toLowerCase().trim(),
      'telephone': telephone.trim(),
      'adresseClient': adresseClient.trim(),
      'genreClient': genreClient?.trim(),
      'province': province?.trim(),
      'ville': ville?.trim(),
      'commune': commune?.trim(),
      'avenue': avenue?.trim(),
      'numero': numero?.trim(),
      'acceptTerms': acceptTerms,
      'subscribeNewsletter': subscribeNewsletter,
      'marketingConsent': marketingConsent,
    };
  }
}

class RegistrationResult {
  final bool success;
  final Map<String, dynamic>? data;
  final String? message;

  RegistrationResult.success(this.data, this.message) : success = true;
  RegistrationResult.error(this.message) : success = false, data = null;
}

class EmailAvailabilityResult {
  final bool available;
  final String message;

  EmailAvailabilityResult({required this.available, required this.message});
}
```

### **Vue.js - Composant d'Inscription**

```vue
<!-- components/ClientRegistrationForm.vue -->
<template>
  <div class="registration-container">
    <div class="form-header">
      <h2>Créer votre compte CongoTravel</h2>
      <p>Inscrivez-vous pour réserver vos voyages en quelques clics</p>
    </div>

    <form @submit.prevent="handleSubmit" class="registration-form">
      <!-- Informations de base -->
      <div class="form-section">
        <h3>Informations personnelles</h3>
        
        <div class="form-row">
          <div class="form-group">
            <label for="nomClient">Nom complet *</label>
            <input
              id="nomClient"
              v-model="form.nomClient"
              type="text"
              required
              placeholder="Jean Dupont"
              :class="{ 'error': errors.nomClient }"
            />
            <span class="error-message" v-if="errors.nomClient">{{ errors.nomClient }}</span>
          </div>

          <div class="form-group">
            <label for="genreClient">Genre</label>
            <select id="genreClient" v-model="form.genreClient">
              <option value="">Sélectionner</option>
              <option value="M">Masculin</option>
              <option value="F">Féminin</option>
              <option value="Autre">Autre</option>
            </select>
          </div>
        </div>

        <div class="form-group">
          <label for="emailClient">Email *</label>
          <div class="input-with-validation">
            <input
              id="emailClient"
              v-model="form.emailClient"
              type="email"
              required
              placeholder="jean.dupont@example.com"
              @blur="checkEmailAvailability"
              :class="{ 'error': errors.emailClient, 'success': emailAvailable }"
            />
            <div class="validation-icon" v-if="emailStatus">
              <span :class="emailAvailable ? 'success' : 'error'">
                {{ emailAvailable ? '✓' : '✗' }}
              </span>
            </div>
          </div>
          <span class="error-message" v-if="errors.emailClient">{{ errors.emailClient }}</span>
          <small class="availability-message" :class="emailAvailable ? 'success' : 'error'">
            {{ emailStatus }}
          </small>
        </div>

        <div class="form-group">
          <label for="telephone">Téléphone *</label>
          <input
            id="telephone"
            v-model="form.telephone"
            type="tel"
            required
            placeholder="+243123456789"
            :class="{ 'error': errors.telephone }"
          />
          <span class="error-message" v-if="errors.telephone">{{ errors.telephone }}</span>
        </div>
      </div>

      <!-- Adresse -->
      <div class="form-section">
        <h3>Adresse</h3>
        
        <div class="form-group">
          <label for="adresseClient">Adresse complète (optionnel)</label>
          <textarea
            id="adresseClient"
            v-model="form.adresseClient"
            required
            placeholder="123 Avenue Test, Kinshasa"
            rows="3"
            :class="{ 'error': errors.adresseClient }"
          ></textarea>
          <span class="error-message" v-if="errors.adresseClient">{{ errors.adresseClient }}</span>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label for="province">Province</label>
            <input
              id="province"
              v-model="form.province"
              type="text"
              placeholder="Kinshasa"
            />
          </div>

          <div class="form-group">
            <label for="ville">Ville</label>
            <input
              id="ville"
              v-model="form.ville"
              type="text"
              placeholder="Kinshasa"
            />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label for="commune">Commune</label>
            <input
              id="commune"
              v-model="form.commune"
              type="text"
              placeholder="Lemba"
            />
          </div>

          <div class="form-group">
            <label for="avenue">Avenue</label>
            <input
              id="avenue"
              v-model="form.avenue"
              type="text"
              placeholder="Huile"
            />
          </div>

          <div class="form-group">
            <label for="numero">Numéro</label>
            <input
              id="numero"
              v-model="form.numero"
              type="text"
              placeholder="123"
            />
          </div>
        </div>
      </div>

      <!-- Consentements -->
      <div class="form-section">
        <h3>Préférences</h3>
        
        <div class="checkbox-group">
          <label class="checkbox-label required">
            <input
              type="checkbox"
              v-model="form.acceptTerms"
              required
            />
            <span>
              J'accepte les 
              <a href="/conditions" target="_blank">conditions d'utilisation</a> *
            </span>
          </label>

          <label class="checkbox-label">
            <input
              type="checkbox"
              v-model="form.subscribeNewsletter"
            />
            <span>S'abonner à la newsletter pour recevoir les offres spéciales</span>
          </label>

          <label class="checkbox-label">
            <input
              type="checkbox"
              v-model="form.marketingConsent"
            />
            <span>J'accepte de recevoir des communications marketing</span>
          </label>
        </div>
      </div>

      <!-- Actions -->
      <div class="form-actions">
        <button
          type="submit"
          class="submit-btn"
          :disabled="isSubmitting || !isFormValid"
          :class="{ 'loading': isSubmitting }"
        >
          <span v-if="isSubmitting">
            <i class="spinner"></i>
            Inscription en cours...
          </span>
          <span v-else>S'inscrire</span>
        </button>
      </div>

      <!-- Messages -->
      <div class="messages">
        <div class="success-message" v-if="successMessage">
          <i class="icon-success"></i>
          {{ successMessage }}
          <div class="next-steps" v-if="registrationData">
            <p>Prochaines étapes :</p>
            <ul>
              <li>Confirmez votre email (si requis)</li>
              <li>Connectez-vous à votre compte</li>
              <li>Réservez votre premier voyage</li>
            </ul>
          </div>
        </div>

        <div class="error-message" v-if="errorMessage">
          <i class="icon-error"></i>
          {{ errorMessage }}
          <button @click="errorMessage = ''" class="close-btn">×</button>
        </div>
      </div>
    </form>
  </div>
</template>

<script>
import { ClientRegistrationService } from '@/services/clientRegistrationService';

export default {
  name: 'ClientRegistrationForm',
  data() {
    return {
      form: {
        nomClient: '',
        emailClient: '',
        telephone: '',
        adresseClient: '',
        genreClient: '',
        province: '',
        ville: '',
        commune: '',
        avenue: '',
        numero: '',
        acceptTerms: false,
        subscribeNewsletter: false,
        marketingConsent: false
      },
      errors: {},
      isSubmitting: false,
      successMessage: '',
      errorMessage: '',
      emailStatus: '',
      emailAvailable: false,
      registrationData: null,
      clientService: new ClientRegistrationService()
    };
  },
  computed: {
    isFormValid() {
      return this.form.nomClient && 
             this.form.emailClient && 
             this.form.telephone && 
             this.form.acceptTerms &&
             this.emailAvailable &&
             Object.keys(this.errors).length === 0;
    }
  },
  methods: {
    async checkEmailAvailability() {
      if (!this.form.emailClient || !this.isValidEmail(this.form.emailClient)) {
        this.emailStatus = '';
        return;
      }

      this.emailStatus = 'Vérification en cours...';
      
      try {
        const result = await this.clientService.checkEmailAvailability(this.form.emailClient);
        this.emailAvailable = result.available;
        this.emailStatus = result.message;
        
        if (!result.available) {
          this.errors.emailClient = 'Cet email est déjà utilisé';
        } else {
          delete this.errors.emailClient;
        }
      } catch (error) {
        this.emailStatus = '';
        console.error('Erreur de vérification email:', error);
      }
    },

    isValidEmail(email) {
      const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
      return emailRegex.test(email);
    },

    validateForm() {
      this.errors = {};

      if (!this.form.nomClient || this.form.nomClient.length < 2) {
        this.errors.nomClient = 'Le nom doit contenir au moins 2 caractères';
      }

      if (!this.form.emailClient) {
        this.errors.emailClient = 'L\'email est obligatoire';
      } else if (!this.isValidEmail(this.form.emailClient)) {
        this.errors.emailClient = 'L\'email doit être valide';
      }

      if (!this.form.telephone) {
        this.errors.telephone = 'Le téléphone est obligatoire';
      } else if (!/^\+?[0-9\s\-\(\)]{8,20}$/.test(this.form.telephone)) {
        this.errors.telephone = 'Le format du téléphone est invalide';
      }

      if (this.form.adresseClient && this.form.adresseClient.length > 500) {
        this.errors.adresseClient = 'L\'adresse ne peut pas dépasser 500 caractères';
      }

      if (!this.form.acceptTerms) {
        this.errors.acceptTerms = 'Vous devez accepter les conditions d\'utilisation';
      }

      return Object.keys(this.errors).length === 0;
    },

    async handleSubmit() {
      if (!this.validateForm()) {
        return;
      }

      this.isSubmitting = true;
      this.errorMessage = '';

      try {
        const result = await this.clientService.registerClient(this.form);
        
        if (result.success) {
          this.successMessage = result.message;
          this.registrationData = result.data;
          this.$emit('registration-success', result.data);
          
          // Réinitialiser le formulaire après succès
          setTimeout(() => {
            this.resetForm();
          }, 3000);
        } else {
          this.errorMessage = result.message;
        }
      } catch (error) {
        this.errorMessage = 'Erreur lors de l\'inscription. Veuillez réessayer.';
        console.error('Erreur d\'inscription:', error);
      } finally {
        this.isSubmitting = false;
      }
    },

    resetForm() {
      this.form = {
        nomClient: '',
        emailClient: '',
        telephone: '',
        adresseClient: '',
        genreClient: '',
        province: '',
        ville: '',
        commune: '',
        avenue: '',
        numero: '',
        acceptTerms: false,
        subscribeNewsletter: false,
        marketingConsent: false
      };
      this.errors = {};
      this.emailStatus = '';
      this.emailAvailable = false;
      this.successMessage = '';
      this.registrationData = null;
    }
  }
};
</script>

<style scoped>
.registration-container {
  max-width: 600px;
  margin: 0 auto;
  padding: 2rem;
}

.form-header {
  text-align: center;
  margin-bottom: 2rem;
}

.form-header h2 {
  color: #2c3e50;
  margin-bottom: 0.5rem;
}

.form-header p {
  color: #7f8c8d;
  margin: 0;
}

.form-section {
  margin-bottom: 2rem;
}

.form-section h3 {
  color: #34495e;
  margin-bottom: 1rem;
  padding-bottom: 0.5rem;
  border-bottom: 2px solid #ecf0f1;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #2c3e50;
}

.form-group label.required::after {
  content: ' *';
  color: #e74c3c;
}

.form-group input,
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 0.75rem;
  border: 2px solid #ecf0f1;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.3s;
}

.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #3498db;
}

.form-group input.error,
.form-group select.error,
.form-group textarea.error {
  border-color: #e74c3c;
}

.form-group input.success {
  border-color: #27ae60;
}

.input-with-validation {
  position: relative;
}

.validation-icon {
  position: absolute;
  right: 1rem;
  top: 50%;
  transform: translateY(-50%);
  font-weight: bold;
}

.validation-icon.success {
  color: #27ae60;
}

.validation-icon.error {
  color: #e74c3c;
}

.error-message {
  color: #e74c3c;
  font-size: 0.875rem;
  margin-top: 0.25rem;
  display: block;
}

.availability-message {
  font-size: 0.75rem;
  margin-top: 0.25rem;
  display: block;
}

.availability-message.success {
  color: #27ae60;
}

.availability-message.error {
  color: #e74c3c;
}

.checkbox-group {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.checkbox-label {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  cursor: pointer;
  font-size: 0.9rem;
}

.checkbox-label input[type="checkbox"] {
  width: auto;
  margin-top: 0.25rem;
}

.checkbox-label a {
  color: #3498db;
  text-decoration: none;
}

.checkbox-label a:hover {
  text-decoration: underline;
}

.submit-btn {
  width: 100%;
  padding: 1rem;
  background: linear-gradient(135deg, #3498db, #2980b9);
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 1.1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.submit-btn:hover:not(:disabled) {
  background: linear-gradient(135deg, #2980b9, #21618c);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(52, 152, 219, 0.3);
}

.submit-btn:disabled {
  background: #bdc3c7;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.submit-btn.loading {
  background: linear-gradient(135deg, #f39c12, #e67e22);
}

.spinner {
  display: inline-block;
  width: 1rem;
  height: 1rem;
  border: 2px solid transparent;
  border-top: 2px solid currentColor;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-right: 0.5rem;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.success-message,
.error-message {
  padding: 1rem;
  border-radius: 8px;
  margin-top: 1rem;
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
}

.success-message {
  background: #d5f4e6;
  color: #27ae60;
  border: 1px solid #27ae60;
}

.error-message {
  background: #fdf2f2;
  color: #e74c3c;
  border: 1px solid #e74c3c;
}

.close-btn {
  background: none;
  border: none;
  color: inherit;
  font-size: 1.2rem;
  cursor: pointer;
  margin-left: auto;
}

.next-steps {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid rgba(39, 174, 96, 0.3);
}

.next-steps p {
  margin: 0 0 0.5rem 0;
  font-weight: 600;
}

.next-steps ul {
  margin: 0;
  padding-left: 1.5rem;
}

.next-steps li {
  margin-bottom: 0.25rem;
}

@media (max-width: 768px) {
  .form-row {
    grid-template-columns: 1fr;
    gap: 0.5rem;
  }
  
  .registration-container {
    padding: 1rem;
  }
}
</style>
```

---

## 🛡️ Bonnes Pratiques

### **Pour les Développeurs**

#### **Sécurité**
1. **HTTPS obligatoire** : Jamais de HTTP
2. **Validation côté client** : Améliore UX
3. **Validation côté serveur** : Garantit intégrité
4. **Rate limiting** : Protège contre abus
5. **Logging structuré** : Traçabilité complète

#### **Performance**
1. **Validation email** : En temps réel
2. **Feedback utilisateur** : Messages clairs
3. **Gestion d'erreurs** : Graceful degradation
4. **Cache approprié** : Rate limiting efficace

#### **UX/UI**
1. **Formulaire progressif** : Étapes logiques
2. **Feedback immédiat** : Validation en temps réel
3. **Messages d'erreur** : Clairs et actionnables
4. **Mobile-first** : Responsive design

### **Pour les Utilisateurs**

#### **Protection des Données**
1. **RGPD compliant** : Consentement explicite
2. **Data minimization** : Collecte nécessaire
3. **Transparence** : Conditions claires
4. **Control** : Opt-in marketing

#### **Expérience Utilisateur**
1. **Inscription rapide** : < 2 minutes
2. **Validation instantanée** : Feedback immédiat
3. **Messages clairs** : Compréhension facile
4. **Support multi-langues** : Accessibilité

---

## 📊 Monitoring et Analytics

### **Métriques Essentielles**

#### **Conversion**
- **Taux d'inscription** : % visiteurs → inscrits
- **Abandon formulaire** : Étape de décrochage
- **Temps d'inscription** : Durée moyenne
- **Source de trafic** : Origine des utilisateurs

#### **Techniques**
- **Performance API** : Temps de réponse
- **Taux d'erreur** : Erreurs 4xx/5xx
- **Rate limiting** : Limites atteintes
- **Disponibilité** : Uptime du service

#### **Business**
- **Inscriptions/jour** : Volume quotidien
- **Répartition géographique** : Localisation
- **Appareils utilisés** : Mobile/Desktop
- **Heures de pointe** : Pics d'activité

### **Logs Structurés**

#### **Inscription Réussie**
```json
{
  "timestamp": "2026-04-24T08:45:00Z",
  "level": "Information",
  "message": "Client inscrit avec succès",
  "data": {
    "clientId": 1,
    "email": "jean.dupont@example.com",
    "ipAddress": "192.168.1.100",
    "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
    "source": "Public Registration",
    "duration": 1250,
    "newsletterOptIn": true,
    "marketingOptIn": false
  }
}
```

#### **Erreur d'Inscription**
```json
{
  "timestamp": "2026-04-24T08:47:00Z",
  "level": "Warning",
  "message": "Tentative d'inscription échouée",
  "data": {
    "email": "existing@example.com",
    "reason": "Email déjà utilisé",
    "ipAddress": "192.168.1.101",
    "attempts": 2
  }
}
```

---

## 🚀 Déploiement et Maintenance

### **Configuration Requise**

#### **Infrastructure**
- **Load balancer** : Pour haute disponibilité
- **CDN** : Pour assets statiques
- **SSL Certificate** : HTTPS obligatoire
- **Rate limiting** : Protection DDOS

#### **Application**
- **.NET 6+** : Runtime requis
- **Memory cache** : Pour rate limiting
- **Logging** : Structuré et centralisé
- **Monitoring** : Health checks

### **Tests de Recette**

#### **Fonctionnels**
1. **Inscription valide** : Création compte
2. **Email dupliqué** : Rejet approprié
3. **Validation formulaire** : Erreurs claires
4. **Rate limiting** : Protection active

#### **Sécurité**
1. **HTTPS** : Connexion sécurisée
2. **Input validation** : Protection XSS
3. **Rate limiting** : Anti-abus
4. **Audit trail** : Traçabilité

#### **Performance**
1. **Temps réponse** : < 500ms
2. **Charge simultanée** : 100+ utilisateurs
3. **Memory usage** : Stable sous charge
4. **Error rate** : < 1%

---

## 📞 Support et Dépannage

### **Problèmes Communs**

#### **Erreurs 429 - Too Many Requests**
```bash
# Solution : Attendre la durée spécifiée
retry-after = 600  # 10 minutes
```

#### **Erreurs 409 - Email Existant**
```bash
# Solution : Vérifier avec check-email avant inscription
POST /api/client/check-email
```

#### **Erreurs 400 - Validation**
```bash
# Solution : Vérifier le format des données
- Email : format RFC standard
- Téléphone : format international
- Nom : caractères valides uniquement
```

### **Contact Support**

- **Documentation** : `/CLIENT_REGISTRATION_API_GUIDE.md`
- **Status API** : `https://api.congotravel.cd/api/health`
- **Support technique** : support@congotravel.cd
- **Urgences** : emergency@congotravel.cd

---

## 📈 Évolutions Futures

### **Fonctionnalités Prévues**

#### **Phase 2**
- **Validation email** : Confirmation par email
- **Authentification** : Login post-inscription
- **Profil client** : Gestion du compte
- **Historique réservations** : Vue client

#### **Phase 3**
- **OAuth providers** : Google, Facebook
- **Mobile app** : Application native
- **Notifications push** : Alertes mobiles
- **Wallet intégré** : Paiements simplifiés

### **Améliorations Techniques**

#### **Performance**
- **GraphQL** : Queries optimisées
- **Caching avancé** : Redis cluster
- **Microservices** : Scalabilité horizontale
- **CDN global** : Latence réduite

#### **Sécurité**
- **2FA** : Double authentification
- **JWT refresh** : Tokens renouvelables
- **CSP headers** : Protection XSS
- **Rate limiting avancé** : Machine learning

---

**🎉 Le système d'inscription publique est maintenant prêt pour une utilisation en production !**

Les clients peuvent s'inscrire en toute sécurité avec une expérience utilisateur optimale et une protection robuste contre les abus.
