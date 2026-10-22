/** One meter reading. kwhUsed is the difference from the reading before it
 * (null for the first). */
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
