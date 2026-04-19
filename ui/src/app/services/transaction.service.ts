import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TransactionPayload {
  userId: string;
  amount: number;
  location: string;
  timestamp: string;
}

export interface TransactionResponse {
  riskScore: number;
  riskLevel: 'Low' | 'Medium' | 'High';
}

export interface AlertItem {
  userId: string;
  riskScore: number;
  riskLevel: 'Low' | 'Medium' | 'High';
  timestamp: string;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  private readonly baseUrl = '/api/v1/transactions';

  constructor(private readonly http: HttpClient) {}

  submitTransaction(payload: TransactionPayload): Observable<TransactionResponse> {
    return this.http.post<TransactionResponse>(this.baseUrl, payload);
  }
}
