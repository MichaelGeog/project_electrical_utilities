import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { errorText } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { Meter } from '../../core/auth.models';

@Component({
  selector: 'app-admin-home',
  imports: [DatePipe, DecimalPipe],
  template: `
    <main class="page">
      <h2>All meters</h2>

      @if (error()) {
        <p class="error">{{ error() }}</p>
      } @else if (meters() === null) {
        <p class="muted">Loading...</p>
      } @else {
        <div class="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Meter</th><th>Customer</th><th>Account</th><th>Address</th>
                <th>Status</th><th class="num">Latest (kWh)</th><th>Read on</th>
              </tr>
            </thead>
            <tbody>
              @for (m of meters(); track m.meterId) {
                <tr>
                  <td>{{ m.meterNumber }}</td>
                  <td>{{ m.customerName }}</td>
                  <td>{{ m.accountNumber }}</td>
                  <td>{{ m.serviceAddress }}</td>
                  <td><span class="badge">{{ m.status }}</span></td>
                  <td class="num">{{ m.latestKwhReading === null ? '-' : (m.latestKwhReading | number: '1.2-2') }}</td>
                  <td>{{ m.latestReadingDate ? (m.latestReadingDate | date: 'mediumDate' : 'UTC') : '-' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </main>
  `,
})
export class AdminHome {
  private readonly api = inject(ApiService);

  protected readonly meters = signal<Meter[] | null>(null);
  protected readonly error = signal('');

  constructor() {
    this.api.allMeters().subscribe({
      next: (list) => this.meters.set(list),
      error: (e) => this.error.set(errorText(e, 'Could not load meters.')),
    });
  }
}
