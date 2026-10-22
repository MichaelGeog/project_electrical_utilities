import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

/** The header, the role-based menu and the logout button around every page after login. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="bar" [class.admin]="auth.role() === 'Admin'">
      <strong>Utilities Portal</strong>

      <nav>
        @if (auth.role() === 'Admin') {
          <a routerLink="/admin" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Meters</a>
          <a routerLink="/admin/customers" routerLinkActive="active">Customers</a>
          <a routerLink="/admin/readings" routerLinkActive="active">Readings</a>
          <a routerLink="/admin/bills" routerLinkActive="active">Bills</a>
        } @else {
          <a routerLink="/dashboard" routerLinkActive="active">My meters</a>
          <a routerLink="/bills" routerLinkActive="active">My bills</a>
        }
      </nav>

      <span class="who">{{ auth.email() }} ({{ auth.role() }})</span>
      <button type="button" (click)="auth.logout()">Log out</button>
    </header>

    <router-outlet />
  `,
  styles: `
    .bar { display: flex; flex-wrap: wrap; gap: 12px 20px; align-items: center; padding: 12px 16px; background: #1f5fbf; color: #fff; }
    .bar.admin { background: #2b2f3a; }
    nav { display: flex; flex-wrap: wrap; gap: 14px; }
    nav a { color: #fff; text-decoration: none; opacity: .8; padding-bottom: 2px; }
    nav a.active { opacity: 1; border-bottom: 2px solid #fff; }
    .who { margin-left: auto; font-size: .9rem; }
    button { background: #fff; color: #1c2430; border: 0; padding: 6px 12px; border-radius: 6px; cursor: pointer; }
  `,
})
export class Shell {
  protected readonly auth = inject(AuthService);
}
