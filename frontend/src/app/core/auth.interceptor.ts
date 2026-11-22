import { HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.token;

  if (!token || request.url.includes('/api/auth/login')) {
    return next(request);
  }

  const withToken: HttpRequest<unknown> = request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

  return next(withToken).pipe(
    catchError(error => {
      if (error.status === 401) {
        auth.logout();
      }
      return throwError(() => error);
    })
  );
};
