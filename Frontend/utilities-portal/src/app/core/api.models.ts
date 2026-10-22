/** One meter reading. kwhUsed is the difference from the reading before it (null for the first). */
export interface Reading {
  id: string;
  meterId: string;
  readingDate: string;
  kwhReading: number;
  kwhUsed: number | null;
}

/** Matches BillDto from the API. */
export interface Bill {
  billId: string;
  customerId: string;
  accountNumber: string;
  customerName: string;
  meterId: string;
  meterNumber: string;
  periodStart: string;
  periodEnd: string;
  kwhUsed: number;
  ratePerKwh: number;
  amount: number;
  status: 'Unpaid' | 'Paid';
  dueDate: string;
  paidDate: string | null;
}

/** Matches CustomerDto from the API (admin screens only). */
export interface Customer {
  customerId: string;
  accountNumber: string;
  fullName: string;
  email: string;
  phone: string;
  serviceAddress: string;
  region: string;
  isActive: boolean;
  activeMeterCount: number;
}

export interface CreateReadingRequest {
  meterId: string;
  readingDate: string;
  kwhReading: number;
}

export interface GenerateBillRequest {
  meterId: string;
  periodStart: string;
  periodEnd: string;
}

export interface CreateCustomerRequest {
  email: string;
  password: string;
  fullName: string;
  phone: string;
  serviceAddress: string;
  region: string;
}

export interface UpdateCustomerRequest {
  fullName: string;
  phone: string;
  serviceAddress: string;
  region: string;
}

export interface CreateMeterRequest {
  customerId: string;
  meterNumber: string;
  /** YYYY-MM-DD. Leave out to use today. */
  installedDate?: string;
}

export interface UpdateMeterRequest {
  customerId: string;
  meterNumber: string;
  status: string;
}
