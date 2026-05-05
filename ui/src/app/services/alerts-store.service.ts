import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, OnDestroy, signal } from '@angular/core';
import { Subscription, interval } from 'rxjs';
import { Alert, AlertService } from './alert.service';

@Injectable({
  providedIn: 'root',
})
export class AlertsStoreService implements OnDestroy {
  private readonly refreshIntervalMs = 15000;
  private readonly maxAlerts = 20;
  private refreshSubscription?: Subscription;

  readonly alerts = signal<Alert[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal('');

  constructor(private readonly alertService: AlertService) {}

  start(): void {
    if (this.refreshSubscription) {
      return;
    }

    this.fetchAlerts();
    this.refreshSubscription = interval(this.refreshIntervalMs).subscribe(() => this.fetchAlerts());
  }

  refreshNow(): void {
    this.fetchAlerts();
  }

  ngOnDestroy(): void {
    this.refreshSubscription?.unsubscribe();
    this.refreshSubscription = undefined;
  }

  private fetchAlerts(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.alertService.getAlerts(this.maxAlerts).subscribe({
      next: (alerts) => {
        this.alerts.set(alerts);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage.set(this.getFriendlyErrorMessage(error));
      },
    });
  }

  private getFriendlyErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Unable to connect to the fraud scoring API.';
    }

    return error.error?.message ?? 'Alerts could not be loaded. Please retry.';
  }
}
