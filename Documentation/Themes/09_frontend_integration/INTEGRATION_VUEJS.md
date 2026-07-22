# 🌐 Intégration Vue.js - Dashboards CongoTravel API

## 📦 Dépendances requises

```bash
npm install axios vue-router vuex pinia chart.js vue-chartjs
npm install @vue/composition-api @vueuse/core
```

---

## 🔐 Service d'Authentification

### Service Auth
```javascript
// src/services/authService.js
import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api';

class AuthService {
  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Intercepteur pour ajouter le token
    this.api.interceptors.request.use((config) => {
      const token = localStorage.getItem('access_token');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    // Intercepteur pour gérer les erreurs
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          this.logout();
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  async login(email, password) {
    try {
      const response = await this.api.post('/Utilisateur/authentifier', {
        emailOuTelephone: email,
        motDePasse: password,
        fcmToken: 'string',
        deviceType: 'web',
        deviceModel: 'Vue.js App',
        osVersion: '1.0.0',
      });

      const { accessToken, utilisateur } = response.data;
      
      // Sauvegarder le token
      localStorage.setItem('access_token', accessToken);
      localStorage.setItem('user_info', JSON.stringify(utilisateur));

      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.message || 'Login failed');
    }
  }

  logout() {
    localStorage.removeItem('access_token');
    localStorage.removeItem('user_info');
  }

  getToken() {
    return localStorage.getItem('access_token');
  }

  getUser() {
    const userInfo = localStorage.getItem('user_info');
    return userInfo ? JSON.parse(userInfo) : null;
  }

  isLoggedIn() {
    return !!this.getToken();
  }

  getApi() {
    return this.api;
  }
}

export default new AuthService();
```

### Pinia Store - Auth
```javascript
// src/stores/auth.js
import { defineStore } from 'pinia';
import authService from '@/services/authService';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null,
    token: null,
    isAuthenticated: false,
  }),

  actions: {
    async login(email, password) {
      try {
        const response = await authService.login(email, password);
        
        this.user = response.utilisateur;
        this.token = response.accessToken;
        this.isAuthenticated = true;

        return response;
      } catch (error) {
        throw error;
      }
    },

    logout() {
      authService.logout();
      this.user = null;
      this.token = null;
      this.isAuthenticated = false;
    },

    initializeAuth() {
      const token = authService.getToken();
      const user = authService.getUser();
      
      if (token && user) {
        this.token = token;
        this.user = user;
        this.isAuthenticated = true;
      }
    },
  },

  getters: {
    userRoles: (state) => state.user?.roles || [],
    isSuperAdmin: (state) => state.user?.roles?.includes('Super-Admin') || false,
    isGerant: (state) => state.user?.roles?.includes('Gérant') || false,
    isClient: (state) => state.user?.roles?.includes('Client') || false,
    isTechnicien: (state) => state.user?.roles?.includes('Technicien') || false,
    userSocieteId: (state) => state.user?.idSociete,
  },
});
```

---

## 🏢 Dashboard Super-Admin - Vue.js

### Service Dashboard
```javascript
// src/services/dashboardService.js
import authService from './authService';

class DashboardService {
  async getSuperAdminDashboard() {
    try {
      const response = await authService.getApi().get('/SuperAdmin/dashboard');
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.message || 'Dashboard error');
    }
  }

  async getGerantDashboard(idSociete) {
    try {
      const response = await authService.getApi().get(`/Gerant/dashboard/${idSociete}`);
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.message || 'Dashboard error');
    }
  }

  async getClientDashboard() {
    try {
      const response = await authService.getApi().get('/ClientDashboard');
      return response.data;
    } catch (error) {
      throw new Error(error.response?.data?.message || 'Dashboard error');
    }
  }
}

export default new DashboardService();
```

