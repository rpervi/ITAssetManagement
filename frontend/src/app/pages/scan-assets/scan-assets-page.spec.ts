import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ScanAssetsPage } from './scan-assets-page';

describe('ScanAssetsPage', () => {
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ScanAssetsPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('looks up a manually scanned asset and displays the details', () => {
    const fixture = TestBed.createComponent(ScanAssetsPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#barcode-value') as HTMLInputElement;
    const form = input.closest('form') as HTMLFormElement;
    input.value = 'AST-000017';
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    const request = httpTesting.expectOne('/api/stock/assets/AST-000017');
    request.flush({
      assetUnitId: 17,
      productId: 4,
      assetCode: 'AST-000017',
      assetName: 'Laptop',
      assetType: 'Individual',
      categoryName: 'Computers',
      manufacturer: 'Example',
      model: 'L1',
      serialNumber: 'SERIAL-17',
      assignedEmployee: 'Test Employee',
      department: 'IT',
      currentLocation: 'Building A',
      status: 'Assigned',
      purchaseDate: '2026-10-19',
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('AST-000017');
    expect(fixture.nativeElement.textContent).toContain('Test Employee');
    expect(fixture.nativeElement.textContent).toContain('View Asset Details');
  });

  it('does not send another lookup while the first scan is processing', () => {
    const fixture = TestBed.createComponent(ScanAssetsPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#barcode-value') as HTMLInputElement;
    const form = input.closest('form') as HTMLFormElement;
    input.value = 'AST-000017';
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    const request = httpTesting.expectOne('/api/stock/assets/AST-000017');
    input.value = 'AST-000018';
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    httpTesting.expectNone('/api/stock/assets/AST-000018');

    request.flush({
      assetUnitId: 17,
      productId: 4,
      assetCode: 'AST-000017',
      assetName: 'Laptop',
      assetType: 'Individual',
      categoryName: 'Computers',
      manufacturer: 'Example',
      model: 'L1',
      serialNumber: null,
      assignedEmployee: null,
      department: null,
      currentLocation: null,
      status: 'Available',
      purchaseDate: '2026-10-19',
    });
    fixture.detectChanges();
  });

  it('reports an asset code that is not found', () => {
    const fixture = TestBed.createComponent(ScanAssetsPage);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#barcode-value') as HTMLInputElement;
    const form = input.closest('form') as HTMLFormElement;
    input.value = 'AST-404';
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    httpTesting.expectOne('/api/stock/assets/AST-404').flush({}, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No asset was found for barcode "AST-404".');
  });
});
