import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  imports: [RouterLink],
  selector: 'app-unauthorized',
  styles: [
    `
      .panel {
        padding: 1.25rem;
      }

      a {
        color: var(--accent);
      }
    `
  ],
  template: `
    <section class="surface panel">
      <h2>Not allowed</h2>
      <p>Your role does not have permission to open this page.</p>
      <p><a routerLink="/dashboard">Back to Dashboard</a></p>
    </section>
  `
})
export class Unauthorized {}
