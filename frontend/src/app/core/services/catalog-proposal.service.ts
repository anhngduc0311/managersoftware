import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResponse } from './organization.service';

export interface CatalogProposalDto {
  id: string;
  organizationId: string;
  organizationName?: string;
  proposedByUserId: string;
  proposedByUserName?: string;
  softwareName: string;
  description?: string;
  status: 'Pending' | 'Accepted' | 'Rejected';
  rejectionReason?: string;
  createdSoftwareId?: string;
  createdSoftwareName?: string;
  reviewedByUserId?: string;
  reviewedByUserName?: string;
  reviewedAt?: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class CatalogProposalService {
  private http = inject(HttpClient);
  private baseUrl = '/api/v1/catalog-proposals';

  getProposals(params?: { status?: string; organizationId?: string; page?: number; pageSize?: number }): Observable<PagedResponse<CatalogProposalDto>> {
    let httpParams = new HttpParams();
    if (params?.status) httpParams = httpParams.set('status', params.status);
    if (params?.organizationId) httpParams = httpParams.set('organizationId', params.organizationId);
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResponse<CatalogProposalDto>>(this.baseUrl, { params: httpParams });
  }

  getProposalById(id: string): Observable<CatalogProposalDto> {
    return this.http.get<CatalogProposalDto>(`${this.baseUrl}/${id}`);
  }

  createProposal(data: { organizationId: string; softwareName: string; description?: string }): Observable<CatalogProposalDto> {
    return this.http.post<CatalogProposalDto>(this.baseUrl, data);
  }

  acceptProposal(id: string, data: { existingSoftwareId?: string | null; newSoftwareCode?: string | null; categoryId?: string | null; vendorId?: string | null }): Observable<any> {
    return this.http.post(`${this.baseUrl}/${id}/accept`, data);
  }

  rejectProposal(id: string, reason: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${id}/reject`, { reason });
  }
}
