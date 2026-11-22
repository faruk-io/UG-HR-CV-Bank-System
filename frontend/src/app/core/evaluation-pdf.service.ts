import { Injectable } from '@angular/core';
import { InterviewFormModel } from './evaluation-print';

interface CanvasItem {
  type: 'rect' | 'line';
  x?: number;
  y?: number;
  w?: number;
  h?: number;
  x1?: number;
  y1?: number;
  x2?: number;
  y2?: number;
  lineWidth?: number;
  lineColor?: string;
}

interface Block {
  text?: string | Block[];
  stack?: Block[];
  table?: { widths: (number | string)[]; body: Row[]; headerRows?: number };
  canvas?: CanvasItem[];
  svg?: string;
  width?: number;
  height?: number;
  layout?: unknown;
  margin?: [number, number, number, number];
  colSpan?: number;
  rowSpan?: number;
  bold?: boolean;
  fillColor?: string;
  color?: string;
  fontSize?: number;
  alignment?: 'left' | 'center' | 'right';
  minRows?: number;
}

type Cell = string | number | Block;
type Row = Cell[];

const BAND = '#e8ecf3';
const LINE = '#8d99ac';
const GREY = '#4b5563';
const INK = '#1f2937';
const BOX = 7;

const GRID = {
  hLineWidth: () => 0.5,
  vLineWidth: () => 0.5,
  hLineColor: () => LINE,
  vLineColor: () => LINE,
  paddingLeft: () => 2.5,
  paddingRight: () => 2.5,
  paddingTop: () => 0.8,
  paddingBottom: () => 0.8
};

const PLAIN = {
  ...GRID,
  hLineWidth: () => 0,
  vLineWidth: () => 0
};

const FLAT = {
  ...PLAIN,
  paddingLeft: () => 0,
  paddingRight: () => 6,
  paddingTop: () => 0,
  paddingBottom: () => 0
};

/** A cell that spans several columns, followed by the placeholders pdfmake expects. */
function row(cells: Cell[], columns: number): Row {
  const out: Row = [];
  for (const cell of cells) {
    out.push(cell);
    const span = typeof cell === 'object' && cell.colSpan ? cell.colSpan : 1;
    for (let i = 1; i < span; i++) out.push('');
  }
  while (out.length < columns) out.push('');
  return out;
}

function band(title: string, columns: number): Row {
  return row([{ text: title, colSpan: columns, bold: true, fillColor: BAND }], columns);
}

function label(text: string): Block {
  return { text, color: GREY };
}

function pair(labelText: string, value: string): Cell[] {
  return [label(labelText), value];
}

/** The empty square HR ticks on paper, crossed when the record says it happened. */
function boxShapes(checked: boolean, x: number, y = 1): CanvasItem[] {
  const shapes: CanvasItem[] = [{ type: 'rect', x, y, w: BOX, h: BOX, lineWidth: 0.8, lineColor: INK }];
  if (checked) {
    const pad = 1.3;
    shapes.push(
      { type: 'line', x1: x + pad, y1: y + pad, x2: x + BOX - pad, y2: y + BOX - pad, lineWidth: 0.9, lineColor: INK },
      { type: 'line', x1: x + BOX - pad, y1: y + pad, x2: x + pad, y2: y + BOX - pad, lineWidth: 0.9, lineColor: INK }
    );
  }
  return shapes;
}

function tickCell(checked: boolean, columnWidth: number): Block {
  return { canvas: boxShapes(checked, Math.max(1, (columnWidth - 5 - BOX) / 2)) };
}

function table(widths: (number | string)[], body: Row[], layout: unknown = GRID): Block {
  return { table: { widths, body }, layout, margin: [0, 2, 0, 3] };
}

const TICK = 30;
const ORIGINAL = 45;

@Injectable({ providedIn: 'root' })
export class EvaluationPdfService {
  private logoSvg?: string | null;

  async download(model: InterviewFormModel, fileName: string): Promise<void> {
    const pdfMake = await this.pdfMake();
    const logo = await this.loadLogo();
    await pdfMake.createPdf(this.build(model, logo)).download(fileName);
  }

  private async loadLogo(): Promise<string | null> {
    if (this.logoSvg !== undefined) return this.logoSvg;

    try {
      const response = await fetch('images/logo-full.svg');
      if (!response.ok) throw new Error(`logo fetch ${response.status}`);
      // svg-to-pdfkit ignores <style> blocks, so the LIMITED lettering needs a direct fill.
      this.logoSvg = (await response.text()).replace(/class="st2"/g, 'fill="#595a5c"');
    } catch {
      this.logoSvg = null;
    }
    return this.logoSvg;
  }

  private async pdfMake(): Promise<PdfMakeLike> {
    const instance = await this.resolvePdfMake();
    if (!instance.fontsLoaded) {
      const fontsModule = (await import('pdfmake/build/vfs_fonts')) as unknown as {
        default?: Record<string, string>;
      };
      const fonts = fontsModule.default ?? (fontsModule as unknown as Record<string, string>);
      instance.addVirtualFileSystem(
        Object.fromEntries(Object.entries(fonts).filter(([key]) => key.endsWith('.ttf')))
      );
      instance.fontsLoaded = true;
    }
    return instance;
  }

