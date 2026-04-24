import { Injectable } from '@angular/core';
import { forkJoin, Observable } from 'rxjs';
import { RiskSummary, TransactionAlert, TransactionService } from './transaction.service';

export interface RiskDashboardData {
  summary: RiskSummary;
  alerts: TransactionAlert[];
}

@Injectable({
  providedIn: 'root',
})
export class RiskDashboardService {
  constructor(private readonly transactionService: TransactionService) {}

  loadDashboard(limit = 10): Observable<RiskDashboardData> {
    return forkJoin({
      summary: this.transactionService.getRiskSummary(),
      alerts: this.transactionService.getAlerts(limit),
    });
  }
}
