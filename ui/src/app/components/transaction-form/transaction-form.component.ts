import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TransactionResponse } from '../../services/transaction.service';

@Component({
  selector: 'app-transaction-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './transaction-form.component.html',
  styleUrl: './transaction-form.component.css',
})
export class TransactionFormComponent {
  form = input.required<FormGroup>();
  submitting = input.required<boolean>();
  status = input.required<string>();
  statusTone = input.required<'success' | 'error'>();
  result = input<TransactionResponse | null>(null);

  formSubmit = output<void>();

  isFieldInvalid(fieldName: 'userId' | 'amount' | 'location' | 'timestamp'): boolean {
    const control = this.form().get(fieldName);
    return !!(control && control.touched && control.invalid);
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
}
