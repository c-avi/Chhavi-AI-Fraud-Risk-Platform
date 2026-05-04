import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable, filter, map, of, switchMap, take, timer } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TransactionRequest {
  userId: string;
  amount: number;
  location: string;
  timestamp: string | null;
}

export interface TransactionResponse {
  transactionId: number;
  userId?: string;
  amount?: number;
  location?: string;
  timestamp?: string;
  riskScore: number;
  riskLevel: string;
  scoringStatus?: string;
  scoringError?: string | null;
}

export interface RiskSummary {
  totalTransactionsProcessed: number;
  highRiskAlertsCount: number;
  averageFraudRiskScore: number;
}

interface RiskScorePayload {
  transactionId: number;
  riskScore: number;
  riskLevel: string;
}

interface AcceptedPayload {
  transactionId: number;
  status: string;
  statusUrl: string;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  private readonly baseUrl = `${(environment as { apiUrl?: string; apiBaseUrl: string }).apiUrl ?? environment.apiBaseUrl}/transactions`;

  constructor(private readonly http: HttpClient) {}

  submitTransaction(data: TransactionRequest): Observable<TransactionResponse> {
    const idempotencyKey =
      typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
        ? crypto.randomUUID()
        : `${Date.now()}-${Math.random().toString(36).slice(2)}`;

    return this.http
      .post<RiskScorePayload | AcceptedPayload>(this.baseUrl, data, {
        observe: 'response',
        headers: { 'Idempotency-Key': idempotencyKey },
      })
      .pipe(
        switchMap((res: HttpResponse<RiskScorePayload | AcceptedPayload>) => {
          if (res.status === 200 && res.body && this.isRiskScorePayload(res.body)) {
            return of(this.mapReplayToResponse(res.body));
          }

          if (res.status === 202 && res.body && this.isAcceptedPayload(res.body)) {
            return this.pollUntilSettled(res.body.transactionId);
          }

          throw new Error(`Unexpected fraud scoring response (${res.status}).`);
        })
      );
  }

  getTransaction(transactionId: number): Observable<TransactionResponse> {
    return this.http.get<TransactionResponse>(`${this.baseUrl}/${transactionId}`);
  }

  getRiskSummary(): Observable<RiskSummary> {
    return this.http.get<RiskSummary>(`${this.baseUrl}/summary`);
  }

  private pollUntilSettled(transactionId: number): Observable<TransactionResponse> {
    return timer(0, 400).pipe(
      switchMap(() => this.getTransaction(transactionId)),
      filter((t) => t.scoringStatus !== 'Pending'),
      take(1)
    );
  }

  private mapReplayToResponse(body: RiskScorePayload): TransactionResponse {
    return {
      transactionId: body.transactionId,
      riskScore: body.riskScore,
      riskLevel: body.riskLevel,
      scoringStatus: 'Completed',
    };
  }

  private isRiskScorePayload(
    body: RiskScorePayload | AcceptedPayload
  ): body is RiskScorePayload {
    return (
      typeof (body as RiskScorePayload).riskScore === 'number' &&
      typeof (body as RiskScorePayload).riskLevel === 'string'
    );
  }

  private isAcceptedPayload(
    body: RiskScorePayload | AcceptedPayload
  ): body is AcceptedPayload {
    return typeof (body as AcceptedPayload).statusUrl === 'string';
  }
}
