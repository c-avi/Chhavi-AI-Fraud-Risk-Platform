import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TransactionPayload {
  transactionId: string;
  customerId: string;
  amount: number;
  merchant: string;
  channel: string;
  location: string;
}

export interface TransactionResponse {
  transactionId: string;
  riskScore: number;
  riskLevel: string;
  flagged?: boolean;
}

export interface AlertItem {
  id: string;
  customer: string;
  score: number;
  status: string;
}

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  private readonly baseUrl = '/api/v1';

  constructor(private readonly http: HttpClient) {}

  submitTransaction(payload: TransactionPayload): Observable<TransactionResponse> {
    return this.http.post<TransactionResponse>(`${this.baseUrl}/transactions`, payload);
  }

  getAlerts(): Observable<AlertItem[]> {
    return this.http.get<AlertItem[]>(`${this.baseUrl}/alerts`);
  }
}
