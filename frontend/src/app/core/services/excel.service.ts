import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ImportRowErrorDto {
  rowIndex: number;
  columnName?: string;
  errorMessage: string;
  rawValue?: string;
  errorCode?: string;
}

export interface ImportBatchDto {
  id: string;
  uploadedByUserId: string;
  originalFileName: string;
  storageKey: string;
  status: string;
  totalRows: number;
  validRows: number;
  errorRows: number;
  createdAt: string;
  validatedAt?: string | null;
  committedAt?: string | null;
  failureReason?: string | null;
  errors?: ImportRowErrorDto[];
}

export interface ExportFilterDto {
  softwareId?: string | null;
  organizationId?: string | null;
  status?: string | null;
  searchTerm?: string | null;
}

export interface ExportRequestDto {
  id: string;
  requestedByUserId: string;
  status: string;
  storageKey?: string | null;
  fileName?: string | null;
  fileSizeBytes?: number | null;
  totalRecords?: number | null;
  createdAt: string;
  completedAt?: string | null;
  expiresAt?: string | null;
  failureReason?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class ExcelService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/deployments/excel';

  downloadTemplate(): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/template`, {
      responseType: 'blob'
    });
  }

  uploadImport(file: File): Observable<ImportBatchDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ImportBatchDto>(`${this.baseUrl}/import`, formData);
  }

  getImportBatch(batchId: string): Observable<ImportBatchDto> {
    return this.http.get<ImportBatchDto>(`${this.baseUrl}/import/${batchId}`);
  }

  validateImportBatch(batchId: string): Observable<{ isValid: boolean; batch: ImportBatchDto }> {
    return this.http.post<{ isValid: boolean; batch: ImportBatchDto }>(`${this.baseUrl}/import/${batchId}/validate`, {});
  }

  commitImportBatch(batchId: string): Observable<{ success: boolean; committedCount: number; message: string }> {
    return this.http.post<{ success: boolean; committedCount: number; message: string }>(`${this.baseUrl}/import/${batchId}/commit`, {});
  }

  requestExport(filter: ExportFilterDto): Observable<ExportRequestDto> {
    return this.http.post<ExportRequestDto>(`${this.baseUrl}/export`, filter);
  }

  getExportStatus(exportId: string): Observable<ExportRequestDto> {
    return this.http.get<ExportRequestDto>(`${this.baseUrl}/export/${exportId}`);
  }

  downloadExport(exportId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/export/${exportId}/download`, {
      responseType: 'blob'
    });
  }
}
