import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { AlertsStoreService } from '../../services/alerts-store.service';

@Component({
  selector: 'app-alerts-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alerts-panel.component.html',
  styleUrl: './alerts-panel.component.css',
})
export class AlertsPanelComponent implements OnInit {
  private readonly alertsStore = inject(AlertsStoreService);

  readonly alerts = this.alertsStore.alerts;
  readonly loading = this.alertsStore.loading;
  readonly errorMessage = this.alertsStore.errorMessage;

  ngOnInit(): void {
    this.alertsStore.start();
  }

  loadAlerts(): void {
    this.alertsStore.refreshNow();
  }
}
