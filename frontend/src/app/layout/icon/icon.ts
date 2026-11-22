import { Component, input } from '@angular/core';

@Component({
  selector: 'app-icon',
  template: `<svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
    <use [attr.href]="'/images/icons.svg#i-' + name()"></use>
  </svg>`,
  styles: `
    :host {
      display: inline-flex;
      vertical-align: -0.2em;
    }

    svg {
      width: 1.15em;
      height: 1.15em;
      fill: none;
      stroke: currentColor;
      stroke-width: 1.7;
      stroke-linecap: round;
      stroke-linejoin: round;
    }
  `
})
export class Icon {
  readonly name = input.required<string>();
}
