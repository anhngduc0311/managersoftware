import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from './deployment.service';

export interface AuditLogDto {
  id: string;
  actorId?: string | null;
  actorName?: string | null;
  action: string;
  entityType: string;
  entityId: string;
  organizationId?: string | null;
  organizationName?: string | null;
  beforeJson?: string | null;
  afterJson?: string | null;
  occurredAt: string;
  correlationId?: string | null;
}

export interface AuditFilterDto {
  page?: number;
  pageSize?: number;
  entityType?: string;
  action?: string;
  searchTerm?: string;
  organizationId?: string;
  fromDate?: string;
  toDate?: string;
  correlationId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/audit-logs';

  getAuditLogs(filter: AuditFilterDto): Observable<PagedResult<AuditLogDto>> {
    let params = new HttpParams()
      .set('page', (filter.page || 1).toString())
      .set('pageSize', (filter.pageSize || 20).toString());

    if (filter.entityType) params = params.set('entityType', filter.entityType);
    if (filter.action) params = params.set('action', filter.action);
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.organizationId) params = params.set('organizationId', filter.organizationId);
    if (filter.fromDate) params = params.set('fromDate', filter.fromDate);
    if (filter.toDate) params = params.set('toDate', filter.toDate);
    if (filter.correlationId) params = params.set('correlationId', filter.correlationId);

    return this.http.get<PagedResult<AuditLogDto>>(this.baseUrl, { params });
  }

  getAuditLogById(id: string): Observable<AuditLogDto> {
    return this.http.get<AuditLogDto>(`${this.baseUrl}/${id}`);
  }
}
