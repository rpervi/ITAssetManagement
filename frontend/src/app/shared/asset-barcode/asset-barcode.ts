import { AfterViewInit, Component, ElementRef, Input, OnChanges, SimpleChanges, ViewChild, signal } from '@angular/core';
import JsBarcode from 'jsbarcode';

@Component({
  selector: 'app-asset-barcode',
  template: `
    <svg #barcode role="img" [attr.aria-label]="'Code 128 barcode for ' + value"></svg>
    @if (renderError()) {
      <span class="render-error" role="alert">{{ renderError() }}</span>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      min-width: 150px;
      align-items: center;
      gap: 6px;
    }

    svg {
      width: 150px;
      height: 38px;
    }

    .render-error {
      color: #a72c23;
    }
  `,
})
export class AssetBarcode implements AfterViewInit, OnChanges {
  @Input({ required: true }) value = '';
  @ViewChild('barcode') private barcode?: ElementRef<SVGSVGElement>;

  protected readonly renderError = signal('');

  ngAfterViewInit(): void {
    this.renderBarcode();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['value'] && this.barcode) {
      this.renderBarcode();
    }
  }

  private renderBarcode(): void {
    const svg = this.barcode?.nativeElement;
    if (!svg) return;

    this.renderError.set('');
    try {
      JsBarcode(svg, this.value, {
        format: 'CODE128',
        displayValue: false,
        margin: 4,
        height: 38,
        width: 2,
      });
    } catch (error) {
      this.renderError.set(
        error instanceof Error ? error.message : 'Could not generate this asset barcode.',
      );
    }
  }
}
