export type DocumentScanStatus = 'Pending' | 'Scanning' | 'Clean' | 'Rejected';

export interface DocumentDto {
  id: string;
  originalName: string;
  contentType: string;
  sizeBytes: number;
  checksumSha256: string;
  scanStatus: DocumentScanStatus;
  scanMessage?: string;
  uploadedByUserId: string;
  uploadedByUserName: string;
  createdAt: string;
}

export interface DocumentAttachmentDto {
  attachmentId: string;
  documentId: string;
  originalName: string;
  contentType: string;
  sizeBytes: number;
  scanStatus: DocumentScanStatus;
  scanMessage?: string;
  entityType: 'Contract' | 'DeploymentRevision';
  entityId: string;
  attachedByUserId: string;
  attachedByUserName: string;
  attachedAt: string;
}

export interface UploadDocumentResponse {
  attachmentId: string;
  documentId: string;
  originalName: string;
  scanStatus: DocumentScanStatus;
  sizeBytes: number;
}
