import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { RiskDashboardService } from './services/risk-dashboard.service';
import { RiskSummary, TransactionAlert, TransactionService } from './services/transaction.service';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { TransactionFormComponent } from './components/transaction-form/transaction-form.component';
import { AlertsPanelComponent } from './components/alerts-panel/alerts-panel.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DashboardComponent, TransactionFormComponent, AlertsPanelComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  private readonly transactionService = inject(TransactionService);
  private readonly riskDashboardService = inject(RiskDashboardService);

  // --- AUTHENTICATION & NAVIGATION STATE ---
  isLoggedIn = signal(false);
  mode = signal<'login' | 'signup'>('login');
  submitting = signal(false);
  status = signal('');
  statusTone = signal<'success' | 'error'>('success');
  apiErrorMessage = signal('');
  dashboardLoading = signal(false);
  alertsLoading = signal(false);
  riskSummary = signal<RiskSummary>({
    totalTransactionsProcessed: 0,
    highRiskAlertsCount: 0,
    averageFraudRiskScore: 0,
  });
  alertQueue = signal<TransactionAlert[]>([]);

  // --- FORMS ---
  loginForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(8)])
  });

  signupForm = new FormGroup({
    fullName: new FormControl('', [Validators.required]),
    organization: new FormControl('', [Validators.required]),
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(8)]),
    confirmPassword: new FormControl('', [Validators.required])
  });

  // --- AUTHENTICATION METHODS ---
  setMode(newMode: 'login' | 'signup') {
    this.mode.set(newMode);
    this.status.set(''); // Clear status when switching
  }

  hasError(formType: 'login' | 'signup', controlName: string, errorType: string) {
    const form = formType === 'login' ? this.loginForm : this.signupForm;
    const control = form.controls[controlName as keyof typeof form.controls];
    
    return control?.touched && control?.hasError(errorType);
  }

  get passwordMismatch(): boolean {
    const pass = this.signupForm.get('password')?.value;
    const confirm = this.signupForm.get('confirmPassword')?.value;
    return !!(pass && confirm && pass !== confirm);
  }

  submitLogin() {
    if (this.loginForm.valid) {
      this.submitting.set(true);
      this.status.set('Authenticating with Fraud Risk Engine...');
      
      setTimeout(() => {
        this.status.set('Login Successful!');
        this.statusTone.set('success');
        this.submitting.set(false);
        this.isLoggedIn.set(true); // Switches UI to Dashboard
        this.loadDashboard();
      }, 1500);
    }
  }

  submitSignup() {
    if (this.signupForm.valid && !this.passwordMismatch) {
      this.submitting.set(true);
      this.status.set('Creating secure profile...');

      setTimeout(() => {
        this.status.set('Account Created! Please Sign In.');
        this.statusTone.set('success');
        this.submitting.set(false);
        this.setMode('login');
      }, 1500);
    }
  }

  // --- EXISTING DASHBOARD DATA (Preserved) ---
  readonly capabilityCards = [
    {
      title: 'Transaction Logging',
      description: 'Capture transactional events with customer, device, and channel context for end-to-end traceability.',
      accent: 'var(--accent)',
    },
    {
      title: 'Pattern Analysis',
      description: 'Detect anomalies in spending velocity, login habits, merchant behavior, and location variance.',
      accent: 'var(--warn)',
    },
    {
      title: 'Fraud Risk Scoring',
      description: 'Score each transaction in real time using behavioral signals, historical trends, and configurable rules.',
      accent: 'var(--success)',
    },
    {
      title: 'Alert Generation',
      description: 'Trigger investigator-ready alerts with severity labels, score reasons, and recommended actions.',
      accent: '#f8d66d',
    },
  ];

  readonly scoringSignals = [
    { name: 'Velocity Spike', value: 87, state: 'critical' },
    { name: 'Device Mismatch', value: 71, state: 'warning' },
    { name: 'Geo Deviation', value: 63, state: 'warning' },
    { name: 'Merchant Risk', value: 42, state: 'stable' },
  ];

  readonly auditPoints = [
    'Immutable event timeline for every scored transaction',
    'Analyst notes and actions retained for compliance review',
    'Exportable audit reports for internal and external stakeholders',
  ];

  loadAlerts(): void {
    this.alertsLoading.set(true);
    this.transactionService.getAlerts(10).subscribe({
      next: (alerts) => {
        this.alertQueue.set(alerts);
        this.alertsLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.alertsLoading.set(false);
        this.apiErrorMessage.set(this.getFriendlyApiErrorMessage(error));
      },
    });
  }

  dismissApiError(): void {
    this.apiErrorMessage.set('');
  }

  private loadDashboard(): void {
    this.dashboardLoading.set(true);
    this.riskDashboardService.loadDashboard(10).subscribe({
      next: ({ summary, alerts }) => {
        this.riskSummary.set(summary);
        this.alertQueue.set(alerts);
        this.dashboardLoading.set(false);
        this.alertsLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.dashboardLoading.set(false);
        this.alertsLoading.set(false);
        this.apiErrorMessage.set(this.getFriendlyApiErrorMessage(error));
      },
    });
  }

  private getFriendlyApiErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Unable to connect to the fraud scoring API. Check that the backend is running on http://localhost:5257.';
    }

    if (error.status >= 500) {
      return 'The fraud scoring service is temporarily unavailable. Please retry in a moment.';
    }

    return error.error?.message ?? 'We could not reach the fraud scoring API. Check that the backend is running on http://localhost:5257 and try again.';
  }

}
