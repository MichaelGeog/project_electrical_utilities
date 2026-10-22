import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { errorText } from '../../core/api-error';
import { Customer } from '../../core/api.models';
import { ApiService } from '../../core/api.service';
import { REGIONS } from '../../core/regions';

@Component({
  selector: 'app-admin-customers',
  imports: [ReactiveFormsModule],
  template: `
    <main class="page">
      <h2>Customers</h2>

      @if (message()) {
        <p [class]="isError() ? 'error' : 'success'" role="status">{{ message() }}</p>
      }

      <!-- Edit panel: only shown while a customer is being edited -->
      @if (editing(); as c) {
        <section class="card">
          <h3>Edit {{ c.fullName }} ({{ c.accountNumber }})</h3>
          <p class="muted">{{ c.email }} - the email and account number can't be changed.</p>
          <form class="row" [formGroup]="editForm" (ngSubmit)="saveEdit()" novalidate>
            <div class="field">
              <label for="e-name">Full name</label>
              <input id="e-name" formControlName="fullName" />
              <small class="error">{{ err(editForm.controls.fullName) }}</small>
            </div>
            <div class="field">
              <label for="e-phone">Phone (10 digits)</label>
              <input id="e-phone" type="tel" maxlength="10" formControlName="phone" />
              <small class="error">{{ err(editForm.controls.phone) }}</small>
            </div>
            <div class="field">
              <label for="e-address">Service address</label>
              <input id="e-address" formControlName="serviceAddress" />
              <small class="error">{{ err(editForm.controls.serviceAddress) }}</small>
            </div>
            <div class="field">
              <label for="e-region">Region</label>
              <select id="e-region" formControlName="region">
                @for (r of regions; track r) {
                  <option [value]="r">{{ r }}</option>
                }
              </select>
            </div>
            <button class="primary" type="submit" [disabled]="saving()">Save changes</button>
            <button type="button" (click)="editing.set(null)">Cancel</button>
          </form>
        </section>
      }

      <section class="card">
        <h3>Add a customer</h3>
        <p class="muted">
          This creates the customer's login too. Give them the temporary password; the account
          number is assigned automatically.
        </p>
        <form class="row" [formGroup]="createForm" (ngSubmit)="create()" novalidate>
          <div class="field">
            <label for="c-email">Email</label>
            <input id="c-email" type="email" formControlName="email" autocomplete="off" />
            <small class="error">{{ err(createForm.controls.email) }}</small>
          </div>
          <div class="field">
            <label for="c-pass">Temporary password</label>
            <input id="c-pass" type="text" formControlName="password" autocomplete="off" />
            <small class="error">{{ err(createForm.controls.password) }}</small>
          </div>
          <div class="field">
            <label for="c-name">Full name</label>
            <input id="c-name" formControlName="fullName" />
            <small class="error">{{ err(createForm.controls.fullName) }}</small>
          </div>
          <div class="field">
            <label for="c-phone">Phone (10 digits)</label>
            <input id="c-phone" type="tel" maxlength="10" formControlName="phone" />
            <small class="error">{{ err(createForm.controls.phone) }}</small>
          </div>
          <div class="field">
            <label for="c-address">Service address</label>
            <input id="c-address" formControlName="serviceAddress" />
            <small class="error">{{ err(createForm.controls.serviceAddress) }}</small>
          </div>
          <div class="field">
            <label for="c-region">Region</label>
            <select id="c-region" formControlName="region">
              <option value="">Choose a region</option>
              @for (r of regions; track r) {
                <option [value]="r">{{ r }}</option>
              }
            </select>
            <small class="error">{{ err(createForm.controls.region) }}</small>
          </div>
          <button class="primary" type="submit" [disabled]="saving()">
            {{ saving() ? 'Saving...' : 'Add customer' }}
          </button>
        </form>
      </section>

      <section class="card table-wrap">
        @if (customers() === null) {
          <p class="muted">Loading...</p>
        } @else if (customers()!.length === 0) {
          <p class="muted">No customers yet.</p>
        } @else {
          <table>
            <thead>
              <tr>
                <th>Account</th><th>Name</th><th>Email</th><th>Phone</th><th>Region</th>
                <th class="num">Meters</th><th>Status</th><th></th>
              </tr>
            </thead>
            <tbody>
              @for (c of customers(); track c.customerId) {
                <tr>
                  <td>{{ c.accountNumber }}</td>
                  <td>{{ c.fullName }}</td>
                  <td>{{ c.email }}</td>
                  <td>{{ c.phone }}</td>
                  <td>{{ c.region }}</td>
                  <td class="num">{{ c.activeMeterCount }}</td>
                  <td>
                    <span class="badge" [class.paid]="c.isActive" [class.unpaid]="!c.isActive">
                      {{ c.isActive ? 'Active' : 'Deactivated' }}
                    </span>
                  </td>
                  <td>
                    <button type="button" (click)="startEdit(c)">Edit</button>
                    <button type="button" (click)="toggleActive(c)">
                      {{ c.isActive ? 'Deactivate' : 'Reactivate' }}
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>
    </main>
  `,
  styles: `
    td button { margin-right: 6px; padding: 4px 10px; border: 1px solid #c5ccd6; border-radius: 6px; background: #fff; cursor: pointer; }
    form.row button[type='button'] { padding: 10px 16px; border: 1px solid #c5ccd6; border-radius: 6px; background: #fff; cursor: pointer;}
    small.error { min-height: 1em; }
  `,
})
export class AdminCustomers {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly regions = REGIONS;
  protected readonly customers = signal<Customer[] | null>(null);
  protected readonly editing = signal<Customer | null>(null);
  protected readonly saving = signal(false);
  protected readonly message = signal('');
  protected readonly isError = signal(false);

