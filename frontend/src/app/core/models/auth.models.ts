export interface UserScope {
  id?: string;
  roleId?: string;
  roleCode: string;
  roleName?: string;
  scopeType: 'Global' | 'Organization';
  organizationId?: string;
  organizationName?: string;
  includeDescendants: boolean;
  validFrom?: string;
  validTo?: string;
  permissions?: string[];
}

export interface CurrentUser {
  id: string;
  username: string;
  displayName: string;
  email?: string;
  isAuthenticated: boolean;
  roles: string[];
  permissions: string[];
  scopes: UserScope[];
}

export interface LoginResponse extends CurrentUser {}

export interface CsrfTokenResponse {
  token: string;
}
