export interface BackgroundJobDto {
  id: string;
  type: string;
  payloadJson: string;
  status: 'Queued' | 'Running' | 'Succeeded' | 'Failed';
  attempts: number;
  maxAttempts: number;
  leaseOwner?: string;
  leaseUntil?: string;
  nextRunAt: string;
  createdAt: string;
  completedAt?: string;
  lastError?: string;
}
