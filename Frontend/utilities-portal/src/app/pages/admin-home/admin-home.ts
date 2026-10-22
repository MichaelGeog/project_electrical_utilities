import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { errorText } from '../../core/api-error';
import { Customer } from '../../core/api.models';
import { ApiService } from '../../core/api.service';
import { Meter } from '../../core/auth.models';

@Component({
  selector: 'app-admin-home',
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  template: `
    <main class="page">
      <h2>Meters</h2>

      @if (message()) {
        <p [class]="isError() ? 'error' : 'success'" role="status">{{ message() }}</p>
      }

      <!-- Edit panel: only shown while a meter is being edited -->
      @if (editing(); as m) {
        <section class="card">
          <h3>Edit meter {{ m.meterNumber }}</h3>
          <p class="muted">
            A meter that already has bills can't move to another customer. Set it to Inactive and
            add a new meter for the new customer instead.
          </p>
          <form class="row" [formGroup]="editForm" (ngSubmit)="saveEdit()" novalidate>
            <div class="field">
              <label for="e-customer">Customer</label>
              <select id="e-customer" formControlName="customerId">
                @for (c of ownerChoices(); track c.customerId) {
                  <option [value]="c.customerId">{{ c.accountNumber }} - {{ c.fullName }}</option>
                }
              </select>
            </div>
            <div class="field">
              <label for="e-number">Meter number</label>
              <input id="e-number" formControlName="meterNumber" maxlength="20" />
              <small class="error">{{ err(editForm.controls.meterNumber) }}</small>
            </div>
            <div class="field">
              <label for="e-status">Status</label>
              <select id="e-status" formControlName="status">
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
              </select>
            </div>
            <button class="primary" type="submit" [disabled]="saving()">Save changes</button>
            <button type="button" (click)="editing.set(null)">Cancel</button>
          </form>
        </section>
      }

      <section class="card">
        <h3>Add a meter</h3>
        <form class="row" [formGroup]="createForm" (ngSubmit)="create()" novalidate>
          <div class="field">
            <label for="c-customer">Customer</label>
            <select id="c-customer" formControlName="customerId">
              <option value="">Choose a customer</option>
              @for (c of activeCustomers(); track c.customerId) {
                <option [value]="c.customerId">{{ c.accountNumber }} - {{ c.fullName }}</option>
              }
            </select>
            <small class="error">{{ err(createForm.controls.customerId) }}</small>
          </div>
          <div class="field">
            <label for="c-number">Meter number</label>
            <input id="c-number" formControlName="meterNumber" maxlength="20" placeholder="M-1006" />
            <small class="error">{{ err(createForm.controls.meterNumber) }}</small>
          </div>
          <div class="field">
            <label for="c-date">Installed (optional)</label>
            <input id="c-date" type="date" formControlName="installedDate" [max]="today" />
          </div>
          <button class="primary" type="submit" [disabled]="saving()">
            {{ saving() ? 'Saving...' : 'Add meter' }}
          </button>
        </form>
      </section>

      <section class="card table-wrap">
        @if (meters() === null) {
          <p class="muted">Loading...</p>
        } @else {
          <table>
            <thead>
              <tr>
                <th>Meter</th><th>Customer</th><th>Account</th><th>Address</th><th>Status</th>
                <th class="num">Latest (kWh)</th><th>Read on</th><th></th>
              </tr>
            </thead>
            <tbody>
              @for (m of meters(); track m.meterId) {
                <tr>
                  <td>{{ m.meterNumber }}</td>
                  <td>{{ m.customerName }}</td>
                  <td>{{ m.accountNumber }}</td>
                  <td>{{ m.serviceAddress }}</td>
                  <td><span class="badge" [class.paid]="m.status === 'Active'">{{ m.status }}</span></td>
                  <td class="num">{{ m.latestKwhReading === null ? '-' : (m.latestKwhReading | number: '1.2-2') }}</td>
                  <td>{{ m.latestReadingDate ? (m.latestReadingDate | date: 'mediumDate' : 'UTC') : '-' }}</td>
                  <td><button type="button" (click)="startEdit(m)">Edit</button></td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>
    </main>
  `,
  styles: `
    td button { padding: 4px 10px; border: 1px solid #c5ccd6; border-radius: 6px; background: #fff; cursor: pointer; }
    form.row button[type='button'] { padding: 10px 16px; border: 1px solid #c5ccd6; border-radius: 6px; background: #fff; cursor: pointer; }
    small.error { min-height: 1em; }
  `,
})
export class AdminHome {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly today = new Date().toLocaleDateString('en-CA');
  protected readonly meters = signal<Meter[] | null>(null);
  protected readonly customers = signal<Customer[]>([]);
  protected readonly editing = signal<Meter | null>(null);
  protected readonly saving = signal(false);
  protected readonly message = signal('');
  protected readonly isError = signal(false);

