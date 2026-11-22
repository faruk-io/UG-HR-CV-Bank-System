import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { DashboardService } from '../../core/dashboard.service';
import {
  DashboardSummary,
  decisionLabel,
  interviewStatusLabel
} from '../../core/dashboard.models';
import { statusLabel } from '../../core/candidate.models';
import { Icon } from '../../layout/icon/icon';

interface Card {
  label: string;
  value: number;
  hint: string;
  icon: string;
  tone: 'blue' | 'teal' | 'violet' | 'green';
}

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink, Icon],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly service = inject(DashboardService);

  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal('');

  protected readonly recentCandidates = computed(() => this.summary()?.recentCandidates ?? []);
  protected readonly recentInterviews = computed(() => this.summary()?.recentInterviews ?? []);
  protected readonly recentJoining = computed(() => this.summary()?.recentJoining ?? []);
  protected readonly recentResigned = computed(() => this.summary()?.recentResigned ?? []);
  protected readonly totalJoining = computed(() => this.summary()?.totalJoining ?? 0);
  protected readonly totalResigned = computed(() => this.summary()?.totalResigned ?? 0);

  protected readonly cards = computed<Card[]>(() => {
    const s = this.summary();
    if (!s) return [];

    return [
      { label: 'Total Candidates', value: s.totalCandidates, hint: 'Registered in CV Bank', icon: 'user', tone: 'blue' },
      { label: 'CV Uploaded', value: s.totalCvUploaded, hint: 'Candidates with a current CV', icon: 'file-text', tone: 'teal' },
      { label: 'Total Interviews', value: s.totalInterviews, hint: 'Evaluation records', icon: 'clipboard', tone: 'violet' },
      { label: 'Selected', value: s.selected, hint: 'Status: Selected', icon: 'check', tone: 'green' }
    ] as Card[];
  });

  ngOnInit(): void {
    this.loadSummary();
  }

  protected readonly decisionLabel = decisionLabel;
  protected readonly interviewStatusLabel = interviewStatusLabel;
  protected readonly statusLabel = statusLabel;

  protected refresh(): void {
    this.loadSummary();
  }

  private loadSummary(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.summary().subscribe({
      next: data => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(this.service.errorMessage(err));
        this.loading.set(false);
      }
    });
  }
}