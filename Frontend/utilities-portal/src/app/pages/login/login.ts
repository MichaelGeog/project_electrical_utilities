import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  template: `
    <main class="wrap">
      <form class="card" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <h1>Utilities Portal</h1>
        <p class="sub">Sign in to your account</p>

        <label for="email">Email</label>
        <input id="email" type="email" formControlName="email" autocomplete="username" />
        @if (showError('email')) {
          <small class="error">Enter a valid email address.</small>
        }

        <label for="password">Password</label>
        <input id="password" type="password" formControlName="password" autocomplete="current-password" />
        @if (showError('password')) {
          <small class="error">Password is required.</small>
        }

        @if (errorMessage()) {
          <p class="error banner" role="alert">{{ errorMessage() }}</p>
        }

        <button type="submit" [disabled]="loading()">
          {{ loading() ? 'Signing in...' : 'Sign in' }}
        </button>
      </form>
    </main>
  `,
  styles: `
    .wrap { min-height: 100vh; display: grid; place-items: center; padding: 16px; background: #f3f5f8; }
    .card { width: 100%; max-width: 380px; background: #fff; padding: 28px; border-radius: 10px;
            box-shadow: 0 2px 12px rgba(0,0,0,.08); display: flex; flex-direction: column; gap: 6px; }
    h1 { margin: 0; font-size: 1.5rem; }
    .sub { margin: 0 0 12px; color: #667; }
    label { font-weight: 600; margin-top: 8px; }
    input { padding: 10px; border: 1px solid #c5ccd6; border-radius: 6px; font-size: 1rem; }
    button { margin-top: 16px; padding: 11px; border: 0; border-radius: 6px; background: #1f5fbf;
             color: #fff; font-size: 1rem; cursor: pointer; }
    button:disabled { opacity: .6; cursor: default; }
    .error { color: #b3261e; }
    .banner { margin: 8px 0 0; }
  `,
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal('');

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected showError(name: 'email' | 'password'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    const { email, password } = this.form.getRawValue();
    this.auth.login(email.trim(), password).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.router.navigate([this.auth.homePath(response.role)]);
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage.set(
          error.status === 401
            ? 'Incorrect email or password.'
            : 'Could not reach the server. Please try again.',
        );
      },
    });
  }
}
