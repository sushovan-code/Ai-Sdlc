import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { fieldErrors, problemMessage } from '../../../core/api/problem';
import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../../../core/notifications/toast.service';
import { Icon } from '../../../shared/ui/icon/icon';
import { AuthLayout } from '../auth-layout/auth-layout';

const MIN_PASSWORD_LENGTH = 12;
const MAX_DISPLAY_NAME_LENGTH = 100;

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value as string | undefined;
  const confirm = group.get('confirmPassword')?.value as string | undefined;
  return password === confirm ? null : { mismatch: true };
}

type FieldName = 'displayName' | 'email' | 'password' | 'confirmPassword';

@Component({
  selector: 'aisdlc-register',
  imports: [ReactiveFormsModule, RouterLink, AuthLayout, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.html',
  styleUrl: '../auth-form.scss',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly minPasswordLength = MIN_PASSWORD_LENGTH;
  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      displayName: ['', [Validators.required, Validators.maxLength(MAX_DISPLAY_NAME_LENGTH)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatch },
  );
  protected readonly submitting = signal(false);
  protected readonly serverErrors = signal<Record<string, string[]>>({});

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.serverErrors.set({});
    const { displayName, email, password } = this.form.getRawValue();
    this.auth
      .register({ displayName: displayName.trim(), email: email.trim(), password })
      .subscribe({
        next: (user) => {
          this.toast.success(`Account created. Welcome, ${user.displayName}.`);
          void this.router.navigateByUrl('/');
        },
        error: (error: unknown) => {
          this.serverErrors.set(fieldErrors(error));
          this.toast.error(problemMessage(error));
          this.submitting.set(false);
        },
      });
  }

  protected showError(control: FieldName): boolean {
    const field = this.form.controls[control];
    return field.invalid && field.touched;
  }

  protected showMismatch(): boolean {
    return this.form.hasError('mismatch') && this.form.controls.confirmPassword.touched;
  }
}
