import { HttpErrorResponse } from '@angular/common/http';

/**
 * Turns an API error into one readable sentence for the screen.
 * The API answers with either validation errors ({ errors: { Field: ["message"] } })
 * or a ProblemDetails body whose "title" holds our message.
 */
export function errorText(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return 'Could not reach the server.';
    }

    const body = error.error;
    if (body && typeof body === 'object') {
      if (body.errors && typeof body.errors === 'object') {
        const messages = (Object.values(body.errors) as string[][]).flat();
        if (messages.length > 0) {
          return messages.join(' ');
        }
      }
      if (typeof body.title === 'string' && body.title) {
        return body.title;
      }
    }
  }
  return fallback;
}
