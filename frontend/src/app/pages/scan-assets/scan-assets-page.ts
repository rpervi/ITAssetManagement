import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { RouterLink } from '@angular/router';
import { BarcodeFormat } from '@zxing/library';
import { BrowserMultiFormatReader, IScannerControls } from '@zxing/browser';
import { AssetBarcodeDetails } from '../stock/stock.models';
import { StockService } from '../stock/stock.service';

@Component({
  imports: [
    MatButtonModule,
    MatCardModule,
    DatePipe,
    MatIconModule,
    MatProgressBarModule,
    MatSnackBarModule,
    RouterLink,
  ],
  selector: 'app-scan-assets-page',
  styleUrl: './scan-assets-page.css',
  templateUrl: './scan-assets-page.html',
})
export class ScanAssetsPage implements OnDestroy {
  private readonly stockService = inject(StockService);
  private readonly snackBar = inject(MatSnackBar);
  private scanner?: BrowserMultiFormatReader;
  private scannerControls?: IScannerControls;

  @ViewChild('videoElement') private videoElement?: ElementRef<HTMLVideoElement>;

  protected readonly asset = signal<AssetBarcodeDetails | null>(null);
  protected readonly loading = signal(false);
  protected readonly cameraActive = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly scanMessage = signal('');
  private processingScan = false;

  ngOnDestroy(): void {
    this.stopCamera();
  }

  protected async startCamera(): Promise<void> {
    this.errorMessage.set('');
    this.scanMessage.set('');
    this.asset.set(null);
    if (!navigator.mediaDevices?.getUserMedia) {
      this.errorMessage.set(
        'Camera access is unavailable. Use HTTPS or localhost, or scan with a USB barcode reader.',
      );
      return;
    }

    this.scanner = new BrowserMultiFormatReader();
    this.scanner.possibleFormats = [BarcodeFormat.QR_CODE, BarcodeFormat.CODE_128];
    this.cameraActive.set(true);
    try {
      this.scannerControls = await this.scanner.decodeFromVideoDevice(
        undefined,
        this.videoElement?.nativeElement,
        (result, error) => {
          if (result && !this.processingScan) {
            void this.lookupAsset(result.getText());
          } else if (error && error.name !== 'NotFoundException') {
            this.errorMessage.set('Could not read the camera stream. Check camera permissions and try again.');
          }
        },
      );
    } catch (error) {
      this.cameraActive.set(false);
      const name = error instanceof Error ? error.name : '';
      this.errorMessage.set(
        name === 'NotAllowedError' || name === 'PermissionDeniedError'
          ? 'Camera permission was denied. Allow camera access in your browser settings, or use a USB barcode reader.'
          : 'Could not start the camera. Check camera permissions and try again.',
      );
    }
  }

  protected stopCamera(): void {
    this.scannerControls?.stop();
    this.scannerControls = undefined;
    this.scanner = undefined;
    this.cameraActive.set(false);
  }

  protected async submitManualScan(event: Event, input: HTMLInputElement): Promise<void> {
    event.preventDefault();
    const code = input.value.trim();
    if (!code || this.processingScan) return;
    input.value = '';
    await this.lookupAsset(code);
  }

  protected resetScan(): void {
    this.asset.set(null);
    this.errorMessage.set('');
    this.scanMessage.set('');
  }

  private async lookupAsset(rawCode: string): Promise<void> {
    const assetCode = rawCode.trim();
    if (!assetCode || this.processingScan) return;

    this.processingScan = true;
    this.stopCamera();
    this.asset.set(null);
    this.errorMessage.set('');
    this.scanMessage.set('');
    this.loading.set(true);
    this.stockService.getAssetByBarcode(assetCode).subscribe({
      next: (asset) => {
        this.asset.set(asset);
        this.scanMessage.set(`Asset ${asset.assetCode} found.`);
        this.snackBar.open(this.scanMessage(), 'Dismiss', { duration: 3000 });
        this.loading.set(false);
        this.processingScan = false;
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        this.processingScan = false;
        if (error.status === 404) {
          this.errorMessage.set(`No asset was found for barcode "${assetCode}".`);
        } else {
          this.errorMessage.set(
            error.error?.detail ?? 'Could not look up this asset. Please try again.',
          );
        }
      },
    });
  }
}
