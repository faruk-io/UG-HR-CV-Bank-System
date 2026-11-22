import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { Icon } from '../../layout/icon/icon';

@Component({
  imports: [ReactiveFormsModule, Icon],
  selector: 'app-login',
  styleUrl: './login.scss',
  templateUrl: './login.html'
})
export class Login {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly errorMessage = signal('');
  protected readonly busy = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    userName: ['', Validators.required],
    password: ['', Validators.required]
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { userName, password } = this.form.getRawValue();
    this.busy.set(true);
    this.errorMessage.set('');

    this.auth.login(userName, password).subscribe({
      next: () => {
        this.busy.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: response => {
        this.busy.set(false);
        this.errorMessage.set(
          response.status === 401
            ? 'Invalid user name or password.'
            : 'Could not reach the server. Ask the administrator to start the CvBank API.'
        );
      }
    });
  }
}