### Composant Dashboard Super-Admin
```vue
<!-- src/views/SuperAdminDashboard.vue -->
<template>
  <div class="super-admin-dashboard">
    <div class="dashboard-header">
      <h1>Dashboard Super-Admin</h1>
      <div class="refresh-button">
        <button @click="refreshDashboard" :disabled="loading">
          <i class="fas fa-sync-alt" :class="{ 'fa-spin': loading }"></i>
          Actualiser
        </button>
      </div>
    </div>

    <div v-if="loading" class="loading">
      <div class="spinner"></div>
      <p>Chargement du dashboard...</p>
    </div>

    <div v-else-if="error" class="error">
      <i class="fas fa-exclamation-triangle"></i>
      <p>{{ error }}</p>
      <button @click="refreshDashboard">Réessayer</button>
    </div>

    <div v-else class="dashboard-content">
      <!-- Statistiques Globales -->
      <section class="global-stats">
        <h2>Statistiques Globales</h2>
        <div class="stats-grid">
          <div class="stat-card">
            <div class="stat-icon blue">
              <i class="fas fa-building"></i>
            </div>
            <div class="stat-content">
              <h3>{{ dashboard.globalStatistiques.totalSocietes }}</h3>
              <p>Total Sociétés</p>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon green">
              <i class="fas fa-users"></i>
            </div>
            <div class="stat-content">
              <h3>{{ formatNumber(dashboard.globalStatistiques.totalClients) }}</h3>
              <p>Total Clients</p>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon orange">
              <i class="fas fa-chart-line"></i>
            </div>
            <div class="stat-content">
              <h3>{{ formatCurrency(dashboard.globalStatistiques.caMoisEnCours) }}</h3>
              <p>CA Mois</p>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon purple">
              <i class="fas fa-percentage"></i>
            </div>
            <div class="stat-content">
              <h3>{{ dashboard.globalStatistiques.tauxRecouvrementMoisEnCours }}%</h3>
              <p>Taux Recouvrement</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Tendances -->
      <section class="trends">
        <h2>Tendances Mensuelles</h2>
        <div class="charts-grid">
          <div class="chart-container">
            <h3>Chiffre d'Affaires</h3>
            <TrendChart 
              :data="dashboard.tendances.chiffreAffaires" 
              :title="'Chiffre d\'Affaires'"
              color="#3B82F6"
            />
          </div>
          <div class="chart-container">
            <h3>Taux de Recouvrement</h3>
            <TrendChart 
              :data="dashboard.tendances.tauxRecouvrement" 
              :title="'Taux de Recouvrement'"
              color="#10B981"
            />
          </div>
        </div>
      </section>

      <!-- Top Sociétés -->
      <section class="top-societes">
        <div class="section-grid">
          <div class="top-ca">
            <h2>Top 5 Sociétés (CA)</h2>
            <div class="societe-list">
              <div 
                v-for="(societe, index) in dashboard.top5SocietesCA" 
                :key="societe.idSociete"
                class="societe-item"
              >
                <div class="rank">{{ index + 1 }}</div>
                <div class="societe-info">
                  <h4>{{ societe.nomSociete }}</h4>
                  <p>{{ societe.nombreClients }} clients</p>
                </div>
                <div class="societe-ca">
                  {{ formatCurrency(societe.chiffreAffaires) }}
                </div>
              </div>
            </div>
          </div>

          <div class="top-recouvrement">
            <h2>Top 5 Sociétés (Recouvrement)</h2>
            <div class="societe-list">
              <div 
                v-for="(societe, index) in dashboard.top5SocietesRecouvrement" 
                :key="societe.idSociete"
                class="societe-item"
              >
                <div class="rank">{{ index + 1 }}</div>
                <div class="societe-info">
                  <h4>{{ societe.nomSociete }}</h4>
                  <p>{{ formatCurrency(societe.montantCollecte) }} collectés</p>
                </div>
                <div class="societe-rate">
                  <div class="rate">{{ societe.tauxRecouvrement }}%</div>
                  <div class="progress-bar">
                    <div 
                      class="progress-fill" 
                      :style="{ width: societe.progression + '%' }"
                    ></div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- Alertes Critiques -->
      <section class="alerts" v-if="dashboard.alertesCritiques.length > 0">
        <h2>Alertes Critiques</h2>
        <div class="alerts-list">
          <div 
            v-for="alerte in dashboard.alertesCritiques" 
            :key="alerte.id"
            class="alert-item"
            :class="alerte.gravite.toLowerCase()"
          >
            <div class="alert-icon">
              <i class="fas fa-exclamation-triangle"></i>
            </div>
            <div class="alert-content">
              <h4>{{ alerte.type }}</h4>
              <p>{{ alerte.message }}</p>
              <small>{{ formatDate(alerte.dateAlerte) }}</small>
            </div>
            <div class="alert-status">
              <i 
                :class="alerte.estLue ? 'fas fa-check-circle' : 'fas fa-circle'"
                :class="{ 'read': alerte.estLue, 'unread': !alerte.estLue }"
              ></i>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script>
import { ref, onMounted } from 'vue';
import dashboardService from '@/services/dashboardService';
import TrendChart from '@/components/TrendChart.vue';

export default {
  name: 'SuperAdminDashboard',
  components: {
    TrendChart,
  },
  setup() {
    const dashboard = ref(null);
    const loading = ref(false);
    const error = ref(null);

    const loadDashboard = async () => {
      loading.value = true;
      error.value = null;
      
      try {
        dashboard.value = await dashboardService.getSuperAdminDashboard();
      } catch (err) {
        error.value = err.message;
      } finally {
        loading.value = false;
      }
    };

    const refreshDashboard = () => {
      loadDashboard();
    };

    const formatCurrency = (amount) => {
      return new Intl.NumberFormat('fr-FR', {
        style: 'currency',
        currency: 'XAF',
      }).format(amount);
    };

    const formatNumber = (number) => {
      return new Intl.NumberFormat('fr-FR').format(number);
    };

    const formatDate = (dateString) => {
      return new Date(dateString).toLocaleString('fr-FR');
    };

    onMounted(() => {
      loadDashboard();
    });

    return {
      dashboard,
      loading,
      error,
      refreshDashboard,
      formatCurrency,
      formatNumber,
      formatDate,
    };
  },
};
</script>

<style scoped>
.super-admin-dashboard {
  padding: 20px;
  max-width: 1400px;
  margin: 0 auto;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
}

.dashboard-header h1 {
  color: #1f2937;
  font-size: 2rem;
  margin: 0;
}

.refresh-button button {
  background: #3b82f6;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: background 0.3s;
}

.refresh-button button:hover {
  background: #2563eb;
}

.refresh-button button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.loading, .error {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  text-align: center;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f4f6;
  border-top: 4px solid #3b82f6;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 20px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error {
  color: #ef4444;
}

.error button {
  margin-top: 20px;
  background: #ef4444;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
}

.dashboard-content {
  display: flex;
  flex-direction: column;
  gap: 30px;
}

.global-stats h2 {
  color: #1f2937;
  margin-bottom: 20px;
  font-size: 1.5rem;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.stat-card {
  background: white;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  display: flex;
  align-items: center;
  gap: 16px;
  transition: transform 0.2s, box-shadow 0.2s;
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.stat-icon {
  width: 60px;
  height: 60px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  color: white;
}

.stat-icon.blue { background: #3b82f6; }
.stat-icon.green { background: #10b981; }
.stat-icon.orange { background: #f59e0b; }
.stat-icon.purple { background: #8b5cf6; }

.stat-content h3 {
  margin: 0;
  font-size: 1.8rem;
  font-weight: bold;
  color: #1f2937;
}

.stat-content p {
  margin: 4px 0 0 0;
  color: #6b7280;
  font-size: 0.9rem;
}

.trends h2 {
  color: #1f2937;
  margin-bottom: 20px;
  font-size: 1.5rem;
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: 20px;
}

.chart-container {
  background: white;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.chart-container h3 {
  margin: 0 0 20px 0;
  color: #1f2937;
  font-size: 1.1rem;
}

.section-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: 20px;
}

.top-ca, .top-recouvrement {
  background: white;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.top-ca h2, .top-recouvrement h2 {
  margin: 0 0 20px 0;
  color: #1f2937;
  font-size: 1.2rem;
}

.societe-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.societe-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px;
  background: #f9fafb;
  border-radius: 8px;
  transition: background 0.2s;
}

.societe-item:hover {
  background: #f3f4f6;
}

.rank {
  width: 32px;
  height: 32px;
  background: #3b82f6;
  color: white;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 0.9rem;
}

.societe-info {
  flex: 1;
}

.societe-info h4 {
  margin: 0;
  color: #1f2937;
  font-size: 1rem;
}

.societe-info p {
  margin: 4px 0 0 0;
  color: #6b7280;
  font-size: 0.85rem;
}

.societe-ca {
  font-weight: bold;
  color: #10b981;
  font-size: 1rem;
}

.societe-rate {
  text-align: right;
}

.rate {
  font-weight: bold;
  color: #8b5cf6;
  font-size: 1rem;
  margin-bottom: 4px;
}

.progress-bar {
  width: 100px;
  height: 8px;
  background: #e5e7eb;
  border-radius: 4px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: #8b5cf6;
  transition: width 0.3s ease;
}

.alerts {
  background: white;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.alerts h2 {
  margin: 0 0 20px 0;
  color: #1f2937;
  font-size: 1.2rem;
}

.alerts-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.alert-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  border-radius: 8px;
  border-left: 4px solid;
}

.alert-item.élevée {
  border-left-color: #ef4444;
  background: #fef2f2;
}

.alert-item.moyenne {
  border-left-color: #f59e0b;
  background: #fffbeb;
}

.alert-item.faible {
  border-left-color: #10b981;
  background: #f0fdf4;
}

.alert-icon {
  color: #6b7280;
  font-size: 1.2rem;
}

.alert-content {
  flex: 1;
}

.alert-content h4 {
  margin: 0;
  color: #1f2937;
  font-size: 1rem;
}

.alert-content p {
  margin: 4px 0;
  color: #4b5563;
  font-size: 0.9rem;
}

.alert-content small {
  color: #9ca3af;
  font-size: 0.8rem;
}

.alert-status .read {
  color: #10b981;
}

.alert-status .unread {
  color: #ef4444;
}

@media (max-width: 768px) {
  .dashboard-header {
    flex-direction: column;
    gap: 16px;
    align-items: stretch;
  }

  .stats-grid {
    grid-template-columns: 1fr;
  }

  .charts-grid,
  .section-grid {
    grid-template-columns: 1fr;
  }

  .societe-item {
    flex-direction: column;
    align-items: stretch;
    text-align: center;
  }
}
</style>
```
