import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { errorText } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { Meter } from '../../core/auth.models';

@Component({
  selector: 'app-customer-home',
  imports: [RouterLink, DatePipe, DecimalPipe],
  template: `
    <main class="page">
      <h2>My meters</h2>

      @if (error()) {
        <p class="error">{{ error() }}</p>
      } @else if (meters() === null) {
        <p class="muted">Loading...</p>
      } @else if (meters()!.length === 0) {
        <p class="muted">No meters found on your account.</p>
      } @else {
        @for (m of meters(); track m.meterId) {
          <section class="card">
            <h3>{{ m.meterNumber }} <span class="badge">{{ m.status }}</span></h3>
            <p class="muted">{{ m.serviceAddress }} &middot; Account {{ m.accountNumber }}</p>
            <p>
              Latest reading:
              @if (m.latestKwhReading !== null) {
                <strong>{{ m.latestKwhReading | number: '1.2-2' }} kWh</strong>
                on {{ m.latestReadingDate | date: 'mediumDate' : 'UTC' }}
              } @else {
                none yet
              }
            </p>

            <p><a [routerLink]="['/usage', m.meterId]">View usage history</a></p>
          </section>
        }
      }
    </main>
  `,
})
export class CustomerHome {
  private readonly api = inject(ApiService);

  protected readonly meters = signal<Meter[] | null>(null);
  protected readonly error = signal('');

  constructor() {
    this.api.myMeters().subscribe({
      next: (list) => this.meters.set(list),
      error: (e) => this.error.set(errorText(e, 'Could not load your meters.')),
    });
  }
}
