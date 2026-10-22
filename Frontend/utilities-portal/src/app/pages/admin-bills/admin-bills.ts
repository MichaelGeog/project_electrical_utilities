import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { errorText } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { Bill } from '../../core/api.models';
import { Meter } from '../../core/auth.models';

@Component({
  selector: 'app-admin-bills',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, DecimalPipe],
  template: `
    <main class="page">
      <h2>Bills</h2>

      <section class="card">
        <h3>Generate a bill</h3>
        <p class="muted">
          The usage comes from the meter readings on the start and end dates, so both dates
          need a reading.
        </p>
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
            <label for="start">Period start</label>
            <input id="start" type="date" formControlName="periodStart" />
          </div>

          <div class="field">
            <label for="end">Period end</label>
            <input id="end" type="date" formControlName="periodEnd" />
          </div>

          <button class="primary" type="submit" [disabled]="saving()">
            {{ saving() ? 'Generating...' : 'Generate bill' }}
          </button>
        </form>

        @if (message()) {
          <p [class]="isError() ? 'error' : 'success'" role="status">{{ message() }}</p>
        }
      </section>

      <section class="card table-wrap">
        <div class="row" style="margin-bottom: 8px">
          <div class="field">
            <label for="filter">Show</label>
            <select id="filter" [value]="statusFilter()" (change)="changeFilter($any($event.target).value)">
              <option value="">All bills</option>
              <option value="Unpaid">Unpaid only</option>
              <option value="Paid">Paid only</option>
            </select>
          </div>
        </div>

        @if (bills() === null) {
          <p class="muted">Loading...</p>
        } @else if (bills()!.length === 0) {
          <p class="muted">No bills found.</p>
        } @else {
          <table>
            <thead>
              <tr>
                <th>Customer</th><th>Meter</th><th>Period</th><th class="num">Used (kWh)</th>
                <th class="num">Amount</th><th>Due</th><th>Status</th><th></th>
              </tr>
            </thead>
            <tbody>
              @for (b of bills(); track b.billId) {
                <tr>
                  <td>{{ b.customerName }}</td>
                  <td>{{ b.meterNumber }}</td>
                  <td>{{ b.periodStart | date: 'MMM d' }} - {{ b.periodEnd | date: 'MMM d, y' }}</td>
                  <td class="num">{{ b.kwhUsed | number: '1.2-2' }}</td>
                  <td class="num">{{ b.amount | currency }}</td>
                  <td>{{ b.dueDate | date: 'mediumDate' }}</td>
                  <td>
                    <span class="badge" [class.paid]="b.status === 'Paid'" [class.unpaid]="b.status === 'Unpaid'">
                      {{ b.status }}
                    </span>
                  </td>
                  <td>
                    @if (b.status === 'Unpaid') {
                      <button type="button" (click)="markPaid(b)">Mark paid</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>
    </main>
  `,
})
export class AdminBills {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly meters = signal<Meter[]>([]);
  protected readonly bills = signal<Bill[] | null>(null);
  protected readonly statusFilter = signal('');
  protected readonly saving = signal(false);
  protected readonly message = signal('');
  protected readonly isError = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    meterId: ['', Validators.required],
    periodStart: ['', Validators.required],
    periodEnd: ['', Validators.required],
  });

  constructor() {
    this.api.allMeters().subscribe({
      next: (list) => this.meters.set(list),
      error: (e) => this.show(errorText(e, 'Could not load meters.'), true),
    });
    this.loadBills();
  }

  protected changeFilter(status: string): void {
    this.statusFilter.set(status);
    this.loadBills();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.show('Choose a meter and both dates.', true);
      return;
    }

    this.saving.set(true);
    this.message.set('');

    // The date inputs already give YYYY-MM-DD, the format the API expects for a date.
    this.api.generateBill(this.form.getRawValue()).subscribe({
      next: (bill) => {
        this.saving.set(false);
        this.show(`Bill created for ${bill.customerName}: ${bill.amount.toFixed(2)} USD.`, false);
        this.loadBills();
      },
      error: (e) => {
        this.saving.set(false);
        this.show(errorText(e, 'Could not generate the bill.'), true);
      },
    });
  }

  protected markPaid(bill: Bill): void {
    if (!confirm(`Mark the ${bill.meterNumber} bill for ${bill.customerName} as paid?`)) {
      return;
    }

    this.api.payBill(bill.billId).subscribe({
      next: (paid) => {
        this.show(`Marked paid: ${paid.customerName}, ${paid.meterNumber}.`, false);
        this.loadBills();
      },
      error: (e) => this.show(errorText(e, 'Could not mark the bill as paid.'), true),
    });
  }

  private loadBills(): void {
    this.bills.set(null);
    this.api.allBills(this.statusFilter()).subscribe({
      next: (list) => this.bills.set(list),
      error: (e) => this.show(errorText(e, 'Could not load bills.'), true),
    });
  }

  private show(text: string, error: boolean): void {
    this.message.set(text);
    this.isError.set(error);
  }
}