  /** Customers a new meter can be given to (deactivated ones can't own meters). */
  protected readonly activeCustomers = computed(() => this.customers().filter((c) => c.isActive));

  /** For the edit panel: active customers, plus the current owner even if deactivated. */
  protected readonly ownerChoices = computed(() => {
    const current = this.editing()?.customerId;
    return this.customers().filter((c) => c.isActive || c.customerId === current);
  });

  protected readonly createForm = this.fb.nonNullable.group({
    customerId: ['', Validators.required],
    meterNumber: ['', [Validators.required, Validators.maxLength(20)]],
    installedDate: [''],
  });

  protected readonly editForm = this.fb.nonNullable.group({
    customerId: ['', Validators.required],
    meterNumber: ['', [Validators.required, Validators.maxLength(20)]],
    status: ['Active', Validators.required],
  });

  constructor() {
    this.loadMeters();
    this.api.customers().subscribe({
      next: (list) => this.customers.set(list),
      error: (e) => this.show(errorText(e, 'Could not load customers.'), true),
    });
  }

  protected err(control: AbstractControl): string {
    if (!control.invalid || !(control.touched || control.dirty)) {
      return '';
    }
    if (control.hasError('required')) return 'Required.';
    if (control.hasError('maxlength')) return 'Too long.';
    return 'Invalid.';
  }

  protected create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      this.show('Fix the highlighted fields.', true);
      return;
    }

    const { customerId, meterNumber, installedDate } = this.createForm.getRawValue();
    this.saving.set(true);
    this.message.set('');

    this.api
      .createMeter({
        customerId,
        meterNumber: meterNumber.trim(),
        installedDate: installedDate || undefined,
      })
      .subscribe({
        next: (m) => {
          this.saving.set(false);
          this.createForm.reset();
          this.show(`Added meter ${m.meterNumber} to ${m.customerName}.`, false);
          this.loadMeters();
        },
        error: (e) => {
          this.saving.set(false);
          this.show(errorText(e, 'Could not add the meter.'), true);
        },
      });
  }

  protected startEdit(m: Meter): void {
    this.editing.set(m);
    this.editForm.reset({ customerId: m.customerId, meterNumber: m.meterNumber, status: m.status });
    this.message.set('');
  }

  protected saveEdit(): void {
    const m = this.editing();
    if (!m) {
      return;
    }
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      this.show('Fix the highlighted fields.', true);
      return;
    }

    const form = this.editForm.getRawValue();
    this.saving.set(true);
    this.api.updateMeter(m.meterId, { ...form, meterNumber: form.meterNumber.trim() }).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.editing.set(null);
        this.show(`Saved meter ${updated.meterNumber}.`, false);
        this.loadMeters();
      },
      error: (e) => {
        this.saving.set(false);
        // The API explains rules like "this meter already has bills" in plain words.
        this.show(errorText(e, 'Could not save the meter.'), true);
      },
    });
  }

  private loadMeters(): void {
    this.api.allMeters().subscribe({
      next: (list) => this.meters.set(list),
      error: (e) => this.show(errorText(e, 'Could not load meters.'), true),
    });
  }

  private show(text: string, error: boolean): void {
    this.message.set(text);
    this.isError.set(error);
  }
}
