import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Role } from './auth.models';
import { AuthService } from './auth.service';

/** Only logged-in users get through. Everyone else goes to /login. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? true : router.createUrlTree(['/login']);
};

/**
 * Only users with the given role get through.
 * A logged-in user with the wrong role is sent to their own home page.
 * This is for convenience only: the API enforces the real rules.
 */
export const roleGuard =
  (requiredRole: Role): CanActivateFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isLoggedIn()) {
      return router.createUrlTree(['/login']);
    }
    return auth.role() === requiredRole ? true : router.createUrlTree([auth.homePath()]);
  };
