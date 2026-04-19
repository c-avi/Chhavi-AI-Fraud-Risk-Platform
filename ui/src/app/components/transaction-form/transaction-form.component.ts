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
}
