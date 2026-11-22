import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { PagedResult } from '../../core/candidate.models';
import {
  RoleOption,
  UserListItem,
  UserQuery,
  blankUserQuery,
  permissionLabel
} from '../../core/user.models';
import { UserService } from '../../core/user.service';

interface UserForm {
  id: number | null;
  fullName: string;
  userName: string;
  email: string;
  mobileNumber: string;
  roleId: number | null;
  isActive: boolean;
  password: string;
  newPassword: string;
}

function emptyForm(): UserForm {
  return {
    id: null,
    fullName: '',
    userName: '',
    email: '',
    mobileNumber: '',
    roleId: null,
    isActive: true,
    password: '',
    newPassword: ''
  };
}

import { Icon } from '../../layout/icon/icon';

@Component({
  selector: 'app-users',
  imports: [CommonModule, FormsModule, Icon],
  templateUrl: './users.html',
  styleUrl: './users.scss'
})
export class Users implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly service = inject(UserService);

  protected readonly query = signal<UserQuery>(blankUserQuery());
  protected readonly page = signal<PagedResult<UserListItem>>({ items: [], total: 0, page: 1, pageSize: 20 });
  protected readonly roles = signal<RoleOption[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly panelOpen = signal(false);
  protected readonly form = signal<UserForm>(emptyForm());

  private searchTimer: ReturnType<typeof setTimeout> | undefined;

  ngOnInit(): void {
    this.service.roles().subscribe(items => this.roles.set(items));
    this.load();
  }

  protected get q(): UserQuery {
    return this.query();
  }

  protected get editing(): boolean {
    return this.form().id !== null;
  }

  protected get isNew(): boolean {
    return this.form().id === null;
  }

  protected get selectedRole(): RoleOption | undefined {
    return this.roles().find(role => role.id === this.form().roleId);
  }

  protected get from(): number {
    const { total, page, pageSize } = this.page();
    return total === 0 ? 0 : (page - 1) * pageSize + 1;
  }

  protected get to(): number {
    const { total, page, pageSize } = this.page();
    return Math.min(page * pageSize, total);
  }

  protected get lastPage(): number {
    return Math.max(1, Math.ceil(this.page().total / this.page().pageSize));
  }

  protected isSelf(user: { id: number | null }): boolean {
    return this.auth.currentUser()?.userId === user.id;
  }

  protected patch(partial: Partial<UserQuery>): void {
    this.query.update(current => ({ ...current, ...partial }));
    this.load();
  }

  protected onSearch(value: string | null): void {
    this.query.update(current => ({ ...current, search: value, page: 1 }));
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.load(), 350);
  }

  protected hasFilters(): boolean {
    const q = this.query();
    return !!(q.search || q.roleId) || q.isActive !== null;
  }

  protected clearFilters(): void {
    this.query.set(blankUserQuery());
    this.load();
  }

  protected create(): void {
    this.error.set('');
    this.notice.set('');
    this.panelOpen.set(true);
    this.form.set({ ...emptyForm(), roleId: this.roles()[0]?.id ?? null });
  }

  protected edit(user: UserListItem): void {
    this.error.set('');
    this.notice.set('');
    this.panelOpen.set(true);
    this.form.set({
      id: user.id,
      fullName: user.fullName,
      userName: user.userName,
      email: user.email ?? '',
      mobileNumber: user.mobileNumber ?? '',
      roleId: user.roleId,
      isActive: user.isActive,
      password: '',
      newPassword: ''
    });
  }

  protected cancel(): void {
    this.panelOpen.set(false);
    this.form.set(emptyForm());
    this.error.set('');
  }

  protected field(patch: Partial<UserForm>): void {
    this.form.update(current => ({ ...current, ...patch }));
  }

  protected save(): void {
    const form = this.form();
    const problem = this.validate(form);

    if (problem) {
      this.error.set(problem);
      return;
    }

    this.saving.set(true);
    this.error.set('');
    this.notice.set('');

    const request = {
      fullName: form.fullName.trim(),
      userName: form.userName.trim(),
      email: form.email.trim() || null,
      mobileNumber: form.mobileNumber.trim() || null,
      roleId: form.roleId as number,
      isActive: form.isActive,
      password: this.isNew ? form.password : undefined,
      newPassword: this.editing && form.newPassword ? form.newPassword : null
    };

    const call = this.editing
      ? this.service.update(form.id as number, request)
      : this.service.create(request);

    const wasEditing = this.editing;

    call.subscribe({
      next: saved => {
        this.saving.set(false);
        this.panelOpen.set(false);
        this.form.set(emptyForm());
        this.notice.set(
          wasEditing
            ? `${saved.fullName} updated.`
            : `${saved.fullName} created with login name ${saved.userName}.`
        );
        this.load();
      },
      error: error => {
        this.saving.set(false);
        this.error.set(this.service.errorMessage(error, 'The user could not be saved.'));
      }
    });
  }

  protected remove(user: UserListItem): void {
    if (!window.confirm(`Delete ${user.fullName} (${user.userName})? Their activity log stays.`)) {
      return;
    }

    this.error.set('');
    this.notice.set('');
    this.service.remove(user.id).subscribe({
      next: () => {
        this.notice.set(`${user.fullName} deleted.`);
        if (this.form().id === user.id) {
          this.form.set(emptyForm());
        }
        this.load();
      },
      error: error => this.error.set(this.service.errorMessage(error, 'The user could not be deleted.'))
    });
  }

  protected rolePermissions(role: RoleOption | undefined): string {
    return role ? role.permissions.map(permissionLabel).join(', ') : '';
  }

  private validate(form: UserForm): string | null {
    if (!form.fullName.trim()) return 'Full name is required.';
    if (this.isNew && !form.userName.trim()) return 'Login name is required.';
    if (!form.roleId) return 'Choose a role.';

    const password = this.isNew ? form.password : form.newPassword;

    if (this.isNew || form.newPassword) {
      if (password.length < 8) return 'The password must be at least 8 characters.';
      if (!/[A-Za-z]/.test(password) || !/\d/.test(password)) {
        return 'The password must contain at least one letter and one digit.';
      }
    }

    return null;
  }

  private load(): void {
    this.loading.set(true);
    this.service.list(this.query()).subscribe({
      next: result => {
        this.page.set(result);
        this.loading.set(false);
      },
      error: error => {
        this.loading.set(false);
        this.error.set(this.service.errorMessage(error, 'The user list could not be loaded.'));
      }
    });
  }
}
