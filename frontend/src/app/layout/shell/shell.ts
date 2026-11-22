import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { SettingsService } from '../../core/settings.service';
import { ChangePasswordDialog } from '../change-password-dialog/change-password-dialog';
import { Icon } from '../icon/icon';

interface MenuItem {
  label: string;
  path: string;
  permission?: string;
  icon: string;
}

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet, ChangePasswordDialog, Icon],
  selector: 'app-shell',
  styleUrl: './shell.scss',
  templateUrl: './shell.html'
})
export class Shell implements OnInit {
  protected readonly auth = inject(AuthService);
  protected readonly settings = inject(SettingsService);
  protected readonly year = new Date().getFullYear();

  protected readonly passwordPanelOpen = signal(false);
  protected readonly collapsed = signal(localStorage.getItem('cvb-sidebar-collapsed') === '1');

  protected toggleSidebar(): void {
    this.collapsed.update(value => !value);
    localStorage.setItem('cvb-sidebar-collapsed', this.collapsed() ? '1' : '0');
  }

  private readonly menu: MenuItem[] = [
    { label: 'Dashboard', path: '/dashboard', permission: 'candidate.view', icon: 'dashboard' },
    { label: 'CV Bank', path: '/candidates', permission: 'candidate.view', icon: 'file-text' },
    { label: 'Evaluations', path: '/evaluations', permission: 'evaluation.view', icon: 'clipboard' },
    { label: 'Joining List', path: '/joining', icon: 'user-plus' },
    { label: 'Resigned List', path: '/resigned', icon: 'user-minus' },
    { label: 'Reports', path: '/reports', permission: 'report.view', icon: 'chart' },
    { label: 'Users', path: '/users', permission: 'user.manage', icon: 'user' },
    { label: 'Activity Log', path: '/activity-log', permission: 'activity.log.view', icon: 'clock' },
    { label: 'Master Data', path: '/master-data', permission: 'settings.manage', icon: 'database' },
    { label: 'Settings', path: '/settings', permission: 'settings.manage', icon: 'sliders' }
  ];

  protected readonly visibleMenu = computed(() =>
    this.menu.filter(item => !item.permission || this.auth.can(item.permission))
  );

  protected readonly initials = computed(() => {
    const parts = (this.auth.currentUser()?.fullName ?? '').trim().split(/\s+/).slice(0, 2);
    return parts.map(part => part[0]?.toUpperCase() ?? '').join('') || '•';
  });

  ngOnInit(): void {
    this.settings.ensureDisplay();
  }
}
