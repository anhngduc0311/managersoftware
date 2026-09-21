import { Injectable, inject, signal, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subscription, interval } from 'rxjs';
import { NotificationDto, NotificationListResponse } from '../models/notification.models';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class NotificationService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly baseUrl = '/api/v1/notifications';

  readonly unreadCount = signal<number>(0);
  readonly recentNotifications = signal<NotificationDto[]>([]);
  readonly isLoading = signal<boolean>(false);

  private pollSubscription?: Subscription;

  constructor() {
    this.initPolling();
  }

  private initPolling(): void {
    // Initial fetch if user is logged in
    if (this.authService.isAuthenticated()) {
      this.refreshUnreadCount();
      this.loadRecent();
    }

    // Poll unread count every 30 seconds
    this.pollSubscription = interval(30000).subscribe(() => {
      if (this.authService.isAuthenticated()) {
        this.refreshUnreadCount();
      }
    });
  }

  refreshUnreadCount(): void {
    this.http.get<{ count: number }>(`${this.baseUrl}/unread-count`).subscribe({
      next: (res) => this.unreadCount.set(res.count),
      error: () => {}
    });
  }

  loadRecent(): void {
    this.isLoading.set(true);
    this.http.get<NotificationListResponse>(`${this.baseUrl}?page=1&pageSize=10`).subscribe({
      next: (res) => {
        this.recentNotifications.set(res.items);
        this.unreadCount.set(res.unreadCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  getNotifications(page = 1, pageSize = 20, unreadOnly = false): Observable<NotificationListResponse> {
    const query = `${this.baseUrl}?page=${page}&pageSize=${pageSize}&unreadOnly=${unreadOnly}`;
    return this.http.get<NotificationListResponse>(query);
  }

  markAsRead(notificationId: string): Observable<void> {
    return new Observable<void>((observer) => {
      this.http.post<void>(`${this.baseUrl}/${notificationId}/read`, {}).subscribe({
        next: () => {
          this.recentNotifications.update((list) =>
            list.map((n) => (n.id === notificationId ? { ...n, readAt: new Date().toISOString() } : n))
          );
          this.unreadCount.update((count) => Math.max(0, count - 1));
          observer.next();
          observer.complete();
        },
        error: (err) => observer.error(err)
      });
    });
  }

  markAllAsRead(): Observable<{ markedCount: number }> {
    return new Observable<{ markedCount: number }>((observer) => {
      this.http.post<{ markedCount: number }>(`${this.baseUrl}/read-all`, {}).subscribe({
        next: (res) => {
          this.recentNotifications.update((list) =>
            list.map((n) => ({ ...n, readAt: new Date().toISOString() }))
          );
          this.unreadCount.set(0);
          observer.next(res);
          observer.complete();
        },
        error: (err) => observer.error(err)
      });
    });
  }

  ngOnDestroy(): void {
    this.pollSubscription?.unsubscribe();
  }
}
