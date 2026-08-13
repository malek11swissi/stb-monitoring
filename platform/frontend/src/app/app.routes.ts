import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login.component').then(m => m.LoginComponent) },
  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./features/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'users', canActivate: [authGuard], loadComponent: () => import('./features/users.component').then(m => m.UsersComponent) },
  { path: 'profile', canActivate: [authGuard], loadComponent: () => import('./features/profile.component').then(m => m.ProfileComponent) },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' }
];
