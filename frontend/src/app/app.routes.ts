import { Routes } from '@angular/router';
import { LayoutComponent } from '@layout/layout.component';
import { authGuard } from '@core/guards/auth.guard';
import { permissionGuard } from '@core/guards/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('@features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'software',
        pathMatch: 'full'
      },
      {
        path: 'software',
        loadComponent: () => import('@features/software/software-list.component').then(m => m.SoftwareListComponent)
      },
      {
        path: 'software/:id',
        loadComponent: () => import('@features/software/software-detail.component').then(m => m.SoftwareDetailComponent)
      },
      {
        path: 'admin/users',
        canActivate: [permissionGuard('access.manage')],
        loadComponent: () => import('@features/admin/users/user-list.component').then(m => m.UserListComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'software'
  }
];
