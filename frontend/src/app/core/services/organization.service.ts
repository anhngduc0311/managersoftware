import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OrganizationDto {
  id: string;
  code: string;
  name: string;
  parentId?: string;
  parentName?: string;
  isActive: boolean;
  validFrom: string;
  validTo?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface OrganizationTreeNode {
  id: string;
  code: string;
  name: string;
  parentId?: string;
  isActive: boolean;
  validFrom: string;
  validTo?: string;
  children: OrganizationTreeNode[];
}

export interface OrganizationHistoryDto {
  id: string;
  name: string;
  parentId?: string;
  validFrom: string;
  validTo?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface OrganizationSuccessionDto {
  id: string;
  predecessorId: string;
  predecessorCode: string;
  successorId: string;
  successorCode: string;
  effectiveDate: string;
  note?: string;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class OrganizationService {
  private http = inject(HttpClient);
  private baseUrl = '/api/v1/organizations';

  getOrganizations(params?: { search?: string; asOf?: string; isActive?: boolean; page?: number; pageSize?: number }): Observable<PagedResponse<OrganizationDto>> {
    let httpParams = new HttpParams();
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.asOf) httpParams = httpParams.set('asOf', params.asOf);
    if (params?.isActive !== undefined) httpParams = httpParams.set('isActive', params.isActive.toString());
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResponse<OrganizationDto>>(this.baseUrl, { params: httpParams });
  }

  getOrganizationTree(asOf?: string): Observable<OrganizationTreeNode[]> {
    let httpParams = new HttpParams();
    if (asOf) httpParams = httpParams.set('asOf', asOf);
    return this.http.get<OrganizationTreeNode[]>(`${this.baseUrl}/tree`, { params: httpParams });
  }

  getOrganizationById(id: string, asOf?: string): Observable<OrganizationDto> {
    let httpParams = new HttpParams();
    if (asOf) httpParams = httpParams.set('asOf', asOf);
    return this.http.get<OrganizationDto>(`${this.baseUrl}/${id}`, { params: httpParams });
  }

  createOrganization(data: { code: string; name: string; parentId?: string | null; validFrom: string; validTo?: string | null }): Observable<OrganizationDto> {
    return this.http.post<OrganizationDto>(this.baseUrl, data);
  }

  updateOrganization(id: string, data: { name: string; parentId?: string | null; validFrom: string; validTo?: string | null; isActive: boolean }): Observable<OrganizationDto> {
    return this.http.put<OrganizationDto>(`${this.baseUrl}/${id}`, data);
  }

  getOrganizationHistory(id: string): Observable<OrganizationHistoryDto[]> {
    return this.http.get<OrganizationHistoryDto[]>(`${this.baseUrl}/${id}/history`);
  }

  getSuccessions(): Observable<OrganizationSuccessionDto[]> {
    return this.http.get<OrganizationSuccessionDto[]>(`${this.baseUrl}/successions`);
  }

  createSuccession(data: { predecessorId: string; successorId: string; effectiveDate: string; note?: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/successions`, data);
  }
}
