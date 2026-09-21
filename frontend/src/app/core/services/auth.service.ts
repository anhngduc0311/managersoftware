import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of, switchMap } from 'rxjs';
import { CurrentUser, LoginResponse, CsrfTokenResponse } from '@core/models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private currentUserSignal = signal<CurrentUser | null>(null);
  private isInitializedSignal = signal<boolean>(false);

  readonly currentUser = computed(() => this.currentUserSignal());
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);
  readonly isInitialized = computed(() => this.isInitializedSignal());

  constructor() {}

  initApp(): Promise<void> {
    return new Promise<void>((resolve) => {
      this.http.get<CsrfTokenResponse>('/api/v1/auth/csrf').pipe(
        catchError(() => of({ token: '' })),
        switchMap(() => this.fetchCurrentUser()),
        catchError(() => of(null))
      ).subscribe({
        next: (user) => {
          if (user) {
            this.currentUserSignal.set(user);
          } else {
            this.currentUserSignal.set(null);
          }
          this.isInitializedSignal.set(true);
          resolve();
        },
        error: () => {
          this.currentUserSignal.set(null);
          this.isInitializedSignal.set(true);
          resolve();
        }
      });
    });
  }

  fetchCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>('/api/v1/auth/me').pipe(
      tap((user) => this.currentUserSignal.set(user))
    );
  }


  login(credentials: { username: string; password: string }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/v1/auth/login', credentials).pipe(
      tap((user) => {
        this.currentUserSignal.set(user);
      })
    );
  }

  logout(): Observable<any> {
    return this.http.post('/api/v1/auth/logout', {}).pipe(
      tap(() => {
        this.currentUserSignal.set(null);
        this.router.navigate(['/login']);
      }),
      catchError(() => {
        this.currentUserSignal.set(null);
        this.router.navigate(['/login']);
        return of(null);
      })
    );
  }

  changePassword(data: { currentPassword: string; newPassword: string }): Observable<any> {
    return this.http.post('/api/v1/auth/change-password', data);
  }

  hasPermission(permission: string, orgId?: string): boolean {
    const user = this.currentUserSignal();
    if (!user) return false;

    // Check direct permission in user's permissions array
    if (!user.permissions.includes(permission)) {
      return false;
    }

    if (!orgId) {
      return true;
    }

    // If specific organization checked, ensure user has global grant or matching org scope
    return user.scopes.some(s =>
      s.scopeType === 'Global' ||
      s.organizationId === orgId
    );
  }

  hasAnyPermission(permissions: string[]): boolean {
    const user = this.currentUserSignal();
    if (!user) return false;
    return permissions.some(p => user.permissions.includes(p));
  }

  hasRole(roleCode: string): boolean {
    const user = this.currentUserSignal();
    return user?.roles.includes(roleCode) ?? false;
  }
}
