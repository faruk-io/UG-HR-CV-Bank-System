import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppSettings, SettingsUpdateRequest, UPLOAD_MB_OPTIONS } from '../../core/settings.models';
import { SettingsService } from '../../core/settings.service';
import { Icon } from '../../layout/icon/icon';

@Component({
  selector: 'app-settings',
  imports: [CommonModule, FormsModule, RouterLink, Icon],
  templateUrl: './settings.html',
  styleUrl: './settings.scss'
})
export class Settings implements OnInit {
  private readonly service = inject(SettingsService);

  protected readonly loaded = signal<AppSettings | null>(null);
  protected readonly form = signal<SettingsUpdateRequest>({
    companyName: '',
    companyAddress: '',
    maxUploadMb: UPLOAD_MB_OPTIONS[0]
  });
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');

  protected readonly uploadOptions = UPLOAD_MB_OPTIONS;

  protected readonly isDirty = computed(() => {
    const saved = this.loaded();
    const form = this.form();
    if (!saved) {
      return false;
    }

    return (
      form.companyName.trim() !== saved.companyName ||
      (form.companyAddress ?? '').trim() !== (saved.companyAddress ?? '') ||
      form.maxUploadMb !== saved.maxUploadMb
    );
  });

  ngOnInit(): void {
    this.load();
  }

  protected patch(partial: Partial<SettingsUpdateRequest>): void {
    this.form.update(current => ({ ...current, ...partial }));
    this.notice.set('');
  }

  protected revert(): void {
    const saved = this.loaded();
    if (!saved) {
      return;
    }

    this.form.set({
      companyName: saved.companyName,
      companyAddress: saved.companyAddress ?? '',
      maxUploadMb: saved.maxUploadMb
    });
    this.error.set('');
    this.notice.set('');
  }

  protected save(): void {
    const problem = this.validate(this.form());

    if (problem) {
      this.error.set(problem);
      return;
    }

    this.saving.set(true);
    this.error.set('');
    this.notice.set('');

    this.service.save(this.form()).subscribe({
      next: saved => {
        this.saving.set(false);
        this.loaded.set(saved);
        this.notice.set('Settings saved. Screens and uploads use the new values immediately.');
      },
      error: error => {
        this.saving.set(false);
        this.error.set(this.service.errorMessage(error, 'The settings could not be saved.'));
      }
    });
  }

  private validate(form: SettingsUpdateRequest): string | null {
    if (!form.companyName.trim()) {
      return 'The company name is required.';
    }

    if (form.companyName.trim().length > 200) {
      return 'The company name is longer than 200 characters.';
    }

    if ((form.companyAddress ?? '').trim().length > 500) {
      return 'The address is longer than 500 characters.';
    }

    return null;
  }

  private load(): void {
    this.loading.set(true);
    this.service.view().subscribe({
      next: saved => {
        this.loaded.set(saved);
        this.form.set({
          companyName: saved.companyName,
          companyAddress: saved.companyAddress ?? '',
          maxUploadMb: saved.maxUploadMb
        });
        this.loading.set(false);
      },
      error: error => {
        this.loading.set(false);
        this.error.set(this.service.errorMessage(error, 'The settings could not be loaded.'));
      }
    });
  }
}
