import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './core/auth.guard';
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login.component').then(m => m.LoginComponent) },
  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./features/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'systems', canActivate: [permissionGuard], data:{permission:'systems.read'}, loadComponent: () => import('./features/systems.component').then(m => m.SystemsComponent) },
  { path: 'systems/:id', canActivate: [permissionGuard], data:{permission:'systems.read'}, loadComponent: () => import('./features/system-detail.component').then(m => m.SystemDetailComponent) },
  { path: 'users', canActivate: [permissionGuard], data:{permission:'users.read'}, loadComponent: () => import('./features/users.component').then(m => m.UsersComponent) },
  { path: 'roles', canActivate: [permissionGuard], data:{permission:'roles.read'}, loadComponent: () => import('./features/roles.component').then(m => m.RolesComponent) },
  { path: 'audit', canActivate: [permissionGuard], data:{permission:'audit.read'}, loadComponent: () => import('./features/audit.component').then(m => m.AuditComponent) },
  { path: 'forbidden', loadComponent: () => import('./features/forbidden.component').then(m => m.ForbiddenComponent) },
  { path: 'profile', canActivate: [authGuard], loadComponent: () => import('./features/profile.component').then(m => m.ProfileComponent) },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' }
];
