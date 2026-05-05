import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { AppComponent } from './app/app.component';
import { bankingErrorInterceptor } from './app/interceptors/banking-error.interceptor';

bootstrapApplication(AppComponent, {
  providers: [provideAnimations(), provideHttpClient(withInterceptors([bankingErrorInterceptor]))],
}).catch((error) => console.error(error));
