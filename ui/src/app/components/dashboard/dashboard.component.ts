import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { Alert } from '../../services/alert.service';
import { RiskSummary } from '../../services/transaction.service';

interface SummaryCard {
  label: string;
  value: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent {
  summary = input.required<RiskSummary>();
  alerts = input.required<Alert[]>();
  loading = input<boolean>(false);

  summaryCards(): SummaryCard[] {
    const summary = this.summary();

    return [
      {
        label: 'Total Transactions Processed',
        value: summary.totalTransactionsProcessed.toLocaleString(),
      },
      {
        label: 'High-Risk Alerts Count',
        value: summary.highRiskAlertsCount.toLocaleString(),
      },
      {
        label: 'Average Fraud Risk Score',
        value: summary.averageFraudRiskScore.toFixed(1),
      },
    ];
  }

  visibleAlerts(): Alert[] {
    return this.alerts().slice(0, 3);
  }

  riskMeterClass(score: number): string {
    if (score >= 75) {
      return 'high';
    }

    if (score >= 50) {
      return 'medium';
    }

    return 'low';
  }
}
