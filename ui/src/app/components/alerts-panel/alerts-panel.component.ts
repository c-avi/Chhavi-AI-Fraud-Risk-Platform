import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { AlertItem } from '../../services/transaction.service';

@Component({
  selector: 'app-alerts-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alerts-panel.component.html',
  styleUrl: './alerts-panel.component.css',
})
export class AlertsPanelComponent {
  alerts = input.required<AlertItem[]>();
  loading = input.required<boolean>();

  refresh = output<void>();

  getLevelClass(level: AlertItem['riskLevel']): string {
    return level.toLowerCase();
  }
}
