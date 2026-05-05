import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const bankingErrorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 429 && error.status !== 403) {
        return throwError(() => error);
      }

      const message = error.status === 429
        ? buildRateLimitMessage(error)
        : 'Access to this protected banking resource is forbidden for your current role.';

      return throwError(() => new HttpErrorResponse({
        headers: error.headers,
        status: error.status,
        statusText: error.statusText,
        url: error.url ?? undefined,
        redirected: error.redirected,
        error: {
          ...(typeof error.error === 'object' && error.error ? error.error : {}),
          message,
        },
      }));
    })
  );

function buildRateLimitMessage(error: HttpErrorResponse): string {
  const retryAfter = error.headers.get('Retry-After');
  return retryAfter
    ? `Banking API rate limit exceeded. Retry after ${retryAfter} seconds.`
    : 'Banking API rate limit exceeded. Please retry shortly.';
}
