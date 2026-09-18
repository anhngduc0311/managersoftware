import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  // Extract XSRF-TOKEN cookie value
  const getCookie = (name: string): string | null => {
    const matches = document.cookie.match(new RegExp('(?:^|; )' + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + '=([^;]*)'));
    return matches ? decodeURIComponent(matches[1]) : null;
  };

  const xsrfToken = getCookie('XSRF-TOKEN');

  let clonedReq = req.clone({
    withCredentials: true
  });

  if (xsrfToken && req.method !== 'GET' && req.method !== 'HEAD') {
    clonedReq = clonedReq.clone({
      setHeaders: {
        'X-XSRF-TOKEN': xsrfToken
      }
    });
  }

  return next(clonedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/api/v1/auth/login')) {
        // Redirect to login on unauthenticated error
        router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    })
  );
};
