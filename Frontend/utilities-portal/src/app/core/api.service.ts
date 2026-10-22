import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Meter } from './auth.models';
import {
  Bill,
  CreateCustomerRequest,
  CreateMeterRequest,
  CreateReadingRequest,
  CreateServiceRequest,
  Customer,
  GenerateBillRequest,
  Outage,
  Reading,
  ServiceRequest,
  UpdateCustomerRequest,
  UpdateMeterRequest,
} from './api.models';

/** Every call to the Web API lives here, so the pages never build URLs themselves. */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // Meters and readings
  myMeters(): Observable<Meter[]> {
    return this.http.get<Meter[]>(`${this.base}/meters/my`);
  }

  allMeters(): Observable<Meter[]> {
    return this.http.get<Meter[]>(`${this.base}/meters`);
  }

  createMeter(request: CreateMeterRequest): Observable<Meter> {
    return this.http.post<Meter>(`${this.base}/meters`, request);
  }

  updateMeter(meterId: string, request: UpdateMeterRequest): Observable<Meter> {
    return this.http.put<Meter>(`${this.base}/meters/${meterId}`, request);
  }

  readings(meterId: string): Observable<Reading[]> {
    return this.http.get<Reading[]>(`${this.base}/meters/${meterId}/readings`);
  }

  createReading(request: CreateReadingRequest): Observable<Reading> {
    return this.http.post<Reading>(`${this.base}/readings`, request);
  }

  // Bills
  myBills(): Observable<Bill[]> {
    return this.http.get<Bill[]>(`${this.base}/bills/my`);
  }

  allBills(status: string): Observable<Bill[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<Bill[]>(`${this.base}/bills`, { params });
  }

  generateBill(request: GenerateBillRequest): Observable<Bill> {
    return this.http.post<Bill>(`${this.base}/bills/generate`, request);
  }

  // Customers (admin)
  customers(): Observable<Customer[]> {
    return this.http.get<Customer[]>(`${this.base}/customers`);
  }

  createCustomer(request: CreateCustomerRequest): Observable<Customer> {
    return this.http.post<Customer>(`${this.base}/customers`, request);
  }

  updateCustomer(customerId: string, request: UpdateCustomerRequest): Observable<Customer> {
    return this.http.put<Customer>(`${this.base}/customers/${customerId}`, request);
  }

  setCustomerActive(customerId: string, isActive: boolean): Observable<Customer> {
    return this.http.put<Customer>(`${this.base}/customers/${customerId}/active`, { isActive });
  }

  payBill(billId: string): Observable<Bill> {
    return this.http.post<Bill>(`${this.base}/bills/${billId}/pay`, {});
  }

  // Service requests
  myRequests(): Observable<ServiceRequest[]> {
    return this.http.get<ServiceRequest[]>(`${this.base}/service-requests/my`);
  }

  createRequest(request: CreateServiceRequest): Observable<ServiceRequest> {
    return this.http.post<ServiceRequest>(`${this.base}/service-requests`, request);
  }

  allRequests(status: string): Observable<ServiceRequest[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<ServiceRequest[]>(`${this.base}/service-requests`, { params });
  }

  updateRequestStatus(requestId: string, status: string): Observable<ServiceRequest> {
    return this.http.put<ServiceRequest>(`${this.base}/service-requests/${requestId}/status`, { status });
  }

  // Outage panel (simulated data)
  outages(): Observable<Outage[]> {
    return this.http.get<Outage[]>(`${this.base}/outages`);
  }
}
