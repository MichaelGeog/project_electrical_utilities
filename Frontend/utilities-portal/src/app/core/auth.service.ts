import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse, Role } from './auth.models';

const STORAGE_KEY = 'utilities.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // The logged-in session, or null. A signal, so screens update when it changes.
  private readonly _session = signal<LoginResponse | null>(this.load());

  readonly session = this._session.asReadonly();
  readonly isLoggedIn = computed(() => this._session() !== null);
  readonly role = computed<Role | null>(() => this._session()?.role ?? null);
  readonly email = computed(() => this._session()?.email ?? '');
  readonly token = computed(() => this._session()?.token ?? null);

  /** Where each role lands after login. */
  homePath(role: Role | null = this.role()): string {
    return role === 'Admin' ? '/admin' : '/dashboard';
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(
        tap((response) => {
          this._session.set(response);
          this.save(response);
        }),
      );
  }

  logout(): void {
    this._session.set(null);
    this.save(null);
    this.router.navigate(['/login']);
  }

  // localStorage can be blocked or empty (private windows etc.), so every access is wrapped.
  private load(): LoginResponse | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as LoginResponse) : null;
    } catch {
      return null;
    }
  }

  private save(session: LoginResponse | null): void {
    try {
      if (session) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
      } else {
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      // Ignore: the app still works for this tab, the login just won't survive a refresh.
    }
  }
}
