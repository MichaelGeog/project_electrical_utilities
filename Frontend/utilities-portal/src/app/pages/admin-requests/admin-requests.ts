import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { errorText } from '../../core/api-error';
import { ServiceRequest } from '../../core/api.models';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-admin-requests',
  imports: [DatePipe],
  template: `
    <main class="page">
      <h2>Service requests</h2>

      @if (message()) {
        <p [class]="isError() ? 'error' : 'success'" role="status">{{ message() }}</p>
      }

      <section class="card table-wrap">
        <div class="filter">
          <label for="filter">Show</label>
          <select id="filter" [value]="filter()" (change)="changeFilter($any($event.target).value)">
            <option value="">All requests</option>
            <option value="Open">Open</option>
            <option value="In Progress">In Progress</option>
            <option value="Resolved">Resolved</option>
          </select>
        </div>

        @if (requests() === null) {
          <p class="muted">Loading...</p>
        } @else if (requests()!.length === 0) {
          <p class="muted">No requests found.</p>
        } @else {
          <table>
            <thead>
              <tr><th>Sent</th><th>Customer</th><th>Type</th><th>Description</th><th>Status</th><th>Last update</th></tr>
            </thead>
            <tbody>
              @for (r of requests(); track r.id) {
                <tr>
                  <td>{{ r.createdAt | date: 'MMM d, y' }}</td>
                  <td>{{ r.customerName }}<br /><small class="muted">{{ r.accountNumber }}</small></td>
                  <td>{{ r.type }}</td>
                  <td class="desc">{{ r.description }}</td>
                  <td>
                    <select [value]="r.status" [attr.aria-label]="'Status of request from ' + r.customerName"
                            (change)="setStatus(r, $any($event.target).value)">
                      @for (s of statuses; track s) {
                        <option [value]="s" [selected]="s === r.status">{{ s }}</option>
                      }
                    </select>
                  </td>
                  <td>{{ r.updatedAt | date: 'MMM d, h:mm a' }}</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>
    </main>
  `,
  styles: `
    .desc { white-space: normal; min-width: 220px; max-width: 380px; }
    .filter { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
    .filter label { font-weight: 600; }
  `,
})
export class AdminRequests {
  private readonly api = inject(ApiService);

  protected readonly statuses = ['Open', 'In Progress', 'Resolved'];
  protected readonly requests = signal<ServiceRequest[] | null>(null);
  protected readonly filter = signal('');
  protected readonly message = signal('');
  protected readonly isError = signal(false);

  constructor() {
    this.load();
  }

  protected changeFilter(status: string): void {
    this.filter.set(status);
    this.load();
  }

  protected setStatus(request: ServiceRequest, status: string): void {
    if (status === request.status) {
      return;
    }

    this.api.updateRequestStatus(request.id, status).subscribe({
      next: (updated) => {
        this.show(`${updated.customerName}'s request is now ${updated.status}.`, false);
        this.load();
      },
      error: (e) => {
        this.show(errorText(e, 'Could not update the status.'), true);
        this.load(); // put the dropdown back to the saved value
      },
    });
  }

  private load(): void {
    this.api.allRequests(this.filter()).subscribe({
      next: (list) => this.requests.set(list),
      error: (e) => this.show(errorText(e, 'Could not load the requests.'), true),
    });
  }

  private show(text: string, error: boolean): void {
    this.message.set(text);
    this.isError.set(error);
  }
}
