import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { errorText } from '../../core/api-error';
import { ApiService } from '../../core/api.service';
import { Bill } from '../../core/api.models';

@Component({
  selector: 'app-customer-bills',
  imports: [CurrencyPipe, DatePipe, DecimalPipe],
  template: `
    <main class="page">
      <h2>My bills</h2>

      @if (error()) {
        <p class="error">{{ error() }}</p>
      } @else if (bills() === null) {
        <p class="muted">Loading...</p>
      } @else if (bills()!.length === 0) {
        <p class="muted">You have no bills yet.</p>
      } @else {
        <div class="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Meter</th><th>Period</th><th class="num">Used (kWh)</th>
                <th class="num">Amount</th><th>Due</th><th>Status</th>
              </tr>
            </thead>
            <tbody>
              @for (b of bills(); track b.billId) {
                <tr>
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
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </main>
  `,
})
export class CustomerBills {
  private readonly api = inject(ApiService);

  protected readonly bills = signal<Bill[] | null>(null);
  protected readonly error = signal('');

  constructor() {
    this.api.myBills().subscribe({
      next: (list) => this.bills.set(list),
      error: (e) => this.error.set(errorText(e, 'Could not load your bills.')),
    });
  }
}
