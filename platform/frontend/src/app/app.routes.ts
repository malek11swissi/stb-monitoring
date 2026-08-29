/** Carte des pages et permissions nécessaires pour y accéder. */
import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './core/auth.guard';
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login.component').then(m => m.LoginComponent) },
  { path: 'forgot-password', loadComponent: () => import('./features/password-recovery.component').then(m => m.PasswordRecoveryComponent) },
  { path: 'reset-password', loadComponent: () => import('./features/password-recovery.component').then(m => m.PasswordRecoveryComponent) },
  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./features/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'systems', canActivate: [permissionGuard], data:{permission:'systems.read'}, loadComponent: () => import('./features/systems.component').then(m => m.SystemsComponent) },
  { path: 'systems/:id', canActivate: [permissionGuard], data:{permission:'systems.read'}, loadComponent: () => import('./features/system-detail.component').then(m => m.SystemDetailComponent) },
  { path: 'alerts', canActivate: [permissionGuard], data:{permission:'alerts.read'}, loadComponent: () => import('./features/alerts.component').then(m => m.AlertsComponent) },
  { path: 'alerts/:id', canActivate: [permissionGuard], data:{permission:'alerts.read'}, loadComponent: () => import('./features/alert-detail.component').then(m => m.AlertDetailComponent) },
  { path: 'alert-rules', canActivate: [permissionGuard], data:{permission:'alert_rules.manage'}, loadComponent: () => import('./features/alert-rules.component').then(m => m.AlertRulesComponent) },
  { path: 'incidents', canActivate: [permissionGuard], data:{permission:'incidents.read'}, loadComponent: () => import('./features/incidents.component').then(m => m.IncidentsComponent) },
  { path: 'incidents/:id', canActivate: [permissionGuard], data:{permission:'incidents.read'}, loadComponent: () => import('./features/incident-detail.component').then(m => m.IncidentDetailComponent) },
  { path: 'notifications', canActivate: [permissionGuard], data:{permission:'notifications.read'}, loadComponent: () => import('./features/notifications.component').then(m => m.NotificationsComponent) },
  { path: 'calendar', canActivate: [permissionGuard], data:{permission:'maintenance.read'}, loadComponent: () => import('./features/calendar.component').then(m => m.CalendarComponent) },
  { path: 'users', canActivate: [permissionGuard], data:{permission:'users.read'}, loadComponent: () => import('./features/users.component').then(m => m.UsersComponent) },
  { path: 'audit', canActivate: [permissionGuard], data:{permission:'audit.read'}, loadComponent: () => import('./features/audit.component').then(m => m.AuditComponent) },
  { path: 'sla', canActivate: [permissionGuard], data:{permission:'sla.manage'}, loadComponent: () => import('./features/sla.component').then(m => m.SlaComponent) },
  { path: 'forbidden', loadComponent: () => import('./features/forbidden.component').then(m => m.ForbiddenComponent) },
  { path: 'profile', canActivate: [authGuard], loadComponent: () => import('./features/profile.component').then(m => m.ProfileComponent) },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' }
];
