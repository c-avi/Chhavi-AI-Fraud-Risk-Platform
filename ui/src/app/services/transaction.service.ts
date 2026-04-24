import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

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

export interface TransactionAlert {
  userId: string;
  riskScore: number;
  riskLevel: 'Low' | 'Medium' | 'High';
  timestamp: string;
}

export interface RiskSummary {
  totalTransactionsProcessed: number;
  highRiskAlertsCount: number;
  averageFraudRiskScore: number;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  private readonly baseUrl = `${environment.apiBaseUrl}/transaction`;

  constructor(private readonly http: HttpClient) {}

  submitTransaction(payload: TransactionPayload): Observable<TransactionResponse> {
    return this.http.post<TransactionResponse>(this.baseUrl, payload);
  }

  getRiskSummary(): Observable<RiskSummary> {
    return this.http.get<RiskSummary>(`${this.baseUrl}/summary`);
  }

  getAlerts(limit = 10): Observable<TransactionAlert[]> {
    return this.http.get<TransactionAlert[]>(`${this.baseUrl}/alerts?limit=${limit}`);
  }
}
