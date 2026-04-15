import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  readonly liveMetrics = [
    { label: 'Transactions Processed', value: '1.28M', delta: '+12.4%' },
    { label: 'High-Risk Alerts', value: '214', delta: '-8.1%' },
    { label: 'Model Confidence', value: '96.7%', delta: '+2.3%' },
    { label: 'Audit Coverage', value: '99.1%', delta: '+0.9%' },
  ];

  readonly capabilityCards = [
    {
      title: 'Transaction Logging',
      description:
        'Capture transactional events with customer, device, and channel context for end-to-end traceability.',
      accent: 'var(--accent)',
    },
    {
      title: 'Pattern Analysis',
      description:
        'Detect anomalies in spending velocity, login habits, merchant behavior, and location variance.',
      accent: 'var(--warn)',
    },
    {
      title: 'Fraud Risk Scoring',
      description:
        'Score each transaction in real time using behavioral signals, historical trends, and configurable rules.',
      accent: 'var(--success)',
    },
    {
      title: 'Alert Generation',
      description:
        'Trigger investigator-ready alerts with severity labels, score reasons, and recommended actions.',
      accent: '#f8d66d',
    },
  ];

  readonly scoringSignals = [
    { name: 'Velocity Spike', value: 87, state: 'critical' },
    { name: 'Device Mismatch', value: 71, state: 'warning' },
    { name: 'Geo Deviation', value: 63, state: 'warning' },
    { name: 'Merchant Risk', value: 42, state: 'stable' },
  ];

  readonly alertQueue = [
    { id: 'ALT-2048', customer: 'Northbridge Retail', score: 92, status: 'Immediate review' },
    { id: 'ALT-2054', customer: 'Atlas Payments', score: 86, status: 'Escalated' },
    { id: 'ALT-2058', customer: 'Halo Commerce', score: 78, status: 'Monitor closely' },
  ];

  readonly auditPoints = [
    'Immutable event timeline for every scored transaction',
    'Analyst notes and actions retained for compliance review',
    'Exportable audit reports for internal and external stakeholders',
  ];
}
