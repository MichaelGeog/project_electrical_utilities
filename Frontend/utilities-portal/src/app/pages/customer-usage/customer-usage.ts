import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { errorText } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { Reading } from '../../core/api.models';

@Component({
  selector: 'app-customer-usage',
  imports: [RouterLink, DatePipe, DecimalPipe],
  template: `
    <main class="page">
      <p><a routerLink="/dashboard">&larr; Back to my meters</a></p>
      <h2>Usage history</h2>

      @if (error()) {
        <p class="error">{{ error() }}</p>
      } @else if (readings() === null) {
        <p class="muted">Loading...</p>
      } @else if (readings()!.length === 0) {
        <p class="muted">No readings recorded for this meter yet.</p>
      } @else {
        <div class="card table-wrap">
          <table>
            <thead>
              <tr><th>Date</th><th class="num">Meter reading (kWh)</th><th class="num">Used since last (kWh)</th></tr>
            </thead>
            <tbody>
              @for (r of readings(); track r.id) {
                <tr>
                  <td>{{ r.readingDate | date: 'mediumDate' : 'UTC' }}</td>
                  <td class="num">{{ r.kwhReading | number: '1.2-2' }}</td>
                  <td class="num">{{ r.kwhUsed === null ? '-' : (r.kwhUsed | number: '1.2-2') }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </main>
  `,
})
export class CustomerUsage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);

  protected readonly readings = signal<Reading[] | null>(null);
  protected readonly error = signal('');

  constructor() {
    const meterId = this.route.snapshot.paramMap.get('meterId') ?? '';
    this.api.readings(meterId).subscribe({
      next: (list) => this.readings.set(list),
      // Someone else's meter or a made-up id comes back as 404 from the API.
      error: (e) => this.error.set(errorText(e, 'Could not load the usage history.')),
    });
  }
}
