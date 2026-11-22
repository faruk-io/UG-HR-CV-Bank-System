import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CandidateService } from '../../core/candidate.service';
import { DashboardService } from '../../core/dashboard.service';
import { CandidateSummary, RatingLine } from '../../core/dashboard.models';
import { formatBytes } from '../../core/candidate.models';
import { SettingsService } from '../../core/settings.service';
import { Icon } from '../../layout/icon/icon';

interface RatingRow {
  left: RatingLine;
  right: RatingLine | null;
}

@Component({
  selector: 'app-candidate-summary',
  imports: [CommonModule, RouterLink, Icon],
  templateUrl: './candidate-summary.html',
  styleUrl: './candidate-summary.scss'
})
export class CandidateSummaryPage implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly dashboard = inject(DashboardService);
  private readonly sanitizer = inject(DomSanitizer);
  protected readonly candidates = inject(CandidateService);
  protected readonly settings = inject(SettingsService);

  protected readonly summary = signal<CandidateSummary | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly cvPreview = signal<SafeResourceUrl | null>(null);

  private candidateId = 0;
  private rawPreviewUrl: string | null = null;

  protected readonly formatBytes = formatBytes;

  ngOnInit(): void {
    this.candidateId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set('');
    this.dashboard.candidateSummary(this.candidateId).subscribe({
      next: data => {
        this.summary.set(data);
        this.loading.set(false);
        if (data.cvDocuments.length) this.openCv(data.cvDocuments[0].documentId);
      },
      error: err => {
        this.error.set(this.dashboard.errorMessage(err, 'Candidate summary could not be loaded.'));
        this.loading.set(false);
      }
    });
  }

  protected print(): void {
    window.print();
  }

  protected openCv(documentId: number): void {
    this.candidates.fetchDocument(documentId, true).subscribe({
      next: blob => {
        this.revokePreview();
        this.rawPreviewUrl = URL.createObjectURL(blob);
        this.cvPreview.set(this.sanitizer.bypassSecurityTrustResourceUrl(this.rawPreviewUrl));
      },
      error: err => this.error.set(this.dashboard.errorMessage(err, 'The CV could not be opened.'))
    });
  }

  protected downloadCv(documentId: number, fileName: string): void {
    this.candidates.fetchDocument(documentId, false).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: err => this.error.set(this.dashboard.errorMessage(err, 'The CV could not be downloaded.'))
    });
  }

  protected ratingPairs(ratings: RatingLine[]): RatingRow[] {
    const rows: RatingRow[] = [];
    for (let i = 0; i < ratings.length; i += 2) {
      rows.push({ left: ratings[i], right: ratings[i + 1] ?? null });
    }
    return rows;
  }

  private revokePreview(): void {
    if (this.rawPreviewUrl) URL.revokeObjectURL(this.rawPreviewUrl);
    this.rawPreviewUrl = null;
    this.cvPreview.set(null);
  }
}
