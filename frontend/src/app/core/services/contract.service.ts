import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import {
  ContractDto,
  ContractFilter,
  CreateContractRequest,
  UpdateContractRequest,
  CreateContractItemRequest,
  CreateLicenseEntitlementRequest,
  LicenseAllocationDto,
  CreateAllocationRequest,
  UpdateAllocationRequest
} from '../models/contract.models';
import { PagedResult } from '../services/deployment.service';

@Injectable({
  providedIn: 'root'
})
export class ContractService {
  private readonly http = inject(HttpClient);
  private readonly contractsUrl = '/api/v1/contracts';
  private readonly allocationsUrl = '/api/v1/license-allocations';

  getContracts(filter: ContractFilter): Observable<PagedResult<ContractDto>> {
    let params = new HttpParams()
      .set('page', filter.page.toString())
      .set('pageSize', filter.pageSize.toString());

    if (filter.search) params = params.set('search', filter.search);
    if (filter.organizationId) params = params.set('organizationId', filter.organizationId);
    if (filter.vendorId) params = params.set('vendorId', filter.vendorId);
    if (filter.status) params = params.set('status', filter.status);

    return this.http.get<PagedResult<ContractDto>>(this.contractsUrl, { params });
  }

  getContractWithEtag(id: string): Observable<{ contract: ContractDto; etag: string }> {
    return this.http.get<ContractDto>(`${this.contractsUrl}/${id}`, { observe: 'response' }).pipe(
      map(res => ({
        contract: res.body as ContractDto,
        etag: res.headers.get('ETag') || ''
      }))
    );
  }

  getContractById(id: string): Observable<ContractDto> {
    return this.http.get<ContractDto>(`${this.contractsUrl}/${id}`);
  }

  createContract(request: CreateContractRequest): Observable<ContractDto> {
    return this.http.post<ContractDto>(this.contractsUrl, request);
  }

  updateContract(id: string, request: UpdateContractRequest, etag?: string): Observable<ContractDto> {
    let headers = new HttpHeaders();
    if (etag) {
      headers = headers.set('If-Match', etag);
    }
    return this.http.put<ContractDto>(`${this.contractsUrl}/${id}`, request, { headers });
  }

  addContractItem(contractId: string, request: CreateContractItemRequest): Observable<any> {
    return this.http.post(`${this.contractsUrl}/${contractId}/items`, request);
  }

  deleteContractItem(contractId: string, itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.contractsUrl}/${contractId}/items/${itemId}`);
  }

  addEntitlement(contractId: string, itemId: string, request: CreateLicenseEntitlementRequest): Observable<any> {
    return this.http.post(`${this.contractsUrl}/${contractId}/items/${itemId}/entitlements`, request);
  }

  // License Allocations
  getAllocationsByEntitlement(entitlementId: string): Observable<LicenseAllocationDto[]> {
    return this.http.get<LicenseAllocationDto[]>(`${this.allocationsUrl}/by-entitlement/${entitlementId}`);
  }

  getAllocationsByDeployment(deploymentId: string): Observable<LicenseAllocationDto[]> {
    return this.http.get<LicenseAllocationDto[]>(`${this.allocationsUrl}/by-deployment/${deploymentId}`);
  }

  allocateLicense(request: CreateAllocationRequest): Observable<LicenseAllocationDto> {
    return this.http.post<LicenseAllocationDto>(this.allocationsUrl, request);
  }

  updateAllocation(allocationId: string, request: UpdateAllocationRequest): Observable<LicenseAllocationDto> {
    return this.http.put<LicenseAllocationDto>(`${this.allocationsUrl}/${allocationId}`, request);
  }

  revokeAllocation(allocationId: string): Observable<void> {
    return this.http.delete<void>(`${this.allocationsUrl}/${allocationId}`);
  }
}
