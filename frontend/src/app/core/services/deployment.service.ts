import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  DeploymentDto,
  DeploymentRevisionDto,
  CreateDeploymentRequest,
  UpdateDraftRevisionRequest,
  RejectRevisionRequest,
  DeploymentFilter
} from '../models/deployment.models';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class DeploymentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/deployments';
  private readonly revisionsUrl = '/api/v1/deployment-revisions';

  getDeployments(filter: DeploymentFilter): Observable<PagedResult<DeploymentDto>> {
    let params = new HttpParams()
      .set('page', filter.page.toString())
      .set('pageSize', filter.pageSize.toString());

    if (filter.organizationId) {
      params = params.set('organizationId', filter.organizationId);
    }
    if (filter.softwareId) {
      params = params.set('softwareId', filter.softwareId);
    }
    if (filter.environment) {
      params = params.set('environment', filter.environment);
    }
    if (filter.operationalStatus) {
      params = params.set('operationalStatus', filter.operationalStatus);
    }
    if (filter.workflowStatus) {
      params = params.set('workflowStatus', filter.workflowStatus);
    }
    if (filter.search) {
      params = params.set('search', filter.search);
    }

    return this.http.get<PagedResult<DeploymentDto>>(this.baseUrl, { params });
  }

  getDeploymentById(id: string): Observable<DeploymentDto> {
    return this.http.get<DeploymentDto>(`${this.baseUrl}/${id}`);
  }

  createDeployment(dto: CreateDeploymentRequest): Observable<DeploymentDto> {
    return this.http.post<DeploymentDto>(this.baseUrl, dto);
  }

  createNextRevision(deploymentId: string, version: number): Observable<DeploymentRevisionDto> {
    const headers = new HttpHeaders().set('If-Match', `"${version}"`);
    return this.http.post<DeploymentRevisionDto>(`${this.baseUrl}/${deploymentId}/revisions`, {}, { headers });
  }

  getRevisionById(id: string): Observable<DeploymentRevisionDto> {
    return this.http.get<DeploymentRevisionDto>(`${this.revisionsUrl}/${id}`);
  }

  getRevisionHistory(deploymentId: string): Observable<DeploymentRevisionDto[]> {
    return this.http.get<DeploymentRevisionDto[]>(`${this.revisionsUrl}/history/${deploymentId}`);
  }

  updateDraftRevision(
    revisionId: string,
    dto: UpdateDraftRevisionRequest,
    version: number
  ): Observable<DeploymentRevisionDto> {
    const headers = new HttpHeaders().set('If-Match', `"${version}"`);
    return this.http.put<DeploymentRevisionDto>(`${this.revisionsUrl}/${revisionId}`, dto, { headers });
  }

  submitRevision(revisionId: string, version: number): Observable<DeploymentRevisionDto> {
    const headers = new HttpHeaders().set('If-Match', `"${version}"`);
    return this.http.post<DeploymentRevisionDto>(`${this.revisionsUrl}/${revisionId}/submit`, {}, { headers });
  }

  approveRevision(revisionId: string, version: number): Observable<DeploymentRevisionDto> {
    const headers = new HttpHeaders().set('If-Match', `"${version}"`);
    return this.http.post<DeploymentRevisionDto>(`${this.revisionsUrl}/${revisionId}/approve`, {}, { headers });
  }

  rejectRevision(
    revisionId: string,
    dto: RejectRevisionRequest,
    version: number
  ): Observable<DeploymentRevisionDto> {
    const headers = new HttpHeaders().set('If-Match', `"${version}"`);
    return this.http.post<DeploymentRevisionDto>(`${this.revisionsUrl}/${revisionId}/reject`, dto, { headers });
  }

  reopenRevision(revisionId: string, version: number): Observable<DeploymentRevisionDto> {
    const headers = new HttpHeaders().set('If-Match', `"${version}"`);
    return this.http.post<DeploymentRevisionDto>(`${this.revisionsUrl}/${revisionId}/reopen`, {}, { headers });
  }
}
