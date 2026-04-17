import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { ApiService, AlertItem, TransactionPayload, TransactionResponse } from './services/api.service';
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
  private readonly apiService = inject(ApiService);

  // --- AUTHENTICATION & NAVIGATION STATE ---
  isLoggedIn = signal(false);
  mode = signal<'login' | 'signup'>('login');
  submitting = signal(false);
  status = signal('');
  statusTone = signal<'success' | 'error'>('success');
  transactionSubmitting = signal(false);
  transactionStatus = signal('');
  transactionStatusTone = signal<'success' | 'error'>('success');
  alertsLoading = signal(false);
  transactionResult = signal<TransactionResponse | null>(null);

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

  transactionForm = new FormGroup({
    transactionId: new FormControl('', [Validators.required]),
    customerId: new FormControl('', [Validators.required]),
    amount: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
    merchant: new FormControl('', [Validators.required]),
    channel: new FormControl('card_present', [Validators.required]),
    location: new FormControl('', [Validators.required]),
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
        this.loadAlerts();
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
  readonly liveMetrics = [
    { label: 'Transactions Processed', value: '1.28M', delta: '+12.4%' },
    { label: 'High-Risk Alerts', value: '214', delta: '-8.1%' },
    { label: 'Model Confidence', value: '96.7%', delta: '+2.3%' },
    { label: 'Audit Coverage', value: '99.1%', delta: '+0.9%' },
  ];

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

  readonly alertQueue = signal<AlertItem[]>([
    { id: 'ALT-2048', customer: 'Northbridge Retail', score: 92, status: 'Immediate review' },
    { id: 'ALT-2054', customer: 'Atlas Payments', score: 86, status: 'Escalated' },
    { id: 'ALT-2058', customer: 'Halo Commerce', score: 78, status: 'Monitor closely' },
  ]);

  readonly auditPoints = [
    'Immutable event timeline for every scored transaction',
    'Analyst notes and actions retained for compliance review',
    'Exportable audit reports for internal and external stakeholders',
  ];

  hasTransactionError(controlName: string, errorType: string): boolean {
    const control = this.transactionForm.controls[controlName as keyof typeof this.transactionForm.controls];
    return !!(control?.touched && control?.hasError(errorType));
  }

  loadAlerts(): void {
    this.alertsLoading.set(true);
    this.apiService.getAlerts().subscribe({
      next: (alerts) => {
        if (alerts.length > 0) {
          this.alertQueue.set(alerts);
        }
        this.alertsLoading.set(false);
      },
      error: () => {
        this.alertsLoading.set(false);
      },
    });
  }

  submitTransaction(): void {
    if (this.transactionForm.invalid) {
      this.transactionForm.markAllAsTouched();
      return;
    }

    this.transactionSubmitting.set(true);
    this.transactionStatus.set('Submitting transaction for risk scoring...');
    this.transactionStatusTone.set('success');

    const payload = this.transactionForm.getRawValue() as TransactionPayload;

    this.apiService.submitTransaction(payload).subscribe({
      next: (response) => {
        this.transactionResult.set(response);
        this.transactionStatusTone.set('success');
        this.transactionStatus.set(
          `Risk score updated: ${response.riskScore} (${response.riskLevel})`
        );
        this.transactionSubmitting.set(false);
        this.loadAlerts();
      },
      error: () => {
        this.transactionStatusTone.set('error');
        this.transactionStatus.set('Transaction scoring failed. Please retry.');
        this.transactionSubmitting.set(false);
      },
    });
  }
}