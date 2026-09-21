import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

export const permissionGuard = (...requiredPermissions: string[]): CanActivateFn => {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      router.navigate(['/login']);
      return false;
    }

    if (requiredPermissions.some(p => authService.hasPermission(p))) {
      return true;
    }

    // Redirect to dashboard if lacks permission
    router.navigate(['/dashboard']);
    return false;
  };
};
