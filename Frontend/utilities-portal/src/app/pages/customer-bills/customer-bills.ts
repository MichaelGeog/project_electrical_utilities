import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { errorText } from '../../core/api-error';
import { Bill } from '../../core/api.models';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-customer-bills',
  imports: [CurrencyPipe, DatePipe, DecimalPipe],
  template: `
    <main class="page">
      <h2>My bills</h2>

      @if (message()) {
        <p [class]="isError() ? 'error' : 'success'" role="status">{{ message() }}</p>
      }

      @if (bills() === null) {
        <p class="muted">Loading...</p>
      } @else if (bills()!.length === 0) {
        <p class="muted">You have no bills yet.</p>
      } @else {
        <div class="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Meter</th><th>Period</th><th class="num">Used (kWh)</th>
                <th class="num">Amount</th><th>Due</th><th>Status</th><th></th>
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
                    @if (b.paidDate) {
                      <small class="muted">on {{ b.paidDate | date: 'mediumDate' }}</small>
                    }
                  </td>
                  <td>
                    @if (b.status === 'Unpaid') {
                      <button type="button" [disabled]="payingId() === b.billId" (click)="pay(b)">
                        {{ payingId() === b.billId ? 'Paying...' : 'Pay now' }}
                      </button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <p class="muted">This is a demo: paying a bill only marks it as paid. No money moves.</p>
      }
    </main>
  `,
})
export class CustomerBills {
  private readonly api = inject(ApiService);

  protected readonly bills = signal<Bill[] | null>(null);
  protected readonly payingId = signal('');
  protected readonly message = signal('');
  protected readonly isError = signal(false);

  constructor() {
    this.load();
  }

  protected pay(bill: Bill): void {
    if (!confirm(`Pay ${bill.amount.toFixed(2)} USD for meter ${bill.meterNumber}? (demo only, no real payment)`)) {
      return;
    }

    this.payingId.set(bill.billId);
    this.message.set('');

    this.api.payBill(bill.billId).subscribe({
      next: () => {
        this.payingId.set('');
        this.show('Payment recorded. Thank you!', false);
        this.load();
      },
      error: (e) => {
        this.payingId.set('');
        this.show(errorText(e, 'Could not record the payment.'), true);
      },
    });
  }

  private load(): void {
    this.api.myBills().subscribe({
      next: (list) => this.bills.set(list),
      error: (e) => this.show(errorText(e, 'Could not load your bills.'), true),
    });
  }

  private show(text: string, error: boolean): void {
    this.message.set(text);
    this.isError.set(error);
  }
}
