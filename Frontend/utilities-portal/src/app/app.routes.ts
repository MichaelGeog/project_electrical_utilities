import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards';
import { Shell } from './layout/shell';
import { AdminBills } from './pages/admin-bills/admin-bills';
import { AdminCustomers } from './pages/admin-customers/admin-customers';
import { AdminHome } from './pages/admin-home/admin-home';
import { AdminReadings } from './pages/admin-readings/admin-readings';
import { CustomerBills } from './pages/customer-bills/customer-bills';
import { CustomerHome } from './pages/customer-home/customer-home';
import { CustomerUsage } from './pages/customer-usage/customer-usage';
import { Login } from './pages/login/login';

export const routes: Routes = [
  { path: 'login', component: Login },

  // Everything after login sits inside the Shell (header + menu) and needs a login.
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: CustomerHome, canActivate: [roleGuard('Customer')] },
      { path: 'usage/:meterId', component: CustomerUsage, canActivate: [roleGuard('Customer')] },
      { path: 'bills', component: CustomerBills, canActivate: [roleGuard('Customer')] },

      { path: 'admin', component: AdminHome, canActivate: [roleGuard('Admin')] },
      { path: 'admin/customers', component: AdminCustomers, canActivate: [roleGuard('Admin')] },
      { path: 'admin/readings', component: AdminReadings, canActivate: [roleGuard('Admin')] },
      { path: 'admin/bills', component: AdminBills, canActivate: [roleGuard('Admin')] },
    ],
  },

  { path: '**', redirectTo: 'login' },
];
