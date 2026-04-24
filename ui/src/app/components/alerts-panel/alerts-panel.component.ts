import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { TransactionAlert } from '../../services/transaction.service';

@Component({
  selector: 'app-alerts-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alerts-panel.component.html',
  styleUrl: './alerts-panel.component.css',
})
export class AlertsPanelComponent {
  alerts = input.required<TransactionAlert[]>();
  loading = input.required<boolean>();

  refresh = output<void>();

  getLevelClass(level: TransactionAlert['riskLevel']): string {
    return level.toLowerCase();
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
