import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { StockService } from './stock.service';

describe('StockService', () => {
  let service: StockService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(StockService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('loads active products with cookie credentials', () => {
    service.getProducts().subscribe((products) => expect(products).toEqual([]));

    const request = httpTesting.expectOne('/api/stock/products');
    expect(request.request.method).toBe('GET');
    expect(request.request.withCredentials).toBe(true);
    request.flush([]);
  });

  it('submits bulk receipts to the API with the idempotency key', () => {
    const payload = {
      idempotencyKey: '7e065d85-5368-42c7-9b36-560f16d9e1ad',
      productId: 4,
      quantity: 25,
      unitPrice: 1200,
      purchaseDate: '2026-10-19',
      warrantyExpiryDate: '2027-10-19',
      condition: 'Good' as const,
      supplier: 'Supplier',
      invoiceNumber: 'INV-25',
      remarks: null,
    };

    service.receiveBulk(payload).subscribe();

    const request = httpTesting.expectOne('/api/stock/receive/bulk');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    expect(request.request.withCredentials).toBe(true);
    request.flush({ receiptId: 1 });
  });

  it('requests paged receipt history using the supplied filters', () => {
    service.getReceipts({
      search: 'laptop',
      productId: 4,
      startDate: '2026-10-01',
      endDate: '2026-10-31',
      page: 2,
      pageSize: 10,
      sortBy: 'receivedDate',
      sortDirection: 'desc',
    }).subscribe();

    const request = httpTesting.expectOne(
      (candidate) => candidate.url === '/api/stock/receipts',
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('search')).toBe('laptop');
    expect(request.request.params.get('productId')).toBe('4');
    expect(request.request.params.get('startDate')).toBe('2026-10-01');
    expect(request.request.params.get('endDate')).toBe('2026-10-31');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('10');
    expect(request.request.params.get('sortDirection')).toBe('desc');
    request.flush({ items: [], totalCount: 0, page: 2, pageSize: 10 });
  });

  it('loads inventory availability and individual unit details', () => {
    service.getAvailability().subscribe((availability) =>
      expect(availability).toEqual([]),
    );
    const availabilityRequest = httpTesting.expectOne('/api/stock/availability');
    availabilityRequest.flush([]);

    service.getUnits(4).subscribe((units) => expect(units).toEqual([]));
    const unitsRequest = httpTesting.expectOne('/api/stock/availability/4/units');
    expect(unitsRequest.request.withCredentials).toBe(true);
    unitsRequest.flush([]);
  });
});