  private async resolvePdfMake(): Promise<PdfMakeLike> {
    const mod = (await import('pdfmake/build/pdfmake')) as unknown as Record<string, unknown>;
    const candidate = (typeof mod['createPdf'] === 'function' ? mod : mod['default']) as PdfMakeLike | undefined;

    if (!candidate || typeof candidate.addVirtualFileSystem !== 'function') {
      throw new Error('pdfmake could not be loaded.');
    }
    return candidate;
  }

  private build(m: InterviewFormModel, logo: string | null): unknown {
    return {
      pageSize: 'A4',
      pageMargins: [18, 16, 18, 20],
      defaultStyle: { fontSize: 7.5, font: 'Roboto' },
      content: [
        this.header(m, logo),
        this.candidatePart(m),
        this.addresses(m),
        this.documents(m),
        this.references(m),
        this.ratings(m),
        this.evaluators(m),
        this.decision(m),
        this.finalDecision(m),
        this.approvals(m),
        { text: m.footer, fontSize: 6.5, color: GREY, alignment: 'center', margin: [0, 4, 0, 0] }
      ]
    };
  }

  private header(m: InterviewFormModel, logo: string | null): Block {
    const logoCell: Cell = logo ? { svg: logo, width: 105, height: 38.4, margin: [2, 2, 0, 2] } : '';
    const titleCell: Block = {
      stack: [
        { text: m.title, bold: true, fontSize: 11.5, alignment: 'center' },
        { text: m.dateLabel, bold: true, alignment: 'center', margin: [0, 8, 0, 0] }
      ]
    };
    const addressCell: Block = {
      stack: [
        { text: 'Union Centre', bold: true, alignment: 'right' },
        { text: '68/1 Gulshan Avenue', alignment: 'right' },
        { text: 'Dhaka-1212, Bangladesh', alignment: 'right' }
      ]
    };

    return table([115, '*', 150], [row([logoCell, titleCell, addressCell], 3)], GRID);
  }

  private candidatePart(m: InterviewFormModel): Block {
    return table([115, '*', 120, '*'], [
      band('Candidate’s Part', 4),
      [
        { text: 'Name', rowSpan: 2, color: GREY },
        { text: m.name, rowSpan: 2 },
        label('Position Applied for'),
        m.positionApplied
      ],
      ['', '', label('Concern Name'), m.concernName],
      band('Current Working Details', 4),
      row([...pair('Company Name', m.companyName), ...pair('Present Salary', m.presentSalary)], 4),
      row([...pair('Designation', m.designation), ...pair('Academic Qualification', '')], 4),
      row([...pair('Duration (Current Org.)', m.duration), ...pair('Total Year of Experience', m.experience)], 4),
      row([label('Date of Birth:'), { text: m.dateOfBirth, colSpan: 3 }], 4)
    ]);
  }

  private addresses(m: InterviewFormModel): Block {
    return table(['14%', '28%', '7%', '12%', '9%', '*'], [
      row(['Present Address:', m.presentAddress, 'Area:', '', 'District:', ''], 6),
      row(['Permanent Address:', '', 'Area:', '', 'District:', ''], 6)
    ]);
  }

  private documents(m: InterviewFormModel): Block {
    const rows: Row[] = [band('Submitted Documents', 8)];
    rows.push(
      row(
        ['SL No', 'Details', 'Copy', 'Original Checked', 'SL No', 'Details', 'Copy', 'Original Checked'].map(
          text => ({ text, bold: true, alignment: 'center' })
        ),
        8
      )
    );

    const count = Math.max(m.documentsLeft.length, m.documentsRight.length);
    for (let i = 0; i < count; i++) {
      rows.push(row([...this.checklist(m.documentsLeft[i]), ...this.checklist(m.documentsRight[i])], 8));
    }

    return table([22, '*', TICK, ORIGINAL, 22, '*', TICK, ORIGINAL], rows);
  }

  private checklist(item: ChecklistLike | undefined): Cell[] {
    const copy = tickCell(item?.copy ?? false, TICK);
    const original = tickCell(item?.original ?? false, ORIGINAL);
    if (!item) return ['', '', copy, original];
    return [String(item.sl), item.details ? `${item.name} — ${item.details}` : item.name, copy, original];
  }

  private references(m: InterviewFormModel): Block {
    const blocks = m.references.length ? m.references : [emptyReference(), emptyReference()];
    const body: Row[] = [band('References', 6)];

    for (const ref of blocks) {
      body.push(row([label('Reference Name'), ref.name, label('Designation'), ref.designation, label('Relationship'), ref.relationship], 6));
      body.push(row([{ ...label('Company Name & Location'), colSpan: 2 }, { text: ref.company, colSpan: 2 }, label('Phone'), ref.phone], 6));
      body.push(row([label('Statement'), { text: ref.statement, colSpan: 5, minRows: 2 }], 6));
    }

    return table(['17%', '20%', '13%', '20%', '12%', '*'], body);
  }

