export type Role = 'Admin' | 'Customer';

/** Matches the LoginResponse the API returns (property names are camelCase in JSON). */
export interface LoginResponse {
  token: string;
  expiresAt: string;
  email: string;
  role: Role;
  customerId: string | null;
}

/** Matches MeterDto from the API. */
export interface Meter {
  meterId: string;
  meterNumber: string;
  status: string;
  installedDate: string;
  customerId: string;
  accountNumber: string;
  customerName: string;
  serviceAddress: string;
  latestReadingDate: string | null;
  latestKwhReading: number | null;
}
