import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DashboardFilter {
  asOf?: string;
  organizationId?: string;
  softwareId?: string;
  categoryId?: string;
  operationalStatus?: string;
}

export interface OrgDeploymentStatDto {
  organizationId: string;
  organizationName: string;
  deploymentCount: number;
  activeCount: number;
}

export interface StatusStatDto {
  status: string;
  count: number;
}

export interface EnvironmentStatDto {
  environment: string;
  count: number;
}

export interface DashboardKpiDto {
  totalSoftwareCount: number;
  totalDeploymentsCount: number;
  activeOrganizationsCount: number;
  coveragePercentage: number | null;
  eligibleOrganizationsCount: number;
  eligibleActiveOrganizationsCount: number;
  pendingApprovalCount: number;
  expiringContractsCount: number;
  totalContractAmount: number | null;
  currencyCode: string;
  asOfDate: string;
  generatedAtUtc: string;
  orgBreakdown: OrgDeploymentStatDto[];
  statusBreakdown: StatusStatDto[];
  environmentBreakdown: EnvironmentStatDto[];
}

export interface CoverageEligibilityDto {
  id: string;
  organizationId: string;
  organizationName: string;
  softwareId: string;
  softwareName: string;
  validFrom: string;
  validTo: string | null;
  isEligible: boolean;
  note: string | null;
}

export interface CreateCoverageEligibilityRequest {
  organizationId: string;
  softwareId: string;
  validFrom: string;
  validTo?: string | null;
  isEligible?: boolean;
  note?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/dashboard';

  getOverview(filter?: DashboardFilter): Observable<DashboardKpiDto> {
    let params = new HttpParams();
    if (filter?.asOf) params = params.set('asOf', filter.asOf);
    if (filter?.organizationId) params = params.set('organizationId', filter.organizationId);
    if (filter?.softwareId) params = params.set('softwareId', filter.softwareId);
    if (filter?.categoryId) params = params.set('categoryId', filter.categoryId);
    if (filter?.operationalStatus) params = params.set('operationalStatus', filter.operationalStatus);

    return this.http.get<DashboardKpiDto>(`${this.baseUrl}/overview`, { params });
  }

  getCoverageEligibilities(softwareId?: string): Observable<CoverageEligibilityDto[]> {
    let params = new HttpParams();
    if (softwareId) params = params.set('softwareId', softwareId);
    return this.http.get<CoverageEligibilityDto[]>(`${this.baseUrl}/coverage-eligibility`, { params });
  }

  setCoverageEligibility(request: CreateCoverageEligibilityRequest): Observable<CoverageEligibilityDto> {
    return this.http.post<CoverageEligibilityDto>(`${this.baseUrl}/coverage-eligibility`, request);
  }
}
