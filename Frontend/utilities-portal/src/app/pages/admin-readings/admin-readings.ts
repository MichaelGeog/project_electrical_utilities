import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { errorText } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { Reading } from '../../core/api.models';
import { Meter } from '../../core/auth.models';

/** Today as YYYY-MM-DD in the browser's time zone, the format <input type="date"> uses. */
function todayText(): string {
  return new Date().toLocaleDateString('en-CA');
}

@Component({
  selector: 'app-admin-readings',
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  template: `
    <main class="page">
      <h2>Meter readings</h2>

      <section class="card">
        <h3>Record a reading</h3>
        <form class="row" [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="meter">Meter</label>
            <select id="meter" formControlName="meterId">
              <option value="">Choose a meter</option>
              @for (m of meters(); track m.meterId) {
                <option [value]="m.meterId">{{ m.meterNumber }} - {{ m.customerName }}</option>
              }
            </select>
          </div>

          <div class="field">
            <label for="date">Reading date</label>
            <input id="date" type="date" formControlName="readingDate" [max]="today" />
          </div>

          <div class="field">
            <label for="kwh">Meter reading (kWh)</label>
            <input id="kwh" type="number" step="0.01" min="0" formControlName="kwhReading" />
          </div>

          <button class="primary" type="submit" [disabled]="saving()">
            {{ saving() ? 'Saving...' : 'Save reading' }}
          </button>
        </form>

        @if (message()) {
          <p [class]="isError() ? 'error' : 'success'" role="status">{{ message() }}</p>
        }
      </section>

      @if (form.controls.meterId.value) {
        <section class="card table-wrap">
          <h3>History for the selected meter</h3>
          @if (readings() === null) {
            <p class="muted">Loading...</p>
          } @else if (readings()!.length === 0) {
            <p class="muted">No readings yet.</p>
          } @else {
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
          }
        </section>
      }
    </main>
  `,
})
export class AdminReadings {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly today = todayText();
  protected readonly meters = signal<Meter[]>([]);
  protected readonly readings = signal<Reading[] | null>(null);
  protected readonly saving = signal(false);
  protected readonly message = signal('');
  protected readonly isError = signal(false);

  protected readonly form = this.fb.group({
    meterId: this.fb.nonNullable.control('', Validators.required),
    readingDate: this.fb.nonNullable.control(todayText(), Validators.required),
    kwhReading: this.fb.control<number | null>(null, [Validators.required, Validators.min(0)]),
  });

  constructor() {
    this.api.allMeters().subscribe({
      next: (list) => this.meters.set(list),
      error: (e) => this.show(errorText(e, 'Could not load meters.'), true),
    });

    // Show the history whenever a different meter is chosen.
    this.form.controls.meterId.valueChanges.subscribe((id) => this.loadReadings(id));
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.show('Choose a meter, a date and enter the reading.', true);
      return;
    }

    const { meterId, readingDate, kwhReading } = this.form.getRawValue();
    this.saving.set(true);
    this.message.set('');

    this.api
      .createReading({
        meterId,
        // The API stores readings in UTC; sending midnight UTC keeps the calendar day exact.
        readingDate: `${readingDate}T00:00:00Z`,
        kwhReading: kwhReading as number,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.show('Reading saved.', false);
          this.form.controls.kwhReading.reset(null);
          this.loadReadings(meterId);
        },
        error: (e) => {
          this.saving.set(false);
          this.show(errorText(e, 'Could not save the reading.'), true);
        },
      });
  }

  private loadReadings(meterId: string): void {
    if (!meterId) {
      this.readings.set(null);
      return;
    }
    this.readings.set(null);
    this.api.readings(meterId).subscribe({
      next: (list) => this.readings.set(list),
      error: (e) => this.show(errorText(e, 'Could not load the readings.'), true),
    });
  }

  private show(text: string, error: boolean): void {
    this.message.set(text);
    this.isError.set(error);
  }
}
