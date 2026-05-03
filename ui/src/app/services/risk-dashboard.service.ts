import { Injectable } from '@angular/core';
import { forkJoin, Observable } from 'rxjs';
import { Alert, AlertService } from './alert.service';
import { RiskSummary, TransactionService } from './transaction.service';

export interface RiskDashboardData {
  summary: RiskSummary;
  alerts: Alert[];
}

@Injectable({
  providedIn: 'root',
})
export class RiskDashboardService {
  constructor(
    private readonly transactionService: TransactionService,
    private readonly alertService: AlertService
  ) {}

  loadDashboard(limit = 10): Observable<RiskDashboardData> {
    return forkJoin({
      summary: this.transactionService.getRiskSummary(),
      alerts: this.alertService.getAlerts(limit),
    });
  }
}
