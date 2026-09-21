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
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('@features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'organizations',
        canActivate: [permissionGuard('organizations.read')],
        loadComponent: () => import('@features/organizations/organization-list.component').then(m => m.OrganizationListComponent)
      },
      {
        path: 'organizations/tree',
        canActivate: [permissionGuard('organizations.read')],
        loadComponent: () => import('@features/organizations/organization-tree.component').then(m => m.OrganizationTreeComponent)
      },
      {
        path: 'software',
        canActivate: [permissionGuard('catalog.read')],
        loadComponent: () => import('@features/software/software-list.component').then(m => m.SoftwareListComponent)
      },
      {
        path: 'software/proposals',
        canActivate: [permissionGuard('catalog.read')],
        loadComponent: () => import('@features/software/catalog-proposals.component').then(m => m.CatalogProposalsComponent)
      },
      {
        path: 'software/:id',
        canActivate: [permissionGuard('catalog.read')],
        loadComponent: () => import('@features/software/software-detail.component').then(m => m.SoftwareDetailComponent)
      },
      {
        path: 'deployments',
        canActivate: [permissionGuard('deployments.read')],
        loadComponent: () => import('@features/deployments/deployment-list.component').then(m => m.DeploymentListComponent)
      },
      {
        path: 'deployments/excel',
        canActivate: [permissionGuard('deployments.read', 'reports.export', 'reports.import')],
        loadComponent: () => import('@features/deployments/excel-wizard.component').then(m => m.ExcelWizardComponent)
      },
      {
        path: 'deployments/:id',
        canActivate: [permissionGuard('deployments.read')],
        loadComponent: () => import('@features/deployments/deployment-detail.component').then(m => m.DeploymentDetailComponent)
      },
      {
        path: 'contracts',
        canActivate: [permissionGuard('contracts.read', 'contracts.write', 'licenses.allocate')],
        loadComponent: () => import('@features/contracts/contract-list.component').then(m => m.ContractListComponent)
      },
      {
        path: 'contracts/:id',
        canActivate: [permissionGuard('contracts.read', 'contracts.write', 'licenses.allocate')],
        loadComponent: () => import('@features/contracts/contract-detail.component').then(m => m.ContractDetailComponent)
      },
      {
        path: 'admin/users',
        canActivate: [permissionGuard('access.manage')],
        loadComponent: () => import('@features/admin/users/user-list.component').then(m => m.UserListComponent)
      },
      {
        path: 'admin/roles',
        canActivate: [permissionGuard('access.manage')],
        loadComponent: () => import('@features/admin/roles/role-list.component').then(m => m.RoleListComponent)
      },
      {
        path: 'admin/jobs',
        canActivate: [permissionGuard('jobs.manage')],
        loadComponent: () => import('@features/admin/jobs/job-list.component').then(m => m.JobListComponent)
      },
      {
        path: 'admin/audit',
        canActivate: [permissionGuard('audit.read', 'access.manage')],
        loadComponent: () => import('@features/admin/audit/audit-log-list.component').then(m => m.AuditLogListComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
