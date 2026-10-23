import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/home/home').then((m) => m.HomeComponent) },
  {
    path: 'plan',
    loadComponent: () => import('./features/plan/plan').then((m) => m.PlanComponent),
  },
  {
    path: 'destinations/:id',
    loadComponent: () => import('./features/destination/destination').then((m) => m.DestinationComponent),
  },
  {
    path: 'results',
    loadComponent: () => import('./features/results/results').then((m) => m.ResultsComponent),
  },
  { path: '**', redirectTo: '' },
];
