import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResponse } from './organization.service';

export interface SoftwareCategoryDto {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  createdAt: string;
}

export interface VendorDto {
  id: string;
  code: string;
  name: string;
  contactInfo?: string;
  isActive: boolean;
  createdAt: string;
}

export interface SoftwareReleaseDto {
  id: string;
  softwareId: string;
  versionName: string;
  releaseDate: string;
  supportEndDate?: string;
  createdAt: string;
}

export interface SoftwareDto {
  id: string;
  code: string;
  name: string;
  categoryId: string;
  categoryName: string;
  vendorId: string;
  vendorName: string;
  description?: string;
  lifecycleStatus: 'Active' | 'Deprecated' | 'Retired';
  version: number;
  releaseCount?: number;
  latestRelease?: string;
  releases?: SoftwareReleaseDto[];
  createdAt: string;
  updatedAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SoftwareService {
  private http = inject(HttpClient);

  // Software APIs
  getSoftware(params?: { search?: string; categoryId?: string; vendorId?: string; lifecycleStatus?: string; page?: number; pageSize?: number }): Observable<PagedResponse<SoftwareDto>> {
    let httpParams = new HttpParams();
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params?.vendorId) httpParams = httpParams.set('vendorId', params.vendorId);
    if (params?.lifecycleStatus) httpParams = httpParams.set('lifecycleStatus', params.lifecycleStatus);
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResponse<SoftwareDto>>('/api/v1/software', { params: httpParams });
  }

  getSoftwareById(id: string): Observable<SoftwareDto> {
    return this.http.get<SoftwareDto>(`/api/v1/software/${id}`);
  }

  createSoftware(data: {
    code: string;
    name: string;
    categoryId: string;
    vendorId: string;
    description?: string;
    lifecycleStatus: string;
    initialVersionName?: string | null;
    initialReleaseDate?: string | null;
    initialSupportEndDate?: string | null;
  }): Observable<SoftwareDto> {
    return this.http.post<SoftwareDto>('/api/v1/software', data);
  }


  updateSoftware(id: string, data: { name: string; categoryId: string; vendorId: string; description?: string; lifecycleStatus: string }): Observable<SoftwareDto> {
    return this.http.put<SoftwareDto>(`/api/v1/software/${id}`, data);
  }

  deleteSoftware(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/software/${id}`);
  }

  // Release APIs
  getReleases(softwareId: string): Observable<SoftwareReleaseDto[]> {
    return this.http.get<SoftwareReleaseDto[]>(`/api/v1/software/${softwareId}/releases`);
  }

  createRelease(softwareId: string, data: { versionName: string; releaseDate: string; supportEndDate?: string | null }): Observable<SoftwareReleaseDto> {
    return this.http.post<SoftwareReleaseDto>(`/api/v1/software/${softwareId}/releases`, data);
  }

  updateRelease(releaseId: string, data: { releaseDate: string; supportEndDate?: string | null }): Observable<SoftwareReleaseDto> {
    return this.http.put<SoftwareReleaseDto>(`/api/v1/software-releases/${releaseId}`, data);
  }

  deleteRelease(releaseId: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/software-releases/${releaseId}`);
  }

  // Category APIs
  getCategories(): Observable<SoftwareCategoryDto[]> {
    return this.http.get<SoftwareCategoryDto[]>('/api/v1/software-categories');
  }

  createCategory(data: { code: string; name: string }): Observable<SoftwareCategoryDto> {
    return this.http.post<SoftwareCategoryDto>('/api/v1/software-categories', data);
  }

  updateCategory(id: string, data: { name: string; isActive: boolean }): Observable<SoftwareCategoryDto> {
    return this.http.put<SoftwareCategoryDto>(`/api/v1/software-categories/${id}`, data);
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/software-categories/${id}`);
  }

  // Vendor APIs
  getVendors(): Observable<VendorDto[]> {
    return this.http.get<VendorDto[]>('/api/v1/vendors');
  }

  createVendor(data: { code: string; name: string; contactInfo?: string }): Observable<VendorDto> {
    return this.http.post<VendorDto>('/api/v1/vendors', data);
  }

  updateVendor(id: string, data: { name: string; contactInfo?: string; isActive: boolean }): Observable<VendorDto> {
    return this.http.put<VendorDto>(`/api/v1/vendors/${id}`, data);
  }

  deleteVendor(id: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/vendors/${id}`);
  }
}

