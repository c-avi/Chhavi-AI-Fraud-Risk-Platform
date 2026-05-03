import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Alert, AlertService } from '../../services/alert.service';

@Component({
  selector: 'app-alerts-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alerts-panel.component.html',
  styleUrl: './alerts-panel.component.css',
})
export class AlertsPanelComponent implements OnInit {
  private readonly alertService = inject(AlertService);

  alerts = signal<Alert[]>([]);
  loading = signal(false);
  errorMessage = signal('');

  ngOnInit(): void {
    this.loadAlerts();
  }

  loadAlerts(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.alertService.getAlerts(10).subscribe({
      next: (alerts) => {
        this.alerts.set(alerts);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage.set(error.status === 0
          ? 'Unable to connect to the fraud scoring API.'
          : 'Alerts could not be loaded. Please retry.');
      },
    });
  }

  scoreClass(score: number): string {
    return score >= 70 ? 'high' : 'low';
  }

  riskMeterClass(score: number): string {
    if (score >= 70) {
      return 'high';
    }

    if (score >= 50) {
      return 'medium';
    }

    return 'low';
  }
}
