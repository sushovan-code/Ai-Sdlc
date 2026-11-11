import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth/auth.guards';
import { AppRole } from './core/auth/roles';
import { AppShell } from './shared/layout/app-shell/app-shell';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in · AI-Assisted SDLC',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    title: 'Create account · AI-Assisted SDLC',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.RegisterPage),
  },
  {
    path: '',
    component: AppShell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'projects' },
      {
        path: 'projects',
        title: 'Projects · AI-Assisted SDLC',
        loadComponent: () => import('./features/projects/projects').then((m) => m.ProjectsPage),
      },
      {
        path: 'projects/:projectId',
        title: 'Project · AI-Assisted SDLC',
        loadComponent: () =>
          import('./features/projects/project-workspace/project-workspace').then(
            (m) => m.ProjectWorkspacePage,
          ),
      },
      {
        path: 'users',
        title: 'Users · AI-Assisted SDLC',
        canActivate: [roleGuard(AppRole.Admin)],
        loadComponent: () => import('./features/users/users').then((m) => m.UsersPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
