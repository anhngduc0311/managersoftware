export interface NotificationDto {
  id: string;
  recipientUserId: string;
  type: 'WorkflowSubmitted' | 'WorkflowApproved' | 'WorkflowRejected' | 'System';
  title: string;
  message: string;
  targetRoute?: string;
  readAt?: string;
  createdAt: string;
}

export interface NotificationListResponse {
  items: NotificationDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  unreadCount: number;
}
