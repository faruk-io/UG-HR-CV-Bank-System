import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EvaluationService } from '../../core/evaluation.service';
import { EvaluationPdfService } from '../../core/evaluation-pdf.service';
import { Icon } from '../../layout/icon/icon';
import {
  ChecklistRow,
  EvaluatorBlock,
  InterviewFormModel,
  RatingRow,
  ReferenceBlock,
  buildInterviewFormModel
} from '../../core/evaluation-print';

interface RowPair<T> {
  left: T | null;
  right: T | null;
}

const BLANK_EVALUATOR: EvaluatorBlock = {
  findings: '',
  sign: '',
  name: '',
  designation: '',
  department: ''
};

const BLANK_REFERENCE: ReferenceBlock = {
  name: '',
  designation: '',
  relationship: '',
  company: '',
  phone: '',
  statement: ''
};

@Component({
  selector: 'app-evaluation-print',
  imports: [CommonModule, RouterLink, Icon],
  templateUrl: './evaluation-print.html',
  styleUrl: './evaluation-print.scss'
})
export class EvaluationPrint implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(EvaluationService);
  private readonly pdf = inject(EvaluationPdfService);

  protected readonly form = signal<InterviewFormModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  protected evaluationId = 0;
  private downloadName = 'Interview-Evaluation-Form.pdf';

  ngOnInit(): void {
    this.evaluationId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  protected readonly ratingRows = computed(() => this.zip<RatingRow>(this.form()?.ratingsLeft, this.form()?.ratingsRight));

  protected readonly documentRows = computed(() => this.zip<ChecklistRow>(this.form()?.documentsLeft, this.form()?.documentsRight));

  protected readonly referenceBlocks = computed(() => {
    const blocks = [...(this.form()?.references ?? [])];
    while (blocks.length < 2) blocks.push(BLANK_REFERENCE);
    return blocks;
  });

  protected readonly evaluatorRows = computed(() => {
    const blocks = [...(this.form()?.evaluators ?? [])];
    while (blocks.length < 2) blocks.push(BLANK_EVALUATOR);
    if (blocks.length % 2 === 1) blocks.push(BLANK_EVALUATOR);

    return Array.from({ length: blocks.length / 2 }, (_, i) => ({
      left: blocks[i * 2],
      right: blocks[i * 2 + 1]
    }));
  });

  protected print(): void {
    window.print();
  }

  protected downloadPdf(): void {
    const model = this.form();
    if (!model) return;

    this.busy.set(true);
    this.error.set('');

    this.pdf.download(model, this.downloadName).then(
      () => this.busy.set(false),
      () => {
        this.error.set('The PDF could not be generated. Use Print instead.');
        this.busy.set(false);
      }
    );
  }

  private zip<T>(left?: T[], right?: T[]): RowPair<T>[] {
    const a = left ?? [];
    const b = right ?? [];
    return Array.from({ length: Math.max(a.length, b.length) }, (_, i) => ({
      left: a[i] ?? null,
      right: b[i] ?? null
    }));
  }

  private load(): void {
    this.loading.set(true);
    this.error.set('');

    this.service.get(this.evaluationId).subscribe({
      next: evaluation => {
        this.form.set(buildInterviewFormModel(evaluation));
        this.downloadName = `Interview-Evaluation-${evaluation.candidateCode}-${evaluation.interviewNumber}.pdf`;
        this.loading.set(false);
      },
      error: error => {
        this.error.set(this.service.errorMessage(error, 'This interview form could not be loaded.'));
        this.loading.set(false);
      }
    });
  }
}
