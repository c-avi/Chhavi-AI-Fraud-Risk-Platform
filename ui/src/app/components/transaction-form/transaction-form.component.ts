import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TransactionRequest, TransactionResponse, TransactionService } from '../../services/transaction.service';

@Component({
  selector: 'app-transaction-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './transaction-form.component.html',
  styleUrl: './transaction-form.component.css',
})
export class TransactionFormComponent {
  private readonly transactionService = inject(TransactionService);

  readonly form = new FormGroup({
    userId: new FormControl('', [Validators.required]),
    amount: new FormControl<number | null>(null, [Validators.required, Validators.min(1)]),
    location: new FormControl('', [Validators.required]),
    timestamp: new FormControl(this.getDateTimeLocalValue(), [Validators.required]),
  });
  readonly submitting = signal(false);
  readonly status = signal('');
  readonly statusTone = signal<'success' | 'error'>('success');
  readonly result = signal<TransactionResponse | null>(null);

  isFieldInvalid(fieldName: 'userId' | 'amount' | 'location' | 'timestamp'): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.touched && control.invalid);
  }

  submitTransaction(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue() as {
      userId: string;
      amount: number;
      location: string;
      timestamp: string;
    };
    const payload: TransactionRequest = {
      userId: raw.userId,
      amount: raw.amount,
      location: raw.location,
      timestamp: new Date(raw.timestamp).toISOString(),
    };

    this.submitting.set(true);
    this.statusTone.set('success');
    this.status.set('Submitting transaction for risk scoring...');

    this.transactionService.submitTransaction(payload).subscribe({
      next: (response) => {
        this.result.set(response);
        this.statusTone.set('success');
        this.status.set(
          `Transaction #${response.transactionId} scored ${response.riskScore} (${response.riskLevel}).`
        );
        this.submitting.set(false);
        this.form.controls.timestamp.setValue(this.getDateTimeLocalValue());
      },
      error: (error: HttpErrorResponse) => {
        this.result.set(null);
        this.statusTone.set('error');
        this.status.set(this.getFriendlyApiErrorMessage(error));
        this.submitting.set(false);
      },
    });
  }

  riskMeterClass(score: number): string {
    if (score >= 75) {
      return 'critical';
    }

    if (score >= 50) {
      return 'warning';
    }

    return 'safe';
  }

  private getFriendlyApiErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 400) {
      return 'Validation failed. Check User ID, amount, location, and timestamp.';
    }

    if (error.status === 0) {
      return 'Unable to connect to the fraud scoring API.';
    }

    if (error.status >= 500) {
      return 'Fraud scoring service is temporarily unavailable.';
    }

    return error.error?.message ?? 'Transaction scoring failed. Please retry.';
  }

  private getDateTimeLocalValue(): string {
    const current = new Date();
    current.setMinutes(current.getMinutes() - current.getTimezoneOffset());
    return current.toISOString().slice(0, 16);
  }
}
