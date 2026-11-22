import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated() ? true : router.createUrlTree(['/login']);
};

export const permissionGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const required = route.data?.['permission'] as string | undefined;

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  return !required || auth.can(required) ? true : router.createUrlTree(['/unauthorized']);
};
