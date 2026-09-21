export interface ApprovalDecisionDto {
  id: string;
  deploymentRevisionId: string;
  decision: 'Approved' | 'Rejected';
  reason?: string;
  actorId: string;
  actorUserName: string;
  actorDisplayName: string;
  decidedAt: string;
}

export interface DeploymentRevisionDto {
  id: string;
  deploymentId: string;
  revisionNo: number;
  releaseId?: string;
  releaseVersionName?: string;
  operationalStatus: 'NotInUse' | 'Active' | 'Suspended' | 'Retired';
  startDate?: string;
  goLiveDate?: string;
  responsibleUserId?: string;
  responsibleUserName?: string;
  responsibleUserDisplayName?: string;
  workflowStatus: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  submittedBy?: string;
  submittedByUserName?: string;
  submittedByDisplayName?: string;
  submittedAt?: string;
  approvedAt?: string;
  version: number;
  createdAt: string;
  updatedAt?: string;
  decisions?: ApprovalDecisionDto[];
}

export interface DeploymentDto {
  id: string;
  softwareId: string;
  softwareCode: string;
  softwareName: string;
  organizationId: string;
  organizationCode: string;
  organizationName: string;
  environment: 'Production' | 'Test' | 'Development';
  instanceKey: string;
  currentApprovedRevisionId?: string;
  currentApprovedRevision?: DeploymentRevisionDto;
  activeRevision?: DeploymentRevisionDto;
  version: number;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateDeploymentRequest {
  softwareId: string;
  organizationId: string;
  environment: string;
  instanceKey: string;
  releaseId?: string;
  operationalStatus: string;
  startDate?: string;
  goLiveDate?: string;
  responsibleUserId?: string;
}

export interface UpdateDraftRevisionRequest {
  releaseId?: string;
  operationalStatus: string;
  startDate?: string;
  goLiveDate?: string;
  responsibleUserId?: string;
}

export interface RejectRevisionRequest {
  reason: string;
}

export interface DeploymentFilter {
  organizationId?: string;
  softwareId?: string;
  environment?: string;
  operationalStatus?: string;
  workflowStatus?: string;
  search?: string;
  page: number;
  pageSize: number;
}
