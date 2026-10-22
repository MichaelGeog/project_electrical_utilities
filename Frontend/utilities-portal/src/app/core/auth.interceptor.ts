import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

/**
 * Runs on every HTTP call the app makes:
 *  1. adds "Authorization: Bearer <token>" to calls going to our own API
 *  2. if the API answers 401 (token expired or invalid), logs the user out
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const isOurApi = req.url.startsWith(environment.apiUrl);
  const token = auth.token();

  // Only our own API ever gets the token, never a third-party URL.
  const request =
    isOurApi && token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      const isLoginCall = req.url.endsWith('/auth/login');
      if (error.status === 401 && isOurApi && !isLoginCall) {
        auth.logout();
      }
      return throwError(() => error);
    }),
  );
};
