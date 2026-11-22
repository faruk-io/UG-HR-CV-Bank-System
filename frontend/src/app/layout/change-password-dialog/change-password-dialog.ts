import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { UserService } from '../../core/user.service';

@Component({
  selector: 'app-change-password',
  imports: [FormsModule],
  templateUrl: './change-password-dialog.html',
  styleUrl: './change-password-dialog.scss'
})
export class ChangePasswordDialog {
  @Output() readonly closed = new EventEmitter<void>();

  private readonly auth = inject(AuthService);
  private readonly users = inject(UserService);

  protected readonly current = signal('');
  protected readonly next = signal('');
  protected readonly confirm = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly done = signal(false);

  protected get canSubmit(): boolean {
    return !this.busy() && !!this.current() && !!this.next() && !!this.confirm();
  }

  protected patch(patch: { current?: string; next?: string; confirm?: string }): void {
    if (patch.current !== undefined) this.current.set(patch.current);
    if (patch.next !== undefined) this.next.set(patch.next);
    if (patch.confirm !== undefined) this.confirm.set(patch.confirm);
  }

  protected submit(): void {
    const problem = this.validate();

    if (problem) {
      this.error.set(problem);
      return;
    }

    this.busy.set(true);
    this.error.set('');

    this.users.changeOwnPassword(this.current(), this.next()).subscribe({
      next: () => {
        this.busy.set(false);
        this.current.set('');
        this.next.set('');
        this.confirm.set('');
        this.done.set(true);
      },
      error: error => {
        this.busy.set(false);
        this.error.set(this.users.errorMessage(error, 'The password could not be changed.'));
      }
    });
  }

  protected signOut(): void {
    this.auth.logout();
  }

  private validate(): string | null {
    if (!this.current()) return 'Enter your current password.';
    if (this.next().length < 8) return 'The new password must be at least 8 characters.';
    if (!/[A-Za-z]/.test(this.next()) || !/\d/.test(this.next())) {
      return 'The new password must contain at least one letter and one digit.';
    }
    if (this.next() !== this.confirm()) return 'The two new passwords do not match.';
    return null;
  }
}
