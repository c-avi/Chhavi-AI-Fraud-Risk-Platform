import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Alert {
  id: number;
  transactionId: number;
  riskScore: number;
  message: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class AlertService {
  private readonly baseUrl = `${(environment as { apiUrl?: string; apiBaseUrl: string }).apiUrl ?? environment.apiBaseUrl}/alerts`;

  constructor(private readonly http: HttpClient) {}

  getAlerts(limit = 10): Observable<Alert[]> {
    return this.http.get<Alert[]>(`${this.baseUrl}?limit=${limit}`);
  }
}
