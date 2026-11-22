import { Injectable } from '@angular/core';
import { ReportCell, ReportColumn, ReportData } from './report.models';

const BAND = '#e8ecf3';
const LINE = '#8d99ac';
const GREY = '#4b5563';
const DANGER = '#b00020';
const BRAND = 'Union Group CV Bank';
const MAX_PDF_ROWS = 300;

interface PdfDoc {
  pageSize: string;
  pageOrientation: 'portrait' | 'landscape';
  pageMargins: [number, number, number, number];
  defaultStyle: { fontSize: number; font: string };
  content: unknown[];
}

interface PdfMakeLike {
  addVirtualFileSystem(vfs: Record<string, string>): void;
  createPdf(docDefinition: unknown): { download(fileName?: string): Promise<void> };
  fontsLoaded?: boolean;
}

const GRID = {
  hLineWidth: () => 0.5,
  vLineWidth: () => 0.5,
  hLineColor: () => LINE,
  vLineColor: () => LINE,
  paddingLeft: () => 3,
  paddingRight: () => 3,
  paddingTop: () => 1.5,
  paddingBottom: () => 1.5
};

/** Renders a report as an A4 PDF: portrait up to seven columns, landscape beyond that. */
@Injectable({ providedIn: 'root' })
export class ReportPdfService {
  async download(report: ReportData, fileName: string): Promise<void> {
    const pdfMake = await this.pdfMake();
    await pdfMake.createPdf(this.build(report)).download(fileName);
  }

  private async pdfMake(): Promise<PdfMakeLike> {
    const instance = await this.resolve();
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

  private async resolve(): Promise<PdfMakeLike> {
    const mod = (await import('pdfmake/build/pdfmake')) as unknown as Record<string, unknown>;
    const candidate = (typeof mod['createPdf'] === 'function' ? mod : mod['default']) as PdfMakeLike | undefined;

    if (!candidate || typeof candidate.addVirtualFileSystem !== 'function') {
      throw new Error('pdfmake could not be loaded.');
    }
    return candidate;
  }

  private build(report: ReportData): PdfDoc {
    const wide = report.columns.length > 7;
    const shown = report.rows.slice(0, MAX_PDF_ROWS);

    const content: unknown[] = [
      { text: BRAND, fontSize: 12, bold: true, color: '#16233b' },
      { text: report.title, fontSize: 13, bold: true, margin: [0, 1, 0, 0] },
      { text: report.description, fontSize: 8, color: GREY, margin: [0, 2, 0, 4] },
      { text: `Filters: ${report.filterSummary}`, fontSize: 8, color: GREY },
      {
        text: `Generated ${new Date().toLocaleString()} · ${report.rows.length} row(s)`,
        fontSize: 8,
        color: GREY,
        margin: [0, 0, 0, 4]
      },
      {
        canvas: [{ type: 'line', x1: 0, y1: 0, x2: wide ? 802 : 547, y2: 0, lineWidth: 1, lineColor: LINE }],
        margin: [0, 0, 0, 6]
      },
      {
        table: {
          widths: this.widths(report.columns, wide),
          body: this.body(report.columns, shown),
          headerRows: 1
        },
        layout: GRID
      }
    ];

    if (report.rows.length > shown.length) {
      content.push({
        text: `PDF shows the first ${MAX_PDF_ROWS} of ${report.rows.length} rows. Use Excel for the complete export.`,
        fontSize: 8,
        color: DANGER,
        bold: true,
        margin: [0, 6, 0, 0]
      });
    }

    return {
      pageSize: 'A4',
      pageOrientation: wide ? 'landscape' : 'portrait',
      pageMargins: wide ? [20, 26, 20, 26] : [24, 26, 24, 26],
      defaultStyle: { fontSize: wide ? 7 : 7.5, font: 'Roboto' },
      content
    };
  }

  private body(columns: ReportColumn[], rows: ReportCell[][]): unknown[] {
    const head = columns.map(column => ({
      text: column.label,
      bold: true,
      fillColor: BAND,
      fontSize: columns.length > 9 ? 6 : 6.8
    }));

    return [head, ...rows.map(row => row.map((cell, index) => this.cell(columns[index], cell)))];
  }

  private cell(column: ReportColumn, value: ReportCell): unknown {
    if (value === null) {
      return { text: '—', alignment: 'center', color: GREY };
    }

    const numeric = column.type === 'number' || column.type === 'money' || column.type === 'decimal';
    const centred = column.type === 'date' || column.type === 'datetime';

    return {
      text: this.format(column, value),
      alignment: numeric ? 'right' : centred ? 'center' : 'left'
    };
  }

  private format(column: ReportColumn, value: string | number): string {
    if (typeof value === 'number') {
      if (column.type === 'money') return value.toLocaleString('en-US');
      if (column.type === 'decimal') return value % 1 === 0 ? String(value) : value.toFixed(1);
      return String(value);
    }

    if (column.type === 'date' || column.type === 'datetime') {
      const parsed = new Date(column.type === 'date' ? `${value}T00:00:00` : value);
      return Number.isNaN(parsed.getTime())
        ? value
        : parsed.toLocaleString('en-GB', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            ...(column.type === 'datetime' ? { hour: '2-digit', minute: '2-digit' } : {})
          });
    }

    return value;
  }

  /** The leading column identifies the row so it absorbs the slack; the rest keep a readable minimum. */
  private widths(columns: ReportColumn[], wide: boolean): (number | string)[] {
    const date = wide ? 60 : 64;
    const money = wide ? 55 : 54;
    const small = wide ? 36 : 34;
    const stamp = wide ? 104 : 112;

    return columns.map((column, index) => {
      if (column.type === 'datetime') return index === 0 ? stamp : '*';
      if (index === 0) return '*';
      if (column.type === 'date') return date;
      if (column.type === 'money') return money;
      if (column.type === 'number' || column.type === 'decimal') return small;
      return '*';
    });
  }
}
