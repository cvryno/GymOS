import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./shared/layout/layout.component').then(m => m.LayoutComponent),
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'members', loadComponent: () => import('./features/members/members.component').then(m => m.MembersComponent) },
      { path: 'memberships', loadComponent: () => import('./features/memberships/memberships.component').then(m => m.MembershipsComponent) },
      { path: 'classes', loadComponent: () => import('./features/classes/classes.component').then(m => m.ClassesComponent) },
      { path: 'instructors', loadComponent: () => import('./features/instructors/instructors.component').then(m => m.InstructorsComponent) },
      { path: 'equipment', loadComponent: () => import('./features/equipment/equipment.component').then(m => m.EquipmentComponent) },
      { path: 'access', loadComponent: () => import('./features/access/access.component').then(m => m.AccessComponent) },
      { path: 'invoices', loadComponent: () => import('./features/invoices/invoices.component').then(m => m.InvoicesComponent) },
    ]
  },
  { path: '**', redirectTo: '/dashboard' }
];