  protected readonly createForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    phone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    serviceAddress: ['', [Validators.required, Validators.maxLength(200)]],
    region: ['', Validators.required],
  });

  protected readonly editForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    phone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    serviceAddress: ['', [Validators.required, Validators.maxLength(200)]],
    region: ['', Validators.required],
  });

  constructor() {
    this.load();
  }

  /** The first problem with a field, or '' while it's fine or untouched. */
  protected err(control: AbstractControl): string {
    if (!control.invalid || !(control.touched || control.dirty)) {
      return '';
    }
    if (control.hasError('required')) return 'Required.';
    if (control.hasError('email')) return 'Enter a valid email.';
    if (control.hasError('minlength')) return 'At least 8 characters.';
    if (control.hasError('maxlength')) return 'Too long.';
    if (control.hasError('pattern')) return 'Exactly 10 digits.';
    return 'Invalid.';
  }

  protected create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      this.show('Fix the highlighted fields.', true);
      return;
    }

    this.saving.set(true);
    this.message.set('');

    const form = this.createForm.getRawValue();
    this.api
      .createCustomer({
        ...form,
        email: form.email.trim(),
        fullName: form.fullName.trim(),
        serviceAddress: form.serviceAddress.trim(),
      })
      .subscribe({
        next: (c) => {
          this.saving.set(false);
          this.createForm.reset();
          this.show(`Added ${c.fullName} (${c.accountNumber}).`, false);
          this.load();
        },
        error: (e) => {
          this.saving.set(false);
          this.show(errorText(e, 'Could not add the customer.'), true);
        },
      });
  }

  protected startEdit(c: Customer): void {
    this.editing.set(c);
    this.editForm.reset({
      fullName: c.fullName,
      phone: c.phone,
      serviceAddress: c.serviceAddress,
      region: c.region,
    });
    this.message.set('');
  }

  protected saveEdit(): void {
    const c = this.editing();
    if (!c) {
      return;
    }
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      this.show('Fix the highlighted fields.', true);
      return;
    }

    this.saving.set(true);
    this.api.updateCustomer(c.customerId, this.editForm.getRawValue()).subscribe({
      next: (updated) => {
        this.saving.set(false);
        this.editing.set(null);
        this.show(`Saved changes for ${updated.fullName}.`, false);
        this.load();
      },
      error: (e) => {
        this.saving.set(false);
        this.show(errorText(e, 'Could not save the changes.'), true);
      },
    });
  }

  protected toggleActive(c: Customer): void {
    const turningOff = c.isActive;
    if (
      turningOff &&
      !confirm(`Deactivate ${c.fullName}? They won't be able to log in. Their records are kept.`)
    ) {
      return;
    }

    this.api.setCustomerActive(c.customerId, !c.isActive).subscribe({
      next: (updated) => {
        this.show(`${updated.fullName} is now ${updated.isActive ? 'active' : 'deactivated'}.`, false);
        this.load();
      },
      error: (e) => this.show(errorText(e, 'Could not change the status.'), true),
    });
  }

  private load(): void {
    this.api.customers().subscribe({
      next: (list) => this.customers.set(list),
      error: (e) => this.show(errorText(e, 'Could not load customers.'), true),
    });
  }

  private show(text: string, error: boolean): void {
    this.message.set(text);
    this.isError.set(error);
  }
}
