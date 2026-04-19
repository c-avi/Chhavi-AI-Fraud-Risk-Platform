import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { AlertItem } from '../../services/transaction.service';

export interface MetricItem {
  label: string;
  value: string;
  delta: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent {
  metrics = input.required<MetricItem[]>();
  alerts = input.required<AlertItem[]>();
}
