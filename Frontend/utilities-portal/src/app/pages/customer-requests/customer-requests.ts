import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { errorText } from '../../core/api-error';
import { ServiceRequest } from '../../core/api.models';
import { ApiService } from '../../core/api.service';

@Component({
  selector: 'app-customer-requests',
  imports: [ReactiveFormsModule, DatePipe],
  template: `
    <main class="page">
      <h2>Support requests</h2>

      <section class="card">
        <h3>New request</h3>
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="row">
            <div class="field">
              <label for="type">Type</label>
              <select id="type" formControlName="type">
                @for (t of types; track t) {
                  <option [value]="t">{{ t }}</option>
                }
              </select>
            </div>
          </div>

          <div class="row">
            <div class="field wide">
              <label for="description">What's going on?</label>
              <textarea id="description" formControlName="description" maxlength="1000"
                        placeholder="Describe the problem..."></textarea>
              <small class="error">{{ err(form.controls.description) }}</small>
            </div>
          </div>

          <button class="primary" type="submit" [disabled]="saving()">
            {{ saving() ? 'Sending...' : 'Send request' }}
          </button>
          <span class="muted"> {{ form.controls.description.value.length }}/1000</span>
        </form>

        @if (message()) {
          <p [class]="isError() ? 'error' : 'success'" role="status">{{ message() }}</p>
        }
      </section>

      <section class="card table-wrap">
        <h3>My requests</h3>
        @if (requests() === null) {
          <p class="muted">Loading...</p>
        } @else if (requests()!.length === 0) {
          <p class="muted">You haven't sent any requests yet.</p>
        } @else {
          <table>
            <thead>
              <tr><th>Sent</th><th>Type</th><th>Description</th><th>Status</th><th>Last update</th></tr>
            </thead>
            <tbody>
              @for (r of requests(); track r.id) {
                <tr>
                  <td>{{ r.createdAt | date: 'MMM d, y' }}</td>
                  <td>{{ r.type }}</td>
                  <td class="desc">{{ r.description }}</td>
                  <td>
                    <span class="badge" [class.unpaid]="r.status === 'Open'"
                          [class.progress]="r.status === 'In Progress'" [class.paid]="r.status === 'Resolved'">
                      {{ r.status }}
                    </span>
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
    .desc { white-space: normal; min-width: 220px; max-width: 420px; }
    form .row { display: flex; margin-bottom: 4px; }
  `,
})
export class CustomerRequests {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly types = ['Outage', 'Billing', 'Meter Issue', 'Other'];
  protected readonly requests = signal<ServiceRequest[] | null>(null);
  protected readonly saving = signal(false);
  protected readonly message = signal('');
  protected readonly isError = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    type: ['Billing', Validators.required],
    description: ['', [Validators.required, Validators.maxLength(1000)]],
  });

  constructor() {
    this.load();
  }

  protected err(control: AbstractControl): string {
    if (!control.invalid || !(control.touched || control.dirty)) {
      return '';
    }
    return control.hasError('required') ? 'Please describe the problem.' : 'Too long.';
  }

  protected submit(): void {
    const { type, description } = this.form.getRawValue();
    if (this.form.invalid || description.trim() === '') {
      this.form.controls.description.markAsTouched();
      this.form.controls.description.setErrors({ required: true });
      this.show('Please describe the problem.', true);
      return;
    }

    this.saving.set(true);
    this.message.set('');

    this.api.createRequest({ type, description: description.trim() }).subscribe({
      next: () => {
        this.saving.set(false);
        this.form.reset({ type: 'Billing', description: '' });
        this.show('Request sent. We will update its status here.', false);
        this.load();
      },
      error: (e) => {
        this.saving.set(false);
        this.show(errorText(e, 'Could not send the request.'), true);
      },
    });
  }

  private load(): void {
    this.api.myRequests().subscribe({
      next: (list) => this.requests.set(list),
      error: (e) => this.show(errorText(e, 'Could not load your requests.'), true),
    });
  }

  private show(text: string, error: boolean): void {
    this.message.set(text);
    this.isError.set(error);
  }
}