  private ratings(m: InterviewFormModel): Block {
    const body: Row[] = [
      band('Fill-up by Evaluators Only', 4),
      row([{ text: 'Attributes Rating', colSpan: 4, bold: true }], 4),
      row([{ text: m.ratingScale, colSpan: 4, fontSize: 7 }], 4),
      row(['DIMENSIONS', 'RATING', 'DIMENSIONS', 'RATING'].map(text => ({ text, bold: true, alignment: 'center' })), 4)
    ];

    for (let i = 0; i < Math.max(m.ratingsLeft.length, m.ratingsRight.length); i++) {
      const left = m.ratingsLeft[i];
      const right = m.ratingsRight[i];
      body.push(row([left?.name ?? '', left?.rating ?? '', right?.name ?? '', right?.rating ?? ''], 4));
    }

    body.push(row([{ text: `Total Rating: ${m.totalRating} / (Out of ${m.maxRating})`, colSpan: 4, bold: true, alignment: 'right' }], 4));

    return table(['31%', '19%', '31%', '19%'], body);
  }

  private evaluators(m: InterviewFormModel): Block {
    const blocks = [...m.evaluators];
    while (blocks.length < 2) blocks.push(emptyEvaluator());
    if (blocks.length % 2 === 1) blocks.push(emptyEvaluator());

    const body: Row[] = [];
    for (let i = 0; i < blocks.length; i += 2) {
      body.push(row([this.evaluatorCell(blocks[i]), this.evaluatorCell(blocks[i + 1])], 2));
    }

    return table(['50%', '50%'], body);
  }

  private evaluatorCell(ev: EvaluatorLike): Block {
    return {
      stack: [
        { text: `Findings: ${ev.findings}`, minRows: 3 },
        { text: `Sign: ${ev.sign}` },
        { text: `Name: ${ev.name}` },
        { table: { widths: ['50%', '50%'], body: [[{ text: `Designation: ${ev.designation}` }, { text: `Department: ${ev.department}` }]] }, layout: FLAT }
      ]
    };
  }

  private decision(m: InterviewFormModel): Block {
    const cells: Cell[] = [];
    for (const d of m.decisions) {
      cells.push({ canvas: boxShapes(d.marked, 1, 1.2) }, { text: d.label, bold: true });
    }

    return table(['18%', '32%', '18%', '32%'], [
      row([label('Number of Person Interview Taken'), m.personsTaken, label('Expected Salary'), m.expectedSalary], 4),
      row([label('Recommendation:'), m.recommendation, label('Notice Period'), m.noticePeriod], 4),
      row(
        [
          label('Decision :'),
          { colSpan: 3, table: { widths: [15, 'auto', 15, 'auto', 15, 'auto', 15, 'auto'], body: [cells] }, layout: FLAT, margin: [0, 1, 0, 0] }
        ],
        4
      )
    ]);
  }

  private finalDecision(m: InterviewFormModel): Block {
    const widths: (number | string)[] = ['auto', '*', 'auto', '*', 'auto', '*', 'auto', '*'];

    return table(widths, [
      band('Final Decision', 8),
      row(
        [
          label('Designation Fixed:'),
          m.designationFixed,
          label('Salary Fixed:'),
          m.salaryFixed,
          label('Grade:'),
          m.grade,
          label('DOJ:'),
          m.doj
        ],
        8
      ),
      row([{ text: 'Remarks:', colSpan: 8, color: GREY }], 8),
      row([{ text: m.remarks, colSpan: 8, minRows: 2, margin: [0, 1, 0, 6] }], 8)
    ]);
  }

  private approvals(m: InterviewFormModel): Block {
    const columns = Math.max(m.approvals.length, 5);
    const widths = Array.from({ length: columns }, () => `${Math.floor(10000 / columns) / 100}%`);

    const titles = m.approvals.map(a => ({ text: a.title, bold: true, alignment: 'center' as const }));
    const decided = m.approvals.map(a => ({ text: a.decision, alignment: 'center' as const, color: GREY }));
    const signed = m.approvals.map(a => ({ text: a.sign, alignment: 'center' as const, minRows: 2 }));

    return table(widths, [
      band('Approvals', columns),
      row(titles, columns),
      row(decided, columns),
      row(signed, columns)
    ]);
  }
}

interface PdfMakeLike {
  addVirtualFileSystem(vfs: Record<string, string>): void;
  createPdf(docDefinition: unknown): { download(fileName?: string): Promise<void> };
  fontsLoaded?: boolean;
}

interface ChecklistLike {
  sl: number;
  name: string;
  details: string;
  copy: boolean;
  original: boolean;
}

interface EvaluatorLike {
  findings: string;
  sign: string;
  name: string;
  designation: string;
  department: string;
}

function emptyReference() {
  return { name: '', designation: '', relationship: '', company: '', phone: '', statement: '' };
}

function emptyEvaluator(): EvaluatorLike {
  return { findings: '', sign: '', name: '', designation: '', department: '' };
}
