import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../../auth/auth.service';
import { GenerateBarcodesPage } from './generate-barcodes-page';

describe('GenerateBarcodesPage', () => {
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [GenerateBarcodesPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('lists individual products and renders a barcode for selected product assets', () => {
    const fixture = TestBed.createComponent(GenerateBarcodesPage);
    fixture.detectChanges();
    httpTesting.expectOne('/api/stock/availability').flush([
      {
        productId: 4,
        productName: 'Laptop',
        categoryName: 'Computers',
        trackingType: 'Individual',
        availableQuantity: 1,
        assignedQuantity: 0,
        unavailableQuantity: 0,
        damagedQuantity: 0,
        minimumStock: 0,
      },
      {
        productId: 5,
        productName: 'Cable',
        categoryName: 'Accessories',
        trackingType: 'Bulk',
        availableQuantity: 10,
        assignedQuantity: 0,
        unavailableQuantity: 0,
        damagedQuantity: 0,
        minimumStock: 0,
      },
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Laptop');
    expect(fixture.nativeElement.textContent).toContain('Cable');
    expect(fixture.nativeElement.textContent).not.toContain('Create unique barcodes');
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    httpTesting.expectOne('/api/stock/availability/4/units').flush([
      {
        assetUnitId: 17,
        assetCode: 'AST-000017',
        serialNumber: 'SERIAL-17',
        purchaseDate: '2026-10-19',
        warrantyExpiryDate: null,
        purchaseCost: 1000,
        status: 'Available',
        condition: 'Good',
      },
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('AST-000017');
    expect(fixture.nativeElement.querySelector('svg rect')).not.toBeNull();
  });

  it('lets authorized staff convert bulk units and then generate their labels', () => {
    TestBed.inject(AuthService).currentUser.set({
      userId: 1,
      username: 'it-staff',
      email: 'it-staff@example.test',
      roles: ['ITStaff'],
    });
    const fixture = TestBed.createComponent(GenerateBarcodesPage);
    fixture.detectChanges();
    httpTesting.expectOne('/api/stock/availability').flush([
      {
        productId: 5,
        productName: 'Cable',
        categoryName: 'Accessories',
        trackingType: 'Bulk',
        availableQuantity: 10,
        assignedQuantity: 0,
        unavailableQuantity: 0,
        damagedQuantity: 0,
        minimumStock: 0,
      },
    ]);
    fixture.detectChanges();

    const generateButton = [...fixture.nativeElement.querySelectorAll('button')]
      .find((button: HTMLButtonElement) => button.textContent.includes('Create unique barcodes')) as HTMLButtonElement;
    generateButton.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('This cannot be undone from this page.');
    [...fixture.nativeElement.querySelectorAll('button')]
      .find((button: HTMLButtonElement) => button.textContent.includes('Confirm conversion'))
      .click();

    const conversionRequest = httpTesting.expectOne('/api/stock/products/5/convert-to-individual');
    expect(conversionRequest.request.method).toBe('POST');
    conversionRequest.flush({ productId: 5, assetsCreated: 10 });
    fixture.detectChanges();

    httpTesting.expectOne('/api/stock/availability').flush([
      {
        productId: 5,
        productName: 'Cable',
        categoryName: 'Accessories',
        trackingType: 'Individual',
        availableQuantity: 10,
        assignedQuantity: 0,
        unavailableQuantity: 0,
        damagedQuantity: 0,
        minimumStock: 0,
      },
    ]);
    httpTesting.expectOne('/api/stock/availability/5/units').flush([
      {
        assetUnitId: 31,
        assetCode: 'AST-000031',
        serialNumber: null,
        purchaseDate: '2026-10-19',
        warrantyExpiryDate: null,
        purchaseCost: 120,
        status: 'Available',
        condition: 'Good',
      },
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Created 10 individually tracked asset(s)');
    expect(fixture.nativeElement.textContent).toContain('Print labels');
    expect(fixture.nativeElement.querySelector('svg rect')).not.toBeNull();
  });
});
