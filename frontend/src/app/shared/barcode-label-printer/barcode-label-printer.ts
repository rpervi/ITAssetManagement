import { Component, Input, OnChanges, signal, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import JsBarcode from 'jsbarcode';
import * as QRCode from 'qrcode';
import { PrintableAsset } from '../../pages/stock/stock.models';

type BarcodeFormat = 'CODE128' | 'QR';
type LabelSize = '50x30' | '70x35' | '100x50';

@Component({
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  selector: 'app-barcode-label-printer',
  styleUrl: './barcode-label-printer.css',
  templateUrl: './barcode-label-printer.html',
})
export class BarcodeLabelPrinter implements OnChanges {
  @Input() assets: PrintableAsset[] = [];

  protected readonly labels = signal<Array<PrintableAsset & { image: string }>>([]);
  protected readonly format = signal<BarcodeFormat>('CODE128');
  protected readonly labelSize = signal<LabelSize>('70x35');
  protected readonly companyName = signal(
    typeof localStorage === 'undefined' ? '' : localStorage.getItem('itam-company-name') ?? '',
  );
  protected readonly renderError = signal('');
  protected readonly companyNameError = signal('');

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['assets']) {
      void this.renderLabels();
    }
  }

  protected async updateFormat(format: BarcodeFormat): Promise<void> {
    this.format.set(format);
    await this.renderLabels();
  }

  protected updateCompanyName(value: string): void {
    this.companyName.set(value);
    this.companyNameError.set('');
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('itam-company-name', value);
    }
  }

  protected updateLabelSize(size: LabelSize): void {
    this.labelSize.set(size);
  }

  protected print(): void {
    if (!this.companyName().trim()) {
      this.companyNameError.set('Enter your company name to print complete asset labels.');
      return;
    }

    window.print();
  }

  private async renderLabels(): Promise<void> {
    this.renderError.set('');
    try {
      const labels = await Promise.all(this.assets.map(async (asset) => {
        let image: string;
        if (this.format() === 'QR') {
          image = await QRCode.toDataURL(asset.assetCode, {
            errorCorrectionLevel: 'Q',
            margin: 1,
            width: 260,
          });
        } else {
          const canvas = document.createElement('canvas');
          JsBarcode(canvas, asset.assetCode, {
            format: 'CODE128',
            displayValue: false,
            margin: 8,
            height: 65,
            width: 2,
          });
          image = canvas.toDataURL('image/png');
        }

        return { ...asset, image };
      }));
      this.labels.set(labels);
    } catch {
      this.renderError.set('Could not generate barcode labels. Check the asset codes and try again.');
    }
  }
}
