import { Component, ElementRef, OnInit, effect, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  MASTER_DATA_FIELD,
  MASTER_DATA_TABS,
  MasterDataItem,
  MasterDataKind,
  MasterDataView,
  emptyMasterDataView
} from '../../core/settings.models';
import { SettingsService } from '../../core/settings.service';
import { Icon } from '../../layout/icon/icon';

@Component({
  selector: 'app-master-data',
  imports: [CommonModule, FormsModule, Icon],
  templateUrl: './master-data.html',
  styleUrl: './master-data.scss'
})
export class MasterData implements OnInit {
  private readonly service = inject(SettingsService);

  protected readonly tabs = MASTER_DATA_TABS;
  protected readonly view = signal<MasterDataView>(emptyMasterDataView());
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');

  protected readonly newNames = signal<Record<MasterDataKind, string>>({
    concerns: '',
    designations: '',
    'job-positions': ''
  });

  protected readonly renaming = signal<{ kind: MasterDataKind; id: number } | null>(null);
  protected readonly renameValue = signal('');

  private readonly renameField = viewChild<ElementRef<HTMLInputElement>>('renameInput');

  constructor() {
    effect(() => {
      const field = this.renameField()?.nativeElement;

      if (field) {
        field.focus();
        field.select();
      }
    });
  }

  ngOnInit(): void {
    this.load();
  }

  protected items(kind: MasterDataKind): MasterDataItem[] {
    return this.view()[MASTER_DATA_FIELD[kind]];
  }

  protected setNewName(kind: MasterDataKind, value: string | null): void {
    this.newNames.update(current => ({ ...current, [kind]: value ?? '' }));
  }

  protected isNewNameEmpty(kind: MasterDataKind): boolean {
    return this.newNames()[kind].trim().length === 0;
  }

  protected isRenaming(kind: MasterDataKind, id: number): boolean {
    const current = this.renaming();
    return !!current && current.kind === kind && current.id === id;
  }

  protected add(kind: MasterDataKind): void {
    const name = this.newNames()[kind].trim();

    if (!name) {
      this.error.set('Type the name you want to add first.');
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.notice.set('');

    this.service.addMasterData(kind, name).subscribe({
      next: view => {
        this.busy.set(false);
        this.view.set(view);
        this.newNames.update(current => ({ ...current, [kind]: '' }));
        this.notice.set(`"${name}" added.`);
      },
      error: error => {
        this.busy.set(false);
        this.error.set(this.service.errorMessage(error, 'The new name could not be added.'));
      }
    });
  }

  protected startRename(kind: MasterDataKind, item: MasterDataItem): void {
    this.error.set('');
    this.notice.set('');
    this.renaming.set({ kind, id: item.id });
    this.renameValue.set(item.name);
  }

  protected cancelRename(): void {
    this.renaming.set(null);
    this.renameValue.set('');
  }

  protected applyRename(): void {
    const target = this.renaming();
    const name = this.renameValue().trim();

    if (!target || !name) {
      this.error.set('Type the new name first.');
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.notice.set('');

    this.service.renameMasterData(target.kind, target.id, name).subscribe({
      next: view => {
        this.busy.set(false);
        this.view.set(view);
        this.renaming.set(null);
        this.renameValue.set('');
        this.notice.set(`Renamed to "${name}". Existing records now show the new name.`);
      },
      error: error => {
        this.busy.set(false);
        this.error.set(this.service.errorMessage(error, 'The name could not be changed.'));
      }
    });
  }

  protected toggleActive(kind: MasterDataKind, item: MasterDataItem): void {
    const target = !item.isActive;

    if (!target && !window.confirm(this.deactivateWarning(item))) {
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.notice.set('');

    this.service.setMasterDataActive(kind, item.id, target).subscribe({
      next: view => {
        this.busy.set(false);
        this.view.set(view);
        this.notice.set(
          target
            ? `"${item.name}" is active again and back in the dropdowns.`
            : `"${item.name}" is no longer offered in the dropdowns.`
        );
      },
      error: error => {
        this.busy.set(false);
        this.error.set(this.service.errorMessage(error, 'The status could not be changed.'));
      }
    });
  }

  private deactivateWarning(item: MasterDataItem): string {
    const used = item.usedByRecords > 0
      ? `It is used by ${item.usedByRecords} existing record(s); they keep their current value.`
      : 'It is not used by any stored record yet.';

    return `Deactivate "${item.name}"?\n\n${used}\nIt simply stops being offered for new entries, and you can activate it again later.`;
  }

  private load(): void {
    this.loading.set(true);
    this.service.masterData().subscribe({
      next: view => {
        this.view.set(view);
        this.loading.set(false);
      },
      error: error => {
        this.loading.set(false);
        this.error.set(this.service.errorMessage(error, 'The master data lists could not be loaded.'));
      }
    });
  }
}
