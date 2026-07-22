# MODULE 07 — Dashboards admin (Vue.js)

> Retour : [Document maître](DOCUMENTATION_COMPLETE_INTEGRATION_FRONTEND.md)
>
> Persona principal : **back-office Vue.js**

---

## Endpoints dashboards

| Controller | Route | Rôle cible |
|------------|-------|------------|
| Dashboard | `GET /api/Dashboard/{idSociete}` | Admin société |
| GerantDashboard | `GET /api/GerantDashboard` | Gérant |
| FinancierDashboard | `GET /api/FinancierDashboard` | Financier |
| CaissierDashboard | `GET /api/CaissierDashboard` | Caissier |
| CaissierDashboard | `GET /api/CaissierDashboard/rapport-caisse` | Rapport caisse |
| SuperAdminDashboard | `GET /api/SuperAdminDashboard` | Super-Admin |
| EvenementDashboard | `GET /api/events/dashboard` | Billetterie événement |
| FinanceReporting | `GET /api/FinanceReporting/paiements/summary` | Reporting paiements |
| Statistiques | `GET /api/Statistiques/...` | Stats générales |

Query communes : `idSociete`, `idSite`, `dateDebut`, `dateFin`.

---

## Service Vue.js — Dashboard gérant

```js
// src/services/dashboardService.js
import api from './api';

export async function fetchGerantDashboard(params) {
  const { data } = await api.get('/GerantDashboard', {
    params: {
      idSociete: params.idSociete,
      dateDebut: params.dateDebut,
      dateFin: params.dateFin,
    },
  });
  return data;
}
```

---

## Composable Pinia

```js
// src/stores/dashboardStore.js
import { defineStore } from 'pinia';
import { fetchGerantDashboard } from '@/services/dashboardService';

export const useDashboardStore = defineStore('dashboard', {
  state: () => ({ kpis: null, loading: false, error: null }),
  actions: {
    async load(filters) {
      this.loading = true;
      try {
        this.kpis = await fetchGerantDashboard(filters);
      } catch (e) {
        this.error = e.response?.data?.message ?? 'Erreur';
      } finally {
        this.loading = false;
      }
    },
  },
});
```

---

## Graphiques Chart.js

```vue
<script setup>
import { Line } from 'vue-chartjs';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);

const props = defineProps({ labels: Array, values: Array });
const chartData = computed(() => ({
  labels: props.labels,
  datasets: [{ label: 'Recettes', data: props.values, borderColor: '#2563eb' }],
}));
</script>

<template>
  <Line :data="chartData" />
</template>
```

---

## Routing par rôle

```js
const routes = [
  { path: '/dashboard/gerant', component: GerantDashboard, meta: { role: 'Gerant' } },
  { path: '/dashboard/financier', component: FinancierDashboard, meta: { role: 'Financier' } },
  { path: '/dashboard/super-admin', component: SuperAdminDashboard, meta: { role: 'Super-Admin' } },
];
```

Rediriger après login selon `nomRole` ou `primaryRole`.

---

## Finance reporting

```
GET /api/FinanceReporting/paiements/summary?idSociete=1&dateDebut=2026-05-01&dateFin=2026-05-31
```

Réponse agrégée : totaux par méthode, devise, site.

---

## Références backend

- [`DOCUMENTATION_DASHBOARDS.md`](../07_dashboards_reporting/DOCUMENTATION_DASHBOARDS.md)
- [`INTEGRATION_VUEJS.md`](INTEGRATION_VUEJS.md) — exemples détaillés dashboards
- [`DOCUMENTATION_COMPLETE_DASHBOARD.md`](../../DOCUMENTATION_COMPLETE_DASHBOARD.md)
