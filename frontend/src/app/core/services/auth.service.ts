import { Injectable, signal, computed } from '@angular/core';

export interface CurrentUser {
  id: string;
  username: string;
  displayName: string;
  roles: string[];
  permissions: string[];
  scopes: {
    roleCode: string;
    scopeType: 'Global' | 'Organization';
    organizationId?: string;
    organizationName?: string;
    includeDescendants: boolean;
  }[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSignal = signal<CurrentUser | null>({
    id: '00000000-0000-0000-0000-000000000001',
    username: 'admin',
    displayName: 'Quản trị viên Hệ thống',
    roles: ['SystemAdmin'],
    permissions: ['access.manage', 'settings.manage', 'jobs.manage', 'organizations.read', 'catalog.read'],
    scopes: [{
      roleCode: 'SystemAdmin',
      scopeType: 'Global',
      includeDescendants: true
    }]
  });

  readonly currentUser = computed(() => this.currentUserSignal());
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);

  setUser(user: CurrentUser | null) {
    this.currentUserSignal.set(user);
  }

  hasPermission(permission: string): boolean {
    const user = this.currentUserSignal();
    return user?.permissions.includes(permission) ?? false;
  }
}
