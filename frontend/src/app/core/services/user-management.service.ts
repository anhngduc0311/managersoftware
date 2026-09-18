import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResponse } from './organization.service';
import { UserScope } from '@core/models/auth.models';

export interface UserDetailDto {
  id: string;
  userName: string;
  displayName: string;
  email?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  scopes?: UserScope[];
}

export interface RoleDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  permissions: {
    id: string;
    code: string;
    name: string;
    groupName: string;
  }[];
}

export interface PermissionDto {
  id: string;
  code: string;
  name: string;
  groupName: string;
}

export interface UserGrantDetailDto {
  id: string;
  userId: string;
  userName: string;
  userDisplayName: string;
  roleId: string;
  roleCode: string;
  roleName: string;
  scopeType: 'Global' | 'Organization';
  organizationId?: string;
  includeDescendants: boolean;
  validFrom: string;
  validTo?: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class UserManagementService {
  private http = inject(HttpClient);

  // User APIs
  getUsers(params?: { search?: string; isActive?: boolean; page?: number; pageSize?: number }): Observable<PagedResponse<UserDetailDto>> {
    let httpParams = new HttpParams();
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.isActive !== undefined) httpParams = httpParams.set('isActive', params.isActive.toString());
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResponse<UserDetailDto>>('/api/v1/users', { params: httpParams });
  }

  getUserById(id: string): Observable<UserDetailDto> {
    return this.http.get<UserDetailDto>(`/api/v1/users/${id}`);
  }

  createUser(data: { username: string; displayName: string; email?: string; password: string }): Observable<UserDetailDto> {
    return this.http.post<UserDetailDto>('/api/v1/users', data);
  }

  updateUser(id: string, data: { displayName: string; email?: string; isActive: boolean }): Observable<UserDetailDto> {
    return this.http.put<UserDetailDto>(`/api/v1/users/${id}`, data);
  }

  resetPassword(id: string, newPassword: string): Observable<any> {
    return this.http.post(`/api/v1/users/${id}/reset-password`, { newPassword });
  }

  toggleLock(id: string): Observable<any> {
    return this.http.post(`/api/v1/users/${id}/toggle-lock`, {});
  }

  // Roles & Permissions APIs
  getRoles(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>('/api/v1/roles');
  }

  getPermissions(): Observable<PermissionDto[]> {
    return this.http.get<PermissionDto[]>('/api/v1/permissions');
  }

  // Grant APIs
  getGrants(params?: { userId?: string; roleId?: string; organizationId?: string }): Observable<UserGrantDetailDto[]> {
    let httpParams = new HttpParams();
    if (params?.userId) httpParams = httpParams.set('userId', params.userId);
    if (params?.roleId) httpParams = httpParams.set('roleId', params.roleId);
    if (params?.organizationId) httpParams = httpParams.set('organizationId', params.organizationId);

    return this.http.get<UserGrantDetailDto[]>('/api/v1/user-role-scopes', { params: httpParams });
  }

  createGrant(data: { userId: string; roleId: string; scopeType: string; organizationId?: string | null; includeDescendants: boolean; validFrom: string; validTo?: string | null }): Observable<any> {
    return this.http.post('/api/v1/user-role-scopes', data);
  }

  updateGrant(id: string, data: { includeDescendants: boolean; validFrom: string; validTo?: string | null }): Observable<any> {
    return this.http.put(`/api/v1/user-role-scopes/${id}`, data);
  }

  revokeGrant(id: string): Observable<any> {
    return this.http.delete(`/api/v1/user-role-scopes/${id}`);
  }
}
