import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { AlertItem, TransactionPayload, TransactionResponse, TransactionService } from './services/transaction.service';
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

  // --- AUTHENTICATION & NAVIGATION STATE ---
  isLoggedIn = signal(false);
  mode = signal<'login' | 'signup'>('login');
  submitting = signal(false);
  status = signal('');
  statusTone = signal<'success' | 'error'>('success');
  transactionSubmitting = signal(false);
  transactionStatus = signal('');
  transactionStatusTone = signal<'success' | 'error'>('success');
  apiErrorMessage = signal('');
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
    userId: new FormControl('', [Validators.required]),
    amount: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
    location: new FormControl('', [Validators.required]),
    timestamp: new FormControl(this.getDateTimeLocalValue(), [Validators.required]),
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
    { userId: 'user-042', riskScore: 92, riskLevel: 'High', timestamp: new Date(Date.now() - 120000).toISOString() },
    { userId: 'user-128', riskScore: 65, riskLevel: 'Medium', timestamp: new Date(Date.now() - 240000).toISOString() },
    { userId: 'user-210', riskScore: 22, riskLevel: 'Low', timestamp: new Date(Date.now() - 360000).toISOString() },
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
    setTimeout(() => this.alertsLoading.set(false), 300);
  }

  submitTransaction(): void {
    if (this.transactionForm.invalid) {
      this.transactionForm.markAllAsTouched();
      return;
    }

    this.apiErrorMessage.set('');
    this.transactionSubmitting.set(true);
    this.transactionStatus.set('Submitting transaction for risk scoring...');
    this.transactionStatusTone.set('success');

    const rawPayload = this.transactionForm.getRawValue() as {
      userId: string;
      amount: number;
      location: string;
      timestamp: string;
    };
    const payload: TransactionPayload = {
      userId: rawPayload.userId,
      amount: rawPayload.amount,
      location: rawPayload.location,
      timestamp: new Date(rawPayload.timestamp).toISOString(),
    };

    this.transactionService.submitTransaction(payload).subscribe({
      next: (response) => {
        this.transactionResult.set(response);
        this.transactionStatusTone.set('success');
        this.transactionStatus.set(
          `Risk score updated: ${response.riskScore} (${response.riskLevel})`
        );
        this.transactionSubmitting.set(false);
        this.alertQueue.update((items) => [
          {
            userId: payload.userId,
            riskScore: response.riskScore,
            riskLevel: response.riskLevel,
            timestamp: payload.timestamp,
          },
          ...items,
        ]);
        this.transactionForm.controls.timestamp.setValue(this.getDateTimeLocalValue());
      },
      error: (error: HttpErrorResponse) => {
        this.transactionStatusTone.set('error');
        this.transactionStatus.set('Transaction scoring failed. Please retry.');
        this.apiErrorMessage.set(this.getFriendlyApiErrorMessage(error));
        this.transactionResult.set(null);
        this.transactionSubmitting.set(false);
      },
    });
  }

  dismissApiError(): void {
    this.apiErrorMessage.set('');
  }

  private getFriendlyApiErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Unable to connect to the fraud scoring API. Check that the backend is running on http://localhost:5257.';
    }

    if (error.status >= 500) {
      return 'The fraud scoring service is temporarily unavailable. Please retry in a moment.';
    }

    return 'We could not reach the fraud scoring API. Check that the backend is running on http://localhost:5257 and try again.';
  }

  private getDateTimeLocalValue(): string {
    const current = new Date();
    current.setMinutes(current.getMinutes() - current.getTimezoneOffset());
    return current.toISOString().slice(0, 16);
  }
}