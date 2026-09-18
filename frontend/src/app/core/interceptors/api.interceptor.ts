import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ProblemDetails } from '../models/problem-details.model';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  // Generate or retrieve correlation ID
  const correlationId = crypto.randomUUID();

  // Clone request with credentials (cookies) and X-Correlation-ID header
  let headers = req.headers.set('X-Correlation-ID', correlationId);

  // Extract XSRF-TOKEN from cookie if present and attach to header for mutation requests
  if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method.toUpperCase())) {
    const xsrfCookie = document.cookie
      .split('; ')
      .find(row => row.startsWith('XSRF-TOKEN='));

    if (xsrfCookie) {
      const xsrfToken = decodeURIComponent(xsrfCookie.split('=')[1]);
      headers = headers.set('X-XSRF-TOKEN', xsrfToken);
    }
  }

  const modifiedReq = req.clone({
    headers,
    withCredentials: true
  });

  return next(modifiedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      let problem: ProblemDetails | null = null;
      if (error.error && typeof error.error === 'object') {
        problem = error.error as ProblemDetails;
      }

      if (error.status === 401) {
        // Unauthenticated -> redirect to login if not already there
        if (!router.url.includes('/login')) {
          router.navigate(['/login']);
        }
      }

      return throwError(() => ({
        originalError: error,
        problem: problem ?? {
          status: error.status,
          title: error.statusText || 'Đã có lỗi xảy ra',
          detail: error.message
        }
      }));
    })
  );
};
