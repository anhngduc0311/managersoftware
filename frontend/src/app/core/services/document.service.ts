import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DocumentAttachmentDto, UploadDocumentResponse } from '../models/document.models';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/documents';

  getAttachments(entityType: string, entityId: string): Observable<DocumentAttachmentDto[]> {
    const params = new HttpParams()
      .set('entityType', entityType)
      .set('entityId', entityId);

    return this.http.get<DocumentAttachmentDto[]>(`${this.baseUrl}/by-entity`, { params });
  }

  uploadDocument(file: File, entityType: string, entityId: string): Observable<UploadDocumentResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    formData.append('entityType', entityType);
    formData.append('entityId', entityId);

    return this.http.post<UploadDocumentResponse>(`${this.baseUrl}/upload`, formData);
  }

  downloadDocument(documentId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${documentId}/download`, {
      responseType: 'blob'
    });
  }

  unlinkAttachment(attachmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/attachments/${attachmentId}`);
  }
}
